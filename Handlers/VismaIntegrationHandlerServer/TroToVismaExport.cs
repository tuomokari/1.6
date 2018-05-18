using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Globalization;
using MongoDB.Bson;
using MongoDB.Shared;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using SystemsGarden.mc2.Common;
using SystemsGarden.mc2.RemoteConnector.Handlers.TroIntegrationCommon;

namespace SystemsGarden.mc2.RemoteConnector.Handlers.VismaIntegrationHandlerServer
{
    public sealed class TroToVismaExport
    {
        #region Constants

            const double MinDayLengthWithLunchBreak = 6;
            const double LunchBreakLength = 0.5;

        #endregion

        #region Members

        private ILogger logger;
        private string filePath;
        private MongoDatabase database;

        private string userFilter;
        private string projectFilter;
        private string payrollPeriodFilter;

        // Number of timehseet entries to hold in memory at one time.

        private DataTree config;

        private List<TroToVismaCsvLine> rawCsvLines = new List<TroToVismaCsvLine>();
        private List<TroToVismaCsvLine> processedCsvLines = new List<TroToVismaCsvLine>();

        private string exportFileName;
        private string exportFileNameAnnotated;
        private string exportFileNameAnnotatedTotals;
        private FileStream ExportFile;
        private FileStream ExportFileAnnotated;
        private FileStream ExportFileAnnotatedTotals;

        private int MaxExportFailureCount { get; set; }
        private int MinTimeFragmentSize { get; set; }

        private HashSet<ObjectId> failedExports = new HashSet<ObjectId>();

		#endregion

		#region Constructors

		public TroToVismaExport(
            ILogger logger,
            string filePath, 
            MongoDatabase database,
            DataTree config)
        {
            this.logger = logger.CreateChildLogger("TroToAXExport");
            this.filePath = filePath;

            if (!filePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                filePath += Path.DirectorySeparatorChar;

            this.database = database;
            this.config = config;

            if (!this.filePath.EndsWith(Convert.ToString(Path.DirectorySeparatorChar)))
                this.filePath += Path.DirectorySeparatorChar;

            // Make sure integration folder exists
            var di = new DirectoryInfo(filePath);
            di.Create();

            Init();
        }

        #endregion

        #region Main export function

        public void ExportDocuments(string userFilter, string projectFilter, string payrollPeriodFilter)
        {
            this.userFilter = userFilter;
            this.projectFilter = projectFilter;
            this.payrollPeriodFilter = payrollPeriodFilter;
    
            try
            {
                lock(database)
                {
                    StartExport();

                    IEnumerable<BsonDocument> timesheetsCursor = ExportWorkerHours();

                    IEnumerable<BsonDocument> absenceEntriesCursor = ExportAbsenceEntries();

                    IEnumerable<BsonDocument> dailyEntriesCursor = ExportDailyEntries();

                    PostProcessData();

                    WriteExportedDataToDisk();

                    MarkEntriesAsExported(timesheetsCursor, "timesheetentry");
                    MarkEntriesAsExported(absenceEntriesCursor, "absenceentry");
                    MarkEntriesAsExported(dailyEntriesCursor, "dayentry");

                    LockPayrollPeriod();
                }
            }
            finally
            {
                EndExport();
            }
        }

        public void CancelDocumentExport(string userFilter, string projectFilter, string payrollPeriodFilter)
        {
            this.userFilter = userFilter;
            this.projectFilter = projectFilter;
            this.payrollPeriodFilter = payrollPeriodFilter;

            try
            {
                lock (database)
                {
                    MongoCursor<BsonDocument> timesheetentryCursor = GetExportedEntries("timesheetentry");
                    MarkEntriesAsUnexported(timesheetentryCursor, "timesheetentry");

                    MongoCursor<BsonDocument> dayentryCursor = GetExportedEntries("dayentry");
                    MarkEntriesAsUnexported(dayentryCursor, "dayentry");

                    MongoCursor<BsonDocument> absenceentryCursor = GetExportedEntries("absenceentry");
                    MarkEntriesAsUnexported(absenceentryCursor, "absenceentry");
                }
            }
            finally
            {
                EndExport();
            }
        }

        #endregion

        #region Batch and transaction handling

        private void StartExport()
        {
            logger.LogTrace("Looking for documents to export to Visma.");

            const string FilePrefix = "VismaExport";
            const string FilePrefixAnnotated = "VismaExportAnnotated";
            const string FilePrefixAnnotatedTotals = "VismaExportAnnotatedTotals";

            DateTime now = MC2DateTimeValue.Now().ToLocalTime();

            exportFileName = Path.Combine(filePath, FilePrefix + "_" +
                string.Format("{0:0000}-{1:00}-{2:00}-{3:00}-{4:00}",
                now.Year, now.Month, now.Day, now.Hour, now.Second) + ".csv");

            exportFileNameAnnotated = Path.Combine(filePath, FilePrefixAnnotated + "_" +
                string.Format("{0:0000}-{1:00}-{2:00}-{3:00}-{4:00}",
                now.Year, now.Month, now.Day, now.Hour, now.Second) + ".txt");

            exportFileNameAnnotatedTotals = Path.Combine(filePath, FilePrefixAnnotatedTotals + "_" +
                string.Format("{0:0000}-{1:00}-{2:00}-{3:00}-{4:00}",
                now.Year, now.Month, now.Day, now.Hour, now.Second) + ".txt");


            ExportFile = File.OpenWrite(exportFileName);
            ExportFileAnnotated = File.OpenWrite(exportFileNameAnnotated);
            ExportFileAnnotatedTotals = File.OpenWrite(exportFileNameAnnotatedTotals);

            rawCsvLines.Clear();
            processedCsvLines.Clear();
        }

        private void EndExport()
        {
            if (ExportFile != null)
                ExportFile.Close();

            if (ExportFileAnnotated != null)
                ExportFileAnnotated.Close();

            if (ExportFileAnnotatedTotals != null)
                ExportFileAnnotatedTotals.Close();

            ExportFile = null;
            ExportFileAnnotated = null;
            ExportFileAnnotatedTotals = null;
        }

        #endregion

        #region Export worker hours

        private IEnumerable<BsonDocument> ExportWorkerHours()
        {
            try
            {
                MongoCursor<BsonDocument> cursor = GetUnexportedEntries("timesheetentry");

                if (cursor.Count() > 0)
                {
                    logger.LogInfo("Exporting found unexported timesheet entries", cursor.Count());
                }
                else
                {
                    logger.LogFineTrace("No unexported timesheet entries found.");
                    return null;
                }

                List<TimesheetEntryFragment> timesheetEntryFragments = 
                    IntegrationHelpers.GetTimesheetFragments(
                    cursor, 
                    MinTimeFragmentSize,
                    database,
                    logger,
                    failedExports,
					new HashSet<ObjectId>(), // Temp object since this info isn't really needed. Refactor away during payroll handling updatte.
                    false);

                logger.LogInfo(cursor.Count() + " timesheet entries produced " + timesheetEntryFragments.Count + " fragments.");

                ExportWorkTimesheetFragmentsToCsv(timesheetEntryFragments);

                return cursor;
            }
            catch (Exception ex)
            {
                logger.LogError("Exporting hours failed.", ex);
                throw;
            }
        }

        private void ExportWorkTimesheetFragmentsToCsv(List<TimesheetEntryFragment> timesheetEntryFragments)
        {
            foreach (TimesheetEntryFragment fragment in timesheetEntryFragments)
            {
                string payType1 = IntegrationHelpers.PayTypeBasic;
                string payType2 = string.Empty;
                double payTypeFactor1 = 1;
                double payTypeFactor2 = 1;

                if (fragment.PayType != null)
                {
                    payType1 = (string)fragment.PayType["identifier"];
                    payType2 = (string)fragment.PayType.GetValue("identifier2", string.Empty);
                    payTypeFactor1 = (double)fragment.PayType.GetValue("paytypefactor", (double)1);
                    payTypeFactor2 = (double)fragment.PayType.GetValue("paytypefactor2", (double)1);
                }
                else
                {
                    if (fragment.IsTravelTime)
                        payType1 = IntegrationHelpers.PayTypeTravelTime;
                }

                AddPayType(fragment, payType1, payTypeFactor1, true);

                // Add secondary paytype if present. This means that one hour entry can create two lines in the 
                // CSV that have different pay types.
                if (!string.IsNullOrEmpty(payType2))
                    AddPayType(fragment, payType2, payTypeFactor2, false);
            }
        }

        private void AddPayType(TimesheetEntryFragment fragment, string payType, double payTypeFactor, bool isPrimary)
        {
            var csvLine = new TroToVismaCsvLine();
            csvLine.Values[(int)TroToVismaColumns.PersonnelNumber] = fragment.WorkerId;

            csvLine.Values[(int)TroToVismaColumns.Paytype] = payType;

            TimeSpan duration = fragment.End - fragment.Start;

            // Hours + minutes in one value with the accuracy of two decimal spaces.
            Double hours = Math.Round( ((double)duration.Hours + ((double)duration.Minutes) / 60) * payTypeFactor, 2) + ((int)duration.TotalDays * 24);

            csvLine.Values[(int)TroToVismaColumns.Hours] = Convert.ToString(hours, new CultureInfo("fi-FI"));
            csvLine.Values[(int)TroToVismaColumns.Days] = "0";

            // Amount and sum are only added to the primary pay type.
            if (fragment.Detail != null && isPrimary)
            {
                csvLine.Values[(int)TroToVismaColumns.Amount] = Convert.ToString(fragment.Detail.GetValue("amount", 0));
                csvLine.Values[(int)TroToVismaColumns.Euro] = Convert.ToString(fragment.Detail.GetValue("sum", 0), new CultureInfo("fi-FI"));
            }
            else
            {
                csvLine.Values[(int)TroToVismaColumns.Amount] = "0";
                csvLine.Values[(int)TroToVismaColumns.Euro] = "0";
            }

            csvLine.Values[(int)TroToVismaColumns.StartDate] = string.Format("{0:00}.{1:00}.{2:0000}", fragment.Start.Day, fragment.Start.Month, fragment.Start.Year);
            csvLine.Values[(int)TroToVismaColumns.ProfitCenter] = fragment.WorkerProfitCenter;

            if (fragment.Detail != null && (bool)fragment.Detail.GetValue("substractlunchbreak", false))
                csvLine.Values[(int)TroToVismaColumns.LunchBreak] = "true";
            else
                csvLine.Values[(int)TroToVismaColumns.LunchBreak] = "false";

            rawCsvLines.Add(csvLine);
        }

        private MongoCursor<BsonDocument> GetUnexportedEntries(string entrytype)
        {
            return GetEntriesBasedOnExportedStatus(entrytype, false);
        }

        private MongoCursor<BsonDocument> GetExportedEntries(string entrytype)
        {
            return GetEntriesBasedOnExportedStatus(entrytype, true);
        }

        private MongoCursor<BsonDocument> GetEntriesBasedOnExportedStatus(string entrytype, bool exported)
        {
            logger.LogDebug("Getting unexported worker hours.");

            MongoCollection<BsonDocument> payrollPeriodCollection = database.GetCollection("payrollperiod");

            var andQueries = new List<IMongoQuery>();

            // Filter based on payroll period
            if (!string.IsNullOrEmpty(payrollPeriodFilter))
            {
                BsonDocument payrollPeriod = payrollPeriodCollection.FindOne(Query.EQ(DBQuery.Id, new ObjectId(payrollPeriodFilter)));

                andQueries.Add(Query.EQ("clacontract", payrollPeriod["clacontract"]));

                if (entrytype == "dayentry")
                {
                    // Nasty time hack to temporarily fix the issue of date picker not returning utc 00:00 based dates but rather including the time zone.
                    andQueries.Add(Query.GT("date", ((DateTime)payrollPeriod["startdate"]).AddHours(-12)));
                    andQueries.Add(Query.LTE("date", ((DateTime)payrollPeriod["enddate"]).AddHours(-12)));
                }
                else
                {
                    DateTime startDate = (DateTime)payrollPeriod["startdate"];
                    DateTime endDate = (DateTime)payrollPeriod["enddate"];

                    // Convert dates to what would be the date's timestamp in local timezone. This is to take in to account the fact 
                    // that dates are not timestamps and to compare between date and a datetime we must assign a timezone to the date.
                    startDate = new DateTime(startDate.Year, startDate.Month, startDate.Day, 0, 0, 0, DateTimeKind.Local);
                    endDate = new DateTime(endDate.Year, endDate.Month, endDate.Day, 0, 0, 0, DateTimeKind.Local);

                    // Return time as UTC. The end result is to shift the Date's corresponding timestamp to whatever UTC
                    // time would be at the local timezone.
                    andQueries.Add(Query.GT("starttimestamp", startDate.ToUniversalTime()));
                    andQueries.Add(Query.LTE("starttimestamp", endDate.ToUniversalTime()));
                }
            }

            // Filter based on user
            var approvedEntryAndQueries = new List<IMongoQuery>();
            if (!string.IsNullOrEmpty(userFilter))
                andQueries.Add(Query.EQ("user", new ObjectId(userFilter)));

            // Filter based on project
            if (!string.IsNullOrEmpty(projectFilter))
                andQueries.Add(Query.EQ("project", new ObjectId(projectFilter)));


            // Filter baesd on manager acceptance status
            andQueries.Add(Query.EQ("approvedbymanager", true));
            andQueries.Add(Query.EQ("approvedbyworker", true));

            // Filter based on Export status
            andQueries.Add(Query.NE("exported_visma", !exported));

            // Filter out external workers
            andQueries.Add(Query.EQ("internalworker", true));

            // If nothing is selected then export nothing
            if (string.IsNullOrEmpty(userFilter) && string.IsNullOrEmpty(projectFilter) && string.IsNullOrEmpty(payrollPeriodFilter))
            {
                approvedEntryAndQueries.Add(Query.EQ("creator", ""));
                approvedEntryAndQueries.Add(Query.NE("creator", ""));
            }

            MongoCollection<BsonDocument> entryCollection = database.GetCollection(entrytype);

            MongoCursor<BsonDocument> cursor = entryCollection.Find(Query.And(andQueries));

            return cursor;
        }

        #endregion

        #region Export absence entries

        private IEnumerable<BsonDocument> ExportAbsenceEntries()
        {
            try
            {
                MongoCursor<BsonDocument> cursor = GetUnexportedEntries("absenceentry");

                if (cursor.Count() > 0)
                {
                    logger.LogInfo("Exporting found unexported absence entries", cursor.Count());
                }
                else
                {
                    logger.LogFineTrace("No unexported absence entries found.");
                    return null;
                }

                logger.LogInfo(cursor.Count() + " absence entries found.");

                ExportAbsenceEntriesToCSV(cursor);

                return cursor;
            }
            catch (Exception ex)
            {
                logger.LogError("Exporting hours failed.", ex);

                throw;
            }
        }

        private void ExportAbsenceEntriesToCSV(MongoCursor<BsonDocument> cursor)
        {
            foreach (BsonDocument document in cursor)
            {
                var csvLine = new TroToVismaCsvLine();

                if (!document.Contains("user"))
                    throw new Exception("Absence entry is missing pserson");

                if (!document.Contains("absenceentrytype"))
                    throw new Exception("Absence entry is missing absence type");

                ObjectId userId = (ObjectId)document["user"][0];
                ObjectId absenceTypeId = (ObjectId)document["absenceentrytype"][0];

                BsonDocument userDocument = database.GetCollection("user").FindOne(Query.EQ(DBQuery.Id, userId));

                BsonDocument absenceTypeDocument = database.GetCollection("absenceentrytype").FindOne(Query.EQ(DBQuery.Id, absenceTypeId));

                if (userDocument == null)
                    throw new Exception("Person not found for absence entry");

                if (absenceTypeDocument == null)
                    throw new Exception("Absence type not found for absence entry");

                BsonDocument profitcenterDocument = database.GetCollection("profitcenter").FindOne(Query.EQ(DBQuery.Id, userDocument["profitcenter"][0]));

                csvLine.Values[(int)TroToVismaColumns.Paytype] = (string)absenceTypeDocument["identifier"];

                TimeSpan duration = (DateTime)document["endtimestamp"] - (DateTime)document["starttimestamp"];

                // Hours + minutes in one value with the accuracy of two decimal spaces.
                Double hours = (double)duration.Hours + Math.Round(((double)duration.Minutes) / 60, 2);

                csvLine.Values[(int)TroToVismaColumns.Hours] = Convert.ToString(hours, new CultureInfo("fi-FI"));
                csvLine.Values[(int)TroToVismaColumns.Days] = "0";
                csvLine.Values[(int)TroToVismaColumns.PersonnelNumber] = (string)userDocument["identifier"];
                csvLine.Values[(int)TroToVismaColumns.Paytype] = (string)absenceTypeDocument["identifier"];
                csvLine.Values[(int)TroToVismaColumns.Amount] = "0";
                csvLine.Values[(int)TroToVismaColumns.Euro] = "0";
                csvLine.Values[(int)TroToVismaColumns.ProfitCenter] = (string)profitcenterDocument["identifier"];

                DateTime date = (DateTime)document["starttimestamp"];
                csvLine.Values[(int)TroToVismaColumns.StartDate] = string.Format("{0:00}.{1:00}.{2:0000}", date.Day, date.Month, date.Year);

                rawCsvLines.Add(csvLine);
            }
        }

        #endregion

        #region Export daily entries

        private IEnumerable<BsonDocument> ExportDailyEntries()
        {
            try
            {
                MongoCursor<BsonDocument> cursor = GetUnexportedEntries("dayentry");

                if (cursor.Count() > 0)
                {
                    logger.LogInfo("Exporting found unexported daily entries", cursor.Count());
                }
                else
                {
                    logger.LogFineTrace("No unexported daily entries found.");
                    return null;
                }

                logger.LogInfo(cursor.Count() + " daily entries found.");

                ExportDailyEntriesToCSV(cursor);

                return cursor;
            }
            catch (Exception ex)
            {
                logger.LogError("Exporting hours failed.", ex);

                throw;
            }
        }

        private void ExportDailyEntriesToCSV(MongoCursor<BsonDocument> cursor)
        {
            foreach (BsonDocument document in cursor)
            {
                var csvLine = new TroToVismaCsvLine();

                ObjectId userId = (ObjectId)document["user"][0];

                BsonDocument userDocument = database.GetCollection("user").FindOne(Query.EQ(DBQuery.Id, userId));

                BsonDocument dayEntryType = database.GetCollection("dayentrytype").FindOne(Query.EQ(DBQuery.Id, document["dayentrytype"][0]));


                if (userDocument == null)
                    throw new Exception("User not found for daily entry");

                if (dayEntryType == null)
                    throw new Exception("Entry type not for daily entry");

                BsonDocument profitcenterDocument = database.GetCollection("profitcenter").FindOne(Query.EQ(DBQuery.Id, userDocument["profitcenter"][0]));

                if (profitcenterDocument == null)
                    throw new Exception("Profit center not for daily entry");

                csvLine.Values[(int)TroToVismaColumns.PersonnelNumber] = (string)userDocument["identifier"];
                csvLine.Values[(int)TroToVismaColumns.Paytype] = (string)dayEntryType["identifier"];
                csvLine.Values[(int)TroToVismaColumns.Hours] = "0";

                csvLine.Values[(int)TroToVismaColumns.Euro] = "0";
                csvLine.Values[(int)TroToVismaColumns.Amount] = "0";
                csvLine.Values[(int)TroToVismaColumns.Days] = "0";

                // Daily entry amount is set as either euro, pieces or days edepending no daily entry type
                if (dayEntryType.GetValue("unit", string.Empty) == "euro")
                    csvLine.Values[(int)TroToVismaColumns.Euro] = Convert.ToString((int)document.GetValue("amount", 0));
                else if (dayEntryType.GetValue("unit", string.Empty) == "pcs")
                    csvLine.Values[(int)TroToVismaColumns.Amount] = Convert.ToString((int)document.GetValue("amount", 0));
                else
                    csvLine.Values[(int)TroToVismaColumns.Days] = Convert.ToString((int)document.GetValue("amount", 0));
                
                csvLine.Values[(int)TroToVismaColumns.ProfitCenter] = (string)profitcenterDocument["identifier"];

                // XXX: Implemented a quick temporary fix for DT-394. The correct fix is to implement date picker to always return
                //      dates with UTC time 0:00 for correct date.
                DateTime date = ((DateTime)document["date"]).AddHours(12);
                csvLine.Values[(int)TroToVismaColumns.StartDate] = string.Format("{0:00}.{1:00}.{2:0000}", date.Day, date.Month, date.Year);

                rawCsvLines.Add(csvLine);
            }
        }

        #endregion

        #region Common export functions

        private void MarkEntriesAsExported(IEnumerable<BsonDocument> cursor, string entryType)
        {
            if (cursor == null)
                return;

            MongoCollection collection = database.GetCollection(entryType);

            DateTime now = MC2DateTimeValue.Now().ToUniversalTime();

            logger.LogInfo("Marking " + cursor.Count() + " items as completed.");
            foreach (BsonDocument document in cursor)
            {
                document["exported_visma"] = true;
                document["exporttimestamp_visma"] = now;
                document["__readonly"] = true;
                collection.Save(document);

                MarkTimesheetEntryDetailsAsReadonly(document, true);
            }
        }

        private void LockPayrollPeriod()
        {
            // Automatically lock the payroll period
            if (!string.IsNullOrEmpty(payrollPeriodFilter))
            {
                MongoCollection<BsonDocument> payrollPeriodCollection = database.GetCollection("payrollperiod");
                BsonDocument payrollPeriod = payrollPeriodCollection.FindOne(Query.EQ(DBQuery.Id, new ObjectId(payrollPeriodFilter)));
                payrollPeriod["locked"] = true;
                payrollPeriodCollection.Save(payrollPeriod);
            }
        }

        private void MarkTimesheetEntryDetailsAsReadonly(BsonDocument document, bool readonlyState)
        {
            MongoCollection<BsonDocument> timsheetEntryDetailsCollection = database.GetCollection("timesheetentry");
            MongoCursor<BsonDocument> cursor = timsheetEntryDetailsCollection.Find(Query.EQ("parent", document[DBQuery.Id]));

            foreach (BsonDocument entryDetail in cursor)
            {
                entryDetail["__readonly"] = readonlyState;
                timsheetEntryDetailsCollection.Save(entryDetail);
            }
        }


        private void MarkEntriesAsUnexported(IEnumerable<BsonDocument> cursor, string entryType)
        {
            if (cursor == null)
                return;

            MongoCollection collection = database.GetCollection(entryType);

            DateTime now = MC2DateTimeValue.Now().ToUniversalTime();

            logger.LogInfo("Marking " + cursor.Count() + " items as unexported.");
            foreach (BsonDocument document in cursor)
            {
                document["exported_visma"] = false;
                document["exporttimestamp_visma"] = BsonNull.Value;
                document["__readonly"] = false;

                MarkTimesheetEntryDetailsAsReadonly(document, false);

                collection.Save(document, WriteConcern.Acknowledged);
            }
        }

        /// <summary>
        /// Postprocess stage will take the expored data, group it to users, days and payment types. The grouping is
        /// done at this stage mostly for historical reasons but it does provide a decent debug option to observe the
        /// raw data befor postporcess stage.
        /// </summary>
        private void PostProcessData()
        {
            // Dictionary of dictoinaries of DailyHourTotalsHelpers
            // user
            //      date
            //          paytype
            //              helper object with row totals

            var dailyTotals = new DataTree();

            // Count hour totals using helper object
            for(int i = rawCsvLines.Count - 1; i >= 0; i--)
            {
                TroToVismaCsvLine line = rawCsvLines[i];

                string payType = line.Values[(int)TroToVismaColumns.Paytype];
                string userId = line.Values[(int)TroToVismaColumns.PersonnelNumber];
                string date = line.Values[(int)TroToVismaColumns.StartDate];
                string profitCenter = line.Values[(int)TroToVismaColumns.ProfitCenter];
                bool lunchBreak = ((string)line.Values[(int)TroToVismaColumns.LunchBreak] == "true");
                double hours = Convert.ToDouble(line.Values[(int)TroToVismaColumns.Hours], new CultureInfo("fi-FI"));
                double euro = Convert.ToDouble(line.Values[(int)TroToVismaColumns.Euro], new CultureInfo("fi-FI"));
                int days = Convert.ToInt32(line.Values[(int)TroToVismaColumns.Days]);
                int amount = Convert.ToInt32(line.Values[(int)TroToVismaColumns.Amount]);

                decimal currentHours = 0;
                decimal currentEuro = 0;
                int currentDays = 0; 
                int currentAmount = 0;

                if (dailyTotals[userId][date][payType]["hours"].Exists)
                    currentHours = (decimal)dailyTotals[userId][date][payType]["hours"];

                if (dailyTotals[userId][date][payType]["euro"].Exists)
                    currentEuro = (decimal)dailyTotals[userId][date][payType]["euro"];

                if (dailyTotals[userId][date][payType]["days"].Exists)
                    currentDays = (int)dailyTotals[userId][date][payType]["days"];

                if (dailyTotals[userId][date][payType]["amount"].Exists)
                    currentAmount = (int)dailyTotals[userId][date][payType]["amount"];

                dailyTotals[userId][date][payType]["hours"] = (decimal)(currentHours + Convert.ToDecimal(hours));
                dailyTotals[userId][date][payType]["euro"] = (decimal)(currentEuro + Convert.ToDecimal(euro));
                dailyTotals[userId][date][payType]["days"] = currentDays + days;
                dailyTotals[userId][date][payType]["amount"] = currentAmount + amount;
                
                if (lunchBreak)
                    dailyTotals[userId][date][payType]["lunchbreak"] = true;
                
                dailyTotals[userId]["profitcenter"] = profitCenter;
            }

            // Add lunch breaks for applicable pay types
            foreach (DataTree user in dailyTotals)
            {
                foreach(DataTree date in user)
                {
                    // Lunch breaks for basic hours are special and contain pay
                    double basicHours = (double)(decimal)date[IntegrationHelpers.PayTypeBasic]["hours"].GetValueOrDefault((decimal)0);
                    double overtime50 = (double)(decimal)date[IntegrationHelpers.PayTypeOvertime50]["hours"].GetValueOrDefault((decimal)0);
                    double overtime100 = (double)(decimal)date[IntegrationHelpers.PayTypeOvertime100]["hours"].GetValueOrDefault((decimal)0);

                    double totalBasicHours = basicHours + overtime50 + overtime100;

                    // Lunc break is substracted if basic hours amount is greater than (and not equal) to 6h. The corner case
                    // was verified from Visma
                    if (totalBasicHours > MinDayLengthWithLunchBreak)
                    {
                        logger.LogTrace("Adding lunch break", user.Name, date.Name, "Hours before lunch break" + basicHours, "Basic hours");
                        date[IntegrationHelpers.PayTypeBasic]["hours"] = (decimal)(basicHours - LunchBreakLength);
                        date[IntegrationHelpers.PayTypeBasic]["lunchbreak"] = true;
                    }

                    foreach (DataTree payType in date)
                    {
                        if (!payType.Contains("lunchbreak"))
                            continue;

                        double hours = (double)(decimal)payType["hours"].GetValueOrDefault((decimal)0);

                        if (hours > MinDayLengthWithLunchBreak)
                        {
                            logger.LogTrace("Adding lunch break", user.Name, date.Name, "Hours before lunch break" + basicHours, "PayType: " + payType.Name);
                            payType["hours"] = (decimal)(basicHours - LunchBreakLength);
                            payType["lunchbreak"] = true;
                        }
                    }
                }
            }

            foreach (DataTree user in dailyTotals)
            {
                foreach (DataTree date in user)
                {
                    foreach(DataTree payType in date)
                    {
                        var csvLine = new TroToVismaCsvLine();

                        csvLine.Values[(int)TroToVismaColumns.Hours] = Convert.ToString((double)(decimal)payType["hours"].GetValueOrDefault(0), new CultureInfo("fi-FI"));
                        csvLine.Values[(int)TroToVismaColumns.Days] = Convert.ToString(payType["days"].GetValueOrDefault(0));
                        csvLine.Values[(int)TroToVismaColumns.PersonnelNumber] = user.Name;
                        csvLine.Values[(int)TroToVismaColumns.Paytype] = payType.Name;
                        csvLine.Values[(int)TroToVismaColumns.Amount] = Convert.ToString((double)(decimal)payType["amount"].GetValueOrDefault(0), new CultureInfo("fi-FI"));
                        csvLine.Values[(int)TroToVismaColumns.Euro] = Convert.ToString((double)(decimal)payType["euro"].GetValueOrDefault(0), new CultureInfo("fi-FI"));
                        csvLine.Values[(int)TroToVismaColumns.StartDate] = date.Name;
                        csvLine.Values[(int)TroToVismaColumns.ProfitCenter] = user["profitcenter"];

                        processedCsvLines.Add(csvLine);
                    }
                }
            }

            WriteAnnotatedHoursToDisk(dailyTotals);
            WriteAnnotatedTotalHoursToDisk(dailyTotals);
        }

        /// <summary>
        /// Writes annotated hours to disk. Modifies provided hour data.
        /// </summary>
        /// <param name="hourData"></param>
        private void WriteAnnotatedHoursToDisk(DataTree hourData)
        {
            DirectoryInfo di = new DirectoryInfo(filePath);
            if (!di.Exists)
                di.Create();

            foreach (DataTree user in hourData)
            {
                foreach (DataTree date in user)
                {
                    foreach (DataTree payType in date)
                    {
                        List<DataTree> entriesToRemove = new List<DataTree>();

                        foreach (DataTree entryType in payType)
                        {
                            // Remove empty hours, euros etc. from annotated data.
                            if (entryType.Value.ToString() == "0")
                                entriesToRemove.Add(entryType);
                        }

                        foreach (DataTree entryToRemove in entriesToRemove)
                        {
                            entryToRemove.Remove();
                        }
                    }
                }

                // Sort based on date
                user.Sort();
            }

            // Remove after previous loop to prevent chnaging data while iterating.
            foreach (DataTree user in hourData)
            {
                user["profitcenter"].Remove();
            }

            logger.LogInfo("Saving annotated data to disk.", exportFileNameAnnotated);

            try
            {
                byte[] utfBytes = Encoding.UTF8.GetBytes(hourData.ContentView());
                ExportFileAnnotated.Write(utfBytes, 0, utfBytes.Count());
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to export hour data.", ex);
            }
        }

        /// <summary>
        /// Writes annotated hours to disk. Modifies provided hour data. Expects data handled previously by
        /// WriteAnnotatedHoursToDisk.
        /// </summary>
        /// <param name="hourData"></param>
        private void WriteAnnotatedTotalHoursToDisk(DataTree hourData)
        {
            DirectoryInfo di = new DirectoryInfo(filePath);
            if (!di.Exists)
                di.Create();

            foreach (DataTree user in hourData)
            {
                var totals = new DataTree();

                foreach (DataTree date in user)
                {
                    foreach (DataTree payType in date)
                    {
                        List<DataTree> entriesToRemove = new List<DataTree>();

                        foreach (DataTree entryType in payType)
                        {
                            if (entryType.Name == "lunchbreak")
                                continue;

                            DataTree total = totals[payType.Name][entryType.Name].Create();

                            if (total.Empty)
                                total.Value = (decimal)0;

                            total.Value = (decimal)total + (decimal)entryType;
                        }
                    }
                }

                // Clear existing user data and merge totals
                user.Clear();
                user.Merge(totals);
            }


            logger.LogInfo("Saving annotated totals data to disk.", exportFileNameAnnotated);

            try
            {
                byte[] utfBytes = Encoding.UTF8.GetBytes(hourData.ContentView());
                ExportFileAnnotatedTotals.Write(utfBytes, 0, utfBytes.Count());
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to export hour data.", ex);
            }
        }

        private void WriteExportedDataToDisk()
        {
            DirectoryInfo di = new DirectoryInfo(filePath);
            if (!di.Exists)
                di.Create();

            logger.LogInfo("Saving Visma export hour file.", exportFileName);

            var sb = new StringBuilder();

            bool first = true;

            foreach(TroToVismaCsvLine line in processedCsvLines)
            {
                if (!first)
                    sb.AppendLine();
                else
                    first = false;

                // Valid data to export is only up to profit center
                for (int i = 0; i <= (int)TroToVismaColumns.ProfitCenter; i++)
                {
                    if (i != 0)
                        sb.Append(";");
                    
                    sb.Append(line.Values[i]);
                }
            }

            try 
            {
                byte[] utfBytes = Encoding.UTF8.GetBytes(sb.ToString());
                ExportFile.Write(utfBytes, 0, utfBytes.Count());            
            }
            catch(Exception ex)
            {
                logger.LogError("Failed to export hour data.", ex);
            }
        }

        #endregion

        #region Setup

        private void Init()
        {
            MaxExportFailureCount = (int)config["trotovismaexport"]["maxexportfailurecount"].GetValueOrDefault(3);
            MinTimeFragmentSize = (int)config["trotovismaexport"]["mintimefragmentsize"].GetValueOrDefault(0);
        }

        #endregion

        #region HelperClasses

        // Target system's (Visma) column name is commented
        private enum TroToVismaColumns
        {
            PersonnelNumber = 0, // henkilönumero
            Paytype = 1, // palkkalaji
            Hours = 2, // tunnit
            Days = 3, // paivat
            Amount = 4, // maara
            Euro = 5, // eurot
            StartDate = 6, // alku_pvm
            ProfitCenter = 7, // Use some custom field in Visma
            LunchBreak = 8 // Additional column for internal use
        }

        private class TroToVismaCsvLine
        {
            private string[] values;
            public string[] Values { get { return values; }  }

            public const int NumberOfColumns = (int)TroToVismaColumns.LunchBreak + 1;

            public TroToVismaCsvLine()
            {
                values = new string[NumberOfColumns]; 
            }
        }

        #endregion
    }
}
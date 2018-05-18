﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Globalization;
using MongoDB.Shared;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB;
using MongoDB.Bson;
using SystemsGarden.mc2.Common;
using SystemsGarden.mc2.RemoteConnector.Handlers.TroIntegrationCommon;
using System.Diagnostics;
using OfficeOpenXml.Table.PivotTable;
using OfficeOpenXml.Table;
using OfficeOpenXml;

//using System.Runtime.InteropServices;

namespace SystemsGarden.mc2.RemoteConnector.Handlers.PayrollIntegrationHandlerServer
{
    /// <summary>
    /// Actual class to handling payroll export actions ( export / revert)
    /// </summary>
    public sealed class PayrollExport
    {

        #region Members
        private ILogger logger;
        private string filePath;
        private MongoDatabase database;

        /// <summary>
        /// Excel Export
        /// </summary>
        public ExportPivotTable PivotTable { get; set; } = null;

        /// <summary>
        /// PayrollIntegrationHandleServer config
        /// </summary>
        public static DataTree config;
        private static object payrollExportLock = new object();

        /// <summary>
        /// this will keep track of failed entries, which are not exported, generates {timestamp}Errors.txt
        /// </summary>
        private Dictionary<ObjectId, string> failedExports = new Dictionary<ObjectId, string>();
        /// <summary>
        /// this will keep track of something wrong with data but successfully exported, generates {timestamp}ErrorsButExported.txt
        /// </summary>
        private Dictionary<ObjectId, string> failedButSuccessExports = new Dictionary<ObjectId, string>();
        #endregion

        #region Constructors
        /// <summary>
        /// Initial constructor to create instance of PayrollExport
        /// </summary>
        /// <param name="logger">Logger created in PayrollIntegrationHandlerServer</param>
        /// <param name="filePath">Location of payroll export files</param>
        /// <param name="database">basically mc2db</param>
        /// <param name="config">Message["config"] value from Message in PayrollIntegrationHandlerServer</param>
        public PayrollExport(
            ILogger logger,
            string filePath,
            MongoDatabase database,
            DataTree config)
        {
            this.logger = logger.CreateChildLogger("PayrollExport");
            this.filePath = filePath;
            this.database = database;
            PayrollExport.config = config;
        }
        #endregion

        #region PayrollExportStart

        /// <summary>
        /// Helper list to create CSV and Audit files
        /// </summary>
        private List<FieldDetails> fieldNames = new List<FieldDetails>();

        /// <summary>
        /// Gets fieldnames and properties from config.tree
        /// </summary>
        /// <param name="config">Message["fields"]</param>
        private List<FieldDetails> ResolveFieldNamesAndTypes(DataTree config)
        {
            //how many valid entry types
            var entryTypeCount = (int)(config["validtypescountandtypes"].GetValueOrDefault(3));
            string[] validTypeNames = new string[entryTypeCount];
            //names of valid entrytypes
            for (int i = 0; i < entryTypeCount; i++)
            {
                validTypeNames[i] = (string)config["validtypescountandtypes"][i].Name;
            }
            //Point config just for field
            config = config["fields"];
            var fieldCount = (int)config.Count;
            var index = 0;

            foreach (var item in config)
            {
                var tempFieldDetails = new FieldDetails { Index = index++, Name = item.Name };
                //Valid ExportTypes
                if (item.Contains("valid"))
                {
                    string[] arr = new string[entryTypeCount];
                    for (int i = 0; i < entryTypeCount; i++) //initialize with empty string
                    {
                        arr[i] = string.Empty;
                    }

                    for (int i = 0; i < item["valid"].Count; i++)
                    {
                        for (int j = 0; j < validTypeNames.Count(); j++)
                        {
                            if ((string)item["valid"][i].Name == validTypeNames[j])
                            {
                                arr[j] = validTypeNames[j];
                                break;
                            }
                        }
                    }
                    tempFieldDetails.ValidTypes = arr;
                }
                //Spesific format in genral format eg d.M.yyyy or #.00 
                if (item.Contains("format"))
                    tempFieldDetails.Format = item["format"].Value.ToString();
                if (item.Contains("identifier"))
                {
                    tempFieldDetails.Identifier = item["identifier"].Value.ToString();
                    if (item["identifier"].Contains("value"))
                    {
                        tempFieldDetails.IdentifierValue = item["identifier"]["value"].Value.ToString();
                    }
                }
                //Item that need to get rid off preceding zeroes or not 
                if (item.Contains("removeprecedingzeroes"))
                    tempFieldDetails.RemovePrecedingZeroes = (bool)item["removeprecedingzeroes"].Value;

                //Calculation function to do specified calcultation
                if (item.Contains("calculationfunction"))
                    tempFieldDetails.CalculationFunction = (string)item["calculationfunction"].Value;
                //Calculation with value precedingoperator calculationfunction
                if (item.Contains("precedingoperator"))
                    tempFieldDetails.PrecedingOperator = (string)item["precedingoperator"].Value;

                fieldNames.Add(tempFieldDetails);

                //Check and give properties for each of the items in FieldSpan
                if (item.Contains("fieldspan"))
                {
                    tempFieldDetails.IsFieldSpan = true;
                    tempFieldDetails.FieldLength = (int)item["length"].Value;
                    fieldCount += (int)item["fieldspan"].Value - 1;
                    for (int i = 0; i < (int)item["fieldspan"].Value - 1; i++)
                    {
                        tempFieldDetails = new FieldDetails
                        {
                            Index = index++,
                            Name = item.Name + (i + 1),
                            IsFieldSpan = true,
                            FieldLength = tempFieldDetails.FieldLength,
                            ValidTypes = tempFieldDetails.ValidTypes
                        };
                        fieldNames.Add(tempFieldDetails);
                    }
                }
            }
            return fieldNames;
        }

        /// <summary>
        /// Starting the actual exporting whilst also update statuses and logger info
        /// </summary>
        /// <param name="exportTask">Payroll export task</param>
        private void StartExport(PayrollExportTask exportTask)
        {
            try
            {
                UpdateExportStatus(PayrollConstants.Starting, exportTask.ExportId);
                //Some init and caching
                fieldNames = ResolveFieldNamesAndTypes(config);

                //Cache often used collections
                PopulateCollectionsToCache();

                UpdateExportStatus(PayrollConstants.ProcessingData, exportTask.ExportId);

                //lock payrollperiod it if target was CSV export and not Excel report nor invidual user export task.
                InitializeAndLockPayrollPeriod(exportTask.PayrollPeriod, exportTask.ExportType == ExportType.Csv && exportTask.UserId == ObjectId.Empty);

                //Set possible compiled function=null
                CodeDomCalculationParser.CompiledResult = null;
                EntriesToPayroll<Absence> absences = GetAbsencesToExport(exportTask.PayrollPeriod, config, exportTask);
                CodeDomCalculationParser.CompiledResult = null;
                EntriesToPayroll<Timesheet> workHours = GetTimesheetToExport(exportTask.PayrollPeriod, config, exportTask);
                CodeDomCalculationParser.CompiledResult = null;
                EntriesToPayroll<Day> days = GetDaysToExport(exportTask.PayrollPeriod, config, exportTask);
                CodeDomCalculationParser.CompiledResult = null;

                //Value from config, changed to sanitize file/path names -> ReturnSafeString
                var exportFolder = Path.Combine(config["exportrootfolder"].GetValueOrDefault(filePath).ToString(), ReturnSafeString(exportTask.PayrollPeriodName));
                if (!Directory.Exists(exportFolder))
                    Directory.CreateDirectory(exportFolder);

                var now = MC2DateTimeValue.Now().ToLocalTime();
                var nowStr = string.Format("{0:0000}-{1:00}-{2:00}-{3:00}-{4:00}-{5:00}",
                    now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);

                UpdateExportStatus(PayrollConstants.ProcessingData, exportTask.ExportId);
                if (exportTask.ExportType == ExportType.Csv)
                {
                    if (exportTask.UserId == ObjectId.Empty) //whole payrollperiod
                    {
                        if (config["exportallinonefile"].Empty == false)
                        {
                            logger.LogDebug("Exporting all entries in one file");

                            logger.LogDebug("Writing absences to CSV");
                            ExportCsvDocument(Path.Combine(exportFolder, string.Format("{0}_{1}", nowStr, config["exportallinonefile"].GetValueOrDefault("payrollexport.csv"))), absences);

                            logger.LogDebug("Writing days to CSV");
                            ExportCsvDocument(Path.Combine(exportFolder, string.Format("{0}_{1}", nowStr, config["exportallinonefile"].GetValueOrDefault("payrollexport.csv"))), days, true);

                            logger.LogDebug("Writing work hours to CSV");
                            ExportCsvDocument(Path.Combine(exportFolder, string.Format("{0}_{1}", nowStr, config["exportallinonefile"].GetValueOrDefault("payrollexport.csv"))), workHours, true);

                        }
                        else
                        {
                            logger.LogDebug("Export with individual files for each entry type");

                            //Create CSV files, and need to be this order to combine dayentries and timesheetentries
                            logger.LogDebug("Writing absences to CSV");
                            ExportCsvDocument(Path.Combine(exportFolder, string.Format("{0}_{1}", nowStr, config["exportabsencefilecsv"].GetValueOrDefault("absence.csv"))), absences);

                            logger.LogDebug("Writing days to CSV");
                            ExportCsvDocument(Path.Combine(exportFolder, string.Format("{0}_{1}", nowStr, config["exportdayfilecsv"].GetValueOrDefault("timesheetandexpense.csv"))), days);

                            logger.LogDebug("Writing work hours to CSV");
                            //Note the last parameter to do the combination with previous one
                            ExportCsvDocument(Path.Combine(exportFolder, string.Format("{0}_{1}", nowStr, config["exporttimesheetfilecsv"].GetValueOrDefault("timesheetandexpense.csv"))), workHours, true);
                        }
                    }
                    else //invidual user
                    {
                        if (config["exportallinonefile"].Empty == false)
                        {
                            ExportCsvDocument(Path.Combine(exportFolder, string.Format("{0}_{2}_{1}", nowStr, config["exportallinonefile"].GetValueOrDefault("payrollexport.csv"), exportTask.UserName)), absences);
                            ExportCsvDocument(Path.Combine(exportFolder, string.Format("{0}_{2}_{1}", nowStr, config["exportallinonefile"].GetValueOrDefault("payrollexport.csv"), exportTask.UserName)), days, true);
                            ExportCsvDocument(Path.Combine(exportFolder, string.Format("{0}_{2}_{1}", nowStr, config["exportallinonefile"].GetValueOrDefault("payrollexport.csv"), exportTask.UserName)), workHours, true);
                        }
                        else
                        {
                            ExportCsvDocument(Path.Combine(exportFolder, string.Format("{0}_{2}_{1}", nowStr, config["exportabsencefilecsv"].GetValueOrDefault("absence.csv"), exportTask.UserName)), absences);
                            ExportCsvDocument(Path.Combine(exportFolder, string.Format("{0}_{2}_{1}", nowStr, config["exportdayfilecsv"].GetValueOrDefault("timesheetandexpense.csv"), exportTask.UserName)), days);
                            ExportCsvDocument(Path.Combine(exportFolder, string.Format("{0}_{2}_{1}", nowStr, config["exporttimesheetfilecsv"].GetValueOrDefault("timesheetandexpense.csv"), exportTask.UserName)), workHours, true);
                        }
                    }

                    UpdateExportStatus(PayrollConstants.Finalizing, exportTask.ExportId);

                    //All readonly (mark as exportedtovisma)
                    MarkEntriesAsExported(workHours, "timesheetentry");
                    MarkEntriesAsExported(absences, "absenceentry");
                    MarkEntriesAsExported(days, "dayentry");
                }

                if (exportTask.ExportType == ExportType.Excel)
                {
                    // yhdistetään poissaolot ja tunnit yhteen kokoelmaan, jotta saadaan ne sortattua. Excel-paketti ei tätä tue.
                    EntriesToPayroll<Timesheet> allHours = CombineHoursToExport(absences, workHours, config, exportTask); // !!!

                    if (exportTask.UserId == ObjectId.Empty) //whole payrollperiod
                    {
                        UpdateExportStatus(PayrollConstants.GeneratingExcel, exportTask.ExportId);
                        ExportExcelDocument(
                                Path.Combine(exportFolder, string.Format("{0}_{1}", nowStr, config["exportexcelfile"].GetValueOrDefault("payrollexport.xlsx"))),
                                allHours,
                                days,
                                exportTask.Language);
                    }
                    else //invidual user
                    {
                        ExportExcelDocument(
                            Path.Combine(exportFolder, string.Format("{0}_{2}_{1}", nowStr, config["exportexcelfile"].GetValueOrDefault("payrollexport.xlsx"), exportTask.UserName)),
                            allHours,
                            days,
                            exportTask.Language);
                    }
                }

                //Some of the entries not exported
                if (failedExports.Count > 0)
                {
                    UpdateExportStatus(PayrollConstants.CompletedWithErrors, exportTask.ExportId);
                    var errorsFile = Path.Combine(exportFolder, string.Format("{0}_Errors.txt", nowStr));
                    var sb = new StringBuilder();
                    foreach (var item in failedExports)
                    {
                        sb.AppendLine(item.Key + "-" + item.Value);
                    }

                    File.WriteAllText(errorsFile, sb.ToString());
                    //Some of them are failed, so status stays failed
                    if (failedButSuccessExports.Count > 0)
                    {
                        errorsFile = Path.Combine(exportFolder, string.Format("{0}_ErrorsButExported.txt", nowStr));
                        sb = new StringBuilder();
                        foreach (var item in failedButSuccessExports)
                        {
                            sb.AppendLine(item.Key + "-" + item.Value);
                        }
                        File.WriteAllText(errorsFile, sb.ToString());
                    }
                }
                //All of entries are exported but some missing information about the entries
                else if (failedButSuccessExports.Count > 0)  //see failedButSuccessExports
                {
                    if (exportTask.IsAutomatic)
                        UpdateExportStatus(PayrollConstants.CompletedAutomatically, exportTask.ExportId);
                    else
                        UpdateExportStatus(PayrollConstants.Completed, exportTask.ExportId);

                    var errorsFile = Path.Combine(exportFolder, string.Format("{0}_ErrorsButExported.txt", nowStr));
                    var sb = new StringBuilder();
                    foreach (var item in failedButSuccessExports)
                    {
                        sb.AppendLine(item.Key + "-" + item.Value);
                    }
                    File.WriteAllText(errorsFile, sb.ToString());
                }
                else
                {
                    if (exportTask.IsAutomatic)
                        UpdateExportStatus(PayrollConstants.CompletedAutomatically, exportTask.ExportId);
                    else
                        UpdateExportStatus(PayrollConstants.Completed, exportTask.ExportId);
                }
            }
            catch (Exception ex)
            {
                UpdateExportStatus(PayrollConstants.Failed, exportTask.ExportId);
                throw;
            }
        }

        /// <summary>
        /// Mark entries as exported to payroll and locking them
        /// </summary>
        /// <typeparam name="T">Absence,Day or Timesheet</typeparam>
        /// <param name="entryCollection">collection of T</param>
        /// <param name="entryType">absenceentry, dayentry or timesheetentry</param>
        private void MarkEntriesAsExported<T>(EntriesToPayroll<T> entryCollection, string entryType) where T : EntryToPayroll
        {

            MongoCollection<BsonDocument> collection = database.GetCollection(entryType);
            BsonDocument doc;

            DateTime now = MC2DateTimeValue.Now().ToUniversalTime();

            int rowsHandled = 0;

            foreach (var item in entryCollection.Where((p)
                                => (bool)p.Document["exported_visma"] == false && p.Document["approvedbyworker"] == true && p.Document["approvedbymanager"] == true))
            {

                doc = collection.FindOne(Query.EQ("_id", item.Document[DBQuery.Id]));

                if (doc.Contains("exporttimestamp_visma"))
                    logger.LogDebug("Document already contains visma export timestamp", doc[DBDocument.Id], entryType);

                doc["exported_visma"] = true;
                doc["exporttimestamp_visma"] = now;
                collection.Save(doc, WriteConcern.Acknowledged);
                rowsHandled++;

                logger.LogTrace("Marked item as exported", doc[DBDocument.Id]);
            }

            logger.LogInfo("Marked " + rowsHandled + " items as exported.", "Total entries" + entryCollection.Count, entryType);
        }


        /// <summary>
        /// Sets status exported_visma=false and exporttimestamp_visma=null
        /// </summary>
        /// <param name="payrollperiodIdentifier">friendly name of the payrollidentifier, !not ObjectId!</param>
        /// <param name="entryType">absenceentry, dayentry or timeheetentry</param>
        /// <param name="userId">Invidual user, optional</param>
        private void RevertForEntryType(string payrollperiodIdentifier, string entryType, ObjectId? userId = null)
        {
            logger.LogDebug("Revert for entrytype", payrollperiodIdentifier, entryType, userId);

            BsonDocument payrollPeriod = database.GetCollection("payrollperiod").FindOne(Query.EQ("name", payrollperiodIdentifier));

            var andQueries = new List<IMongoQuery>();

            var claidentifier = payrollPeriod["clacontract"][0].ToString();
            andQueries.Add(Query.EQ("clacontract", payrollPeriod["clacontract"][0]));

            //Filter based on dates
            andQueries.Add(Query.GTE("date", (DateTime)payrollPeriod["startdate"]));
            andQueries.Add(Query.LT("date", (DateTime)payrollPeriod["enddate"]));  //Must be LT, since in db it is +1

            //Filter based on exported status
            andQueries.Add(Query.EQ("exported_visma", true));
            //If there is a user as well
            if (userId.HasValue)
            {
                andQueries.Add(Query.EQ("user", userId));
            }


            MongoCollection<BsonDocument> collection = database.GetCollection(entryType);
            MongoCursor<BsonDocument> cursor = collection.Find(Query.And(andQueries));

            foreach (var item in cursor)
            {
                item["exported_visma"] = false;
                item["exporttimestamp_visma"] = BsonNull.Value;
                collection.Save(item, WriteConcern.Acknowledged);
            }
            logger.LogInfo("Reverted and marked " + cursor.Count() + " items as un-exported.");
        }

        /// <summary>
        /// Create Excel document for exported data
        /// </summary>
        /// <param name="file">File to create</param>
        /// <param name="absences">Collection of absence entries to export</param>
        /// <param name="workHours">Collection of work hours entries to export</param>
        /// <param name="days">Collection of day entries to export</param>
        /// <param name="language">ExportTask.Language = DefaultLanguage (en-US)</param>
        private void ExportExcelDocument(
            string file,
            EntriesToPayroll<Timesheet> allHours,
            EntriesToPayroll<Day> days,
            string language)
        {
            try
            {

                OfficeOpenXml.ExcelPackage excelPackage = new ExcelPackage(new FileInfo(file));

                var workSheetName = "Hours";
                ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets.Add(workSheetName);
                //need to start at row 2, since row 1 is header info
                int startingRow = 2;
                startingRow = allHours.AppendToExcelWorksheet<Timesheet>(worksheet, startingRow);

                var pivotRangeName = "hoursPivotRange";

                // Create pivot table for timesheet and absence entries
                PivotTable = new ExportPivotTable(
                    new string[] { "Name" }, /* collapsible rows */
                    new string[] { "Hours" });

                PivotTable.CreatePivotTable(excelPackage, worksheet, pivotRangeName);

                //Day entries
                workSheetName = "Expenses";
                startingRow = 2;
                worksheet = excelPackage.Workbook.Worksheets.Add(workSheetName);
                startingRow = days.AppendToExcelWorksheet<Day>(worksheet, startingRow);
                pivotRangeName = "expensesPivotRange";

                // Create pivot table for day entries
                PivotTable = new ExportPivotTable(
                    new string[] { "Name" }, /* collapsible rows */
                    new string[] { "Amount" });

                PivotTable.CreatePivotTable(excelPackage, worksheet, pivotRangeName);

                excelPackage.Save();
            }
            catch (Exception ex)
            {
                logger.LogError("Exception when creating file.", ex, file);
                throw;
            }
        }

        /// <summary>
        /// Create CSV documents
        /// </summary>
        /// <typeparam name="T">Typed collection based on EntryToPayroll</typeparam>
        /// <param name="file">File to create</param>
        /// <param name="entryCollection">Collection of EntryToPayroll</param>
        /// <param name="append">Optional, whether use exting file to append, if true, skip Header info and use existing file</param>
        private void ExportCsvDocument<T>(string file, EntriesToPayroll<T> entryCollection, bool append = false) where T : EntryToPayroll
        {
            try
            {
                logger.LogInfo("Exporting CSV document", file);

                if (!append)
                {
                    if (File.Exists(file))
                        File.Delete(file);
                }
                //TODO: better implementation for date format needs more refactoring
                string dateFormat = fieldNames.Where(item => item.Format != null).FirstOrDefault().Format;

                using (var sw = new StreamWriter(file, true))
                {
                    // Do or do not create export csv file headers 
                    if ((bool)config["showcsvheader"].GetValueOrDefault(false))
                    {
                        logger.LogDebug("Generate headers for CSV");

                        // All entrytypes in one file
                        if (config["exportallinonefile"].Empty == false && append == false)
                            sw.WriteLine(entryCollection.CreateHeaderForCsv(true));
                        else if (!append) 
                            sw.WriteLine(entryCollection.CreateHeaderForCsv()); //Header information
                    }

                    int linesWritten = 0;
					int duplicateEntries = 0;

					var seenObjectIds = new HashSet<string>();

					foreach (var item in entryCollection.Where((p)
                                => (bool)p.Document["exported_visma"] == false && p.Document["approvedbyworker"] == true && p.Document["approvedbymanager"] == true))
                    {
						if (seenObjectIds.Contains(item.ObjectId))
						{
							logger.LogWarning("Duplicate ObjectId detected when exporting CSV. Not exporting.", item.ObjectId);
							duplicateEntries++;
							continue;
						}

                        string csvLine = item.CreateCsvLine(dateFormat);

                        logger.LogTrace("Writing CSV line", linesWritten, csvLine);

                        sw.WriteLine(csvLine);
                        linesWritten++;

						seenObjectIds.Add(item.ObjectId);
                    }

                    logger.LogDebug("Wrote lines to CSV", linesWritten, "Potential entries: " + entryCollection.Count, "Duplicates: " + duplicateEntries);

                }
            }
            catch (Exception ex)
            {
                logger.LogError("Creating file {0} occured exception - in Collection {1} \n exception info:\n{2}",
                    file, entryCollection.GetType().Name, ex);
            }
        }

        /// <summary>
        /// Day (kulu) entries
        /// </summary>
        /// <param name="payrollPeriod">BsonDocument payrollPeriod</param>
        /// <param name="config">config["fields"]</param>
        /// <param name="exportTask">All viable information for the task</param>
        /// <returns>Collection of EntriesToPayroll of Day></returns>
        private EntriesToPayroll<Day> GetDaysToExport(BsonDocument payrollPeriod, DataTree config, PayrollExportTask exportTask)
        {

            logger.LogInfo("Part 3/3 - Getting unexported daily entries for payrollperiod ", payrollPeriod["name"]);
            var configRoot = config;
            config = config["fields"];


            var andQueries = new List<IMongoQuery>();

            //Get and filter by cla (TES in finnish)
            BsonDocument cla = claCache[((ObjectId)(payrollPeriod["clacontract"][0]))];
            var claidentifier = cla["identifier"].ToString();
            andQueries.Add(Query.EQ("clacontract", payrollPeriod["clacontract"][0]));

            //Filter based on dates
            andQueries.Add(Query.GTE("date", (DateTime)payrollPeriod["startdate"]));
            andQueries.Add(Query.LT("date", (DateTime)payrollPeriod["enddate"])); //Must be LT, since in db it is +1

            andQueries.Add(Query.EQ("internalworker", true));

            //If there is just one user
            if (exportTask.UserId != ObjectId.Empty)
                andQueries.Add(Query.EQ("user", exportTask.UserId));

            MongoCollection<BsonDocument> dayEntries = database.GetCollection("dayentry");
            MongoCursor<BsonDocument> cursor = dayEntries.Find(Query.And(andQueries));

            var entries = new EntriesToPayroll<Day>(fieldNames, exportTask);

            foreach (var item in cursor)
            {
                try
                {
                    //User Info from cache
                    BsonDocument user = userCache[((ObjectId)(item["user"][0]))];
                    item["__UserSortName"] = user["lastname"] + " " + user["firstname"];
                    item["__supervisor__displayname"] = user["__supervisor__displayname"];
                    item["__profitcenter__displayname"] = user["__profitcenter__displayname"];
                    Day entry = new Day();
                    //document
                    entry.Document = item;
                    //Profitcenter
                    if (item.GetValue("profitcenter", string.Empty) != string.Empty)
                    {
                        BsonDocument profitcenter = profitcenterCache[((ObjectId)(item["profitcenter"][0]))];
                        entry.ProfitCenter = profitcenter["identifier"].ToString();
                    }
                    else if (user.GetValue("profitcenter", string.Empty) != string.Empty)
                    {
                        BsonDocument profitcenter = profitcenterCache[((ObjectId)(user["profitcenter"][0]))];
                        entry.ProfitCenter = profitcenter["identifier"].ToString();
                    }

                    if ((bool)config["useridentifier"]["removeprecedingzeroes"].GetValueOrDefault(false))
                        entry.UserIdentifier = user["identifier"].ToString().TrimStart('0');
                    else
                        entry.UserIdentifier = user["identifier"].ToString();

                    //From timesheetentry item
                    entry.StartDate = item["date"].ToUniversalTime();
                    entry.EndDate = item["date"].ToUniversalTime();

                    //Project and workorder info
                    if (item["project"][0] == null)
                    {
                        failedExports.Add((ObjectId)item["_id"], "dayeentry \n" + "Day entry does not contain project , entry " + item[0]);
                        logger.LogError("Day entry does not contain project, entry " + item[0]);
                        throw new Exception("Day entry does not contain project , entry " + item[0]);
                    }
                    BsonDocument project = database.GetCollection("project").FindOne(Query.EQ(DBQuery.Id, item["project"][0]));

                    //If Project is disabled, skip it //Muutokset 2015-09-29
                    if ((bool)project.Contains("disabled") && ((bool)project["disabled"]))
                        continue;

                    //if workorder (työmääräin) //Muutokset 2015-09-29
                    if (project.Contains("projecttype") && (project["projecttype"].ToString() != "PROJECT" && project["projecttype"].ToString() != "__socialproject"))
                    {
                        //remove leading zeros
                        entry.WorkOrder = project["identifier"].ToString().TrimStart('0');

                        //Get Parentproject and assign it to ProjectNumber 
                        BsonDocument parentProject = database.GetCollection("project").FindOne(Query.EQ(DBQuery.Id, project["parentproject"][0])); //project["poski"].ToString().Replace("-", "")));
                        string proj = "";
                        //TODO: this must be checked, since it should not be disabled but lets put something in csv
                        if ((bool)parentProject.GetValue("disabled", false))
                        {
                            ObjectId pProject = (ObjectId)parentProject["_id"];
                            string identifier = parentProject.GetValue("identifier", "").ToString();
                            string userName = item.GetValue("__user__displayname", "").ToString();
                            string date = item.GetValue("date", "").ToString();
                            string type = item.GetValue("__dayentrytype__displayname", "").ToString();

                            failedButSuccessExports.Add((ObjectId)item["_id"],
                                string.Format("Dayentry - Disabled parentproject {5} and identifier {6} for workorder {0} but successfully exported. Person: {1}, Personid: {2}, Date: {3}, Type: {4} ",
                                entry.WorkOrder, userName, entry.UserIdentifier, date, type, pProject, identifier));
                        }
                        else
                        {
                            //get identifier from config and remove '-' chars from string
                            proj = parentProject[config["projectnumber"]["identifier"].Value.ToString()].ToString().Replace("-", "");
                        }
                        //get identifier from config and remove '-' chars from string
                        entry.ProjectNumber = SplitProjectIdentifier(proj, (int)config["projectnumber"]["fieldspan"], (int)config["projectnumber"]["length"]);

                    }
                    else //it's directly to project or it's socialproject
                    {
                        //Remove social project type project name and replace it with empty string array
                        if (project["identifier"].ToString().StartsWith("__social"))
                        {
                            entry.ProjectNumber = new string[(int)config["projectnumber"]["fieldspan"]];
                        }
                        else
                        {
                            entry.ProjectNumber = SplitProjectIdentifier(project["identifier"].ToString(), (int)config["projectnumber"]["fieldspan"], (int)config["projectnumber"]["length"]);
                        }
                    }

                    //PayType info
                    BsonDocument payType = dayPaytypeCache[((ObjectId)(item["dayentrytype"][0]))];
                    entry.PayTypeId = payType["identifier"].ToString();
                    entry.PayType = payType;

                    //CLA info
                    entry.ClaGroup = claidentifier;
                    //Helper for track objectID
                    entry.ObjectId = item["_id"].ToString();


                    //Should use this notation if value does not exists
                    entry.Amount = Convert.ToDouble(item.GetValue("amount", 0));

                    //ExtraFields amountextra1 and amountextra2
                    var fieldChecked = false;
                    foreach (var item2 in fieldNames)
                    {
                        if (fieldChecked) break;
                        if (item2.Name.ToLower() == "amountextra1".ToLower())
                        {
                            if (payType.Contains(item2.Identifier.ToLower()) && payType.GetValue(item2.Identifier.ToLower(), string.Empty) == item2.IdentifierValue.ToLower())
                            {
                                entry.AmountExtra1 = entry.Amount;
                                entry.Amount = 0;
                                fieldChecked = true;
                            }

                        }
                        else if (item2.Name.ToLower() == "amountextra2".ToLower())
                        {
                            if (payType.Contains(item2.Identifier.ToLower()) && payType.GetValue(item2.Identifier.ToLower(), string.Empty) == item2.IdentifierValue.ToLower())
                            {
                                entry.AmountExtra2 = entry.Amount;
                                entry.Amount = 0;
                                fieldChecked = true;
                            }
                        }
                    }


                    //If hours instead of amount (2015-10-29)!
                    if ((string)payType.GetValue("payrollexporttype", PayrollConstants.Amount) == PayrollConstants.Hours)
                    {
                        if ((bool)payType.GetValue("hasprice", false)) //Is there fixed price
                        {
                            if (item.GetValue("price", 0).IsInt32 && item.GetValue("price", 0) > 0)
                            {
                                entry.HourlyRate = (int)item.GetValue("price", 0);
                                entry.Hours = entry.Amount;
                                entry.Amount = 0;
                            }
                            else if (item.GetValue("price", 0).IsDouble && item.GetValue("price", 0) > 0)
                            {
                                entry.HourlyRate = (double)item.GetValue("price", 0);
                                entry.Hours = entry.Amount;
                                entry.Amount = 0;
                            }
                            else
                            {
                                entry.Hours = entry.Amount;
                                entry.Amount = 0;
                            }
                        }
                        else
                        {
                            entry.Hours = entry.Amount;
                            entry.Amount = 0;
                        }
                    }
                    else //Amount and check if there is QuantityRate
                    {
                        if ((bool)payType.GetValue("hasprice", false))
                        {
                            var price = item.GetValue("price", 0);

                            if (price > 0) //There is an initial price
                            {
                                if (price.IsInt32)
                                {
                                    entry.QuantityRate = (int)price;
                                }
                                else if (price.IsDouble)
                                {
                                    entry.QuantityRate = (double)price;
                                }
                            }
                        }
                    }

                    entries.Add(entry);
                }
                catch (Exception ex)
                {
                    //Detailed info for Errors.txt
                    string userName = item.GetValue("__user__displayname", "").ToString();
                    string date = item.GetValue("date", "").ToString();
                    string type = item.GetValue("__dayentrytype__displayname", "").ToString();
                    string amount = item.GetValue("amount", 0).ToString();
                    failedExports.Add((ObjectId)item["_id"], string.Format("Dayentry failed to export. Person: {0}, Date: {1}, Entrytype: {2}, Amount: {3}, Exception: {4}", userName, date, type, amount, ex.Message));

                    logger.LogError("dayentry {0} has problem\n Exception\n{1}", item["_id"], ex.ToString());
                }
            }
            return entries;
        }

        /// <summary>
        /// Absence and workhour entries combining for excel
        /// </summary>
        /// <param name="absences">EntriesToPayroll</param>
        /// <param name="workHours">EntriesToPayroll</param>
        /// <param name="config">config["fields"]</param>
        /// <param name="exportTask">All viable information for the task</param>
        /// <returns>Collection of EntriesToPayroll of Timesheet></returns>!!!!!!!!!!!
        private EntriesToPayroll<Timesheet> CombineHoursToExport(EntriesToPayroll<Absence> absences, EntriesToPayroll<Timesheet> workHours, DataTree config, PayrollExportTask exportTask)
        {
            logger.LogInfo("Part 2/3 - combining absences and workhours for excel ");
            var entries = new EntriesToPayroll<Timesheet>(fieldNames, exportTask);

            foreach (Absence item in absences)
            {
                Timesheet entry = new Timesheet();
                entry.Document = item.Document;
                entry.ProfitCenter = item.ProfitCenter;
                entry.UserIdentifier = item.UserIdentifier;
                entry.StartDate = item.StartDate;
                entry.EndDate = item.EndDate;
                entry.Hours = item.Hours;
                entry.PayTypeId = item.PayTypeId;
                entry.PayType = item.PayType;
                entry.ClaGroup = item.ClaGroup;
                entry.ObjectId = item.ObjectId;
                entries.Add(entry);
            }
            foreach (Timesheet item in workHours)
            {
                Timesheet entry = new Timesheet();
                entry.Document = item.Document;
                entry.ProfitCenter = item.ProfitCenter;
                entry.UserIdentifier = item.UserIdentifier;
                entry.StartDate = item.StartDate;
                entry.EndDate = item.EndDate;
                entry.Hours = item.Hours;
                entry.PayTypeId = item.PayTypeId;
                entry.PayType = item.PayType;
                entry.ClaGroup = item.ClaGroup;
                entry.ObjectId = item.ObjectId;
                entries.Add(entry);
            }
            return entries;
        }


        /// <summary>
        /// Absence (poissaolo) entries
        /// </summary>
        /// <param name="payrollPeriod">BsonDocument payrollPeriod</param>
        /// <param name="config">config["fields"]</param>
        /// <param name="exportTask">All viable information for the task</param>
        /// <returns>Collection of EntriesToPayroll of Absences></returns>
        private EntriesToPayroll<Absence> GetAbsencesToExport(BsonDocument payrollPeriod, DataTree config, PayrollExportTask exportTask)
        {
            logger.LogInfo("Part 2/3 - Getting unexported absences for payrollperiod  ", payrollPeriod["name"]);
            var configRoot = config;
            config = config["fields"];

            var andQueries = new List<IMongoQuery>();

            //Get and filter by cla (TES in finnish)
            BsonDocument cla = claCache[((ObjectId)(payrollPeriod["clacontract"][0]))];
            var claidentifier = cla["identifier"].ToString();
            andQueries.Add(Query.EQ("clacontract", payrollPeriod["clacontract"][0]));

            //Filter based on dates
            andQueries.Add(Query.GTE("date", (DateTime)payrollPeriod["startdate"]));
            andQueries.Add(Query.LT("date", (DateTime)payrollPeriod["enddate"])); //Must be LT, since in db it is +1

            andQueries.Add(Query.EQ("internalworker", true));
            //If there is just one user
            if (exportTask.UserId != ObjectId.Empty)
                andQueries.Add(Query.EQ("user", exportTask.UserId));

            MongoCollection<BsonDocument> absenceEntries = database.GetCollection("absenceentry");
            MongoCursor<BsonDocument> cursor = absenceEntries.Find(Query.And(andQueries));

            var entries = new EntriesToPayroll<Absence>(fieldNames, exportTask);
            foreach (var item in cursor)
            {
                try
                {
                    //User Info from cache
                    BsonDocument user = userCache[((ObjectId)(item["user"][0]))];
                    item["__UserSortName"] = user["lastname"] + " " + user["firstname"];
                    item["__supervisor__displayname"] = user["__supervisor__displayname"];
                    item["__profitcenter__displayname"] = user["__profitcenter__displayname"];

                    Absence entry = new Absence();
                    //document
                    entry.Document = item;

                    //Profitcenter
                    if (item.GetValue("profitcenter", string.Empty) != string.Empty)
                    {
                        BsonDocument profitcenter = profitcenterCache[((ObjectId)(item["profitcenter"][0]))];
                        entry.ProfitCenter = profitcenter["identifier"].ToString();
                    }
                    else if (user.GetValue("profitcenter", string.Empty) != string.Empty)
                    {
                        BsonDocument profitcenter = profitcenterCache[((ObjectId)(user["profitcenter"][0]))];
                        entry.ProfitCenter = profitcenter["identifier"].ToString();
                    }

                    if ((bool)config["useridentifier"]["removeprecedingzeroes"].GetValueOrDefault(false))
                        entry.UserIdentifier = user["identifier"].ToString().TrimStart('0');
                    else
                        entry.UserIdentifier = user["identifier"].ToString();

                    //From absenceentry item
                    entry.StartDate = item["date"].ToUniversalTime();
                    entry.EndDate = item["date"].ToUniversalTime();

                    //Should use this notation if value does not exists
                    entry.Hours = Convert.ToInt32(item.GetValue("duration", 0));

                    BsonDocument payType = absencePaytypeCache[((ObjectId)(item["absenceentrytype"][0]))];
                    entry.PayTypeId = payType["identifier"].ToString();
                    entry.PayType = payType;

                    //CLA info
                    entry.ClaGroup = claidentifier;
                    //Helper for track objectID
                    entry.ObjectId = item["_id"].ToString();

                    entries.Add(entry);
                }
                catch (Exception ex)
                {
                    //Detailed info for Errors.txt
                    string userName = item.GetValue("__user__displayname", "").ToString();
                    string date = item.GetValue("date", "").ToString();
                    string type = item.GetValue("__absenceentrytype__displayname", "").ToString();

                    double amount = MillisecondsToHours(Convert.ToInt32(item.GetValue("duration", 0)));

                    failedExports.Add((ObjectId)item["_id"], string.Format("Absenceentry failed to export. Person: {0}, Date: {1}, Entrytype: {2}, Hours: {3}, Exception: {4}", userName, date, type, amount, ex.Message));

                    logger.LogError("absenceentry {0} has problem\n Exception\n{1}", item["_id"], ex.ToString());
                }
            }
            return entries;
        }

        /// <summary>
        /// Timesheet (tuntikirjaus) entries
        /// </summary>
        /// <param name="payrollPeriod">BsonDocument payrollPeriod</param>
        /// <param name="config">config["fields"]</param>
        /// <param name="exportTask">All viable information for the task</param>
        /// <returns>Collection of EntriesToPayroll of Timesheet></returns>
        private EntriesToPayroll<Timesheet> GetTimesheetToExport(BsonDocument payrollPeriod, DataTree config, PayrollExportTask exportTask)
        {
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
            long startBytes = System.GC.GetTotalMemory(true);
#endif
            logger.LogInfo("Part 1/3 - Getting unexported worker hours for payrollperiod  ", payrollPeriod["name"]);
            var configRoot = config;
            config = config["fields"];


            var andQueries = new List<IMongoQuery>();

            //Get and filter by cla (TES in finnish)
            BsonDocument cla = claCache[((ObjectId)(payrollPeriod["clacontract"][0]))];
            var claidentifier = cla["identifier"].ToString();
            andQueries.Add(Query.EQ("clacontract", payrollPeriod["clacontract"][0]));

            //Filter based on dates
            andQueries.Add(Query.GTE("date", (DateTime)payrollPeriod["startdate"]));
            andQueries.Add(Query.LT("date", (DateTime)payrollPeriod["enddate"]));  //Must be LT, since in db it is +1

            andQueries.Add(Query.EQ("internalworker", true));

            //If there is just one user
            if (exportTask.UserId != ObjectId.Empty)
                andQueries.Add(Query.EQ("user", exportTask.UserId));

            MongoCollection<BsonDocument> timesheetEntries = database.GetCollection("timesheetentry");
            MongoCursor<BsonDocument> cursor = timesheetEntries.Find(Query.And(andQueries));

            var entries = new EntriesToPayroll<Timesheet>(fieldNames, exportTask);

            foreach (var item in cursor)
            {
                try
                {
                    //User Info from cache
                    BsonDocument user = userCache[((ObjectId)(item["user"][0]))];
                    item["__UserSortName"] = user["lastname"] + " " + user["firstname"];
                    item["__supervisor__displayname"] = user["__supervisor__displayname"];
                    item["__profitcenter__displayname"] = user["__profitcenter__displayname"];

                    //PayType info at first since there might be second type (ketjutus)
                    BsonDocument payType = timesheetEntryDetailPaytypeCache[((ObjectId)(item["timesheetentrydetailpaytype"][0]))];

                    Timesheet entry = new Timesheet();

                    entry.PayTypeId = Convert.ToString(payType.GetValue("identifier", ""));
                    entry.PayType = payType;

                    //document
                    entry.Document = item;

                    //Profitcenter
                    if (item.GetValue("profitcenter", string.Empty) != string.Empty)
                    {
                        BsonDocument profitcenter = profitcenterCache[((ObjectId)(item["profitcenter"][0]))];
                        entry.ProfitCenter = profitcenter["identifier"].ToString();
                    }
                    else if (user.GetValue("profitcenter", string.Empty) != string.Empty)
                    {
                        BsonDocument profitcenter = profitcenterCache[((ObjectId)(user["profitcenter"][0]))];
                        entry.ProfitCenter = profitcenter["identifier"].ToString();
                    }
                    else
                    {
                        BsonDocument profitcenter = profitcenterCache[((ObjectId)(user["profitcenter"][0]))];
                        entry.ProfitCenter = profitcenter["identifier"].ToString();

                    }

                    if ((bool)config["useridentifier"]["removeprecedingzeroes"].GetValueOrDefault(false))
                        entry.UserIdentifier = user["identifier"].ToString().TrimStart('0');
                    else
                        entry.UserIdentifier = user["identifier"].ToString();

                    //From timesheetentry item
                    entry.StartDate = item["date"].ToUniversalTime();
                    entry.EndDate = item["date"].ToUniversalTime();
                    //Should use this notation if value does not exists
                    entry.Hours = Convert.ToInt32(item.GetValue("duration", 0));


                    //Project and workorder info
                    if (item["project"][0] == null)
                    {
                        logger.LogError("Timesheet entry does not contain project, entry " + item[0]);
                        throw new Exception("Timesheet entry does not contain project , entry " + item[0]);
                    }

                    //Project and workorder info
                    BsonDocument project = database.GetCollection("project").FindOne(Query.EQ(DBQuery.Id, item["project"][0]));

                    //If Project is disabled, skip it //Muutokset 2015-09-29
                    if ((bool)project.Contains("disabled") && ((bool)project["disabled"]))
                        continue;
                    //if workorder (työmääräin) //Muutokset 2015-09-29
                    if (project.Contains("projecttype") && (project["projecttype"].ToString() != "PROJECT" && project["projecttype"].ToString() != "__socialproject"))
                    {
                        //remove leading zeros
                        entry.WorkOrder = project["identifier"].ToString().TrimStart('0');

                        //Get Parentproject and assign it to ProjectNumber 
                        BsonDocument parentProject = database.GetCollection("project").FindOne(Query.EQ(DBQuery.Id, project["parentproject"][0])); //project["poski"].ToString().Replace("-", "")));

                        string proj = "";
                        //TODO: this must be checked, since it should not be disabled but lets put something in csv
                        if ((bool)parentProject.GetValue("disabled", false))
                        {
                            ObjectId pProject = (ObjectId)parentProject["_id"];
                            string identifier = parentProject.GetValue("identifier", "").ToString();
                            string userName = item.GetValue("__user__displayname", "").ToString();
                            string date = item.GetValue("date", "").ToString();
                            string type = item.GetValue("__timesheetentrydetailpaytype__displayname", "").ToString();

                            failedButSuccessExports.Add((ObjectId)item["_id"],
                                string.Format("Timesheetentry - Disabled parentproject {5} and identifier {6} for workorder {0} but successfully exported. Person: {1}, Personid: {2}, Date: {3}, Type: {4} ",
                                entry.WorkOrder, userName, entry.UserIdentifier, date, type, pProject, identifier));
                        }
                        else
                        {
                            //get identifier from config and remove '-' chars from string
                            proj = parentProject[config["projectnumber"]["identifier"].Value.ToString()].ToString().Replace("-", "");
                        }
                        entry.ProjectNumber = SplitProjectIdentifier(proj, (int)config["projectnumber"]["fieldspan"], (int)config["projectnumber"]["length"]);
                    }
                    else //it's directly to project or it's socialproject
                    {
                        //Remove social project type project name and replace it with enpty string array
                        if (project["identifier"].ToString().StartsWith("__social"))
                        {
                            entry.ProjectNumber = new string[(int)config["projectnumber"]["fieldspan"]];
                        }
                        else
                        {
                            entry.ProjectNumber = SplitProjectIdentifier(project["identifier"].ToString(), (int)config["projectnumber"]["fieldspan"], (int)config["projectnumber"]["length"]);
                        }
                    }
                    //CLA info
                    entry.ClaGroup = claidentifier;
                    //Helper for track objectID
                    entry.ObjectId = item["_id"].ToString();


                    //if has a different price
                    if ((bool)item.GetValue("hasprice", false))
                    {
                        entry.Price = Convert.ToDouble(item.GetValue("price", 0));
                        //Needed at least Are where 1 paytypeid(115) has an invidual price
                        entry.HourlyRate = entry.Price;
                        if (entry.HourlyRate > 0)
                        {
                            var x = 0;
                        }
                    }
                    entries.Add(entry);

                    //if paytype is chainged (ketjutus) then add another entry (ei ollut aikaa liikaa mieltää refactorointia mutta testataan (7.10.2015)
                    if (payType.Contains("identifier2") && !string.IsNullOrEmpty(payType["identifier2"].ToString()))
                    {
                        entry = AddChainedEntry(payType, entry);
                        entries.Add(entry);
                    }

                }
                catch (Exception ex)
                {
                    //Detailed info for Errors.txt
                    string userName = item.GetValue("__user__displayname", "").ToString();
                    string date = item.GetValue("date", "").ToString();
                    string type = item.GetValue("__timesheetentrydetailpaytype__displayname", "").ToString();
                    double amount = 0;
                    if (item.GetValue("duration", 0).IsInt32)
                        amount = (int)(item.GetValue("duration", 0)) / 1000 / 60 / 60;
                    else if (item.GetValue("duration", 0).IsDouble)
                        amount = (int)((double)(item.GetValue("duration", 0)) / 1000 / 60 / 60);
                    failedExports.Add((ObjectId)item["_id"], string.Format("Timesheetentry failed to export. Person: {0}, Date: {1}, Entrytype: {2}, Amount: {3}, Exception: {4}", userName, date, type, amount, ex.Message));

                    logger.LogError("timesheetentry {0} has problem\n Exception\n{1}", item["_id"], ex.ToString());
                }
            }

            //Timetracking and parent
            if ((bool)(configRoot["timetracking"].GetValueOrDefault(false)))
            {
                if ((bool)(configRoot["lunchbreak"].GetValueOrDefault(false)))
                {
                    var lunchBreakValid = (int)configRoot["lunchbreaktimeafter"].GetValueOrDefault(PayrollConstants.MinDayLengthWithLunchBreak);

                    foreach (var item in entries)
                    {
                        try
                        {

                            //if (item.Document.GetValue("parent", "") == "" || )
                            if (item.Document.Contains("parent") == false)
                            {
                                //Find all possible details for this item
                                var found = entries.Where(p => p.Document.Contains("parent") && p.Document["parent"][0] == item.Document[0]);

                                //no detail entries
                                if (found.Count() == 0 && item.Hours > lunchBreakValid && (bool)item.PayType.GetValue("countsasregularhours", false))
                                {
                                    item.Hours -= PayrollConstants.LunchBreakTime;
                                    continue;
                                }
                                var totalsum = 0;
                                foreach (var item2 in found)
                                {
                                    if ((bool)item2.PayType.GetValue("countsasregularhours", false))
                                    {
                                        totalsum += Convert.ToInt32(item2.Hours);
                                    }
                                }
                                if (totalsum > lunchBreakValid)
                                {
                                    item.Hours -= PayrollConstants.LunchBreakTime;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            failedExports.Add((ObjectId)item.Document[0], string.Format("Timesheetentry Timetracking failed to export, Exception: {0}", ex.Message));
                        }
                    }
                }

                foreach (var item in entries)
                {
                    try
                    {
                        //If detail then subtract it from paren
                        if (item.Document.GetValue("parent", null) != null)
                        {
                            var found = entries.Where(p => p.Document[0] == item.Document["parent"][0]).FirstOrDefault();
                            if (found == null)
                                continue;
                            found.Hours -= item.Hours;
                        }
                    }
                    catch (Exception ex)
                    {
                        failedExports.Add((ObjectId)item.Document[0], string.Format("Timesheetentry Timetracking failed to export, Exception: {0}", ex.Message));
                    }
                }
            }

#if DEBUG
            long stopBytes = System.GC.GetTotalMemory(true);
            sw.Stop();
            Debug.WriteLine("Time eleapsed={0}ms, size={1} KB", sw.ElapsedMilliseconds, ((long)(stopBytes - startBytes)) / 1024);
#endif
            return entries;

        }

        /// <summary>
        /// Used in case of chained paytype (ketjutus)
        /// </summary>
        /// <param name="item">Document to have chained entry in paytype (identifier2)</param>
        /// <param name="entry">Timesheet entry type</param>
        /// <returns>Chained timeheet entry</returns>
        /// <seealso cref="EntriesToPayroll{T}"/>
        private Timesheet AddChainedEntry(BsonDocument item, Timesheet entry)
        {
            Timesheet entryChained = new Timesheet();
            entryChained.UserIdentifier = entry.UserIdentifier;
            entryChained.Document = entry.Document;
            entryChained.ClaGroup = entry.ClaGroup;
            entryChained.PaymentGroup = entry.PaymentGroup;
            entryChained.WorkOrder = entry.WorkOrder;
            entryChained.ProjectNumber = entry.ProjectNumber;
            entryChained.Price = entry.Price;
            entryChained.StartDate = entry.StartDate;
            entryChained.EndDate = entry.EndDate;
            entryChained.Hours = entry.Hours * (double)item["paytypefactor2"];
            entryChained.PayTypeId = item["identifier2"].ToString();
            ObjectId entryId = ObjectId.GenerateNewId();
            entryChained.ObjectId = entryId.ToString();

            //Delete possible fix maybe should check for timetracking?
            entryChained.PayType = entry.PayType;

            return entryChained;
        }

        /// <summary>
        /// Splits eg. projectnumber based on fieldspan and maxlength from config.tree
        /// </summary>
        /// <param name="project">String to split</param>
        /// <param name="fieldSpan">Number of indexes in array</param>
        /// <param name="maxLength">Length of index</param>
        /// <returns>array of splitted string</returns>
        private string[] SplitProjectIdentifier(string project, int fieldSpan, int maxLength)
        {
            string[] arr = new string[fieldSpan];
            int index = 0;
            string temp = "";
            for (int i = 0; i < fieldSpan; i++)
            {
                for (int j = 0; j < maxLength; j++)
                {
                    if (index > project.Length - 1) break;
                    temp += project[index];
                    index++;
                }
                arr[i] = temp;
                temp = "";
            }
            return arr;
        }

        /// <summary>
        /// Cached dictionaries
        /// </summary>
        private Dictionary<ObjectId, BsonDocument> userCache;
        private Dictionary<ObjectId, BsonDocument> timesheetEntryDetailPaytypeCache;
        private Dictionary<ObjectId, BsonDocument> absencePaytypeCache;
        private Dictionary<ObjectId, BsonDocument> dayPaytypeCache;
        private Dictionary<ObjectId, BsonDocument> claCache;
        private Dictionary<ObjectId, BsonDocument> profitcenterCache;

        /// <summary>
        /// Fill specific Mongodb collection to Dictionary
        /// </summary>
        /// <param name="collection">Name of the collection in mongodb</param>
        /// <param name="cacheCollection">name of the Dictionary</param>
        /// <returns>Cached Dictionary</returns>
        private Dictionary<ObjectId, BsonDocument> FillCacheCollection(string collection, Dictionary<ObjectId, BsonDocument> cacheCollection)
        {
            cacheCollection = new Dictionary<ObjectId, BsonDocument>();
            MongoCollection<BsonDocument> mongoCol = database.GetCollection(collection);
            MongoCursor<BsonDocument> cursor = mongoCol.FindAll();
            foreach (var item in cursor)
            {
                cacheCollection.Add((ObjectId)item[0], item);
            }
            return cacheCollection;
        }

        /// <summary>
        /// Populate collection to dictonaries
        /// <para>
        /// user,clacontract,timesheetentrydetailpaytype,absenceentrytype,dayentrytype
        /// </para>
        /// </summary>
        private void PopulateCollectionsToCache()
        {
#if (DEBUG)
            var sw = new Stopwatch();
            sw.Start();
            long startBytes = System.GC.GetTotalMemory(true);
#endif
            //user ~> 0,1s, 4K, 9MB
            userCache = FillCacheCollection("user", userCache);
            claCache = FillCacheCollection("clacontract", claCache);
            //timesheetentrydetailpaytype ~> 25ms, 60, 270KB 
            timesheetEntryDetailPaytypeCache = FillCacheCollection("timesheetentrydetailpaytype", timesheetEntryDetailPaytypeCache);
            absencePaytypeCache = FillCacheCollection("absenceentrytype", absencePaytypeCache);
            dayPaytypeCache = FillCacheCollection("dayentrytype", dayPaytypeCache);
            profitcenterCache = FillCacheCollection("profitcenter", profitcenterCache);

#if (DEBUG)
            long stopBytes = System.GC.GetTotalMemory(true);
            Debug.WriteLine("Time eleapsed={0}ms, size={1} KB", sw.ElapsedMilliseconds, ((long)(stopBytes - startBytes)) / 1024);
            Debug.WriteLine("Users={0}, clas={1}, timesheetentrytypess={2}, absenceentrytypes={3}, dayentrytypes={4}",
                userCache.Count, claCache.Count, timesheetEntryDetailPaytypeCache.Count, absencePaytypeCache.Count, dayPaytypeCache.Count);
#endif
        }

        /// <summary>
        /// Get the payrollperiod and lock it if requested while prosessing entries
        /// </summary>
        /// <param name="payrollPeriod">Payroll period</param>
        /// <param name="lockPayrollPeriod">Wheter lock or not, default lock</param>
        private void InitializeAndLockPayrollPeriod(BsonDocument payrollPeriod, bool lockPayrollPeriod = true)
        {
            if (payrollPeriod == null)
                return;

            if (lockPayrollPeriod)
            {
                logger.LogDebug("Locking payroll period", payrollPeriod);
                payrollPeriod["locked"] = true;
                database.GetCollection("payrollperiod").Save(payrollPeriod, WriteConcern.Acknowledged);
            }
        }

        #endregion

        #region Main export stuff

        /// <summary>
        /// Poll's mc2db collection("payrollexport").status == "WaitingToStart"
        /// </summary>
        /// <param name="database">Mongodb from Handler (mc2db)</param>
        /// <param name="isAutomatic">If automatic generation</param>
        /// <param name="period">if automatic then _id of payrollperiod</param>
        /// <returns>Next payroll export task.</returns>
        public static PayrollExportTask GetNextPayrollExportTask(MongoDatabase database, bool isAutomatic = false, BsonDocument period = null)
        {
            var exportTask = new PayrollExportTask();
            BsonDocument payrollExportRecord = null;
            if (isAutomatic)
            {
                MongoCollection<BsonDocument> collection = database.GetCollection("payrollexport");
                BsonDocument doc = new BsonDocument();

                doc["status"] = "WaitingToStart";
                doc["target"] = ExportType.Csv.ToString().ToLower();
                doc["onlyentriesnotacceptedbymanager"] = false;
                doc["onlyentriesnotacceptedbyworker"] = false;
                doc["onlyexportedentries"] = false;
                doc["payrollperiod"] = new BsonArray() { period["_id"] };

                var timestamp = DateTime.UtcNow;
                doc["modified"] = timestamp;
                doc["created"] = timestamp;
                doc["__payrollperiod__displayname"] = period["name"];
                doc["__user__displayname"] = "automatic generator";

                collection.Save(doc, WriteConcern.Acknowledged);
            
                exportTask.IsAutomatic = true;
            }

			payrollExportRecord = database.GetCollection("payrollexport").FindOne(Query.EQ("status", PayrollConstants.WaitingToStart));

            if (payrollExportRecord != null)
            {
                exportTask.ExportId = (ObjectId)payrollExportRecord["_id"];

                exportTask.PayrollPeriodName = Convert.ToString(payrollExportRecord.GetValue("__payrollperiod__displayname", ""));
                exportTask.UserName = Convert.ToString(payrollExportRecord.GetValue("__user__displayname", ""));

                if (payrollExportRecord.Contains("payrollperiod"))
                {
                    exportTask.PayrollPeriodId = (ObjectId)payrollExportRecord["payrollperiod"][0];

                    exportTask.PayrollPeriod = database.GetCollection("payrollperiod").FindOne(Query.EQ("_id", exportTask.PayrollPeriodId));

                    if (exportTask.PayrollPeriod == null)
                    {
                        payrollExportRecord["status"] = PayrollConstants.Failed;
                        database.GetCollection("payrollexport").Save(payrollExportRecord, WriteConcern.Acknowledged);
                        throw new InvalidDataException("Payroll period was specified in task but wasn't found in db: " + exportTask.PayrollPeriodName);
                    }

                }

                if (payrollExportRecord.Contains("user"))
                {
                    exportTask.UserId = (ObjectId)payrollExportRecord["user"][0];
                    exportTask.User = database.GetCollection("user").FindOne(Query.EQ("_id", exportTask.UserId));

                    if (exportTask.User == null)
                    {
                        payrollExportRecord["status"] = PayrollConstants.Failed;
                        database.GetCollection("payrollexport").Save(payrollExportRecord, WriteConcern.Acknowledged);
                        throw new InvalidDataException("User was specified in task but wasn't found in db: " + exportTask.UserName);
                    }
                }

                exportTask.onlyExportEntriesNotAcceptedByManager = (bool)payrollExportRecord.GetValue("onlyentriesnotacceptedbymanager", false);
                exportTask.onlyExportEntriesNotAcceptedByWorker = (bool)payrollExportRecord.GetValue("onlyentriesnotacceptedbyworker", false);
                exportTask.onlyExportExportedEntries = (bool)payrollExportRecord.GetValue("onlyexportedentries", false);

                string exportTargetString = Convert.ToString(payrollExportRecord.GetValue("target", "csv"));
                exportTask.ExportType = (exportTargetString == "excel") ? ExportType.Excel : ExportType.Csv;

                if (exportTask.User == null && exportTask.PayrollPeriod == null)
                {
                    payrollExportRecord["status"] = PayrollConstants.Failed;
                    database.GetCollection("payrollexport").Save(payrollExportRecord, WriteConcern.Acknowledged);
                    throw new InvalidDataException("Invalid payroll export. No payroll period or user specified.");
                }

                return exportTask;
            }

            return null;
        }

        /// <summary>
        /// Starting Export function
        /// </summary>
        /// <param name="exportTask">Payroll export task..</param>
        public void ExportDocuments(PayrollExportTask exportTask)
        {
            lock (payrollExportLock)
            {
                try
                {
                    StartExport(exportTask);
                }
                catch (Exception)
                {
                   UpdateExportStatus(PayrollConstants.Failed, exportTask.ExportId);
                   throw;
                }
            }
        }


        /// <summary>
        /// Update TRO ui for payroll department to see whats happening during the export
        /// </summary>
        /// <param name="status">Contants from ExportStatus static fields</param>
        /// <param name="_id">ObjectId in collection("payrollexport")._id</param>
        private void UpdateExportStatus(string status, ObjectId _id)
        {
            logger.LogInfo("Payroll export status changed", status, _id);

            MongoCollection<BsonDocument> col = database.GetCollection("payrollexport");
            BsonDocument doc = col.FindOne(Query.EQ(DBQuery.Id, _id));
            doc["status"] = status;
            col.Save(doc, WriteConcern.Acknowledged);
            //Testausta varten
            //System.Threading.Thread.Sleep(5000);
        }

        /// <summary>
        /// Sets payrollperiod locked=false and all items in it to status exported_visma=false
        /// </summary>
        /// <param name="message">DataTree with payroll and optionally user information</param>
        public void PayrollRevert(DataTree message)
        {
            lock (payrollExportLock)
            {
                MongoCollection<BsonDocument> payrollPeriodCollection = database.GetCollection("payrollperiod");
                var row = payrollPeriodCollection.FindOne(Query.EQ(DBQuery.Id, ObjectId.Parse(message["payrollperiod"])));
                var payrollperiod = row["name"].ToString();
                logger.LogInfo("Starting to revert payroll {0} ", payrollperiod);

                //If invidual user
                if (message["user"].HasValue)
                {
                    var userId = ObjectId.Parse(message["user"].Value.ToString());
                    RevertForEntryType(payrollperiod, "timesheetentry", userId);
                    RevertForEntryType(payrollperiod, "absenceentry", userId);
                    RevertForEntryType(payrollperiod, "dayentry", userId);
                }
                else
                {
                    RevertForEntryType(payrollperiod, "timesheetentry");
                    RevertForEntryType(payrollperiod, "absenceentry");
                    RevertForEntryType(payrollperiod, "dayentry");
                }
            }
        }
        #endregion

        #region Helper functions

        /// <summary>
        /// Helper function to sanitize file/path names
        /// </summary>
        /// <param name="s">strinig name for file/path</param>
        /// <param name="replaceChar">Optional to give replace char, default=empty string</param>
        /// <returns>Valid name for file/path</returns>
        public static string ReturnSafeString(string s, string replaceChar = "")
        {
            foreach (char character in Path.GetInvalidFileNameChars())
            {
                s = s.Replace(character.ToString(), replaceChar);
            }

            foreach (char character in Path.GetInvalidPathChars())
            {
                s = s.Replace(character.ToString(), replaceChar);
            }
            return (s);
        }

        /// <summary>
        /// Convert (int)milliseconds to (double)hours
        /// </summary>
        /// <param name="milliseconds"></param>
        /// <returns></returns>
        public static double MillisecondsToHours(int milliseconds)
        {
            return (double)(milliseconds / 1000m / 60m / 60m);
        }

        #endregion

    }
}
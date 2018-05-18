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
using MongoDB.Driver.Linq;
using MongoDB.Driver;
using MongoDB.Bson.IO;
using MongoDB.Driver.Builders;
using SystemsGarden.mc2.Common;
using System.Diagnostics;
using OfficeOpenXml.Table.PivotTable;
using OfficeOpenXml.Table;
using OfficeOpenXml;

//using System.Runtime.InteropServices;

namespace BatchProcessEntriesToExcel
{
    /// <summary>
    /// Actual class to handling payroll export actions ( export / revert)
    /// </summary>
    public sealed class PayrollExport
    {

        #region Members
        private string filePath;
        private MongoDatabase database;
        private StreamWriter logWriter;
        //For shorter period of time
        private DateTime startDate = DateTime.Parse("01.06.2016");
        private Stopwatch sw = new Stopwatch();


        /// <summary>
        /// Excel Export
        /// </summary>
        public ExportPivotTable PivotTable { get; set; } = null;


        /// <summary>
        /// PayrollIntegrationHandleServer config
        /// </summary>
        public static DataTree config;
        private object payrollExportLock = new object();

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
        //public PayrollExport(
        //    ILogger logger,
        //    string filePath,
        //    MongoDatabase database,
        //    DataTree config)
        public PayrollExport(
                string filePath,
                MongoDatabase database,
                DataTree config)

        {
            //this.logger = logger.CreateChildLogger("PayrollExport");
            this.logWriter = new StreamWriter(filePath, true);
            this.filePath = filePath;
            this.database = database;
            PayrollExport.config = config;
            sw.Start();
        }

        public PayrollExport(
                string filePath,
                MongoDatabase database)
        {
            this.logWriter = new StreamWriter(filePath, true);
            this.filePath = filePath;
            this.database = database;
            sw.Start();
        }
        #endregion

        #region PayrollExportStart

        /// <summary>
        /// Starting the actual exporting whilst also update statuses and logger info
        /// </summary>
        /// <param name="exportTask">Payroll export task</param>
        private void StartExport()
        {
            try
            {
                //Cache often used collections
                PopulateCollectionsToCache();

                UpdateExportStatus(PayrollConstants.ProcessingData);

                EntriesToPayroll<Absence> absences = GetAbsencesToExport();
                logWriter.Flush();
                EntriesToPayroll<Timesheet> workHours = GetTimesheetToExport();
                //EntriesToPayroll<Timesheet> workHours = new EntriesToPayroll<Timesheet>();

                var now = MC2DateTimeValue.Now().ToLocalTime();
                var nowStr = string.Format("{0:0000}-{1:00}-{2:00}-{3:00}-{4:00}-{5:00}",
                    now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);

                UpdateExportStatus(PayrollConstants.GeneratingExcel);
                ExportExcelDocument(
                        Path.Combine(Properties.Settings.Default.ExcelPath, string.Format("{0}_{1}", nowStr, Properties.Settings.Default.ExcelFile)),
                        absences,
                        workHours, Properties.Settings.Default.Language);
            }
            catch (Exception ex)
            {
                UpdateExportStatus(PayrollConstants.Failed);
                throw ex;
            }

            //Some of the entries not exported
            UpdateExportStatus(PayrollConstants.Completed);
            UpdateExportStatus(string.Format("Alldone Time eleapsed={0}minutes", (sw.Elapsed.TotalMinutes)));
            sw.Stop();
            if (logWriter != null)
                logWriter.Dispose();
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
            EntriesToPayroll<Absence> absences,
            EntriesToPayroll<Timesheet> workHours,
            string language)
        {
            try
            {
                OfficeOpenXml.ExcelPackage excelPackage = new ExcelPackage(new FileInfo(file));

                string workSheetName;
                string pivotRangeName;
                if (language.ToLower() == "fi")
                {
                    workSheetName = "Tunnit";
                    pivotRangeName = "tuntiPivotAlue";
                }
                else
                {
                    workSheetName = "Hours";
                    pivotRangeName = "hoursPivotRange";
                }
                ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets.Add(workSheetName);
                //need to start at row 2, since row 1 is header info
                int startingRow = 2;
                startingRow = absences.AppendToExcelWorksheet<Absence>(worksheet, startingRow);
                startingRow = workHours.AppendToExcelWorksheet<Timesheet>(worksheet, startingRow, false); // 3rd parameter is for robust way not to write header information again


                // Create pivot table for timesheet and absence entries
                if (language.ToLower() == "fi")
                {
                    PivotTable = new ExportPivotTable(
                    new string[] { "Nimi" }, /* collapsible rows */
                    new string[] { "Tunnit" });

                }
                else
                {
                    PivotTable = new ExportPivotTable(
                    new string[] { "Name" }, /* collapsible rows */
                    new string[] { "Hours" });
                }
                PivotTable.CreatePivotTable(excelPackage, worksheet, pivotRangeName);
                excelPackage.Save();
            }
            catch (Exception ex)
            {
                UpdateExportStatus(string.Format("Critical exception when creating file Errorstack {0}", ex));
                throw;
            }
        }


        /// <summary>
        /// Absence (poissaolo) entries
        /// </summary>
        /// <param name="payrollPeriod">BsonDocument payrollPeriod</param>
        /// <param name="config">config["fields"]</param>
        /// <param name="exportTask">All viable information for the task</param>
        /// <returns>Collection of EntriesToPayroll of Absences></returns>
        private EntriesToPayroll<Absence> GetAbsencesToExport()
        {
            UpdateExportStatus("Part 1 - Getting absences");
            MongoCollection<BsonDocument> entriesCollection = database.GetCollection("absenceentry");

#if (DATE)
            var andQueries = new List<IMongoQuery>();
            //Filter based on dates
            andQueries.Add(Query.GTE("starttimestamp", startDate));
            andQueries.Add(Query.LT("endtimestamp", (DateTime.Now)));
            MongoCursor<BsonDocument> cursor = entriesCollection.Find(Query.And(andQueries));
#else
            MongoCursor<BsonDocument> cursor = entriesCollection.FindAll();
#endif
            var entries = new EntriesToPayroll<Absence>();
            foreach (var item in cursor)
            {
                try
                {
                    Absence entry = new Absence();
                    //document
                    entry.Document = item;

                    entry._id = (ObjectId)item[0];
                    entry.ApprovedByWorker = (bool)item.GetValue("approvedbyworker", false);
                    entry.ApprovedByManager = (bool)item.GetValue("approvedbymanager", false);

                    //User Info from cache
                    BsonDocument user = userCache[((ObjectId)(item["user"][0]))];
                    entry.UserName = item.GetValue("__user__displayname", "noname").ToString(); //muutettu
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

                    //NOTE: Removed by request of Oskari Jauhianen 2016-05-27
                    //Only profitcenter that starts with 1
                    //if (entry.ProfitCenter.StartsWith("1") == false)
                    //    continue;

                    entry.UserIdentifier = user["identifier"].ToString();

                    //entry.StartDate = item["starttimestamp"].ToUniversalTime();
                    //entry.EndDate = item["endtimestamp"].ToUniversalTime();

                    entry.StartDate = item["starttimestamp"].ToLocalTime();
                    entry.EndDate = item["endtimestamp"].ToLocalTime();

                    //Should use this notation if value does not exists
                    //entry.Hours = Convert.ToInt32(item.GetValue("duration", 0));
                    entry.Hours = (entry.EndDate - entry.StartDate).TotalMilliseconds;

                    BsonDocument payType = absencePaytypeCache[((ObjectId)(item["absenceentrytype"][0]))];
                    entry.PayTypeId = payType["identifier"].ToString();
                    entry.PayTypeName = payType["name"].ToString();

                    //entry.ClaGroup = claidentifier;

                    entries.Add(entry);
                }
                catch (Exception ex)
                {
                    UpdateExportStatus(string.Format("absenceentry {0} has problem\n Exception\n{1}", item["_id"], ex.ToString()));
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
        private EntriesToPayroll<Timesheet> GetTimesheetToExport()
        {
            UpdateExportStatus("Part 2 - Getting worker hours");
            MongoCollection<BsonDocument> entriesCollection = database.GetCollection("timesheetentry");

#if (DATE)
            var andQueries = new List<IMongoQuery>();
            //Filter based on dates
            andQueries.Add(Query.GTE("starttimestamp", startDate));
            andQueries.Add(Query.LT("endtimestamp", (DateTime.Now)));
            MongoCursor<BsonDocument> cursor = entriesCollection.Find(Query.And(andQueries));
            cursor = entriesCollection.Find(Query.And(andQueries));

#else
            MongoCursor<BsonDocument> cursor = entriesCollection.FindAll();
#endif
            var entries = new EntriesToPayroll<Timesheet>();
            foreach (var item in cursor)
            {
                try
                {
                    Timesheet entry = new Timesheet();
                    //document
                    entry.Document = item;

                    BsonDocument payType = timesheetEntryDetailPaytypeCache[((ObjectId)(item["timesheetentrydetailpaytype"][0]))];
                    entry.PayTypeId = Convert.ToString(payType.GetValue("identifier", ""));
                    entry.PayType = payType;
                    entry.PayTypeName = payType["name"].ToString();

                    entry._id = (ObjectId)item[0];
                    entry.ApprovedByWorker = (bool)item.GetValue("approvedbyworker", false);
                    entry.ApprovedByManager = (bool)item.GetValue("approvedbymanager", false);

                    //User Info from cache
                    BsonDocument user = userCache[((ObjectId)(item["user"][0]))];
                    entry.UserName = item.GetValue("__user__displayname", "noname").ToString();


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

                    //NOTE: Removed by request of Oskari Jauhianen 2016-05-27
                    //Only profitcenter that starts with 1
                    //if (entry.ProfitCenter.StartsWith("1") == false)
                    //    continue;

                    entry.UserIdentifier = user["identifier"].ToString();

                    entry.StartDate = item["starttimestamp"].ToLocalTime();
                    entry.EndDate = item["endtimestamp"].ToLocalTime();


                    //Should use this notation if value does not exists
                    entry.Hours = Convert.ToInt32(item.GetValue("duration", 0));
                    //Tämä oli showstopperi vanhasta versiosta... 2016-06-12
                    //entry.Hours = (entry.EndDate - entry.StartDate).TotalMilliseconds;

                    //Project info
                    if (item["project"][0] == null)
                    {
                        UpdateExportStatus("Timesheet entry does not contain project, entry " + item[0]);
                        throw new Exception("Timesheet entry does not contain project , entry " + item[0]);
                    }

                    //Project and workorder info
                    BsonDocument project = database.GetCollection("project").FindOne(Query.EQ(DBQuery.Id, item["project"][0]));

                    entry.ProjectNumber = project["identifier"].ToString();

                    //CLA info
                    //entry.ClaGroup = claidentifier;
                    entries.Add(entry);
                    //Debug.WriteLine(entry.PayTypeName);

                }
                catch (Exception ex)
                {
                    UpdateExportStatus(string.Format("timesheetentry {0} has problem\n Exception\n{1}", item["_id"], ex.ToString()));
                }

            }
            //Timetracking and parent
            UpdateExportStatus("Part 2 - Finished worker hours total " + entries.Count);
            //**********tähän jäätiin 2016-06-12 ja varmaan oikea

            //return entries;


            //var counterX = 0;
            foreach (var item in entries)
            {
                //If detail
                if (item.Document.GetValue("parent", null) != null)
                {
                    var found = entries.Where(p => p.Document[0] == item.Document["parent"][0]).FirstOrDefault();
                    if (found == null)
                        continue;
                    found.Hours -= item.Hours;
                }
            }
            return entries;

            //#if(DEBUG)
            //            Debug.WriteLine(counterX);
            //#endif
            //            //Pass 2 for timesheetentrydetails
            //            //            pass2:

            //            //            UpdateExportStatus("Part 2 - Getting worker details");

            //            //            entriesCollection = database.GetCollection("timesheetentrydetail");
            //            //#if (DATE)
            //            //            //Filter on dates
            //            //            andQueries = new List<IMongoQuery>();
            //            //            andQueries.Add(Query.GTE("created", startDate));
            //            //            andQueries.Add(Query.LT("created", (DateTime.Now)));
            //            //            cursor = entriesCollection.Find(Query.And(andQueries));
            //            //#else
            //            //            //cursor = timesheetEntries.Find(Query.And(andQueries));
            //            //            cursor = entriesCollection.FindAll();
            //            //#endif
            //            //            foreach (var item in cursor)
            //            //            {
            //            //                try
            //            //                {
            //            //                    Timesheet entry = new Timesheet();
            //            //                    entry.Document = item;
            //            //                    entry.IsTimesheetEntryDetail = true;

            //            //                    BsonDocument payType = timesheetEntryDetailPaytypeCache[((ObjectId)(item["timesheetentrydetailpaytype"][0]))];
            //            //                    entry.PayTypeId = Convert.ToString(payType.GetValue("identifier", "no paytype"));
            //            //                    entry.PayTypeName = Convert.ToString(payType.GetValue("name", "no paytype name"));
            //            //                    BsonArray test = (BsonArray)item.GetValue("timesheetentry", null);
            //            //                    if (test.Count < 1)
            //            //                        continue;

            //            //                    BsonDocument timeSheetEntry = database.GetCollection("timesheetentry").FindOne(Query.EQ(DBQuery.Id, item["timesheetentry"][0]));
            //            //                    if (timeSheetEntry == null)
            //            //                        continue;

            //            //                    entry._id = (ObjectId)timeSheetEntry[0];
            //            //                    BsonDocument user = userCache[((ObjectId)(timeSheetEntry["user"][0]))];
            //            //                    if ((bool)user["internalworker"] == false)
            //            //                        continue;

            //            //                    BsonDocument profitcenter = profitcenterCache[((ObjectId)(user["profitcenter"][0]))];
            //            //                    entry.ProfitCenter = profitcenter["identifier"].ToString();

            //            //                    //NOTE: Removed by request of Oskari Jauhianen 2016-05-27
            //            //                    //Only profitcenter that starts with 1
            //            //                    //if (entry.ProfitCenter.StartsWith("1") == false)
            //            //                    //    continue;

            //            //                    entry.Hours = Convert.ToInt32(item.GetValue("duration", 0));
            //            //                    entries.Add(entry);
            //            //                }
            //            //                catch (Exception ex)
            //            //                {
            //            //                    UpdateExportStatus(string.Format("timesheetentrydetail {0} has problem\n Exception\n{1}", item["_id"], ex.ToString()));
            //            //                }
            //            //            }

            //            //            UpdateExportStatus("Part 2 - Finished worker details total " + entries.Count);

            //            //            UpdateExportStatus("Part 3 - Starting details with timesheetentries");

            //            //            //Timetracking and parent
            //            //            foreach (var item in entries)
            //            //            {
            //            //                //BsonDocument tempTimeSheetEntry = database.GetCollection("timesheetentry").FindOne(Query.EQ(DBQuery.Id, item._id));

            //            //                //If detail
            //            //                if (item.Document.GetValue("timesheetentry", null) != null)
            //            //                {
            //            //                    var found = entries.Where(p => p.Document[0] == item.Document["timesheetentry"][0]).FirstOrDefault();
            //            //                    if (found == null)
            //            //                        continue;
            //            //                    found.Hours -= item.Hours;
            //            //                    //*************
            //            //                    item.StartDate = found.StartDate;
            //            //                    item.UserIdentifier = found.UserIdentifier;
            //            //                    item.UserName = found.UserName;
            //            //                    item.ProjectNumber = found.ProjectNumber;
            //            //                }
            //            //            }
            //            //            UpdateExportStatus("Part 3 - Finished details with timesheetentries");

            //            //            long stopBytes = System.GC.GetTotalMemory(true);
            //            //            UpdateExportStatus(string.Format("Absences & WorkSheets Time eleapsed={0} minutes", (sw.Elapsed.TotalMinutes)));
            //            //            UpdateExportStatus("Number of details written " + entries.Count);
            //            return entries;

        }

        /// <summary>
        /// Cached dictionaries
        /// </summary>
        private Dictionary<ObjectId, BsonDocument> userCache;
        private Dictionary<ObjectId, BsonDocument> timesheetEntryDetailPaytypeCache;
        private Dictionary<ObjectId, BsonDocument> absencePaytypeCache;
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
            userCache = FillCacheCollection("user", userCache);
            claCache = FillCacheCollection("clacontract", claCache);
            timesheetEntryDetailPaytypeCache = FillCacheCollection("timesheetentrydetailpaytype", timesheetEntryDetailPaytypeCache);
            absencePaytypeCache = FillCacheCollection("absenceentrytype", absencePaytypeCache);
            profitcenterCache = FillCacheCollection("profitcenter", profitcenterCache);
        }
        #endregion


        #region Main export stuff

        /// <summary>
        /// Starting Export function
        /// </summary>
        /// <param name="exportTask">Payroll export task..</param>
        public void ExportDocuments()
        {
            lock (payrollExportLock)
            {
                try
                {
                    StartExport();
                }
                catch (Exception ex)
                {
                    UpdateExportStatus(PayrollConstants.Failed + " " + ex.Message);
                    throw;
                }
            }
        }


        /// <summary>
        /// Update TRO ui for payroll department to see whats happening during the export
        /// </summary>
        /// <param name="status">Contants from ExportStatus static fields</param>
        /// <param name="_id">ObjectId in collection("payrollexport")._id</param>
        private void UpdateExportStatus(string status, ObjectId _id = new ObjectId())
        {
            logWriter.WriteLine(DateTime.Now + "--" + status);
        }

        #endregion


        #region Helper functions

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
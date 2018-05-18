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
        #region Members

        private ILogger logger;
        private string filePath;
        private MongoDatabase database;

        // Number of timehseet entries to hold in memory at one time.
        private const int TimesheetEntryExportBufferSize = 3; // Todo: Change to 500

        private DataTree config;

        private List<TroToVismaCsvLine> exportedLines = new List<TroToVismaCsvLine>();

        private FileStream ExportFile;

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

            Init();
        }

        #endregion

        #region Main export function

        public void ExportDocuments()
        {
            try
            {
                StartExport();

                ExportWorkerHours();
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
            DateTime now = DateTime.Now.ToLocalTime();

            string fileName = filePath + FilePrefix + "_" +
            string.Format("{0:0000}-{1:00}-{2:00}-{3:00}:{4:00}",
            now.Year, now.Month, now.Day, now.Hour, now.Second) + ".csv";

            ExportFile = File.OpenWrite(fileName);
        }

        private void EndExport()
        {
            if (ExportFile != null)
                ExportFile.Close();

            ExportFile = null;
        }

        #endregion

        #region Export worker hours

        private void ExportWorkerHours()
        {
            int exportCycles = 0;

            while (true)
            {
                try
                {
                    exportedLines.Clear();

                    MongoCursor<BsonDocument> cursor = GetUnexportedWorkerHours();

                    if (cursor.Count() > 0)
                    {
                        logger.LogInfo("Exporting found unexported timesheet entries", cursor.Count());
                    }
                    else
                    {
                        logger.LogFineTrace("No unexported timesheet entries found.");
                        return;
                    }

                    List<TimesheetEntryFragment> timesheetEntryFragments = IntegrationHelpers.GetTimesheetFragments(cursor, MinTimeFragmentSize, database, logger, failedExports);

                    logger.LogInfo(cursor.Count() + " timesheet entries produced " + timesheetEntryFragments.Count + " fragments.");

                    ExportTimesheetFragmentsToCsv(timesheetEntryFragments);

                    if (exportedLines.Count > 0)
                    {
                        logger.LogInfo("Saving exported Visma fragments to disk.", exportedLines.Count);
                        WriteExportedDataToDisk();
                    }
                    else
                    {
                        logger.LogInfo("No valid timesheet entry fragments found to export. Not saving XML document.");
                    }


                    MarkEntriesAsExported(cursor);
                }
                catch (Exception ex)
                {
                    logger.LogError("Exporting hours failed.", ex);

                    // If even a single pay entry fails, fail the whole process unless skipping is specified in config.
                    if (!(bool)config["skiperrors"])
                        throw;
                }

                exportCycles++;
                if (exportCycles > 100000)
                    throw new HandlerException("Too many Visma export cycles. Export is likely busy looping and i unable to mark items as exported.");
            }
        }

        private void MarkEntriesAsExported(IEnumerable<BsonDocument> cursor)
        {
            MongoCollection collection = database.GetCollection("timesheetentry");

            DateTime now = DateTime.Now.ToUniversalTime();

            logger.LogInfo("Marking " + cursor.Count() + " items as completed.");
            foreach (BsonDocument document in cursor)
            {
                document["exported_visma"] = true;
                document["exporttimestamp_visma"] = now;
                collection.Save(document, WriteConcern.Acknowledged);
            }
        }


        private void WriteExportedDataToDisk()
        {
            DirectoryInfo di = new DirectoryInfo(filePath);
            if (!di.Exists)
                di.Create();

            logger.LogInfo("Saving Visma export hour file.,");

            var sb = new StringBuilder();

            bool first = true;

            foreach(TroToVismaCsvLine line in exportedLines)
            {
                if (!first)
                    sb.AppendLine();
                else
                    first = false;

                for (int i = 0; i < TroToVismaCsvLine.NumberOfColumns; i++)
                {
                    sb.Append(line.Values[i]);
                    sb.Append(";");
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

        private void ExportTimesheetFragmentsToCsv(List<TimesheetEntryFragment> timesheetEntryFragments)
        {
            foreach (TimesheetEntryFragment fragment in timesheetEntryFragments)
            {
                var csvLine = new TroToVismaCsvLine();

                csvLine.Values[(int)TroToVismaColumns.PersonnelNumber] = fragment.WorkerId;
                csvLine.Values[(int)TroToVismaColumns.Paytype] = Convert.ToString(fragment.PayType);
                csvLine.Values[(int)TroToVismaColumns.Hours] = Convert.ToString((fragment.End - fragment.Start).Hours);
                csvLine.Values[(int)TroToVismaColumns.Days] = "0"; // Todo: Figure out if this is ok.
                csvLine.Values[(int)TroToVismaColumns.Amount] = Convert.ToString(fragment.Detail["amount"]);
                csvLine.Values[(int)TroToVismaColumns.Euro] = Convert.ToString(fragment.Detail["sum"], CultureInfo.InvariantCulture);
                csvLine.Values[(int)TroToVismaColumns.StartDate] = string.Format("{0:0000}-{1:00}-{2:00}", fragment.Start.Year, fragment.Start.Month, fragment.Start.Day);

                exportedLines.Add(csvLine);
            }
        }

        private MongoCursor<BsonDocument> GetUnexportedWorkerHours()
        {
            MongoCollection<BsonDocument> collection = database.GetCollection("timesheetentry");

            MongoCursor<BsonDocument> cursor = collection.Find(Query.And(
                    Query.NE("exported_visma", true),
                    Query.Exists("endtimestamp"),
                    Query.Exists("person"),
                    Query.EQ("approvedbyhr", true),
                    Query.Or(
                        Query.NotExists("exportfailurecount_visma"),
                        Query.LT("exportfailurecount_visma", MaxExportFailureCount
                    ))
                ));

            cursor.Limit = TimesheetEntryExportBufferSize;

            return cursor;
        }

        #endregion

        #region Setup

        private void Init()
        {
            MaxExportFailureCount = (int)config["trotovismaexport"]["maxexportfailurecount"].GetValueOrDefault(3);
            MinTimeFragmentSize = (int)config["trotovismaexport"]["mintimefragmentsize"].GetValueOrDefault(60);
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
            ProfitCenter = 7 // Use some custom field in Visma
        }

        private class TroToVismaCsvLine
        {
            private string[] values;
            public string[] Values { get { return values; }  }

            public const int NumberOfColumns = 10;

            public TroToVismaCsvLine()
            {
                values = new string[NumberOfColumns]; 
            }
        }

        #endregion
    }
}
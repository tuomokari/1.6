using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Shared;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using SystemsGarden.mc2.Common;

namespace SystemsGarden.mc2.RemoteConnector.Handlers.TroIntegrationCommon
{
    public class IntegrationHelpers
    {
        public const string ProfitCenterDefaultCategory = "100000";
        public const string PayTypeTravelTime = "402";
        public const string PayTypeBasic = "21";
        public const string PayTypeOvertime50 = "101";
        public const string PayTypeOvertime100 = "102";
        public const string PayTypeTravelOvertime50 = "141";
        public const string PayTypeTravelOvertime100 = "142";

        /// <summary>
        /// Split a timesheet entry into fragments. One fragment for each work type.
        /// </summary>
        /// <param name="timesheetEntryCursor"></param>
        /// <param name="minTimeFragmentSize"></param>
        /// <param name="database"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static List<TimesheetEntryFragment> GetTimesheetFragments(
            MongoCursor<BsonDocument> timesheetEntryCursor,
            int minTimeFragmentSize,
            MongoDatabase database, 
            ILogger logger,
            HashSet<ObjectId> failedExports,
			HashSet<ObjectId> succeededExports,
			bool splitToDays = true)
        {
            var resultFragments = new List<TimesheetEntryFragment>();

            foreach (BsonDocument timesheetEntry in timesheetEntryCursor)
            {
                try
                {
                    TimesheetEntryWithDetails entryWithDetails = GetTimesheetEntryWithDetails(timesheetEntry, database);

                    List<TimesheetEntryFragment> entryFragments = SplitTimesheetEntryWithDetailsToFragments(entryWithDetails, logger);

                    if (splitToDays)
                        entryFragments = SplitTimesheetFragmentsToDays(entryFragments);

                    if (minTimeFragmentSize > 0)
                        RemoveTooShortFragments(entryFragments, logger, minTimeFragmentSize);

                    resultFragments.AddRange(entryFragments.ToArray());

					succeededExports.Add((ObjectId)timesheetEntry[DBQuery.Id]);
				}
				catch (Exception ex)
                {
                    logger.LogError("Failed to handle timesheet entry. Skipping this entry", ex, timesheetEntry[DBQuery.Id]);
                    failedExports.Add((ObjectId)timesheetEntry[DBQuery.Id]);
                }
            }

            return resultFragments;
        }

        public static int EnumerateTroAllocationStatus(string status)
        {
            if (status == "In progress")
                return 1;
            if (status == "Not started")
                return 2;
            if (status == "Done")
                return 9;
            return -1;
        }
        public static string GetSAPRoleByTroEnumeration(int role)
        {
            if (role == 0)
                return "ZE"; // Worker "asentaja"
            if (role == 1)
                return "ZP"; // Workin "TYÖN ALLA - Asentaja"
            if (role == 9)  // Done "VALMIS - Asentaja"
                return "ZV";
            return "ZE"; // default: Worker "Asentaja"
        }

        public static void InsertErpOrderPartner(string orderId, string parNr, MongoDatabase mongoDatabase, ILogger logger)
        {
            string collName = "erpOrderPartner";
            if (!mongoDatabase.CollectionExists(collName))
            {
                logger.LogDebug("Collection doesn't exist. Creating it.", collName);

                mongoDatabase.CreateCollection(collName);
                MongoCollection<BsonDocument> newErpEntriesCollection = mongoDatabase.GetCollection(collName);
                IndexKeysBuilder Key = IndexKeys.Ascending("orderid");
                newErpEntriesCollection.CreateIndex(Key);
            }
            MongoCollection<BsonDocument> erpEntriesCollection = mongoDatabase.GetCollection(collName);
            MongoCursor cursor = erpEntriesCollection.Find(Query.And(Query.EQ("orderid", orderId), Query.EQ("parnr", parNr)));
            if (cursor.Count() == 0)
            {
                BsonDocument orderPartnerDocument = new BsonDocument();
                ObjectId currentProcess_id = ObjectId.GenerateNewId();
                orderPartnerDocument.Set(DBQuery.Id, currentProcess_id);
                orderPartnerDocument.Set("orderid", orderId);
                orderPartnerDocument.Set("parnr", parNr);
                orderPartnerDocument.Set("created", DateTime.UtcNow);
                erpEntriesCollection.Save(orderPartnerDocument, WriteConcern.Acknowledged);
            }
        }
        public static bool ErpOrderPartnerExists(string orderId, string parNr, MongoDatabase mongoDatabase)
        {
            string collName = "erpOrderPartner";
            if (!mongoDatabase.CollectionExists(collName))
                return false;

            MongoCollection<BsonDocument> erpEntriesCollection = mongoDatabase.GetCollection(collName);
            MongoCursor cursor = erpEntriesCollection.Find(Query.And(Query.EQ("orderid", orderId), Query.EQ("parnr", parNr)));
            if (cursor.Count() != 0)
                return true;

            return false;
        }
        public static bool IsQualifiedUserIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return false;
            System.Text.RegularExpressions.Regex reNum = new System.Text.RegularExpressions.Regex(@"^\d+$");
            return reNum.Match(identifier).Success;
        }

        public static DateTime GetDateFromMongoString(string dateStr)
        {
            if (string.IsNullOrEmpty(dateStr))
                return DateTime.MinValue;

            // Formatted like 2015-08-25
            int year = Convert.ToInt32(dateStr.Substring(0, 4));
            int month = Convert.ToInt32(dateStr.Substring(5, 2));
            int day = Convert.ToInt32(dateStr.Substring(8, 2));

            return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
        }
        public static string CutMessageField(string messageField, Int32 fieldLength)
        {
            if (string.IsNullOrEmpty(messageField) || messageField.Length <= fieldLength)
                return messageField;

            char[] charsToTrim = {' ', '\t', '\n', '\r'};
            return messageField.Substring(0, fieldLength).TrimEnd(charsToTrim);
        }
        public static DateTime GetDateFromSAPString(string dateStr)
        {
            if (string.IsNullOrEmpty(dateStr))
                return DateTime.MinValue;

            // Formatted like 2015-08-25
            int year = Convert.ToInt32(dateStr.Substring(0, 4));
            int month = Convert.ToInt32(dateStr.Substring(5, 2));
            int day = Convert.ToInt32(dateStr.Substring(8, 2));
            return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
        }
        // converts SAP date-time-string in local time to DateTime in UTC
        public static DateTime GetDateTimeFromSAPString(string dateStr)
        {
            if (string.IsNullOrEmpty(dateStr))
                return DateTime.MinValue;

            int year = Convert.ToInt32(dateStr.Substring(0, 4));
            int month = Convert.ToInt32(dateStr.Substring(5, 2));
            int day = Convert.ToInt32(dateStr.Substring(8, 2));
            int hour = 0;
            int min = 0;
            int sec = 0;
            int daysToAdd = 0;
            if (dateStr.Length > 18 && dateStr.Substring(10, 1) == "T")
            {
                hour = Convert.ToInt32(dateStr.Substring(11, 2));
                while (hour>23)
                {
                    hour = hour - 24;
                    daysToAdd = 1;
                }
                min = Convert.ToInt32(dateStr.Substring(14, 2));
                sec = Convert.ToInt32(dateStr.Substring(17, 2));
            }
            DateTime localTime = new DateTime(year, month, day, hour, min, sec, DateTimeKind.Local);
            if(daysToAdd!=0)
            {
                TimeSpan timeSpan = new TimeSpan(daysToAdd, 0, 0, 0);
                localTime.Add(timeSpan);
            }
            return TimeZoneInfo.ConvertTimeToUtc(localTime);
        }

        /// <summary>
        /// Return tiemsheet entry split to different parts. First get all detail parts starting from end
        /// and counting using duration and what remains is the main part.
        /// </summary>
        /// <param name="timesheetEntry"></param>
        private static TimesheetEntryWithDetails GetTimesheetEntryWithDetails(
            BsonDocument timesheetEntry,
            MongoDatabase database)
        {
            if (!timesheetEntry.Contains("starttimestamp") || !timesheetEntry.Contains("endtimestamp"))
                throw new HandlerException("Start or end time missing in timesheet entry.");

            MongoCollection<BsonDocument> usersCollection = database.GetCollection("user");
            BsonDocument worker = usersCollection.FindOne(Query.EQ(DBQuery.Id, timesheetEntry["user"][0]));

            MongoCollection<BsonDocument> projectsCollection = database.GetCollection("project");
            BsonDocument project = projectsCollection.FindOne(Query.EQ(DBQuery.Id, (ObjectId)timesheetEntry["project"][0]));

            MongoCollection<BsonDocument> claContractsCollection = database.GetCollection("clacontract");
            BsonDocument claContract = claContractsCollection.FindOne(Query.EQ(DBQuery.Id, (ObjectId)worker["clacontract"][0]));

            MongoCollection<BsonDocument> profitCenterCollection = database.GetCollection("profitcenter");
            BsonDocument profitCenter = profitCenterCollection.FindOne(Query.EQ(DBQuery.Id, (ObjectId)worker["profitcenter"][0]));

            if (project == null)
                throw new HandlerException("Project not found for timesheet entry.");

            if (worker == null)
                throw new HandlerException("Worker not found for timesheet entry.");

            if (claContract == null)
                throw new HandlerException("CLA contract not found for timesheet entry worker.");

            if (profitCenter == null)
                throw new HandlerException("Profit center not found for timesheet entry worker.");

            if (!project.Contains("identifier"))
                throw new HandlerException("Project is missing an identifier.");

            if (!worker.Contains("identifier"))
                throw new HandlerException("Worker is missing an identifier.");

            var timesheetEntryWithDetails = new TimesheetEntryWithDetails();

            timesheetEntryWithDetails.WorkerCategory = IntegrationHelpers.GetCategoryForWorker(claContract, profitCenter);
            timesheetEntryWithDetails.ProjectId = (string)project["identifier"];
            timesheetEntryWithDetails.Note = (string)timesheetEntry.GetValue("note", "");
            timesheetEntryWithDetails.WorkerId = (string)worker["identifier"];
            timesheetEntryWithDetails.WorkerProfitCenter = (string)profitCenter["identifier"];

            MongoCollection<BsonDocument> timsheetEntryDetailsCollection = database.GetCollection("timesheetentry");
            MongoCursor<BsonDocument> cursor = timsheetEntryDetailsCollection.Find(Query.EQ("parent", timesheetEntry[DBQuery.Id]));

            timesheetEntryWithDetails.TimesheetEntry = timesheetEntry;

            foreach (BsonDocument timesheetEntryDetail in cursor)
                AddDetailAndPayTypeToTimesheetEntryWithDetails(timesheetEntryWithDetails, timesheetEntryDetail, database);

            return timesheetEntryWithDetails;
        }

        private static string GetCategoryForWorker(BsonDocument claContract, BsonDocument profitCenter)
        {
            string category = (string)profitCenter.GetValue("categoryid", ProfitCenterDefaultCategory);

            string claContractId = (string)claContract["identifier"];

            string claContractCategory = "";

            if (claContractId == "FI01")
            {
                claContractCategory = "-00";
            }
            else if (claContractId == "FI02")
            {
                claContractCategory = "-00";
            }
            else if (claContractId == "FI03")
            {
                claContractCategory = "-00";
            }
            else if (claContractId == "FI04")
            {
                claContractCategory = "-01";
            }
            else if (claContractId == "FI05")
            {
                claContractCategory = "-01";
            }
            else if (claContractId == "FI06")
            {
                claContractCategory = "-01";
            }
            else if (claContractId == "EXT")
            {
                claContractCategory = "-00";
            }
            else
            {
                claContractCategory = "-00";
            }

            category += claContractCategory;

            return category;
        }

        /// <summary>
        /// Split timesheet entry so that it doesn't span to multiple days
        /// </summary>
        private static List<TimesheetEntryFragment> SplitTimesheetEntryWithDetailsToFragments(TimesheetEntryWithDetails entryWithDetails, ILogger logger)
        {
            BsonDocument timesheetEntry = entryWithDetails.TimesheetEntry;

            DateTime startTime = ((DateTime)timesheetEntry["starttimestamp"]).ToLocalTime();
            DateTime currentEndTimestamp = ((DateTime)timesheetEntry["endtimestamp"]).ToLocalTime();
            DateTime currentStartTimestamp = currentEndTimestamp;

            var timesheetEntryFragments = new List<TimesheetEntryFragment>();

            // Set to true is some details (such as contract pay) mean that regular hours shouldn't be exported.
            bool regularHoursInvalidated = false;

            // Handle detail types that don't count towards regular hours
            foreach (Tuple<BsonDocument, BsonDocument> detailAndPayType in entryWithDetails.EntryDetailsAndPayTypes)
            {
                if ((bool)detailAndPayType.Item2.GetValue("invalidatesregularhours", false))
                    regularHoursInvalidated = true;

                if ((bool)detailAndPayType.Item2.GetValue("countsasregularhours", false))
                    continue;

                var fragment = new TimesheetEntryFragment();

                fragment.Detail = detailAndPayType.Item1;
                fragment.PayType = detailAndPayType.Item2;
                fragment.ProjectCategoryBase = entryWithDetails.WorkerCategory;
                fragment.ProjectId = entryWithDetails.ProjectId;
                fragment.WorkerId = entryWithDetails.WorkerId;
                fragment.WorkerProfitCenter = entryWithDetails.WorkerProfitCenter;

                // Use timesheet entry detail's note if present and base entry's note if not
                fragment.Note = (string)detailAndPayType.Item1.GetValue("note", string.Empty);

                if (string.IsNullOrEmpty(fragment.Note))
                    fragment.Note = entryWithDetails.Note;

                if (!fragment.Detail.Contains("duration"))
                    continue;

                TimeSpan duration = TimeSpan.FromMilliseconds((int)fragment.Detail["duration"]);

                fragment.Start = startTime;
                fragment.End = fragment.Start + duration;

                timesheetEntryFragments.Add(fragment);
            }

            // Handle detail types that count towards regular hours
            foreach (Tuple<BsonDocument, BsonDocument> detailAndPayType in entryWithDetails.EntryDetailsAndPayTypes)
            {
                if (!(bool)detailAndPayType.Item2.GetValue("countsasregularhours", false))
                    continue;

                var fragment = new TimesheetEntryFragment();

                fragment.Detail = detailAndPayType.Item1;
                fragment.PayType = detailAndPayType.Item2;
                fragment.ProjectCategoryBase = entryWithDetails.WorkerCategory;
                fragment.ProjectId = entryWithDetails.ProjectId;
                fragment.WorkerId = entryWithDetails.WorkerId;
                fragment.WorkerProfitCenter = entryWithDetails.WorkerProfitCenter;

                // Use timesheet entry detail's note if present and base entry's note if not
                fragment.Note = (string)detailAndPayType.Item1.GetValue("note", string.Empty);

                if (string.IsNullOrEmpty(fragment.Note))
                    fragment.Note = entryWithDetails.Note;

                if (!fragment.Detail.Contains("duration"))
                    continue;

                TimeSpan duration = TimeSpan.FromMilliseconds((int)fragment.Detail["duration"]);

                fragment.End = currentEndTimestamp;
                currentStartTimestamp = currentEndTimestamp - duration;

                bool breakAfter = false;
                if (currentStartTimestamp < startTime)
                {
                    logger.LogWarning("Timesheet entry details are longer than entry itself. Skipping extra parts.", timesheetEntry[DBQuery.Id]);
                    currentStartTimestamp = startTime;
                    breakAfter = true;
                }

                fragment.Start = currentStartTimestamp;

                timesheetEntryFragments.Add(fragment);

                // Continue processing backwards from current start timestamp
                currentEndTimestamp = currentStartTimestamp;

                if (breakAfter)
                    break;
            }

            // Add the "root" version timesheet entry not based on any detail (overtime etc.)
            if (currentStartTimestamp > startTime && !regularHoursInvalidated)
            {
                var fragment = new TimesheetEntryFragment();

                fragment.IsRootEntry = true;
                fragment.IsTravelTime = (bool)timesheetEntry.GetValue("istraveltime", false);
                fragment.ProjectCategoryBase = entryWithDetails.WorkerCategory;
                fragment.ProjectId = entryWithDetails.ProjectId;
                fragment.WorkerId = entryWithDetails.WorkerId;
                fragment.Note = entryWithDetails.Note;
                fragment.Start = startTime;
                fragment.End = currentStartTimestamp;
                fragment.WorkerProfitCenter = entryWithDetails.WorkerProfitCenter;

                timesheetEntryFragments.Add(fragment);
            }

            return timesheetEntryFragments;
        }

        public static void RemoveTooShortFragments(List<TimesheetEntryFragment> fragments, ILogger logger, int minTimeFragmentSize)
        {
            for (int i = fragments.Count - 1; i > 0; i--)
            {
                TimesheetEntryFragment fragment = fragments[i];

                TimeSpan duration = fragment.End - fragment.Start;

                if (duration.TotalSeconds < minTimeFragmentSize)
                {
                    logger.LogInfo("One of the time fragments is too short and will not be exported.", fragment.Detail[DBQuery.Id], "Duration: " + duration.Seconds, "Minimum time in seconds: " + minTimeFragmentSize);
                    fragments.Remove(fragment);
                }
            }
        }

        public static void RemoveUnexportedFragmentTypes(List<TimesheetEntryFragment> fragments, ILogger logger)
        {
            for (int i = fragments.Count - 1; i >= 0; i--)
            {
                TimesheetEntryFragment fragment = fragments[i];


                if (fragment.PayType != null && fragment.PayType.GetValue("exporttoax") != true)
                {
                    logger.LogDebug("One of the time fragments is of a type not exported to AX.", fragment.PayType.GetValue("name", ""));
                    fragments.Remove(fragment);
                }
            }
        }

        /// <summary>
        /// Split one timesheet fragment so that no fragment crosses day boundary.
        /// </summary>
        /// <param name="timesheetFragments"></param>
        /// <returns></returns>
        public static List<TimesheetEntryFragment> SplitTimesheetFragmentsToDays(List<TimesheetEntryFragment> timesheetFragments)
        {
            var newFragments = new List<TimesheetEntryFragment>();

            foreach (TimesheetEntryFragment fragment in timesheetFragments)
                newFragments.AddRange(RecursivelySplitSingleTimesheetFragmentToDays(fragment));

            return newFragments;
        }

        /// <summary>
        /// Add detail and pay type to TimesheetEntryWithDetails object.
        /// </summary>
        /// <param name="timesheetEntryWithDetails"></param>
        /// <param name="timesheetEntryDetail"></param>
        private static void AddDetailAndPayTypeToTimesheetEntryWithDetails(
            TimesheetEntryWithDetails timesheetEntryWithDetails,
            BsonDocument timesheetEntryDetail,
            MongoDatabase database)
        {
            BsonDocument timesheetEntryDetailType = GetTimesheetEntryDetailPayType(timesheetEntryDetail, database);

            // Order timesheet entry details by priority with a simple bubble sort. Times are 
            // sorted with order so that we get overtime 100% before overtime 50% for example
            int newOrderNumber = 0;
            if (timesheetEntryDetailType.Contains("order"))
                newOrderNumber = (int)timesheetEntryDetailType["order"];

            int index = 0;
            foreach (Tuple<BsonDocument, BsonDocument> documentAndPayTypes in timesheetEntryWithDetails.EntryDetailsAndPayTypes)
            {
                int currentOrderNumber = 0;
                if (documentAndPayTypes.Item2.Contains("order"))
                    currentOrderNumber = (int)documentAndPayTypes.Item2["order"];

                if (newOrderNumber > currentOrderNumber)
                    break;

                index++;
            }

            timesheetEntryWithDetails.EntryDetailsAndPayTypes.Insert(
                index,
                new Tuple<BsonDocument, BsonDocument>(timesheetEntryDetail, timesheetEntryDetailType));
        }

        private static BsonDocument GetTimesheetEntryDetailPayType(BsonDocument timesheetEntryDetail, MongoDatabase database)
        {
            if (!timesheetEntryDetail.Contains("timesheetentrydetailpaytype"))
                throw new HandlerException("Timesheet entry detail pay type missing in timesheet entry detail.");

            MongoCollection<BsonDocument> payTypesCollection = database.GetCollection("timesheetentrydetailpaytype");
            return payTypesCollection.FindOne(Query.EQ(DBQuery.Id, timesheetEntryDetail["timesheetentrydetailpaytype"][0]));
        }

        private static List<TimesheetEntryFragment> RecursivelySplitSingleTimesheetFragmentToDays(TimesheetEntryFragment fragment, int recursionLevel = 0)
        {
            const int RecursionMaxLevel = 30;

            if (recursionLevel > RecursionMaxLevel)
                throw new HandlerException("Cannot split timesheet fragment. It's too long.");

            var newFragments = new List<TimesheetEntryFragment>();

            DateTime localStart = fragment.Start.ToLocalTime();
            DateTime localEnd = fragment.End.ToLocalTime();

            if (localStart.Year != localEnd.Year || localStart.Month != localEnd.Month || localStart.Date != localEnd.Date)
            {
                TimesheetEntryFragment newFragment = (TimesheetEntryFragment)fragment.Clone();

                fragment.End = new DateTime(fragment.Start.Year, fragment.Start.Month, fragment.Start.Day, 23, 59, 59);
                newFragments.Add(fragment);

                newFragment.Start = fragment.End + TimeSpan.FromSeconds(1);

                if (newFragment.Start < newFragment.End)
                    newFragments.AddRange(RecursivelySplitSingleTimesheetFragmentToDays(newFragment, recursionLevel + 1).ToArray());
            }
            else
            {
                newFragments.Add(fragment);
            }

            return newFragments;
        }
    }
}

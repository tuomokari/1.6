using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using MongoDB.Bson;
using MongoDB.Shared;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using SystemsGarden.mc2.Common;
using SystemsGarden.mc2.RemoteConnector.Handlers.TroIntegrationCommon;

namespace SystemsGarden.mc2.RemoteConnector.Handlers.AXIntegrationHandlerServer
{
    sealed class TroToAXExport
    {
        #region Members

        private ILogger logger;
        private string filePathExport;
        private string filePathCopy;
        private MongoDatabase database;

        private XmlDocument exportHoursDocumentBase;
        private XmlDocument exportExpensesDocumentBase;
        private XmlDocument exportItemConsumptionDocumentBase;
        private HashSet<ObjectId> failedExports = new HashSet<ObjectId>();
		private HashSet<ObjectId> succeededExports = new HashSet<ObjectId>();

		private const string TransactionInfoId = "5267A6B5-2C16-402E-BE3F-2DD3B415FE6B";
        private const string ArticleExpensesCategory = "IW_EXPENSE";
        private const string ArticleSuppliesCategory = "SUPPLIES";

        private const string FilePrefixHourData = "HourData";
        private const string FilePrefixItemData = "ItemData";
        private const string FilePrefixExpensesData = "Expenses";        
            
        private int BatchId;
        bool batchStarted;
        DateTime batchStartTimestamp;
        private const string BatchStart = "BatchID";

        private DataTree config;

        private int MaxExportFailureCount { get; set; }
        private int MinTimeFragmentSize { get; set; }

        // Monitor data

        internal int WorkerTimesheetEntryFragmentsExported = 0;
        internal int WorkerTimesheetEntryFilesCreated = 0;
        internal int WorkerTimesheetEntryExportsFailed = 0;
        internal int AssetTimesheetEntriesExported = 0;
        internal int AssetTimesheetEntryFilesCreated = 0;
        internal int AssetTimesheetEntryExportsFailed = 0;
        internal int ItemsExported = 0;
        internal int ItemsFilesCreated = 0;
        internal int ItemExportsFailed = 0;
        internal int ExpensesExported = 0;
        internal int ExpensesFilesCreated = 0;
        internal int ExpenseExportsFailed = 0;

        #endregion

        #region Constructors

        public TroToAXExport(
            ILogger logger,
            string filePathExport,
            string filePathCopy,
            MongoDatabase database,
            DataTree config)
        {
            if (!filePathExport.EndsWith(Path.DirectorySeparatorChar.ToString()))
                filePathExport += Path.DirectorySeparatorChar;

            if (!filePathCopy.EndsWith(Path.DirectorySeparatorChar.ToString()))
                filePathCopy += Path.DirectorySeparatorChar;

            this.logger = logger.CreateChildLogger("TroToAXExport");
            this.filePathExport = filePathExport;
            this.filePathCopy = filePathCopy;

            this.database = database;
            this.config = config;

            if (!this.filePathExport.EndsWith(Convert.ToString(Path.DirectorySeparatorChar)))
                this.filePathExport += Path.DirectorySeparatorChar;

            Init();
        }

        #endregion

        #region Main export function

        public void ExportDocuments()
        {
            StartExport();

            ExportWorkerHours();

            ExportAssetHours();

            ExportItems();

            ExportExpenses();
        }

        #endregion

        #region Batch and transaction handling

        private void StartExport()
        {
            logger.LogTrace("Looking for documents to export to AX.");
            batchStarted = false;
            failedExports.Clear();
			succeededExports.Clear();
        }

        private void StartBatch(XmlDocument exportDocument, string parentTag)
        {
            if (batchStarted)
                return;

            logger.LogDebug("Starting new export batch.");

            batchStarted = true;
            batchStartTimestamp = MC2DateTimeValue.Now().ToUniversalTime();

            if (!database.CollectionExists("axintegration"))
                database.CreateCollection("axintegration");

            MongoCollection<BsonDocument> axIntegrationCollection = database.GetCollection("axintegration");

            BsonDocument transactionInfoDocument = axIntegrationCollection.FindOne(Query.EQ("identifier", TransactionInfoId));

            if (transactionInfoDocument == null)
            {
                transactionInfoDocument = new BsonDocument();
                transactionInfoDocument["identifier"] = TransactionInfoId;
            }

            if (!transactionInfoDocument.Contains("batchid"))
                transactionInfoDocument.Set("batchid", 0);

            // Note that AX export is single threaded and not thread safe
            BatchId = (int)transactionInfoDocument["batchid"] + 1;

            transactionInfoDocument.Set("batchid", BatchId);

            axIntegrationCollection.Save(transactionInfoDocument, WriteConcern.Acknowledged);

            XmlElement batchElement = exportDocument.CreateElement("EFIOriginalBatchID");
            batchElement.InnerText = BatchId.ToString();


            XmlNode parentNode = exportDocument.GetElementsByTagName(parentTag)[0];
            if (parentNode.ChildNodes.Count == 0)
                parentNode.AppendChild(batchElement);
            else
                parentNode.InsertBefore(batchElement, parentNode.FirstChild);
        }

        private void EndBatch()
        {
            batchStarted = false;
        }

        private string GetNextTransactionId()
        {
            const int TransactionIdMaxLength = 20;

            byte[] guidBytes = Guid.NewGuid().ToByteArray();
            string guidBase64Str = Convert.ToBase64String(guidBytes);

            // We lose 2 characters here in case of 20 size transaction id. Uniqueness should still
            // be good enough.
            return guidBase64Str.Substring(0, TransactionIdMaxLength);
        }

        #endregion

        #region Export worker hours

        private void ExportWorkerHours()
        {
            try
            {
                XmlDocument xmlDocument = (XmlDocument)exportHoursDocumentBase.Clone();

                MongoCursor<BsonDocument> cursor = GetUnexportedWorkerHours();

                if (cursor.Count() > 0)
                {
                    logger.LogInfo("Exporting found unexported timesheet entries", cursor.Count());
                    StartBatch(xmlDocument, "EfiAifBuffProjJournalTable");
                }
                else
                {
                    logger.LogFineTrace("No unexported timesheet entries found.");
                    return;
                }

                List<TimesheetEntryFragment> timesheetEntryFragments = IntegrationHelpers.GetTimesheetFragments(cursor, MinTimeFragmentSize, database, logger, failedExports, succeededExports);

                IntegrationHelpers.RemoveUnexportedFragmentTypes(timesheetEntryFragments, logger);

                logger.LogInfo(cursor.Count() + " timesheet entries produced " + timesheetEntryFragments.Count + " fragments.");

                int exportedFragments = ExportTimesheetFragmentsToXml(timesheetEntryFragments, xmlDocument);
                if (exportedFragments > 0)
                {
                    logger.LogInfo("Saving exported fragments to disk.", exportedFragments);
                    WriteExportedDataToDisk(xmlDocument, FilePrefixHourData);

                    WorkerTimesheetEntryFragmentsExported += exportedFragments;
                    WorkerTimesheetEntryFilesCreated++;
                }
                else
                {
                    logger.LogInfo("No valid timesheet entry fragments found to export. Not saving XML document.");
                }

                MarkEntriesAsExported(cursor, "timesheetentry");
            }
            catch(Exception ex)
            {
                logger.LogError("Exporting hours failed.", ex);
                WorkerTimesheetEntryExportsFailed++;
            }
            finally
            {
                EndBatch();
            }
        }

        private MongoCursor<BsonDocument> GetUnexportedWorkerHours()
        {
            MongoCollection<BsonDocument> collection = database.GetCollection("timesheetentry");

            MongoCursor<BsonDocument> cursor = collection.Find(Query.And(
                    Query.NE("exported_ax", true),
                    Query.NotExists("parent"),
                    Query.Exists("user"),
                    Query.EQ("approvedbymanager", true),
                    Query.Or(
                        Query.NotExists("exportfailurecount_ax"),
                        Query.LT("exportfailurecount_ax", MaxExportFailureCount
                    ))
                ));

            return cursor;
        }

        private void MarkEntriesAsExported(IEnumerable<BsonDocument> cursor, string collectionName)
        {
            MongoCollection collection = database.GetCollection(collectionName);

            DateTime now = MC2DateTimeValue.Now().ToUniversalTime();

            logger.LogInfo("Marking " + cursor.Count() + " items as completed in collection " + collectionName);

            foreach (BsonDocument document in cursor)
            {
                if (failedExports.Contains((ObjectId)document[DBQuery.Id]))
                {
                    logger.LogInfo("Marking item as failed because it was found in failed exports list");
                    document["exportfailurecount_ax"] = (int)document.GetValue("exportfailurecount_ax", 0) + 1;
                    WorkerTimesheetEntryExportsFailed++;
                }
				else if (!succeededExports.Contains((ObjectId)document[DBQuery.Id]))
				{
					logger.LogInfo("Item was not found in succeeded list. It's possible it was accepted during the accept procedure and will be exported during the next export.", document[DBQuery.Id].ToString());
					continue;
				}
				else
				{
                    document["exported_ax"] = true;
                    document["exporttimestamp_ax"] = now;
                }

                collection.Save(document, WriteConcern.Acknowledged);
            }
        }

        private void WriteExportedDataToDisk(XmlDocument document, string filePrefix)
        {
            string fileNameExport = filePathExport +  filePrefix + "_" + string.Format("{0:00000000}", BatchId) + ".xml";
            string fileNameCopy = filePathCopy + filePrefix + "_" + string.Format("{0:00000000}", BatchId) + ".xml";

            DirectoryInfo di = new DirectoryInfo(filePathExport);
            if (!di.Exists)
                di.Create();
            
            logger.LogInfo("Saving export hour file", fileNameExport);

            string docStr;

            using (var stringWriter = new StringWriter())
            {
                var writerSettings = new XmlWriterSettings();
                writerSettings.Indent = true;
                writerSettings.NamespaceHandling = NamespaceHandling.Default;
                writerSettings.Encoding = Encoding.UTF8;
                writerSettings.NewLineHandling = NewLineHandling.Entitize;

                using (var xmlTextWriter = XmlWriter.Create(stringWriter, writerSettings))
                {
                    document.WriteTo(xmlTextWriter);
                }

                docStr = stringWriter.GetStringBuilder().ToString();
                docStr = docStr.Replace(" xmlns=\"\"", "");
            }

            File.WriteAllText(fileNameExport, docStr);
            File.WriteAllText(fileNameCopy, docStr);
        }

        private int ExportTimesheetFragmentsToXml(List<TimesheetEntryFragment> timesheetEntryFragments, XmlDocument xmlDocument)
        {
            XmlNode exportParentNode = xmlDocument.GetElementsByTagName("EfiAifBuffProjJournalTable")[0];

            int fragmentsExported = 0;

            foreach(TimesheetEntryFragment fragment in timesheetEntryFragments)
            {
                XmlElement hourEntry = xmlDocument.CreateElement("EfiAifBuffProjJournalTrans");
                hourEntry.SetAttribute("class", "entity");

                XmlElement categoryId = xmlDocument.CreateElement("CategoryId");
                XmlElement transactionId = xmlDocument.CreateElement("EFIOriginalTransactionID");
                XmlElement fromTime = xmlDocument.CreateElement("FromTime");
                XmlElement projId = xmlDocument.CreateElement("ProjId");
                XmlElement projTransDate = xmlDocument.CreateElement("ProjTransDate");  
                XmlElement toTime = xmlDocument.CreateElement("ToTime");
                XmlElement txt = xmlDocument.CreateElement("Txt");
                XmlElement worker = xmlDocument.CreateElement("Worker");

                categoryId.InnerText = GetCategoryIdFromTimesheetFragment(fragment);
                transactionId.InnerText = GetNextTransactionId().ToString();
                fromTime.InnerText = string.Format("{0:00}:{1:00}:00", fragment.Start.Hour, fragment.Start.Minute);
                projId.InnerText = fragment.ProjectId;
                projTransDate.InnerText = string.Format("{0:0000}-{1:00}-{2:00}", fragment.Start.Year, fragment.Start.Month, fragment.Start.Day);

                // End of day is a special case where we provide data with one second accuracy. AX will read this as 24:00.
                if (fragment.End.Hour == 23 && fragment.End.Minute == 59 && fragment.End.Second == 59)
                    toTime.InnerText = string.Format("23:59:59", fragment.End.Hour, fragment.End.Minute);
                else
                    toTime.InnerText = string.Format("{0:00}:{1:00}:00", fragment.End.Hour, fragment.End.Minute);
                txt.InnerText = fragment.Note;
                worker.InnerText = fragment.WorkerId;

                hourEntry.AppendChild(categoryId);
                hourEntry.AppendChild(transactionId);
                hourEntry.AppendChild(fromTime);
                hourEntry.AppendChild(projId);
                hourEntry.AppendChild(projTransDate);
                hourEntry.AppendChild(toTime);
                hourEntry.AppendChild(txt);
                hourEntry.AppendChild(worker);

                exportParentNode.AppendChild(hourEntry);

                fragmentsExported++;
            }

            return fragmentsExported;
        }

        private int ExportAssetTimesheetFragmentsToXml(List<TimesheetEntryFragment> timesheetEntryFragments, XmlDocument xmlDocument)
        {
            XmlNode exportParentNode = xmlDocument.GetElementsByTagName("EfiAifBuffProjJournalTable")[0];

            int fragmentsExported = 0;

            foreach (TimesheetEntryFragment fragment in timesheetEntryFragments)
            {
                XmlElement hourEntry = xmlDocument.CreateElement("EfiAifBuffProjJournalTrans");
                hourEntry.SetAttribute("class", "entity");

                XmlElement categoryId = xmlDocument.CreateElement("CategoryId");
                XmlElement transactionId = xmlDocument.CreateElement("EFIOriginalTransactionID");
                XmlElement fromTime = xmlDocument.CreateElement("FromTime");
                XmlElement projId = xmlDocument.CreateElement("ProjId");
                XmlElement projTransDate = xmlDocument.CreateElement("ProjTransDate");
                XmlElement toTime = xmlDocument.CreateElement("ToTime");
                XmlElement txt = xmlDocument.CreateElement("Txt");
                XmlElement worker = xmlDocument.CreateElement("Worker"); // Contains asset's id in asset export

                categoryId.InnerText = fragment.ProjectCategoryBase;
                transactionId.InnerText = GetNextTransactionId().ToString();
                fromTime.InnerText = string.Format("{0:00}:{1:00}:00", fragment.Start.Hour, fragment.Start.Minute);
                projId.InnerText = fragment.ProjectId;
                projTransDate.InnerText = string.Format("{0:0000}-{1:00}-{2:00}", fragment.Start.Year, fragment.Start.Month, fragment.Start.Day);
                toTime.InnerText = string.Format("{0:00}:{1:00}:00", fragment.End.Hour, fragment.End.Minute);
                txt.InnerText = fragment.Note;
                worker.InnerText = fragment.WorkerId;

                hourEntry.AppendChild(categoryId);
                hourEntry.AppendChild(transactionId);
                hourEntry.AppendChild(fromTime);
                hourEntry.AppendChild(projId);
                hourEntry.AppendChild(projTransDate);
                hourEntry.AppendChild(toTime);
                hourEntry.AppendChild(txt);
                hourEntry.AppendChild(worker);

                exportParentNode.AppendChild(hourEntry);

                fragmentsExported++;
            }

            return fragmentsExported;
        }

        private string GetCategoryIdFromTimesheetFragment(TimesheetEntryFragment timesheetEntryFragment)
        {
            bool isOvertime50 = false;
            bool isOvertime100 = false;
            bool isOvertime150 = false;
            bool isOvertime200 = false;

            string category = timesheetEntryFragment.ProjectCategoryBase;

            // Category is defined explicitly and not built based on different data.
            bool directCategory = false;

            if (timesheetEntryFragment.PayType != null)
            {
                isOvertime50 =(bool)timesheetEntryFragment.PayType.GetValue("isovertime50", false);
                isOvertime100 =(bool)timesheetEntryFragment.PayType.GetValue("isovertime100", false);
                isOvertime150 =(bool)timesheetEntryFragment.PayType.GetValue("isovertime150", false);
                isOvertime200 =(bool)timesheetEntryFragment.PayType.GetValue("isovertime200", false);

                if (timesheetEntryFragment.PayType.Contains("projectcategory") && timesheetEntryFragment.PayType["projectcategory"] != string.Empty)
                {
                    MongoCollection<BsonDocument> projectCategoryCollection = database.GetCollection("projectcategory");
                    BsonDocument categoryElement = projectCategoryCollection.FindOne(Query.EQ(DBQuery.Id, timesheetEntryFragment.PayType["projectcategory"][0]));

                    if (categoryElement == null)
                        throw new Exception("Project category for timesheet entry detail does't exist. Category: " + timesheetEntryFragment.PayType["projectcategory"][0]);

                    category = (string)categoryElement["identifier"];
                    directCategory = true;
                }
            }

            if (!directCategory)
            {
                if (isOvertime50)
                    category += "-050";
                else if (isOvertime100)
                    category += "-100";
                else if (isOvertime150)
                    category += "-150";
                else if (isOvertime200)
                    category += "-200";
                else
                    category += "-000";

                if (timesheetEntryFragment.IsTravelTime)
                    category += "-01";
                else
                    category += "-00";
            }

            return category;
        }

        private void IncreaseExportFailureCount(BsonDocument document, string collectionName)
        {
            int exportFailureCount = (int)document.GetValue("exportfailurecount_ax", 0);

            exportFailureCount++;

            logger.LogInfo("Export failure count for this item is now " + exportFailureCount + " out of " + MaxExportFailureCount);

            document["exportfailurecount_ax"] = exportFailureCount;

            database.GetCollection(collectionName).Save(document);
            failedExports.Add((ObjectId)document[DBQuery.Id]);
        }

        #endregion

        #region Export asset hours

        private void ExportAssetHours()
        {
            try
            {
                XmlDocument xmlDocument = (XmlDocument)exportHoursDocumentBase.Clone();

                MongoCursor<BsonDocument> cursor = GetUnexportedAssetHours();

                if (cursor.Count() > 0)
                {
                    logger.LogInfo("Exporting found unexported asset timesheet entries", cursor.Count());
                    StartBatch(xmlDocument, "EfiAifBuffProjJournalTable");
                }
                else
                {
                    logger.LogFineTrace("No unexported timesheet entries found. (for assets)");
                    return;
                }

                List<TimesheetEntryFragment> timesheetEntryFragments = GetTimesheetFragmentsForAssets(cursor);

                logger.LogInfo(cursor.Count() + " timesheet entries produced " + timesheetEntryFragments.Count + " fragments. (for assets)");

                int exportedFragments = ExportAssetTimesheetFragmentsToXml(timesheetEntryFragments, xmlDocument);

                if (exportedFragments > 0)
                {
                    logger.LogInfo("Saving exported fragments to disk.", exportedFragments);
                    WriteExportedDataToDisk(xmlDocument, FilePrefixHourData);
                    AssetTimesheetEntriesExported += exportedFragments;
                    AssetTimesheetEntryFilesCreated++;
                }
                else
                {
                    logger.LogInfo("No valid timesheet entry fragments found to export. Not saving XML document.");
                }

                MarkEntriesAsExported(cursor, "assetentry");
            }
            catch (Exception ex)
            {
                logger.LogError("Exporting hours failed (for assets).", ex);
                AssetTimesheetEntryExportsFailed++;
            }
            finally
            {
                EndBatch();
            }
        }

        private MongoCursor<BsonDocument> GetUnexportedAssetHours()
        {
            MongoCollection<BsonDocument> collection = database.GetCollection("assetentry");

            MongoCursor<BsonDocument> cursor = collection.Find(Query.And(
                    Query.NE("exported_ax", true),
                    Query.Exists("endtimestamp"),
                    Query.Exists("asset"),
                    Query.EQ("approvedbymanager", true),
                    Query.Or(
                        Query.NotExists("exportfailurecount_ax"),
                        Query.LT("exportfailurecount_ax", MaxExportFailureCount
                    ))
                ));

            return cursor;
        }

        private List<TimesheetEntryFragment> GetTimesheetFragmentsForAssets(MongoCursor<BsonDocument> timesheetEntryCursor)
        {
            var resultFragments = new List<TimesheetEntryFragment>(); ;

            foreach (BsonDocument timesheetEntry in timesheetEntryCursor)
            {
                try
                {
                    TimesheetEntryFragment entryFragment = GetTimesheetFragmentForAsset(timesheetEntry);

                    List<TimesheetEntryFragment> entryFragments = IntegrationHelpers.SplitTimesheetFragmentsToDays(new List<TimesheetEntryFragment> { entryFragment });

                    IntegrationHelpers.RemoveTooShortFragments(entryFragments, logger, MinTimeFragmentSize);

                    resultFragments.AddRange(entryFragments.ToArray());

					succeededExports.Add((ObjectId)timesheetEntry[DBQuery.Id]);
				}
				catch (Exception ex)
                {
                    logger.LogError("Failed to handle timesheet entry (for assets). Skipping this entry", ex, timesheetEntry[DBQuery.Id]);
                    IncreaseExportFailureCount(timesheetEntry, "timesheetentry");
					failedExports.Add((ObjectId)timesheetEntry[DBQuery.Id]);
				}
            }

            return resultFragments;
        }

        /// <summary>
        /// Get timesheet fragment for asset. Fragment can be returned directly because we always have just one fragment
        /// and no extra details (overtime etc...).
        /// </summary>
        /// <param name="timesheetEntry"></param>
        /// <returns></returns>
        private TimesheetEntryFragment GetTimesheetFragmentForAsset(BsonDocument timesheetEntry)
        {
            if (!timesheetEntry.Contains("starttimestamp") || !timesheetEntry.Contains("endtimestamp"))
                throw new HandlerException("Start or end time missing in asset timesheet entry.");

            MongoCollection<BsonDocument> assetsCollection = database.GetCollection("asset");
            BsonDocument asset = assetsCollection.FindOne(Query.EQ(DBQuery.Id, timesheetEntry["asset"][0]));

            MongoCollection<BsonDocument> projectsCollection = database.GetCollection("project");
            IMongoQuery query = Query.EQ(DBQuery.Id, timesheetEntry["project"]);
            BsonDocument project = projectsCollection.FindOne(Query.EQ(DBQuery.Id, (ObjectId)timesheetEntry["project"][0]));

            if (project == null)
                throw new HandlerException("Project not found for asset timesheet entry.");

            if (asset == null)
                throw new HandlerException("Asset not found for timesheet entry.");

            if (!project.Contains("identifier"))
                throw new HandlerException("Project is missing an identifier.");

            if (!asset.Contains("identifier"))
                throw new HandlerException("Asset is missing an identifier.");

            if (!asset.Contains("projectcategory"))
                throw new HandlerException("Asset is missing category.");

            MongoCollection<BsonDocument> projectCategories = database.GetCollection("projectcategory");
            BsonDocument projectCategory = projectCategories.FindOne(Query.EQ(DBQuery.Id, asset["projectcategory"][0]));

            var timesheetEntryWithDetails = new TimesheetEntryWithDetails();
            var fragment = new TimesheetEntryFragment();

            fragment.Detail = timesheetEntry;
            fragment.IsRootEntry = true;
            fragment.ProjectCategoryBase = (string)projectCategory["identifier"];
            fragment.ProjectId = (string)project["identifier"];
            fragment.Start = ((DateTime)timesheetEntry["starttimestamp"]).ToLocalTime();
            fragment.End = ((DateTime)timesheetEntry["endtimestamp"]).ToLocalTime();
            fragment.WorkerId = (string)asset["identifier"];

            return fragment;
        }


        #endregion

        #region Export Items

        private void ExportItems()
        {
            try
            {
                XmlDocument xmlDocument = (XmlDocument)exportItemConsumptionDocumentBase.Clone();

                List<BsonDocument> articles = GetUnexportedArticleItems();

                if (articles.Count() > 0)
                {
                    logger.LogInfo("Exporting found unexported article entries", articles.Count());
                    StartBatch(xmlDocument, "InventJournalTable");
                }
                else
                {
                    logger.LogFineTrace("No unexported article entries found.");
                    return;
                }

                int exportedItems = ExportArticleEntriesToXml(articles, xmlDocument);

                if (exportedItems > 0)
                {
                    logger.LogInfo("Saving exported items to disk", exportedItems);
                    WriteExportedDataToDisk(xmlDocument, FilePrefixItemData);
                    ItemsExported += exportedItems;
                    ItemsFilesCreated++;
                }
                else
                {
                    logger.LogInfo("Exporting items resulted no items being written to disk.");
                }

                MarkEntriesAsExported(articles, "articleentry");
            }
            catch(Exception ex)
            {
                logger.LogError("Failed to export items.", ex);
                ItemExportsFailed++;
            }
            finally
            {
                EndBatch();
            }
        }

        private int ExportArticleEntriesToXml(List<BsonDocument> articlesToExport, XmlDocument xmlDocument)
        {
            XmlNode exportParentNode = xmlDocument.GetElementsByTagName("InventJournalTable")[0];

            int articlesExported = 0;

            foreach (BsonDocument articleEntry in articlesToExport)
            {
                try
                {
                    BsonDocument article = GetArticleForArticleEntry(articleEntry);

                    if (!article.Contains("identifier"))
                        throw new Exception("article is missing an identifier");

                    XmlElement itemEntry = xmlDocument.CreateElement("InventJournalTrans");
                    itemEntry.SetAttribute("class", "entity");

                    XmlElement itemId = xmlDocument.CreateElement("ItemId");
                    XmlElement projId = xmlDocument.CreateElement("ProjId");
                    XmlElement quantity = xmlDocument.CreateElement("Qty");
                    XmlElement transDate = xmlDocument.CreateElement("TransDate");
                    XmlElement transactionId = xmlDocument.CreateElement("EFIOriginalTransactionID");
                    XmlElement txt = xmlDocument.CreateElement("Txt");

                    itemId.InnerText = (string)article["identifier"];
                    projId.InnerText = GetProjectIdForArticleEntry(articleEntry);
                    quantity.InnerText = Convert.ToString(articleEntry.GetValue("amount", 0));

                    DateTime timeStamp = (DateTime)articleEntry.GetValue("timestamp", MC2DateTimeValue.Now());
                    transDate.InnerText = string.Format("{0:0000}-{1:00}-{2:00}", timeStamp.Year, timeStamp.Month, timeStamp.Day);
                    transactionId.InnerText = GetNextTransactionId().ToString();
                    txt.InnerText = (string)articleEntry.GetValue("note", string.Empty);

                    itemEntry.AppendChild(transactionId);
                    itemEntry.AppendChild(itemId);
                    itemEntry.AppendChild(projId);
                    itemEntry.AppendChild(quantity);
                    itemEntry.AppendChild(transDate);
                    
                    // Note: text not added as of yet

                    exportParentNode.AppendChild(itemEntry);

                    articlesExported++;

					succeededExports.Add((ObjectId)articleEntry[DBQuery.Id]);
				}
				catch (Exception ex)
                {
                    logger.LogError("Failed to handle article entry. Skipping this entry", ex, articleEntry[DBQuery.Id]);
                    IncreaseExportFailureCount(articleEntry, "articleentry");
                }                
            }

            return articlesExported;
        }

        private BsonDocument GetArticleForArticleEntry(BsonDocument articleEntry)
        {
            MongoCollection<BsonDocument> articlesCollection = database.GetCollection("article");

            if (!articleEntry.Contains("article"))
                throw new HandlerException("Article entry is missing article.");

            BsonDocument article = articlesCollection.FindOne(Query.EQ(DBQuery.Id, articleEntry["article"][0]));

            if (article == null || article.Contains("identifer"))
                throw new HandlerException("Article not found in article entry or article is missing an identifier.");

            return article;
        }

        private string GetProjectIdForArticleEntry(BsonDocument article)
        {
            MongoCollection<BsonDocument> projectsCollection = database.GetCollection("project");

            if (!article.Contains("project"))
                throw new HandlerException("Article is missing a project.");

            BsonDocument project = projectsCollection.FindOne(Query.EQ(DBQuery.Id, article["project"][0]));

            if (project == null || project.Contains("identifer"))
                throw new HandlerException("Article project not found or project is missing identifier.");

            return (string)project["identifier"];
        }

        private List<BsonDocument> GetUnexportedArticleItems()
        {
            MongoCollection<BsonDocument> collection = database.GetCollection("articleentry");

            MongoCursor<BsonDocument> cursor = collection.Find(Query.And(
                    Query.NE("exported_ax", true),
                    Query.EQ("approvedbymanager", true),
                    Query.Or(
                        Query.NotExists("exportfailurecount_ax"),
                        Query.LTE("exportfailurecount_ax", MaxExportFailureCount
                    ))
                ));

            var results = new List<BsonDocument>();

            // Todo: Conditions for export may need to be added here.
            foreach(BsonDocument article in cursor)
                results.Add(article);

            return results;
        }

        #endregion

        #region Export expenses

        private void ExportExpenses()
        {
            try
            {
                XmlDocument xmlDocument = (XmlDocument)exportExpensesDocumentBase.Clone();

                List<BsonDocument> expenses = GetUnexportedExpenses();

                if (expenses.Count() > 0)
                {
                    logger.LogInfo("Exporting found unexported expenses", expenses.Count());
                    StartBatch(xmlDocument, "JournalTable");
                }
                else
                {
                    logger.LogFineTrace("No unexported expenses found.");
                    return;
                }

                int exportedItems = ExportExpenseEntriesToXml(expenses, xmlDocument);

                if (exportedItems > 0)
                {
                    logger.LogInfo("Saving exported items to disk", exportedItems);
                    WriteExportedDataToDisk(xmlDocument, FilePrefixExpensesData);
                    ExpensesExported += exportedItems;
                    ExpensesFilesCreated++;
                }
                else
                {
                    logger.LogInfo("Exporting items resulted no items being written to disk.");
                }

                MarkEntriesAsExported(expenses, "dayentry");
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to export items.", ex);
                ExpenseExportsFailed++;
            }
            finally
            {
                EndBatch();
            }
        }

        private List<BsonDocument> GetUnexportedExpenses()
        {
            MongoCollection<BsonDocument> collection = database.GetCollection("dayentry");

            MongoCursor<BsonDocument> cursor = collection.Find(Query.And(
                    Query.NE("exported_ax", true),
                    Query.EQ("approvedbymanager", true),
                    Query.Or(
                        Query.NotExists("exportfailurecount_ax"),
                        Query.LTE("exportfailurecount_ax", MaxExportFailureCount
                    ))
                ));

            var results = new List<BsonDocument>();

            // Todo: Conditions for export may need to be added here.
            foreach (BsonDocument dayentry in cursor)
                results.Add(dayentry);

            return results;
        }

        private int ExportExpenseEntriesToXml(List<BsonDocument> expensesToExport, XmlDocument xmlDocument)
        {
            XmlNode exportParentNode = xmlDocument.GetElementsByTagName("JournalTable")[0];

            int expensesExported = 0;

            foreach (BsonDocument expenseDocument in expensesToExport)
            {
                try
                {
                    string categoryIdString = GetProjectCategoryForExpense(expenseDocument);

                    // No categoryId means the expense is not exported to AX.
                    if (categoryIdString == null)
                        continue;

                    if (!expenseDocument.Contains("user"))
                        throw new Exception("User is missing from expense");

                    MongoCollection<BsonDocument> usersCollection = database.GetCollection("user");
                        BsonDocument workerDocument = usersCollection.FindOne(Query.EQ(DBQuery.Id, expenseDocument["user"][0]));

                        if (!workerDocument.Contains("identifier"))
                            throw new Exception("Worker document is missing an identifier");

                    XmlElement expenseElement = xmlDocument.CreateElement("JournalTrans");
                    expenseElement.SetAttribute("class", "entity");

                    XmlElement expenseInnerElement = xmlDocument.CreateElement("ProjTrans");
                    expenseInnerElement.SetAttribute("class", "entity");

                    XmlElement categoryId = xmlDocument.CreateElement("CategoryId");
                    XmlElement projId = xmlDocument.CreateElement("ProjId");
                    XmlElement quantity = xmlDocument.CreateElement("Qty");
                    XmlElement worker = xmlDocument.CreateElement("Worker");
                    XmlElement transDate = xmlDocument.CreateElement("TransDate");
                    XmlElement transactionId = xmlDocument.CreateElement("EFIOriginalTransactionID");

                    transactionId.InnerText = GetNextTransactionId().ToString();
                    categoryId.InnerText = categoryIdString;
                    projId.InnerText = GetProjectIdForArticleEntry(expenseDocument);
                    quantity.InnerText = Convert.ToString(expenseDocument.GetValue("amount", 0));
                    worker.InnerText = (string)workerDocument["identifier"];

                    DateTime timeStamp = (DateTime)expenseDocument.GetValue("date", MC2DateTimeValue.Now());
                    transDate.InnerText = string.Format("{0:0000}-{1:00}-{2:00}", timeStamp.Year, timeStamp.Month, timeStamp.Day);

                    expenseElement.AppendChild(transactionId);
                    expenseElement.AppendChild(transDate);
                    expenseElement.AppendChild(expenseInnerElement);
                    expenseInnerElement.AppendChild(categoryId);
                    expenseInnerElement.AppendChild(projId);
                    expenseInnerElement.AppendChild(quantity);
                    expenseInnerElement.AppendChild(worker);

                    exportParentNode.AppendChild(expenseElement);
                    expensesExported++;
					succeededExports.Add((ObjectId)expenseDocument[DBQuery.Id]);
				}
				catch (Exception ex)
                {
                    logger.LogError("Failed to handle expense entry. Skipping this entry", ex, expenseDocument[DBQuery.Id]);
                    IncreaseExportFailureCount(expenseDocument, "dayentry");
                }
            }

            return expensesExported;
        }

        private string GetProjectCategoryForExpense(BsonDocument expenseEntry)
        {
            MongoCollection<BsonDocument> expenseTypesCollection = database.GetCollection("dayentrytype");
            MongoCollection<BsonDocument> projectCategoriesCollection = database.GetCollection("projectcategory");

            if (!expenseEntry.Contains("dayentrytype"))
                throw new HandlerException("Expense entry is missing expense type.");

            BsonDocument expenseType = expenseTypesCollection.FindOne(Query.EQ(DBQuery.Id, expenseEntry["dayentrytype"][0]));

            if (expenseType == null)
                throw new HandlerException("Day entry type not found in dayentry.");

            if (!expenseType.Contains("projectcategory") || !expenseType["projectcategory"].IsBsonArray)
            {
                logger.LogInfo("Expense entry type (dayentrytype) doesn't contain project category and will not be exported to AX.");
                return null;
            }

            BsonDocument projectCategory = projectCategoriesCollection.FindOne(Query.EQ(DBQuery.Id, expenseType["projectcategory"][0]));

            if (projectCategory == null || !projectCategory.Contains("identifier"))
                throw new HandlerException("No valid project category was found for expense (dayentrytype).");
            
            return (string)projectCategory["identifier"];
        }

        #endregion

        #region Setup

        private void Init()
        {
            InitBaseDocuments();

            MaxExportFailureCount = (int)config["trotoaxexport"]["maxexportfailurecount"].GetValueOrDefault(3);
            MinTimeFragmentSize = (int)config["trotoaxexport"]["mintimefragmentsize"].GetValueOrDefault(60); // seconds
        }

        private void InitBaseDocuments()
        {
            exportHoursDocumentBase = new XmlDocument();
            string exportHoursBaseXml =
    @"<?xml version=""1.0"" encoding=""UTF-8""?>
    <Envelope xmlns=""http://schemas.microsoft.com/dynamics/2011/01/documents/Message"">
	<Header>
		<Company>FI12</Company>
		<Action>http://tempuri.org/EfiAIFBuffProjectHourJournalService/create</Action>
	</Header>
	<Body>
		<MessageParts xmlns=""http://schemas.microsoft.com/dynamics/2011/01/documents/Message"">
			<EfiAIFBuffProjectHourJournal xmlns=""http://schemas.microsoft.com/dynamics/2008/01/documents/EfiAIFBuffProjectHourJournal"">
				<EfiAifBuffProjJournalTable class=""entity"">
					<JournalNameId>SilmuHours</JournalNameId>

                </EfiAifBuffProjJournalTable>
	        </EfiAIFBuffProjectHourJournal>
		</MessageParts>
	</Body>
    </Envelope>";
            exportHoursDocumentBase.LoadXml(exportHoursBaseXml);

            exportExpensesDocumentBase = new XmlDocument();
            string exportExpensesBaseXml =
    @"<?xml version=""1.0"" encoding=""UTF-8""?>
    <Envelope xmlns=""http://schemas.microsoft.com/dynamics/2011/01/documents/Message"">
	<Header>
		<Company>FI12</Company>
		<Action>http://tempuri.org/EFIAIFLedgerJourTableService/create</Action>
	</Header>
	<Body>
		<MessageParts xmlns=""http://schemas.microsoft.com/dynamics/2011/01/documents/Message"">
			<EFIAIFLedgerJourTable xmlns=""http://schemas.microsoft.com/dynamics/2008/01/documents/EFIAIFLedgerJourTable"">
				<JournalTable class=""entity"">
					<JournalName>SilmuExp</JournalName>

                </JournalTable>
	        </EFIAIFLedgerJourTable>
		</MessageParts>
	</Body>
    </Envelope>";
            exportExpensesDocumentBase.LoadXml(exportExpensesBaseXml);

            exportItemConsumptionDocumentBase = new XmlDocument();
            string exportItemConsumptionBaseXml =
    @"<?xml version=""1.0"" encoding=""UTF-8""?>
    <Envelope xmlns=""http://schemas.microsoft.com/dynamics/2011/01/documents/Message"">
	<Header>
		<Company>FI12</Company>
		<Action>http://tempuri.org/EFIAIFInvJourTableService/create</Action>
	</Header>
	<Body>
		<MessageParts xmlns=""http://schemas.microsoft.com/dynamics/2011/01/documents/Message"">
			<EFIAIFInvJourTable xmlns=""http://schemas.microsoft.com/dynamics/2008/01/documents/EFIAIFInvJourTable"">
				<InventJournalTable class=""entity"">
					<JournalNameId>SilmuItems</JournalNameId>
                </InventJournalTable >
	        </EFIAIFInvJourTable>
		</MessageParts>
	</Body>
    </Envelope>";
            exportItemConsumptionDocumentBase.LoadXml(exportItemConsumptionBaseXml);
        }

        #endregion

    }
}
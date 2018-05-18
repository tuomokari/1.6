using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using SystemsGarden.mc2.RemoteConnector.Handlers;
using SystemsGarden.mc2.Common;
using SystemsGarden.mc2.Common.Constants;
using SystemsGarden.mc2.RemoteConnector.Handlers.CoreServerHandlers.MongoDBHandler;
using MongoDB.Bson;
using MongoDB.Shared;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using SystemsGarden.mc2.RemoteConnector.Handlers.TroIntegrationCommon;
using System.Xml.Serialization;
using System.Threading;

namespace SystemsGarden.mc2.RemoteConnector.Handlers.SapIntegrationHandlerServer
{
    public class SapToTroImport
    {

        #region Members

        private const string IncomingFileLocation = "Incoming";
        private const string DoneFileLocation = "Done";
        private const string FailedFileLocation = "Failed";

        private const string WorkerFileNameEnd = "{";
        private const string ProjectFileNameEnd = "Project";
        private const string ArticleFileNameEnd = "Article";

        private const string ProfitCenterNameSpace = "http://www.sap.com/abapxml";
        private const string BusinessAreaNameSpace = "http://www.sap.com/abapxml";
        private const string ActivityTypeNameSpace = "http://www.sap.com/abapxml";
        private const string FunctionalAreaNameSpace = "http://www.sap.com/abapxml";
        private const string MaterialGroupNameSpace = "http://www.sap.com/abapxml";
        private const string MaterialNameSpace = "http://www.sap.com/abapxml";
        private const string CustomerNameSpace = "http://www.sap.com/abapxml";
        private const string ProjectNameSpace = "http://www.sap.com/abapxml";
        private const string OrderNameSpace = "http://www.sap.com/abapxml";
        private const string OrderPartnerNameSpace = "http://www.sap.com/abapxml";
        private const string WorkerNameSpace = "http://Are.RoutePersonData.RouteEnvelopeSchema";
        private const string WorkerNameSpaceSQL = "http://schemas.microsoft.com/dynamics/2006/02/documents/EmplTable";

        private ILogger logger;
        private string filePath;
        private MongoDatabase mongoDatabase;
        private MongoDBHandlerServer mongoDBHandler;
        private string sqlConnectionString;
        private SqlConnection sqlConnection;
        private int maxIterations;

        private bool currentProcessFailed = false;
        private string currentProcessFailedReason = "";
        private ObjectId currentProcess_id;
        private string statisticalCollectionName = "";

        private object lockObject = new object();
        private DataTree invalidatedCacheItems = new DataTree();

        private IntegrationEvents integrationEvents;

        private ManualResetEvent stopping;

        private double logTime1;
        private double logTime2;
        private double logTime3;
        private double logTime4;
        private double logTime5;
        private double logTime6;
        private double logTime7;
        private double logTime1c;
        private double logTime2c;
        private double logTime3c;
        private double logTime4c;
        private double logTime5c;
        private double logTime6c;
        #endregion

        #region  Constructor

        public SapToTroImport(
            ILogger logger,
            string integrationFolderSapToTro,
            MongoDBHandlerServer mongoDBHandler,
            string sqlConnectionString,
            DataTree initializationMessage,
            ManualResetEvent stopping)
        {
            this.logger = logger.CreateChildLogger("SAPToTroImport");
            this.filePath = integrationFolderSapToTro;
            this.mongoDatabase = mongoDBHandler.Database;
            this.mongoDBHandler = mongoDBHandler;
            this.sqlConnectionString = sqlConnectionString;
            this.stopping = stopping;
            maxIterations = (int)initializationMessage["maxmessagesperiteration"].GetValueOrDefault(100);

            if (!this.filePath.EndsWith(Convert.ToString(Path.DirectorySeparatorChar)))
                this.filePath += Path.DirectorySeparatorChar;

        }

        #endregion

        #region Primary import function

        internal void ImportDocuments()
        {
            if (!MongoDBHandlerServer.SchemaApplied)
            {
                logger.LogDebug("Waiting for MongoDBHandler to receive schema and configuration before starting SAP integration.");
                return;
            }

                ProcessFiles();

            ProcessDatabase();
        }

        #endregion

        #region Import from database

        private void ProcessDatabase()
        {
            try
            {
                string cursorQuery = "SELECT TOP(@rowcount) messageContent, messageType,id FROM TroIntMessagesIn WITH (NOLOCK) WHERE status in ('new') ORDER BY id";
                using (sqlConnection = new SqlConnection(sqlConnectionString))
                {
                    SqlCommand sqlCommand = new SqlCommand(cursorQuery, sqlConnection);
                    sqlCommand.Parameters.Add("@rowcount", SqlDbType.Int);
                    sqlCommand.Parameters["@rowcount"].Value = maxIterations.ToString();
                    sqlConnection.Open();
                    SqlDataReader sqlReader = sqlCommand.ExecuteReader();
                    while (sqlReader.Read())
                    {
                        string messageId = "";
                        string messageType = "";
                        statisticalCollectionName = "";
                        try
                        {
                            currentProcessFailed = false;
                            currentProcessFailedReason = "";

                            messageId = sqlReader.GetInt32(2).ToString();
                            messageType = sqlReader.GetString(1);
                            XmlDocument xmldoc = new XmlDocument();
                            xmldoc.LoadXml(sqlReader.GetString(0));
                            logger.LogDebug("Process message:", messageId, sqlReader["messageType"]);

                            DBDocumentMessage(messageId, messageType, "in process", "", statisticalCollectionName);


                            if (messageType == "ProfitCenter")
                            {
                                statisticalCollectionName = "profitcenter";
                                ImportProfitCentersToTro(messageId, xmldoc, messageType);
                            }
                            else if (messageType == "BusinessArea")
                            {
                                statisticalCollectionName = "businessarea";
                                ImportBusinessAreasToTro(messageId, xmldoc, messageType);
                            }
                            else if (messageType == "ActivityType")
                            {
                                statisticalCollectionName = "projectcategory";
                                ImportActivityTypesToTro(messageId, xmldoc, messageType);
                            }
                            else if (messageType == "FunctionalArea")
                            {
                                statisticalCollectionName = "functionalarea";
                                ImportFunctionalAreasToTro(messageId, xmldoc, messageType);
                            }
                            else if (messageType == "MaterialGroup")
                            {
                                statisticalCollectionName = "articlegroup";
                                ImportMaterialGroupsToTro(messageId, xmldoc, messageType);
                            }
                            else if (messageType == "Material")
                            {
                                statisticalCollectionName = "article";
                                ImportMaterialsToTro(messageId, xmldoc, messageType);
                            }
                            else if (messageType == "Project")
                            {
                                statisticalCollectionName = "project";
                                ImportProjectsToTro(messageId, xmldoc, messageType);
                            }
                            else if (messageType == "Order")
                            {
                                statisticalCollectionName = "project";
                                ImportOrdersToTro(messageId, xmldoc, messageType);
                            }
                            else if (messageType == "Customer")
                            {
                                statisticalCollectionName = "customer";
                                ImportCustomersToTro(messageId, xmldoc, messageType);
                            }
                            else if (messageType == "AllAxEmplTable")
                            {
                                statisticalCollectionName = "user";
                                ImportAllAxEmplTableToTro(messageId, xmldoc, messageType);
                            }
                            else if (messageType == "EmplTable")
                            {
                                statisticalCollectionName = "user";
                                ImportEmplTableToTro(messageId, xmldoc, messageType);
                            }
                            else if (messageType == "OrderPartner")
                            {
                                statisticalCollectionName = "allocationentry/project";
                                ImportOrderPartnersToTro(messageId, xmldoc, messageType);
                            }
                        }
                        catch (Exception ex)
                        {
                            currentProcessFailed = true;
                            currentProcessFailedReason += ";ProcessDatabase error:" + ex.Message;
                            logger.LogError("ProcessDatabase error:", ex);
                        }
                        finally
                        {
                            if (messageId != "")
                            {
                                string info = "";
                                string status = "synced";
                                if (currentProcessFailed)
                                {
                                    status = "failed";
                                    info = currentProcessFailedReason;
                                }
                                DBDocumentMessage(messageId, messageType, status, info, statisticalCollectionName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                currentProcessFailed = true;
                currentProcessFailedReason += ";ProcessDatabase error 2:" + ex.Message;
                logger.LogError("ProcessDatabase error 2:", ex);
            }
        }
        private void DBDocumentMessage(string messageId, string messageType, string status, string info, string collectionName)
        {
            try
            {
                using (sqlConnection = new SqlConnection(sqlConnectionString))
                {
                    SqlCommand sqlCommand = new SqlCommand("dbo.TroInCloseMessage", sqlConnection);
                    sqlCommand.CommandType = CommandType.StoredProcedure;

                    sqlCommand.Parameters.Add("@messagetype", SqlDbType.VarChar, 20);
                    sqlCommand.Parameters.Add("@status", SqlDbType.VarChar, 20);
                    sqlCommand.Parameters.Add("@collectionname", SqlDbType.VarChar, 100);
                    sqlCommand.Parameters.Add("@infotext", SqlDbType.VarChar, -1);
                    sqlCommand.Parameters.Add("@id", SqlDbType.Int);
                    sqlCommand.Parameters["@id"].Value = messageId;
                    sqlCommand.Parameters["@status"].Value = status;
                    sqlCommand.Parameters["@messagetype"].Value = messageType;
                    sqlCommand.Parameters["@collectionname"].Value = collectionName;
                    sqlCommand.Parameters["@infotext"].Value = info;

                    sqlConnection.Open();
                    sqlCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                currentProcessFailed = true;
                currentProcessFailedReason += ";DBDocumentMessage error:" + ex.Message;
                logger.LogError("ProcessDatabase:DBDocumentMessage error:", messageId, ex);
            }
        }
        #endregion

        #region Import from files

        private void ProcessFiles()
        {
            lock (lockObject)
            {
                string incomingFolder = filePath + IncomingFileLocation;

                logger.LogTrace("Processing files in incoming folder.", incomingFolder);

                var di = new DirectoryInfo(incomingFolder);

                if (!di.Exists)
                    di.Create();

                foreach (FileInfo fi in di.EnumerateFiles())
                    ProcessFile(fi);

                logger.LogTrace("File processing completed.");
            }
        }

        private void ProcessFile(FileInfo fi)
        {
            logger.LogTrace("Processing file", fi.Name);

            currentProcessFailed = false;
            currentProcessFailedReason = "";

            try
            {
                using (StreamReader sr = new StreamReader(fi.FullName, Encoding.GetEncoding("iso-8859-1")))
                {
                    XmlDocument xmldoc = new XmlDocument();
                    xmldoc.LoadXml(sr.ReadToEnd());
                    string messageId = null;
                    if (fi.Name.StartsWith(WorkerFileNameEnd))
                    {
                        ImportWorkersToTro(messageId, xmldoc);
                    }
                    else if (fi.Name.StartsWith(ProjectFileNameEnd))
                    {
                        ImportProjectsToTro(sr.ReadToEnd());
                    }
                    else if (fi.Name.StartsWith(ArticleFileNameEnd))
                    {
                        ImportArticlesToTro(sr.ReadToEnd());
                    }
                    else
                    {
                        throw new InvalidDataException("Could not map file to any existing SAP import handler.");
                    }
                }

                if (currentProcessFailed)
                    throw new InvalidDataException("Errors encountered when processing file");
                else
                {
                    logger.LogTrace("Successfully processed file for the first time.", fi.Name);
                    MoveFileToDoneLocation(fi);
                }
            }
            catch (Exception ex)
            {
                currentProcessFailed = true;
                currentProcessFailedReason += ex.Message;
                logger.LogError("Processing file failed.", fi.Name, ex);
                MoveFileToFailedLocation(fi);
            }
        }

        private void MoveFileToFailedLocation(FileInfo fi)
        {

            var fiTarget = new FileInfo(
                filePath + FailedFileLocation + Path.DirectorySeparatorChar + fi.Name);

            logger.LogInfo("Moving file to failed location", fi.FullName, fiTarget.FullName);

            if (!new DirectoryInfo(fiTarget.DirectoryName).Exists)
                new DirectoryInfo(fiTarget.DirectoryName).Create();

            if (fiTarget.Exists)
            {
                logger.LogTrace("Target file exists. Removing it.");
                fiTarget.Delete();
            }

            fi.MoveTo(fiTarget.FullName);
        }

        private void MoveFileToDoneLocation(FileInfo fi)
        {
            var fiTarget = new FileInfo(
                filePath + DoneFileLocation + Path.DirectorySeparatorChar + fi.Name);

            logger.LogTrace("Moving file to done location", fi.FullName, fiTarget.FullName);

            if (fiTarget.Exists)
            {
                logger.LogTrace("Target file exists. Removing it.");
                fiTarget.Delete();
            }

            if (!new DirectoryInfo(fiTarget.DirectoryName).Exists)
                new DirectoryInfo(fiTarget.DirectoryName).Create();

            fi.MoveTo(fiTarget.FullName);
        }
        private void RefreshCache(ObjectId idField, string collectionName)
        {
            try
            {
                mongoDBHandler.RefreshDocumentCachedInfo(idField, collectionName);
            }
            catch (Exception ex)
            {
                logger.LogError("RefreshDocumentCachedInfo:", ex);
            }
        }

        #endregion

        #region OrganizationData
        #region ProfitCenter
        private void ImportProfitCentersToTro(string messageId, XmlDocument xmldoc, string messageType)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            int itemsProcessed = 0;

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmldoc.NameTable);
            nsmgr.AddNamespace("x", ProfitCenterNameSpace);
            XmlNodeList nodes = xmldoc.DocumentElement.SelectNodes("//x:abap/x:values/ARRAY/ZTRO_PROFITCTR", nsmgr);
            foreach (XmlNode node in nodes)
            {
                // integrationevent:
                integrationEvents = new IntegrationEvents("SAP", messageId, messageType, (XmlElement)node, "in", logger, mongoDatabase);

                try
                {
                    itemsProcessed++;

                    ImportProfitCenterToTro(messageId, (XmlElement)node);
                }
                catch (Exception ex)
                {
                    currentProcessFailed = true;
                    currentProcessFailedReason += ". " + ex.Message;
                    logger.LogError("Process ProfitCenter:", ex);
                    logger.LogError("Processing ProfitCenter element:", (XmlElement)node);
                }
                finally
                {
                    if (currentProcessFailed)
                    {
                        integrationEvents.Lastfailreason = currentProcessFailedReason;
                        integrationEvents.Failed = true;
                        integrationEvents.Status = "failed";
                    }
                    integrationEvents.createEvent();
                }
            }

            logger.LogDebug("Importing profit centers done.", "Elapsed: " + stopWatch.Elapsed, "Items: " + itemsProcessed, "Per item: " + (stopWatch.Elapsed.TotalMilliseconds / itemsProcessed) + " milliseconds");
        }
        private void ImportProfitCenterToTro(string messageId, XmlElement profitCenterElement)
        {
            string collectionName = "profitcenter";
            string identifier = profitCenterElement["PROFIT_CTR"].InnerText;
            currentProcess_id = ObjectId.Empty;

            integrationEvents.CollectionName = collectionName;
            integrationEvents.Name = identifier;

            if (string.IsNullOrEmpty(identifier))
            {
                logger.LogError("ProfitCenter identifier empty");
                currentProcessFailed = true;
                currentProcessFailedReason += ";ProfitCenter identifier empty";
                return;
            }

            lock (mongoDatabase)
            {
                MongoCollection<BsonDocument> profitCenterCollection = mongoDatabase.GetCollection(collectionName);

                // Find out if document exists
                MongoCursor cursor = profitCenterCollection.Find(Query.EQ("identifier", identifier));

                BsonDocument profitCenterDocument = null;
                BsonDocument profitCenterDocumentOriginal = null;

                if (cursor.Count() == 0)
                {
                    profitCenterDocument = new BsonDocument();
                    profitCenterDocument.SetExtended(collectionName, "created", DateTime.UtcNow);
                    currentProcess_id = ObjectId.GenerateNewId();
                    profitCenterDocument.SetExtended(collectionName, DBQuery.Id, currentProcess_id);
                }
                else if (cursor.Count() == 1)
                {
                    foreach (BsonDocument doc in cursor)
                        profitCenterDocument = doc;

                    profitCenterDocumentOriginal = (BsonDocument)profitCenterDocument.Clone();
                    currentProcess_id = (ObjectId)profitCenterDocument[DBQuery.Id];
                }
                else
                {
                    currentProcessFailedReason = "More than one existing profitCenter found: ";
                    currentProcessFailed = true;
                    logger.LogError(currentProcessFailedReason, identifier, cursor.Count());
                    return;
                }

                SetprofitCenterData(collectionName, profitCenterElement, profitCenterDocument);

                bool dataChanged = true;
                if (cursor.Count() == 1)
                {
                    if (profitCenterDocumentOriginal.CompareTo(profitCenterDocument) == 0)
                    {
                        dataChanged = false;
                    }
                }

                if (dataChanged)
                {
                    profitCenterCollection.Save(profitCenterDocument, WriteConcern.Acknowledged);
                    invalidatedCacheItems.Add(new DataTree(profitCenterDocument[DBQuery.Id].ToString()));
                    RefreshCache((ObjectId)profitCenterDocument[DBQuery.Id], profitCenterCollection.Name);

                    logger.LogDebug("Saving profitCenterDocument.");
                }

                integrationEvents.UpdateInboundmessageSynced(profitCenterDocument, collectionName, currentProcess_id, dataChanged);
            }
        }
        private void SetprofitCenterData(string collectionName, XmlElement profitCenterElement, BsonDocument profitCenterDocument)
        {
            logger.LogDebug("Setting profitCenter data.");

            profitCenterDocument.SetExtended(collectionName, "identifier", profitCenterElement["PROFIT_CTR"].InnerText);
            //profitCenterDocument.Set("categoryid", profitCenterElement["KOKRS"].InnerText);
            profitCenterDocument.SetExtended(collectionName, "name", profitCenterElement["KTEXT"].InnerText);
            profitCenterDocument.SetExtended(collectionName, "disabled", false);
            profitCenterDocument.SetExtended(collectionName, "modified", DateTime.UtcNow);
        }
        #endregion
        #region BusinessArea
        private void ImportBusinessAreasToTro(string messageId, XmlDocument xmldoc, string messageType)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            int itemsProcessed = 0;

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmldoc.NameTable);
            nsmgr.AddNamespace("x", BusinessAreaNameSpace);
            XmlNodeList nodes = xmldoc.DocumentElement.SelectNodes("//x:abap/x:values/ARRAY/ZTRO_BUSINESSAREA", nsmgr);
            foreach (XmlNode node in nodes)
            {
                // integrationevent:
                integrationEvents = new IntegrationEvents("SAP", messageId, messageType, (XmlElement)node, "in", logger, mongoDatabase);
                try
                {
                    itemsProcessed++;
                    ImportBusinessAreaToTro(messageId, (XmlElement)node);
                }
                catch (Exception ex)
                {
                    currentProcessFailed = true;
                    currentProcessFailedReason += ". " + ex.Message;
                    logger.LogError("Process BusinessArea:", ex);
                    logger.LogError("Processing BusinessArea element:", (XmlElement)node);
                }
                finally
                {
                    if (currentProcessFailed)
                    {
                        integrationEvents.Lastfailreason = currentProcessFailedReason;
                        integrationEvents.Failed = true;
                        integrationEvents.Status = "failed";
                    }
                    integrationEvents.createEvent();
                }
            }

            logger.LogDebug("Importing business areas done.", "Elapsed: " + stopWatch.Elapsed, "Items: " + itemsProcessed, "Per item: " + (stopWatch.Elapsed.TotalMilliseconds / itemsProcessed) + " milliseconds");
        }
        private void ImportBusinessAreaToTro(string messageId, XmlElement businessAreaElement)
        {
            string collectionName = "businessarea";
            string identifier = businessAreaElement["BUS_AREA"].InnerText;
            currentProcess_id = ObjectId.Empty;

            integrationEvents.CollectionName = collectionName;
            integrationEvents.Name = identifier;

            if (string.IsNullOrEmpty(identifier))
            {
                currentProcessFailedReason += ";BusinessArea identifier empty";
                currentProcessFailed = true;
                logger.LogError(currentProcessFailedReason);
                return;
            }

            lock (mongoDatabase)
            {
                MongoCollection<BsonDocument> businessAreaCollection = mongoDatabase.GetCollection(collectionName);

                // Find out if document exists
                MongoCursor cursor = businessAreaCollection.Find(Query.EQ("identifier", identifier));

                BsonDocument businessAreaDocument = null;
                BsonDocument businessAreaDocumentOriginal = null;

                if (cursor.Count() == 0)
                {
                    businessAreaDocument = new BsonDocument();
                    businessAreaDocument.SetExtended(collectionName, DBQuery.Id, ObjectId.GenerateNewId());
                    currentProcess_id = ObjectId.GenerateNewId();
                    businessAreaDocument.SetExtended(collectionName, "_id", currentProcess_id);
                    businessAreaDocument.SetExtended(collectionName, "created", DateTime.UtcNow);
                }
                else if (cursor.Count() == 1)
                {
                    foreach (BsonDocument doc in cursor)
                        businessAreaDocument = doc;

                    businessAreaDocumentOriginal = (BsonDocument)businessAreaDocument.Clone();
                    currentProcess_id = (ObjectId)businessAreaDocument["_id"];
                }
                else
                {
                    currentProcessFailedReason += ";More than one existing BusinessArea found: ";
                    currentProcessFailed = true;
                    logger.LogError(currentProcessFailedReason, identifier, cursor.Count());
                    return;
                }


                SetBusinessAreaData(collectionName, businessAreaElement, businessAreaDocument);

                bool dataChanged = true;
                if (cursor.Count() == 1)
                {
                    if (businessAreaDocumentOriginal.CompareTo(businessAreaDocument) == 0)
                    {
                        dataChanged = false;
                    }
                }

                if (dataChanged)
                {
                    businessAreaCollection.Save(businessAreaDocument, WriteConcern.Acknowledged);
                    invalidatedCacheItems.Add(new DataTree(businessAreaDocument[DBQuery.Id].ToString()));
                    RefreshCache((ObjectId)businessAreaDocument[DBQuery.Id], businessAreaCollection.Name);

                    logger.LogDebug("Saving businessAreaDocument.");
                }

                integrationEvents.UpdateInboundmessageSynced(businessAreaDocument, collectionName, currentProcess_id, dataChanged);
            }
        }

        private void SetBusinessAreaData(string collectionName, XmlElement businessAreaElement, BsonDocument businessAreaDocument)
        {
            logger.LogDebug("Setting BusinessArea data.");

            businessAreaDocument.SetExtended(collectionName, "identifier", businessAreaElement["BUS_AREA"].InnerText);
            businessAreaDocument.SetExtended(collectionName, "name", businessAreaElement["GTEXT"].InnerText);
            businessAreaDocument.SetExtended(collectionName, "modified", DateTime.UtcNow);
            businessAreaDocument.SetExtended(collectionName, "disabled", false);
        }
        #endregion
        #region ActivityType
        private void ImportActivityTypesToTro(string messageId, XmlDocument xmldoc, string messageType)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            int itemsProcessed = 0;

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmldoc.NameTable);
            nsmgr.AddNamespace("x", ActivityTypeNameSpace);
            XmlNodeList nodes = xmldoc.DocumentElement.SelectNodes("//x:abap/x:values/ARRAY/ZTRO_ACTIVITYTYPE", nsmgr);
            foreach (XmlNode node in nodes)
            {
                // integrationevent:
                integrationEvents = new IntegrationEvents("SAP", messageId, messageType, (XmlElement)node, "in", logger, mongoDatabase);
                try
                {
                    itemsProcessed++;
                    ImportActivityTypeToTro(messageId, (XmlElement)node);
                }
                catch (Exception ex)
                {
                    currentProcessFailed = true;
                    currentProcessFailedReason += ". " + ex.Message;
                    logger.LogError("Process ActivityType:", ex);
                    logger.LogError("Processing ActivityType element:", (XmlElement)node);
                }
                finally
                {
                    if (currentProcessFailed)
                    {
                        integrationEvents.Lastfailreason = currentProcessFailedReason;
                        integrationEvents.Failed = true;
                        integrationEvents.Status = "failed";
                    }
                    integrationEvents.createEvent();
                }
            }

            logger.LogDebug("Importing profit centers done.", "Elapsed: " + stopWatch.Elapsed, "Items: " + itemsProcessed, "Per item: " + (stopWatch.Elapsed.TotalMilliseconds / itemsProcessed) + " milliseconds");
        }
        private void ImportActivityTypeToTro(string messageId, XmlElement activityTypeElement)
        {
            string collectionName = "projectcategory";
            string identifier = activityTypeElement["ACTIVITY_TYPE"].InnerText;
            currentProcess_id = ObjectId.Empty;

            integrationEvents.CollectionName = collectionName;
            integrationEvents.Name = identifier;

            if (string.IsNullOrEmpty(identifier))
            {
                logger.LogError("ActivityType identifier empty");
                currentProcessFailed = true;
                currentProcessFailedReason += ";ActivityType identifier empty";
                return;
            }

            // activitytypes without LTEXT (longtext) are numerous and not usefull
            if (activityTypeElement["LTEXT"].InnerText == "")
            {
                logger.LogTrace("ActivityType LTEXT empty, type ignored", identifier);
                return;
            }

            lock (mongoDatabase)
            {
                MongoCollection<BsonDocument> activityTypeCollection = mongoDatabase.GetCollection(collectionName);

                // Find out if document exists
                MongoCursor cursor = activityTypeCollection.Find(Query.EQ("identifier", identifier));

                BsonDocument activityTypeDocument = null;
                BsonDocument activityTypeDocumentOriginal = null;

                if (cursor.Count() == 0)
                {
                    activityTypeDocument = new BsonDocument();
                    currentProcess_id = ObjectId.GenerateNewId();
                    activityTypeDocument.SetExtended(collectionName, DBQuery.Id, currentProcess_id);
                    activityTypeDocument.SetExtended(collectionName, "created", DateTime.UtcNow);
                }
                else if (cursor.Count() == 1)
                {
                    foreach (BsonDocument doc in cursor)
                        activityTypeDocument = doc;

                    activityTypeDocumentOriginal = (BsonDocument)activityTypeDocument.Clone();
                    currentProcess_id = (ObjectId)activityTypeDocument[DBQuery.Id];
                }
                else
                {
                    currentProcessFailedReason += ";More than one existing activityType found: ";
                    currentProcessFailed = true;
                    logger.LogError(currentProcessFailedReason, identifier, cursor.Count());
                    return;
                }


                SetactivityTypeData(collectionName, activityTypeElement, activityTypeDocument);

                bool dataChanged = true;
                if (cursor.Count() == 1)
                {
                    if (activityTypeDocumentOriginal.CompareTo(activityTypeDocument) == 0)
                    {
                        dataChanged = false;
                    }
                }

                if (dataChanged)
                {
                    activityTypeCollection.Save(activityTypeDocument, WriteConcern.Acknowledged);
                    invalidatedCacheItems.Add(new DataTree(activityTypeDocument[DBQuery.Id].ToString()));
                    RefreshCache((ObjectId)activityTypeDocument[DBQuery.Id], activityTypeCollection.Name);

                    logger.LogDebug("Saving activityTypeDocument.");
                }

                integrationEvents.UpdateInboundmessageSynced(activityTypeDocument, collectionName, currentProcess_id, dataChanged);
            }
        }

        private void SetactivityTypeData(string collectionName, XmlElement activityTypeElement, BsonDocument activityTypeDocument)
        {
            logger.LogDebug("Setting activityType data.");

            activityTypeDocument.SetExtended(collectionName, "identifier", activityTypeElement["ACTIVITY_TYPE"].InnerText);
            activityTypeDocument.SetExtended(collectionName, "name", activityTypeElement["LTEXT"].InnerText);
            activityTypeDocument.SetExtended(collectionName, "description", "");
            activityTypeDocument.SetExtended(collectionName, "modified", DateTime.UtcNow);
            activityTypeDocument.SetExtended(collectionName, "disabled", false);

            logger.LogDebug("Insert activityType", activityTypeElement["ACTIVITY_TYPE"].InnerText, activityTypeElement["KTEXT"].InnerText);
        }
        #endregion
        #region FunctionalArea
        private void ImportFunctionalAreasToTro(string messageId, XmlDocument xmldoc, string messageType)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            int itemsProcessed = 0;

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmldoc.NameTable);
            nsmgr.AddNamespace("x", FunctionalAreaNameSpace);
            XmlNodeList nodes = xmldoc.DocumentElement.SelectNodes("//x:abap/x:values/ARRAY/ZTRO_FUNCTIONALAREA", nsmgr);
            foreach (XmlNode node in nodes)
            {
                // integrationevent:
                integrationEvents = new IntegrationEvents("SAP", messageId, messageType, (XmlElement)node, "in", logger, mongoDatabase);

                try
                {
                    itemsProcessed++;
                    ImportFunctionalAreaToTro(messageId, (XmlElement)node);
                }
                catch (Exception ex)
                {
                    currentProcessFailed = true;
                    currentProcessFailedReason += ". " + ex.Message;
                    logger.LogError("Process FunctionalArea:", ex);
                    logger.LogError("Processing FunctionalArea element:", (XmlElement)node);
                }
                finally
                {
                    if (currentProcessFailed)
                    {
                        integrationEvents.Lastfailreason = currentProcessFailedReason;
                        integrationEvents.Failed = true;
                        integrationEvents.Status = "failed";
                    }
                    integrationEvents.createEvent();
                }
            }

            logger.LogDebug("Importing functional areas done.", "Elapsed: " + stopWatch.Elapsed, "Items: " + itemsProcessed, "Per item: " + (stopWatch.Elapsed.TotalMilliseconds / itemsProcessed) + " milliseconds");
        }
        private void ImportFunctionalAreaToTro(string messageId, XmlElement functionalAreaElement)
        {
            string collectionName = "functionalarea";
            string identifier = functionalAreaElement["FUNC_AREA"].InnerText;
            currentProcess_id = ObjectId.Empty;

            integrationEvents.CollectionName = collectionName;
            integrationEvents.Name = identifier;

            if (string.IsNullOrEmpty(identifier))
            {
                logger.LogError("FunctionalArea identifier empty");
                currentProcessFailed = true;
                currentProcessFailedReason += ";FunctionalArea identifier empty";
                return;
            }

            lock (mongoDatabase)
            {
                MongoCollection<BsonDocument> functionalAreaCollection = mongoDatabase.GetCollection(collectionName);

                // Find out if document exists
                MongoCursor cursor = functionalAreaCollection.Find(Query.EQ("identifier", identifier));

                BsonDocument functionalAreaDocument = null;
                BsonDocument functionalAreaDocumentOriginal = null;

                if (cursor.Count() == 0)
                {
                    functionalAreaDocument = new BsonDocument();
                    currentProcess_id = ObjectId.GenerateNewId();
                    functionalAreaDocument.SetExtended(collectionName, DBQuery.Id, currentProcess_id);
                    functionalAreaDocument.SetExtended(collectionName, "created", DateTime.UtcNow);
                }
                else if (cursor.Count() == 1)
                {
                    foreach (BsonDocument doc in cursor)
                        functionalAreaDocument = doc;

                    functionalAreaDocumentOriginal = (BsonDocument)functionalAreaDocument.Clone();
                    currentProcess_id = (ObjectId)functionalAreaDocument[DBQuery.Id];
                }
                else
                {
                    currentProcessFailedReason += ";More than one existing functionalarea found: ";
                    currentProcessFailed = true;
                    logger.LogError(currentProcessFailedReason, identifier, cursor.Count());
                    return;
                }

                SetfunctionalAreaData(collectionName, functionalAreaElement, functionalAreaDocument);

                bool dataChanged = true;
                if (cursor.Count() == 1)
                {
                    if (functionalAreaDocumentOriginal.CompareTo(functionalAreaDocument) == 0)
                    {
                        dataChanged = false;
                    }
                }

                if (dataChanged)
                {
                    functionalAreaCollection.Save(functionalAreaDocument, WriteConcern.Acknowledged);
                    invalidatedCacheItems.Add(new DataTree(functionalAreaDocument[DBQuery.Id].ToString()));
                    RefreshCache((ObjectId)functionalAreaDocument[DBQuery.Id], functionalAreaCollection.Name);

                    logger.LogDebug("Saving functionalAreaDocument.");
                }

                integrationEvents.UpdateInboundmessageSynced(functionalAreaDocument, collectionName, currentProcess_id, dataChanged);
            }
        }

        private void SetfunctionalAreaData(string collectionName, XmlElement functionalAreaElement, BsonDocument functionalAreaDocument)
        {
            logger.LogDebug("Setting functionalArea data.");

            functionalAreaDocument.SetExtended(collectionName, "identifier", functionalAreaElement["FUNC_AREA"].InnerText);
            functionalAreaDocument.SetExtended(collectionName, "name", functionalAreaElement["FKBTX"].InnerText);
            functionalAreaDocument.SetExtended(collectionName, "modified", DateTime.UtcNow);
            functionalAreaDocument.SetExtended(collectionName, "disabled", false);

            logger.LogDebug("Insert functionalArea", functionalAreaElement["FUNC_AREA"].InnerText, functionalAreaElement["FKBTX"].InnerText);
        }
        #endregion
        #region MaterialGroup
        private void ImportMaterialGroupsToTro(string messageId, XmlDocument xmldoc, string messageType)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            int itemsProcessed = 0;

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmldoc.NameTable);
            nsmgr.AddNamespace("x", MaterialGroupNameSpace);
            XmlNodeList nodes = xmldoc.DocumentElement.SelectNodes("//x:abap/x:values/ARRAY/ZTRO_MATERIALGROUP", nsmgr);
            foreach (XmlNode node in nodes)
            {
                // integrationevent:
                integrationEvents = new IntegrationEvents("SAP", messageId, messageType, (XmlElement)node, "in", logger, mongoDatabase);

                try
                {
                    itemsProcessed++;
                    ImportMaterialGroupToTro(messageId, (XmlElement)node);
                }
                catch (Exception ex)
                {
                    currentProcessFailed = true;
                    currentProcessFailedReason += ". " + ex.Message;
                    logger.LogError("Process MaterialGroup:", ex);
                    logger.LogError("Processing MaterialGroup element:", (XmlElement)node);
                }
                finally
                {
                    if (currentProcessFailed)
                    {
                        integrationEvents.Lastfailreason = currentProcessFailedReason;
                        integrationEvents.Failed = true;
                        integrationEvents.Status = "failed";
                    }
                    integrationEvents.createEvent();
                }
            }

            logger.LogDebug("Importing material groups done.", "Elapsed: " + stopWatch.Elapsed, "Items: " + itemsProcessed, "Per item: " + (stopWatch.Elapsed.TotalMilliseconds / itemsProcessed) + " milliseconds");
        }
        private void ImportMaterialGroupToTro(string messageId, XmlElement materialGroupElement)
        {
            string collectionName = "articlegroup";
            string identifier = materialGroupElement["MATKL"].InnerText;
            currentProcess_id = ObjectId.Empty;

            integrationEvents.CollectionName = collectionName;
            integrationEvents.Name = identifier;

            if (string.IsNullOrEmpty(identifier))
            {
                logger.LogError("MaterialGroup identifier empty");
                currentProcessFailed = true;
                currentProcessFailedReason += ";MaterialGroup identifier empty";
                return;
            }

            lock (mongoDatabase)
            {
                MongoCollection<BsonDocument> materialGroupCollection = mongoDatabase.GetCollection(collectionName);

                // Find out if document exists
                MongoCursor cursor = materialGroupCollection.Find(Query.EQ("identifier", identifier));

                BsonDocument materialGroupDocument = null;
                BsonDocument materialGrouDocumentOriginal = null;

                if (cursor.Count() == 0)
                {
                    materialGroupDocument = new BsonDocument();
                    currentProcess_id = ObjectId.GenerateNewId();
                    materialGroupDocument.SetExtended(collectionName, DBQuery.Id, currentProcess_id);
                    materialGroupDocument.SetExtended(collectionName, "created", DateTime.UtcNow);
                }
                else if (cursor.Count() == 1)
                {
                    foreach (BsonDocument doc in cursor)
                        materialGroupDocument = doc;

                    currentProcess_id = (ObjectId)materialGroupDocument[DBQuery.Id];
                    materialGrouDocumentOriginal = (BsonDocument)materialGroupDocument.Clone();
                }
                else
                {
                    currentProcessFailedReason += ";More than one existing MaterialGroup found: ";
                    currentProcessFailed = true;
                    logger.LogError(currentProcessFailedReason, identifier, cursor.Count());
                    return;
                }

                SetmaterialGroupData(collectionName, materialGroupElement, materialGroupDocument);

                bool dataChanged = true;
                if (cursor.Count() == 1)
                {
                    if (materialGrouDocumentOriginal.CompareTo(materialGroupDocument) == 0)
                    {
                        dataChanged = false;
                    }
                }

                if (dataChanged)
                {
                    materialGroupCollection.Save(materialGroupDocument, WriteConcern.Acknowledged);
                    invalidatedCacheItems.Add(new DataTree(materialGroupDocument[DBQuery.Id].ToString()));
                    RefreshCache((ObjectId)materialGroupDocument[DBQuery.Id], materialGroupCollection.Name);

                    logger.LogDebug("Saving materialGroupDocument.");
                }

                integrationEvents.UpdateInboundmessageSynced(materialGroupDocument, collectionName, currentProcess_id, dataChanged);
            }
        }

        private void SetmaterialGroupData(string collectionName, XmlElement materialGroupElement, BsonDocument materialGroupDocument)
        {
            logger.LogDebug("Setting MaterialGroup data.");

            materialGroupDocument.SetExtended(collectionName, "identifier", materialGroupElement["MATKL"].InnerText);
            materialGroupDocument.SetExtended(collectionName, "name", materialGroupElement["DESCRIPTION"].InnerText);
            materialGroupDocument.SetExtended(collectionName, "modified", DateTime.UtcNow);
            materialGroupDocument.SetExtended(collectionName, "disabled", false);

            logger.LogDebug("Insert MaterialGroup", materialGroupElement["MATKL"].InnerText, materialGroupElement["DESCRIPTION"].InnerText);
        }
        #endregion
        #region Material
        private void ImportMaterialsToTro(string messageId, XmlDocument xmldoc, string messageType)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            int itemsProcessed = 0;

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmldoc.NameTable);
            nsmgr.AddNamespace("x", MaterialNameSpace);
            XmlNodeList nodes = xmldoc.DocumentElement.SelectNodes("//x:abap/x:values/ARRAY/ZTRO_MATERIAL", nsmgr);
            foreach (XmlNode node in nodes)
            {
                // integrationevent:
                integrationEvents = new IntegrationEvents("SAP", messageId, messageType, (XmlElement)node, "in", logger, mongoDatabase);

                try
                {
                    itemsProcessed++;
                    ImportMaterialToTro(messageId, (XmlElement)node);
                }
                catch (Exception ex)
                {
                    currentProcessFailed = true;
                    currentProcessFailedReason += ". " + ex.Message;
                    logger.LogError("Process Material:", ex);
                    logger.LogError("Processing Material element:", (XmlElement)node);
                }
                finally
                {
                    if (currentProcessFailed)
                    {
                        integrationEvents.Lastfailreason = currentProcessFailedReason;
                        integrationEvents.Failed = true;
                        integrationEvents.Status = "failed";
                    }
                    integrationEvents.createEvent();
                }
            }

            logger.LogDebug("Importing materials to tro done.", "Elapsed: " + stopWatch.Elapsed, "Items: " + itemsProcessed, "Per item: " + (stopWatch.Elapsed.TotalMilliseconds / itemsProcessed) + " milliseconds");
        }
        private void ImportMaterialToTro(string messageId, XmlElement materialElement)
        {
            string collectionName = "article";
            string identifier = materialElement["MATNR"].InnerText;
            currentProcess_id = ObjectId.Empty;

            integrationEvents.CollectionName = collectionName;
            integrationEvents.Name = identifier;

            if (string.IsNullOrEmpty(identifier))
            {
                logger.LogError("Material identifier empty");
                currentProcessFailed = true;
                currentProcessFailedReason += ";Material identifier empty";
                return;
            }

            lock (mongoDatabase)
            {
                MongoCollection<BsonDocument> materialCollection = mongoDatabase.GetCollection(collectionName);

                // Find out if document exists
                MongoCursor cursor = materialCollection.Find(Query.EQ("identifier", identifier));

                BsonDocument materialDocument = null;
                BsonDocument materialDocumentOriginal = null;

                if (cursor.Count() == 0)
                {
                    materialDocument = new BsonDocument();
                    currentProcess_id = ObjectId.GenerateNewId();
                    materialDocument.SetExtended(collectionName, DBQuery.Id, currentProcess_id);
                    materialDocument.SetExtended(collectionName, "created", DateTime.UtcNow);
                }
                else if (cursor.Count() == 1)
                {
                    foreach (BsonDocument doc in cursor)
                        materialDocument = doc;

                    materialDocumentOriginal = (BsonDocument)materialDocument.Clone();
                    currentProcess_id = (ObjectId)materialDocument[DBQuery.Id];
                }
                else
                {
                    currentProcessFailedReason += ";More than one existing Material found: ";
                    currentProcessFailed = true;
                    logger.LogError(currentProcessFailedReason, identifier, cursor.Count());
                    return;
                }

                ObjectId materialGroup_id = SetRefToArticeGroup(materialElement["MATKL"].InnerText);


                SetmaterialData(collectionName, materialElement, materialDocument, materialGroup_id);

                bool dataChanged = true;
                if (cursor.Count() == 1)
                    if (materialDocumentOriginal.CompareTo(materialDocument) == 0)
                        dataChanged = false;
                    else
                        invalidatedCacheItems.Add(new DataTree(materialDocument[DBQuery.Id].ToString()));

                if (dataChanged)
                {
                    logger.LogDebug("Upsert Material", materialElement["MATNR"].InnerText, materialElement["MAKTX"].InnerText);

                    materialDocument.SetExtended(collectionName, "modified", DateTime.UtcNow);
                    materialCollection.Save(materialDocument, WriteConcern.Acknowledged);

                    RefreshCache((ObjectId)materialDocument[DBQuery.Id], materialCollection.Name);
                }

                integrationEvents.UpdateInboundmessageSynced(materialDocument, collectionName, currentProcess_id, dataChanged);
            }
        }

        private ObjectId SetRefToArticeGroup(string identifier)
        {
            ObjectId materialGroup_id;
            // set reference to materialGoup
            MongoCollection<BsonDocument> materialGroupCollection = mongoDatabase.GetCollection("articlegroup");
            MongoCursor cursormaterialGroup = materialGroupCollection.Find(Query.EQ("identifier", identifier));

            BsonDocument materialGroupDocument = null;

            if (cursormaterialGroup.Count() == 0)
            {
                materialGroupDocument = new BsonDocument();
                materialGroup_id = ObjectId.GenerateNewId();
                materialGroupDocument.SetExtended("articlegroup", DBQuery.Id, materialGroup_id);
                materialGroupDocument.SetExtended("articlegroup", "identifier", identifier);
                materialGroupDocument.SetExtended("articlegroup", "name", "");
                materialGroupDocument.SetExtended("articlegroup", "created", DateTime.UtcNow);
                materialGroupDocument.SetExtended("articlegroup", "disabled", true);
                materialGroupCollection.Save(materialGroupDocument, WriteConcern.Acknowledged);
                logger.LogTrace("Created disabled articlegroup", materialGroup_id, identifier);
            }
            else if (cursormaterialGroup.Count() == 1)
            {
                foreach (BsonDocument doc in cursormaterialGroup)
                    materialGroupDocument = doc;

                materialGroup_id = (ObjectId)materialGroupDocument[DBQuery.Id];
            }
            else
            {
                materialGroup_id = ObjectId.Empty;
                currentProcessFailedReason += ";More than one existing MaterialGroup found: ";
                currentProcessFailed = true;
                logger.LogError(currentProcessFailedReason, identifier, cursormaterialGroup.Count());
            }

            return materialGroup_id;
        }

        private void SetmaterialData(string collectionName, XmlElement materialElement, BsonDocument materialDocument, ObjectId materialGroup_id)
        {
            logger.LogDebug("Setting Material data.");

            materialDocument.SetExtended(collectionName, "identifier", materialElement["MATNR"].InnerText);
            materialDocument.SetExtended(collectionName, "name", materialElement["MAKTX"].InnerText);
            materialDocument.SetExtended(collectionName, "unit", materialElement["MEINS"].InnerText);

            // referenssit: MATKL -> tuoteryhmä
            SetDocumentAsArrayItem(materialDocument, collectionName, materialGroup_id, "articlegroup");

            if (materialElement["LVORM"].InnerText == "X")
                materialDocument.SetExtended(collectionName, "disabled", true);
            else
                materialDocument.SetExtended(collectionName, "disabled", false);

        }
        #endregion
        #region Customer
        private void ImportCustomersToTro(string messageId, XmlDocument xmldoc, string messageType)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            int itemsProcessed = 0;

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmldoc.NameTable);
            nsmgr.AddNamespace("x", CustomerNameSpace);
            XmlNodeList nodes = xmldoc.DocumentElement.SelectNodes("//x:abap/x:values/ARRAY/ZTRO_CUSTOMER", nsmgr);
            foreach (XmlNode node in nodes)
            {
                // integrationevent:
                integrationEvents = new IntegrationEvents("SAP", messageId, messageType, (XmlElement)node, "in", logger, mongoDatabase);

                try
                {
                    itemsProcessed++;
                    ImportCustomerToTro(messageId, (XmlElement)node);
                }
                catch (Exception ex)
                {
                    currentProcessFailed = true;
                    currentProcessFailedReason += ". " + ex.Message;
                    logger.LogError("Process Customer:", ex);
                    logger.LogError("Processing Customer element:", (XmlElement)node);
                }
                finally
                {
                    if (currentProcessFailed)
                    {
                        integrationEvents.Lastfailreason = currentProcessFailedReason;
                        integrationEvents.Failed = true;
                        integrationEvents.Status = "failed";
                    }
                    integrationEvents.createEvent();
                }
            }

            logger.LogDebug("Importing customers done.", "Elapsed: " + stopWatch.Elapsed, "Items: " + itemsProcessed, "Per item: " + (stopWatch.Elapsed.TotalMilliseconds / itemsProcessed) + " milliseconds");
        }
        private void ImportCustomerToTro(string messageId, XmlElement customerElement)
        {
            string collectionName = "customer";
            string identifier = customerElement["KUNNR"].InnerText;
            currentProcess_id = ObjectId.Empty;

            integrationEvents.CollectionName = collectionName;
            integrationEvents.Name = identifier;

            if (string.IsNullOrEmpty(identifier))
            {
                logger.LogError("Customer identifier empty");
                currentProcessFailed = true;
                currentProcessFailedReason += ";Customer identifier empty";
                return;
            }

            lock (mongoDatabase)
            {
                MongoCollection<BsonDocument> customerCollection = mongoDatabase.GetCollection(collectionName);

                // Find out if document exists
                MongoCursor cursor = customerCollection.Find(Query.EQ("identifier", identifier));

                BsonDocument customerDocument = null;
                BsonDocument customerDocumentOriginal = null;	//!!

                if (cursor.Count() == 0)
                {
                    customerDocument = new BsonDocument();
                    currentProcess_id = ObjectId.GenerateNewId();
                    customerDocument.SetExtended(collectionName, DBQuery.Id, currentProcess_id);
                    customerDocument.SetExtended(collectionName, "created", DateTime.UtcNow);
                }
                else if (cursor.Count() == 1)
                {
                    foreach (BsonDocument doc in cursor)
                        customerDocument = doc;

                    customerDocumentOriginal = (BsonDocument)customerDocument.Clone();

                    currentProcess_id = (ObjectId)customerDocument[DBQuery.Id];
                }
                else
                {
                    currentProcessFailedReason += ";More than one existing Customers found: ";
                    currentProcessFailed = true;
                    logger.LogError(currentProcessFailedReason, identifier, cursor.Count());
                    return;
                }

                ObjectId refCustomer_id = SetRefToCustomer(customerElement["KUNNR2"].InnerText, "AG");


                // Todo: Check for required values and other conditions here and throw an exception if not set properly

                SetcustomerData(collectionName, customerElement, customerDocument, refCustomer_id);
                bool dataChanged = true;
                if (cursor.Count() == 1)
                    if (customerDocumentOriginal.CompareTo(customerDocument) == 0)
                        dataChanged = false;
                    else
                        invalidatedCacheItems.Add(new DataTree(customerDocument[DBQuery.Id].ToString()));

                if (dataChanged)
                {
                    logger.LogDebug("Upsert Customer", customerElement["KUNNR"].InnerText, customerElement["CUST_NAME1"].InnerText);

                    customerDocument.SetExtended(collectionName, "modified", DateTime.UtcNow);
                    customerCollection.Save(customerDocument, WriteConcern.Acknowledged);
                    RefreshCache((ObjectId)customerDocument[DBQuery.Id], customerCollection.Name);
                }

                integrationEvents.UpdateInboundmessageSynced(customerDocument, collectionName, currentProcess_id, dataChanged);
            }
        }

        private ObjectId SetRefToCustomer(string identifier, string partnerrole)
        {
            ObjectId refCustomer_id;
            if (string.IsNullOrEmpty(identifier))
            {
                refCustomer_id = ObjectId.Empty;
                return refCustomer_id;
            }
            // set reference to customer
            MongoCollection<BsonDocument> refCustomerCollection = mongoDatabase.GetCollection("customer");
            MongoCursor cursorRefCustomer = refCustomerCollection.Find(Query.EQ("identifier", identifier));

            BsonDocument refCustomerDocument = null;

            if (cursorRefCustomer.Count() == 0)
            {
                refCustomerDocument = new BsonDocument();
                refCustomer_id = ObjectId.GenerateNewId();
                refCustomerDocument.SetExtended("customer", DBQuery.Id, refCustomer_id);
                refCustomerDocument.SetExtended("customer", "identifier", identifier);
                refCustomerDocument.SetExtended("customer", "name", "");
                refCustomerDocument.SetExtended("customer", "partnerrole", partnerrole); // Tilausasiakas
                refCustomerDocument.SetExtended("customer", "created", DateTime.UtcNow); // Tilausasiakas
                refCustomerDocument.SetExtended("customer", "disabled", true);
                refCustomerCollection.Save(refCustomerDocument, WriteConcern.Acknowledged);

                logger.LogTrace("Created disabled customer", refCustomer_id, identifier);
            }
            else if (cursorRefCustomer.Count() == 1)
            {
                foreach (BsonDocument doc in cursorRefCustomer)
                    refCustomerDocument = doc;

                refCustomer_id = (ObjectId)refCustomerDocument[DBQuery.Id];
            }
            else
            {
                refCustomer_id = ObjectId.Empty;
                currentProcessFailedReason += ";More than one existing referenced customers found: ";
                currentProcessFailed = true;
                logger.LogError(currentProcessFailedReason, identifier, cursorRefCustomer.Count());
            }

            return refCustomer_id;
        }

        private void SetcustomerData(string collectionName, XmlElement customerElement, BsonDocument customerDocument, ObjectId refCustomer_id)
        {
            logger.LogDebug("Setting Customer data.");

            customerDocument.SetExtended(collectionName, "identifier", customerElement["KUNNR"].InnerText);
            customerDocument.SetExtended(collectionName, "name", customerElement["CUST_NAME1"].InnerText);
            customerDocument.SetExtended(collectionName, "name2", customerElement["CUST_NAME2"].InnerText);
            customerDocument.SetExtended(collectionName, "city", customerElement["CITY"].InnerText);
            customerDocument.SetExtended(collectionName, "streetaddress", customerElement["STREET"].InnerText);
            customerDocument.SetExtended(collectionName, "partnerrole", customerElement["PARVW"].InnerText);
            // referenssit: KUNNR2 -> tilausasiakas
            SetDocumentAsArrayItem(customerDocument, collectionName, refCustomer_id, "ordercustomer");

            if (customerElement["LOEVM"].InnerText == "X")
                customerDocument.SetExtended(collectionName, "disabled", true);
            else
                customerDocument.SetExtended(collectionName, "disabled", false);

        }
        #endregion
        #endregion

        #region Project
        private void ImportProjectsToTro(string messageId, XmlDocument xmldoc, string messageType)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            int itemsProcessed = 0;

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmldoc.NameTable);
            nsmgr.AddNamespace("x", ProjectNameSpace);
            XmlNodeList nodes = xmldoc.DocumentElement.SelectNodes("//x:abap/x:values/ARRAY/ZTRO_PROJECT", nsmgr);
            foreach (XmlNode node in nodes)
            {
                // integrationevent:
                integrationEvents = new IntegrationEvents("SAP", messageId, messageType, (XmlElement)node, "in", logger, mongoDatabase);

                try
                {
                    itemsProcessed++;
                    ImportProjectToTro(messageId, (XmlElement)node);
                }
                catch (Exception ex)
                {
                    currentProcessFailed = true;
                    currentProcessFailedReason += ". " + ex.Message;
                    logger.LogError("Process Project:", ex);
                    logger.LogError("Processing Project element:", (XmlElement)node);
                }
                finally
                {
                    if (currentProcessFailed)
                    {
                        integrationEvents.Lastfailreason = currentProcessFailedReason;
                        integrationEvents.Failed = true;
                        integrationEvents.Status = "failed";
                    }
                    integrationEvents.createEvent();
                }
            }

            logger.LogDebug("Importing projects done.", "Elapsed: " + stopWatch.Elapsed, "Items: " + itemsProcessed, "Per item: " + (stopWatch.Elapsed.TotalMilliseconds / itemsProcessed) + " milliseconds");
        }
        private void ImportProjectToTro(string messageId, XmlElement projectElement)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            string collectionName = "project";
            string externalIdentifier = projectElement["PSPNR"].InnerText;
            currentProcess_id = ObjectId.Empty;

            string identifier = externalIdentifier;
            while (identifier.Substring(0, 1) == "0" && identifier.Length > 1)
                identifier = identifier.Substring(1);

            integrationEvents.CollectionName = collectionName;
            integrationEvents.Name = identifier;

            if (string.IsNullOrEmpty(identifier))
            {
                logger.LogError("Project identifier empty");
                currentProcessFailed = true;
                currentProcessFailedReason += ";Project identifier empty";
                return;
            }

            lock (mongoDatabase)
            {
                MongoCollection<BsonDocument> projectCollection = mongoDatabase.GetCollection(collectionName);

                // Find out if document exists
                MongoCursor cursor = projectCollection.Find(Query.EQ("identifier", identifier));

                BsonDocument projectDocument = null;
                BsonDocument projectDocumentOriginal = null;

                if (cursor.Count() == 0)
                {
                    projectDocument = new BsonDocument();
                    currentProcess_id = ObjectId.GenerateNewId();
                    projectDocument.SetExtended(collectionName, DBQuery.Id, currentProcess_id);
                    projectDocument.SetExtended(collectionName, "created", DateTime.UtcNow);
                }
                else if (cursor.Count() == 1)
                {
                    foreach (BsonDocument doc in cursor)
                        projectDocument = doc;

                    projectDocumentOriginal = (BsonDocument)projectDocument.Clone();
                    currentProcess_id = (ObjectId)projectDocument[DBQuery.Id];
                }
                else
                {
                    currentProcessFailedReason += ";More than one existing Project found: ";
                    currentProcessFailed = true;
                    logger.LogError(currentProcessFailedReason, identifier, cursor.Count());
                    return;
                }

                logger.LogDebug("Importing project, after init.", "Elapsed: " + stopWatch.Elapsed);
                ObjectId refProfitcenter_id = SetRefToProfitcenter(projectElement["PROFIT_CTR"].InnerText);
                logger.LogDebug("Importing project, after refProfitcenter_id.", "Elapsed: " + stopWatch.Elapsed);
                ObjectId refParentproject_id = SetRefToParentproject(projectElement["POSKI"].InnerText);
                logger.LogDebug("Importing project, after refParentproject_id.", "Elapsed: " + stopWatch.Elapsed);
                ObjectId refForeman_id = SetRefToUser(projectElement["PERNR"].InnerText);
                logger.LogDebug("Importing project, after refForeman_id.", "Elapsed: " + stopWatch.Elapsed);
                ObjectId refBA_id = SetRefToBA(projectElement["BUS_AREA"].InnerText);
                logger.LogDebug("Importing project, after refBA_id.", "Elapsed: " + stopWatch.Elapsed);
                ObjectId refFA_id = SetRefToFunctionalarea(projectElement["FUNC_AREA"].InnerText);
                logger.LogDebug("Importing project, after refFA_id.", "Elapsed: " + stopWatch.Elapsed);

                SetprojectData(collectionName, projectElement, projectDocument, refProfitcenter_id, refParentproject_id, refForeman_id, refBA_id, refFA_id, identifier, externalIdentifier);

                bool dataChanged = true;
                if (cursor.Count() == 1)
                    if (projectDocumentOriginal.CompareTo(projectDocument) == 0)
                        dataChanged = false;
                    else
                        invalidatedCacheItems.Add(new DataTree(projectDocument[DBQuery.Id].ToString()));

                if (dataChanged)
                {
                    logger.LogDebug("Upsert Project, Elapsed: " + stopWatch.Elapsed, identifier, projectElement["DESCRIPTION"].InnerText);

                    projectDocument.SetExtended(collectionName, "modified", DateTime.UtcNow);
                    projectCollection.Save(projectDocument, WriteConcern.Acknowledged);
                    RefreshCache((ObjectId)projectDocument[DBQuery.Id], projectCollection.Name);
                }
                else
                {
                    logger.LogDebug("Project data did not change, Elapsed: " + stopWatch.Elapsed, identifier, projectElement["DESCRIPTION"].InnerText);
                }

                integrationEvents.UpdateInboundmessageSynced(projectDocument, collectionName, currentProcess_id, dataChanged);
            }
        }

        private void SetprojectData(string collectionName, XmlElement projectElement, BsonDocument projectDocument, ObjectId refProfitcenter_id, ObjectId refParentproject_id, ObjectId refForeman_id, ObjectId refBA_id, ObjectId refFA_id, string identifier, string externalIdentifier)
        {
            logger.LogDebug("Setting Project data.");

            string troStatus = GetTroStatusFromAreStatus(projectElement["STAT"].InnerText);
            bool projectDisabled = false;
            if (projectElement["LOEVM"].InnerText == "X")
                projectDisabled = true;

            SetDocumentAsArrayItem(projectDocument, collectionName, refProfitcenter_id, "profitcenter");
            SetDocumentAsArrayItem(projectDocument, collectionName, refParentproject_id, "parentproject");
            SetDocumentAsArrayItem(projectDocument, collectionName, refForeman_id, "projectmanager");
            SetDocumentAsArrayItem(projectDocument, collectionName, refBA_id, "businessarea");
            SetDocumentAsArrayItem(projectDocument, collectionName, refFA_id, "functionalarea");

            projectDocument.SetExtended(collectionName, "identifier", identifier);
            projectDocument.SetExtended(collectionName, "externalidentifier", externalIdentifier);
            projectDocument.SetExtended(collectionName, "name", projectElement["DESCRIPTION"].InnerText);
            projectDocument.SetExtended(collectionName, "disabled", projectDisabled);
            projectDocument.SetExtended(collectionName, "posid", projectElement["POSID"].InnerText);
            projectDocument.SetExtended(collectionName, "poski", projectElement["POSKI"].InnerText);
            projectDocument.SetExtended(collectionName, "projecttype", "PROJECT");
            projectDocument.SetExtended(collectionName, "status", troStatus);

            return;
        }


        private ObjectId SetRefToBA(string identifier)
        {
            ObjectId refBA_id;
            if (string.IsNullOrEmpty(identifier))
            {
                refBA_id = ObjectId.Empty;
                return refBA_id;
            }
            // set reference to Business Area
            MongoCollection<BsonDocument> refBACollection = mongoDatabase.GetCollection("businessarea");
            MongoCursor cursorRefBA = refBACollection.Find(Query.EQ("identifier", identifier));

            BsonDocument refBADocument = null;

            if (cursorRefBA.Count() == 0)
            {
                refBADocument = new BsonDocument();
                refBA_id = ObjectId.GenerateNewId();
                refBADocument.SetExtended("businessarea", DBQuery.Id, refBA_id);
                refBADocument.SetExtended("businessarea", "identifier", identifier);
                refBADocument.SetExtended("businessarea", "name", "");
                refBADocument.SetExtended("businessarea", "disabled", true);
                refBADocument.SetExtended("businessarea", "created", DateTime.UtcNow);
                refBACollection.Save(refBADocument, WriteConcern.Acknowledged);
                logger.LogTrace("Created disabled Business Area", refBA_id, identifier);
            }
            else if (cursorRefBA.Count() == 1)
            {
                foreach (BsonDocument doc in cursorRefBA)
                    refBADocument = doc;

                refBA_id = (ObjectId)refBADocument[DBQuery.Id];
            }
            else
            {
                refBA_id = ObjectId.Empty;
                currentProcessFailedReason += ";More than one existing referenced BusinessAreas found: ";
                currentProcessFailed = true;
                logger.LogError(currentProcessFailedReason, identifier, cursorRefBA.Count());
            }

            return refBA_id;
        }

        private ObjectId SetRefToFunctionalarea(string identifier)
        {
            ObjectId refFunctionalArea_id;
            if (string.IsNullOrEmpty(identifier))
            {
                refFunctionalArea_id = ObjectId.Empty;
                return refFunctionalArea_id;
            }
            // set reference to customer
            MongoCollection<BsonDocument> refFACollection = mongoDatabase.GetCollection("functionalarea");
            MongoCursor cursorRefFA = refFACollection.Find(Query.EQ("identifier", identifier));

            BsonDocument refFADocument = null;

            if (cursorRefFA.Count() == 0)
            {
                refFADocument = new BsonDocument();
                refFunctionalArea_id = ObjectId.GenerateNewId();
                refFADocument.SetExtended("functionalarea", DBQuery.Id, refFunctionalArea_id);
                refFADocument.SetExtended("functionalarea", "identifier", identifier);
                refFADocument.SetExtended("functionalarea", "name", "");
                refFADocument.SetExtended("functionalarea", "disabled", true);
                refFADocument.SetExtended("functionalarea", "created", DateTime.UtcNow);
                refFACollection.Save(refFADocument, WriteConcern.Acknowledged);
                logger.LogTrace("Created disabled Functionalarea", refFunctionalArea_id, identifier);
            }
            else if (cursorRefFA.Count() == 1)
            {
                foreach (BsonDocument doc in cursorRefFA)
                    refFADocument = doc;

                refFunctionalArea_id = (ObjectId)refFADocument[DBQuery.Id];
            }
            else
            {
                refFunctionalArea_id = ObjectId.Empty;
                currentProcessFailedReason += ";More than one existing referenced Functionalareas found: ";
                currentProcessFailed = true;
                logger.LogError(currentProcessFailedReason, identifier, cursorRefFA.Count());
            }

            return refFunctionalArea_id;
        }


        private ObjectId SetRefToProfitcenter(string identifier)
        {
            ObjectId refProfitcenter_id;
            if (string.IsNullOrEmpty(identifier))
            {
                refProfitcenter_id = ObjectId.Empty;
                return refProfitcenter_id;
            }
            // set reference to customer
            MongoCollection<BsonDocument> refProfitcenterCollection = mongoDatabase.GetCollection("profitcenter");
            MongoCursor cursorRefProfitcenter = refProfitcenterCollection.Find(Query.EQ("identifier", identifier));

            BsonDocument refProfitcenterDocument = null;

            if (cursorRefProfitcenter.Count() == 0)
            {
                refProfitcenterDocument = new BsonDocument();
                refProfitcenter_id = ObjectId.GenerateNewId();
                refProfitcenterDocument.SetExtended("profitcenter", DBQuery.Id, refProfitcenter_id);
                refProfitcenterDocument.SetExtended("profitcenter", "identifier", identifier);
                refProfitcenterDocument.SetExtended("profitcenter", "name", "");
                refProfitcenterDocument.SetExtended("profitcenter", "disabled", true);
                refProfitcenterDocument.SetExtended("profitcenter", "created", DateTime.UtcNow);
                refProfitcenterCollection.Save(refProfitcenterDocument, WriteConcern.Acknowledged);
                logger.LogTrace("Created disabled profitcenter", refProfitcenter_id, identifier);
            }
            else if (cursorRefProfitcenter.Count() == 1)
            {
                foreach (BsonDocument doc in cursorRefProfitcenter)
                    refProfitcenterDocument = doc;

                refProfitcenter_id = (ObjectId)refProfitcenterDocument[DBQuery.Id];
            }
            else
            {
                refProfitcenter_id = ObjectId.Empty;
                currentProcessFailedReason += ";More than one existing referenced profitcenters found: ";
                currentProcessFailed = true;
                logger.LogError(currentProcessFailedReason, identifier, cursorRefProfitcenter.Count());
            }

            return refProfitcenter_id;
        }

        private ObjectId SetRefToParentproject(string poski)
        {
            // get parent project's poski and then _id
            ObjectId refProject_id = ObjectId.Empty;
            string parentPoski = getParentPoskiFromPoski(poski);

            if (!string.IsNullOrEmpty(parentPoski))
            {
                // set reference to project
                MongoCollection<BsonDocument> refProjectCollection = mongoDatabase.GetCollection("project");
                MongoCursor cursorRefProject = refProjectCollection.Find(Query.And(Query.EQ("poski", parentPoski), Query.EQ("projecttype", "PROJECT")));
                BsonDocument refProjectDocument = null;

                if (cursorRefProject.Count() == 0)
                { // we don't know parent projects identifier - so we can not create a disabled project here
                    logger.LogTrace("Cannot create disabled parent project", poski);
                }
                else if (cursorRefProject.Count() == 1)
                {
                    foreach (BsonDocument doc in cursorRefProject)
                        refProjectDocument = doc;

                    refProject_id = (ObjectId)refProjectDocument[DBQuery.Id];
                }
            }
            return refProject_id;
        }

        string getParentPoskiFromPoski(string poski)
        {
            string parentPoski = string.Empty;
            if (!string.IsNullOrEmpty(poski))
            {
                int dashpos = poski.LastIndexOf('-');
                if (dashpos > 0)
                {
                    parentPoski = poski.Substring(0, dashpos);
                }
            }
            return parentPoski;
        }
        #endregion
        
        #region Orders
        private void ImportOrdersToTro(string messageId, XmlDocument xmldoc, string messageType)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            int itemsProcessed = 0;

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmldoc.NameTable);
            nsmgr.AddNamespace("x", OrderNameSpace);
            XmlNodeList nodes = xmldoc.DocumentElement.SelectNodes("//x:abap/x:values/ARRAY/ZTRO_SERVICEORDER", nsmgr);
            foreach (XmlNode node in nodes)
            {
                // integrationevent:
                integrationEvents = new IntegrationEvents("SAP", messageId, messageType, (XmlElement)node, "in", logger, mongoDatabase);

                try
                {
                    itemsProcessed++;
                    ImportOrderToTro(messageId, (XmlElement)node);
                }
                catch (Exception ex)
                {
                    currentProcessFailed = true;
                    currentProcessFailedReason += ". " + ex.Message;
                    logger.LogError("Process Order:", ex);
                    logger.LogError("Processing Order element:", (XmlElement)node);
                }
                finally
                {
                    if (currentProcessFailed)
                    {
                        integrationEvents.Lastfailreason = currentProcessFailedReason;
                        integrationEvents.Failed = true;
                        integrationEvents.Status = "failed";
                    }
                    integrationEvents.createEvent();
                }
            }

            logger.LogDebug("Importing orders done.", "Elapsed: " + stopWatch.Elapsed, "Items: " + itemsProcessed, "Per item: " + (stopWatch.Elapsed.TotalMilliseconds / itemsProcessed) + " milliseconds");
        }
        private void ImportOrderToTro(string messageId, XmlElement orderElement)
        {
            string collectionName = "project";
            string externalIdentifier = orderElement["ORDERID"].InnerText;
            string identifier = externalIdentifier;
            currentProcess_id = ObjectId.Empty;

            while (identifier.Substring(0, 1) == "0" && identifier.Length > 1)
                identifier = identifier.Substring(1);

            integrationEvents.CollectionName = collectionName;
            integrationEvents.Name = identifier;

            if (string.IsNullOrEmpty(identifier))
            {
                logger.LogError("Order identifier empty");
                currentProcessFailed = true;
                currentProcessFailedReason += ";Order identifier empty";
                return;
            }

            lock (mongoDatabase)
            {
                MongoCollection<BsonDocument> orderCollection = mongoDatabase.GetCollection(collectionName);

                // Find out if document exists
                MongoCursor cursor = orderCollection.Find(Query.EQ("identifier", identifier));

                BsonDocument orderDocument = null;
                BsonDocument orderDocumentOriginal = null;

                if (cursor.Count() == 0)
                {
                    orderDocument = new BsonDocument();
                    currentProcess_id = ObjectId.GenerateNewId();
                    orderDocument.SetExtended(collectionName, DBQuery.Id, currentProcess_id);
                    orderDocument.SetExtended(collectionName, "created", DateTime.UtcNow);
                    if (!(string.IsNullOrEmpty(orderElement["ENTER_DATE"].InnerText) || string.IsNullOrEmpty(orderElement["ENTER_TIME"].InnerText) || orderElement["ENTER_DATE"].InnerText.Substring(0, 4) == "0000"))
                        orderDocument.SetExtended(collectionName, "created", TroIntegrationCommon.IntegrationHelpers.GetDateTimeFromSAPString(orderElement["ENTER_DATE"].InnerText + "T" + orderElement["ENTER_TIME"].InnerText));
                    orderDocument.SetExtended(collectionName, "disabled", false);
                }
                else if (cursor.Count() == 1)
                {
                    foreach (BsonDocument doc in cursor)
                        orderDocument = doc;

                    orderDocumentOriginal = (BsonDocument)orderDocument.Clone();
                    currentProcess_id = (ObjectId)orderDocument[DBQuery.Id];
                }
                else
                {
                    currentProcessFailedReason += ";More than one existing Order found: ";
                    currentProcessFailed = true;
                    logger.LogError(currentProcessFailedReason, identifier, cursor.Count());
                    return;
                }

                string projectIdentifier = orderElement["WBS_ELEM"].InnerText;
                while (projectIdentifier.Substring(0, 1) == "0" && projectIdentifier.Length > 1)
                    projectIdentifier = projectIdentifier.Substring(1);
                RefToProject refToProject = new RefToProject(mongoDatabase, logger, projectIdentifier);
                ObjectId refProject_id = (ObjectId)refToProject.projectDocument["_id"];
                string poski = string.Empty; // "Projektin tunniste"
                if (refToProject.projectDocument.Contains("poski"))
                    poski = (string)refToProject.projectDocument["poski"];
                ObjectId refForeman_id = SetRefToUser(orderElement["FOREMAN"].InnerText);
                ObjectId refProjectcategory_id = SetRefToProjectcategory(orderElement["ACTTYPE"].InnerText);
                ObjectId refBA_id = SetRefToBA(orderElement["BUS_AREA"].InnerText);
                ObjectId refFA_id = SetRefToFunctionalarea(orderElement["FUNC_AREA"].InnerText);
                ObjectId refProfitcenter_id = SetRefToProfitcenter(orderElement["PROFIT_CTR"].InnerText);
                // Todo: Check for required values and other conditions here and throw an exception if not set properly

                string oldStatus = (string)orderDocument.GetValue("status", "");
                SetorderData(collectionName, orderElement, orderDocument, refProject_id, refForeman_id, refProjectcategory_id, refBA_id, refFA_id, refProfitcenter_id, externalIdentifier, identifier, poski);

                bool dataChanged = true;
                if (cursor.Count() == 1)
                    if (orderDocumentOriginal.CompareTo(orderDocument) == 0)
                        dataChanged = false;
                    else
                        invalidatedCacheItems.Add(new DataTree(orderDocument[DBQuery.Id].ToString()));

                if (dataChanged)
                {
                    logger.LogDebug("Upsert Project (Order)", identifier, orderElement["DESCRIPTION"].InnerText);
                    orderDocument.SetExtended(collectionName, "modified", DateTime.UtcNow);
                    orderCollection.Save(orderDocument, WriteConcern.Acknowledged);

                    RefreshCache((ObjectId)orderDocument[DBQuery.Id], orderCollection.Name);

                    if ((string)orderDocument.GetValue("status", "") == IntegrationConstants.ProjectStatus_Done && oldStatus != IntegrationConstants.ProjectStatus_Done && cursor.Count() == 1)
                        UpdateOrderAllocationStatuses((ObjectId)orderDocument[DBQuery.Id], IntegrationConstants.ProjectStatus_Done);
                }
                else
                {
                    logger.LogDebug("Project (Order) data did not change", identifier, orderElement["DESCRIPTION"].InnerText);
                }

                integrationEvents.UpdateInboundmessageSynced(orderDocument, collectionName, currentProcess_id, dataChanged);
            }
        }

        private void UpdateOrderAllocationStatuses(ObjectId orderObjectId, string status)
        {
            lock (mongoDatabase)
            {
                MongoCollection<BsonDocument> allocationEntryCollection = mongoDatabase.GetCollection("allocationentry");
                MongoCursor cursor = allocationEntryCollection.Find(Query.And(Query.EQ("project", orderObjectId), Query.NE("status", status)));
                BsonDocument allocationEntryDocument = null;

                foreach (BsonDocument doc in cursor)
                {
                    allocationEntryDocument = doc;
                    allocationEntryDocument.SetExtended("allocationentry", "status", status);
                    allocationEntryCollection.Save(allocationEntryDocument, WriteConcern.Acknowledged);

                    logger.LogDebug("Updated allocationentry status=Done", allocationEntryDocument[DBQuery.Id].ToString());
                }
            }
        }
        private void SetorderData(string collectionName, XmlElement orderElement, BsonDocument orderDocument, ObjectId refProject_id, ObjectId refForeman_id, ObjectId refProjectcategory_id, ObjectId refBA_id, ObjectId refFA_id, ObjectId refProfitcenter_id, string externalIdentifier, string identifier, string poski)
        {
            logger.LogDebug("Setting Order data.");

            orderDocument.SetExtended(collectionName, "externalidentifier", externalIdentifier);
            orderDocument.SetExtended(collectionName, "identifier", identifier); // externalIdentifier without leading zeroes
            orderDocument.SetExtended(collectionName, "name", orderElement["DESCRIPTION"].InnerText);
            orderDocument.SetExtended(collectionName, "purchasenumber", orderElement["PURCH_NO_C"].InnerText);
            orderDocument.SetExtended(collectionName, "custname1", orderElement["CUST_NAME1"].InnerText);
            orderDocument.SetExtended(collectionName, "custname2", orderElement["CUST_NAME2"].InnerText);
            orderDocument.SetExtended(collectionName, "streetaddress", orderElement["STREET"].InnerText);
            orderDocument.SetExtended(collectionName, "postalcode", orderElement["POST_CODE1"].InnerText);
            orderDocument.SetExtended(collectionName, "city", orderElement["CITY"].InnerText);
            orderDocument.SetExtended(collectionName, "poski", poski);
            //orderDocument.Set("projectstart", orderElement["ENTER_DATE"].InnerText);
            orderDocument.SetExtended(collectionName, "status", GetTroStatusFromAreStatus(orderElement["STATUS"].InnerText));
            orderDocument.SetExtended(collectionName, "arestate", orderElement["STATUS"].InnerText);

            SetDocumentAsArrayItem(orderDocument, collectionName, refProject_id, "parentproject");
            SetDocumentAsArrayItem(orderDocument, collectionName, refForeman_id, "projectmanager");
            SetDocumentAsArrayItem(orderDocument, collectionName, refProjectcategory_id, "projectcategory");
            SetDocumentAsArrayItem(orderDocument, collectionName, refBA_id, "businessarea");
            SetDocumentAsArrayItem(orderDocument, collectionName, refFA_id, "functionalarea");
            SetDocumentAsArrayItem(orderDocument, collectionName, refProfitcenter_id, "profitcenter");

            if (string.IsNullOrEmpty(orderElement["DUEDATE"].InnerText) || string.IsNullOrEmpty(orderElement["DUETIME"].InnerText) || orderElement["DUEDATE"].InnerText.Substring(0, 4) == "0000")
                orderDocument.Remove("duedate");
            else
                orderDocument.SetExtended(collectionName, "duedate", TroIntegrationCommon.IntegrationHelpers.GetDateTimeFromSAPString(orderElement["DUEDATE"].InnerText + "T" + orderElement["DUETIME"].InnerText));

            orderDocument.SetExtended(collectionName, "projecttype", orderElement["ORDER_TYPE"].InnerText); // ?
            orderDocument.SetExtended(collectionName, "arestatus", orderElement["STATUS"].InnerText); // ?
            try
            {
                orderDocument.SetExtended(collectionName, "contactpersonname", orderElement["ZCONTACT"].InnerText); // ?
            }
            catch { }
            orderDocument.SetExtended(collectionName, "disabled", false);
        }

        private string GetTroStatusFromAreStatus(string SAPStatus)
        {
            //	Tro project statuses: Unallocated, In progress, Done, Subcontract
            // Are statuses: 
            //  E0015   4 - Alihankinta
            //  E0016   1 - Resursoimaton
            //  E0017   2 - Kiinnittämätön
            //  E0018   3 - Työn alla
            //  E0019   5 - Työ valmis
            //  E0001   AUKI
            //  E0002   TEPÄ
            //  E0003-E0013 Lukittu 2004 - Lukittu 2014

            string TroStatus = IntegrationConstants.ProjectStatus_Done; // default

            if (SAPStatus == "E0016" || SAPStatus == "E0017")
                TroStatus = IntegrationConstants.ProjectStatus_Unallocated;
            else if (SAPStatus == "E0018" || SAPStatus == "E0001")
                TroStatus = IntegrationConstants.ProjectStatus_InProgress;
            else if (SAPStatus == "E0015")
                TroStatus = IntegrationConstants.ProjectStatus_Subcontract;

            return TroStatus;
        }

        private ObjectId SetRefToProjectcategory(string identifier)
        {
            ObjectId refPC_id;
            if (string.IsNullOrEmpty(identifier))
            {
                refPC_id = ObjectId.Empty;
                return refPC_id;
            }
            // set reference to Business Area
            MongoCollection<BsonDocument> refPCCollection = mongoDatabase.GetCollection("projectcategory");
            MongoCursor cursorRefPC = refPCCollection.Find(Query.EQ("identifier", identifier));

            BsonDocument refPCDocument = null;

            if (cursorRefPC.Count() == 0)
            {
                refPCDocument = new BsonDocument();
                refPC_id = ObjectId.GenerateNewId();
                refPCDocument.SetExtended("projectcategory", DBQuery.Id, refPC_id);
                refPCDocument.SetExtended("projectcategory", "identifier", identifier);
                refPCDocument.SetExtended("projectcategory", "name", "");
                refPCDocument.SetExtended("projectcategory", "disabled", true);
                refPCDocument.SetExtended("projectcategory", "created", DateTime.UtcNow);
                refPCCollection.Save(refPCDocument, WriteConcern.Acknowledged);
                logger.LogTrace("Created disabled Project Category", refPC_id, identifier);
            }
            else if (cursorRefPC.Count() == 1)
            {
                foreach (BsonDocument doc in cursorRefPC)
                    refPCDocument = doc;

                refPC_id = (ObjectId)refPCDocument[DBQuery.Id];
            }
            else
            {
                refPC_id = ObjectId.Empty;
                currentProcessFailedReason += ";More than one existing referenced Project Categories found: ";
                currentProcessFailed = true;
                logger.LogError(currentProcessFailedReason, identifier, cursorRefPC.Count());
            }

            return refPC_id;
        }

        private ObjectId SetRefToUser(string identifier)
        {
            ObjectId refUser_id;
            if (string.IsNullOrEmpty(identifier))
            {
                refUser_id = ObjectId.Empty;
                return refUser_id;
            }
            // set reference 
            MongoCollection<BsonDocument> refUserCollection = mongoDatabase.GetCollection("user");
            MongoCursor cursorRefUser = refUserCollection.Find(Query.EQ("identifier", identifier));

            BsonDocument refUserDocument = null;

            if (cursorRefUser.Count() == 0)
            {
                refUserDocument = new BsonDocument();
                refUser_id = ObjectId.GenerateNewId();
                refUserDocument.SetExtended("user", DBQuery.Id, refUser_id);
                refUserDocument.SetExtended("user", "identifier", identifier);
                refUserDocument.SetExtended("user", "firstname", identifier);
                refUserDocument.SetExtended("user", "lastname", "");
                refUserDocument.SetExtended("user", "email", "");
                refUserDocument.SetExtended("user", "created", DateTime.UtcNow);
                refUserDocument.SetExtended("user", "disabled", true);
                refUserCollection.Save(refUserDocument, WriteConcern.Acknowledged);
                logger.LogTrace("Created disabled user", refUser_id, identifier);
            }
            else if (cursorRefUser.Count() == 1)
            {
                foreach (BsonDocument doc in cursorRefUser)
                    refUserDocument = doc;

                refUser_id = (ObjectId)refUserDocument[DBQuery.Id];
            }
            else
            {
                refUser_id = ObjectId.Empty;
                currentProcessFailedReason += ";More than one existing referenced users found: ";
                currentProcessFailed = true;
                logger.LogError(currentProcessFailedReason, identifier, cursorRefUser.Count());
            }

            return refUser_id;
        }

        #endregion
        
        #region OrderPartners
        private void ImportOrderPartnersToTro(string messageId, XmlDocument xmldoc, string messageType)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            int itemsProcessed = 0;
            int itemsProcessedc = 0;
            int lockTime = 0;
            logTime1 = 0; logTime2 = 0; logTime3 = 0; logTime4 = 0; logTime5 = 0; logTime6 = 0; logTime7 = 0;
            logTime1c = 0; logTime2c = 0; logTime3c = 0; logTime4c = 0; logTime5c = 0; logTime6c = 0;
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmldoc.NameTable);
            nsmgr.AddNamespace("x", OrderPartnerNameSpace);
            XmlNodeList nodes = xmldoc.DocumentElement.SelectNodes("//x:abap/x:values/ARRAY/ZTRO_PARTNER", nsmgr);
            foreach (XmlNode node in nodes)
            {
                // integrationevent:
                integrationEvents = new IntegrationEvents("SAP", messageId, messageType, (XmlElement)node, "in", logger, mongoDatabase);

                try
                {
                    string partnerRole = node["PARTNER_ROLE"].InnerText;
                    if (partnerRole == "ZE" || partnerRole == "ZP" || partnerRole == "ZV")
                    {
                        ImportOrderPartnerToTro(messageId, (XmlElement)node, ref lockTime);
                        itemsProcessed++;
                    }
                    else if (partnerRole == "WE")
                    {
                        ImportProjectCustomerToTro(messageId, (XmlElement)node);
                        itemsProcessedc++;
                    }
                }
                catch (Exception ex)
                {
                    currentProcessFailed = true;
                    currentProcessFailedReason += ". " + ex.Message;
                    logger.LogError("Process OrderPartner:", ex);
                }
                finally
                {
                    if (currentProcessFailed)
                    {
                        integrationEvents.Lastfailreason = currentProcessFailedReason;
                        integrationEvents.Failed = true;
                        integrationEvents.Status = "failed";
                    }
                    integrationEvents.createEvent();
                }
            }
            if (itemsProcessed > 0) {
                logger.LogInfo("Importing order partners done.", "Elapsed: " + stopWatch.Elapsed, "Items: " + itemsProcessed + itemsProcessedc, "Per item: " + (stopWatch.Elapsed.TotalMilliseconds / itemsProcessed) + " milliseconds", "DB lock total: " + (lockTime) + " ms ", "DB lock avg: " + (lockTime / itemsProcessed) + " ms");
                logger.LogInfo("*** Imported allocationentries", itemsProcessed, logTime1, logTime2, logTime3, logTime4, logTime5, logTime6, logTime7);
                logger.LogInfo("*** Imported customerinfos", itemsProcessedc, logTime1c, logTime2c, logTime3c, logTime4c, logTime5c, logTime6c);
            }
        }
        private void ImportOrderPartnerToTro(string messageId, XmlElement orderPartnerElement, ref int lockTime)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            string partnerRole = orderPartnerElement["PARTNER_ROLE"].InnerText;
            string partnerStatus = "";
            if (partnerRole == "ZE")
                partnerStatus = "Not started";  // ZE=Asentaja
            else if (partnerRole == "ZP")
                partnerStatus = IntegrationConstants.ProjectStatus_InProgress;  // ZP=TYÖN ALLA
            else if (partnerRole == "ZV")
                partnerStatus = IntegrationConstants.ProjectStatus_Done;         // ZV=VALMIS - Asentaja
            else
            {
                integrationEvents.DataChanged = false;
                return;
            }
            string collectionName = "allocationentry";
            string orderId = orderPartnerElement["ORDERID"].InnerText;
            while (orderId.Substring(0, 1) == "0" && orderId.Length > 1)
                orderId = orderId.Substring(1);

            string parNr = orderPartnerElement["PARNR"].InnerText;
            string identifier = orderId + " " + parNr;
            currentProcess_id = ObjectId.Empty;

            integrationEvents.CollectionName = collectionName;
            integrationEvents.Name = identifier;

            if (string.IsNullOrEmpty(identifier))
            {
                logger.LogError("Orderpartner identifier empty");
                currentProcessFailed = true;
                currentProcessFailedReason += ";Orderpartner identifier empty";
                return;
            }

            // Create document to entryupdate_erp_allocationentry, so we know that this project/person exists in SAP
            TroIntegrationCommon.IntegrationHelpers.InsertErpOrderPartner(orderId, parNr, mongoDatabase, logger);

            RefToProject refToProject = new RefToProject(mongoDatabase, logger, orderId);
            ObjectId project_id = (ObjectId)refToProject.projectDocument["_id"];

            logTime1 += stopWatch.Elapsed.TotalMilliseconds;
            stopWatch.Restart();

            // If Order is 'Done', do not update allocations:
            string orderStatus = (string)refToProject.projectDocument.GetValue("status", "");
            if (orderStatus == IntegrationConstants.ProjectStatus_Done)
            {
                logger.LogError("Orderpartner data not updated because order status is 'Done'", identifier);
                return;
            }

            ObjectId refUser_id = SetRefToUser(parNr);

            BsonDocument orderPartnerDocument = null;
            var stopWatch2 = new Stopwatch();
            lock (mongoDatabase)
            {
                stopWatch2.Start();
                logTime2 += stopWatch.Elapsed.TotalMilliseconds;
                stopWatch.Restart();
                MongoCollection<BsonDocument> orderPartnerCollection = mongoDatabase.GetCollection(collectionName);
                // fetch untimed allocation entries of this project and this user:
                MongoCursor cursor = orderPartnerCollection.Find(Query.And(Query.EQ("project", project_id), Query.EQ("user", refUser_id)));

                if (cursor.Count() == 0)
                {
                    orderPartnerDocument = new BsonDocument();
                    currentProcess_id = ObjectId.GenerateNewId();
                    orderPartnerDocument.SetExtended(collectionName, DBQuery.Id, currentProcess_id);
                    orderPartnerDocument.SetExtended(collectionName, "created", DateTime.UtcNow);

                    logger.LogDebug("Insert Orderpartner", identifier);
                }
                else // update, if there are allocations only in one state
                {
                    bool doUpdateAllocation = true;
                    string oldPartnerStatus = "";
                    string thisPartnerStatus = "";
                    foreach (BsonDocument doc in cursor)
                    {
                        thisPartnerStatus = Convert.ToString(doc.GetValue("status", ""));
                        if (oldPartnerStatus == "" || oldPartnerStatus == thisPartnerStatus)
                        {
                            // prefer non-timed allocations to be updated from SAP:
                            if (oldPartnerStatus == "" || (Convert.ToString(doc.GetValue("starttimestamp", "")) == "" && Convert.ToString(doc.GetValue("endtimestamp", "")) == ""))
                                orderPartnerDocument = doc;
                            oldPartnerStatus = thisPartnerStatus;
                        }
                        else
                        {
                            doUpdateAllocation = false;
                        }
                    }
                    if (doUpdateAllocation)
                    {
                        invalidatedCacheItems.Add(new DataTree(orderPartnerDocument[DBQuery.Id].ToString()));
                        currentProcess_id = (ObjectId)orderPartnerDocument[DBQuery.Id];

                        logger.LogDebug("Update Orderpartner", identifier);
                    }
                    else
                    {
                        integrationEvents.UpdateInboundmessageSynced(orderPartnerDocument, collectionName, currentProcess_id, true);
                        integrationEvents.ImportedOrExported = false;
                        logger.LogTrace("Not updating order partner because more than one existing Orderpartner statuses found", identifier);
                        return;
                    }
                }

                logTime3 += stopWatch.Elapsed.TotalMilliseconds;
                stopWatch.Restart();
                SetorderPartnerData(collectionName, orderPartnerElement, orderPartnerDocument, refToProject, refUser_id, identifier, partnerStatus);

                logTime4 += stopWatch.Elapsed.TotalMilliseconds;
                stopWatch.Restart();
                orderPartnerCollection.Save(orderPartnerDocument, WriteConcern.Acknowledged);

                logTime5 += stopWatch.Elapsed.TotalMilliseconds;
                stopWatch.Restart();
                RefreshCache((ObjectId)orderPartnerDocument[DBQuery.Id], orderPartnerCollection.Name);
                logTime6 += stopWatch.Elapsed.TotalMilliseconds;
                stopWatch.Restart();
            }
            stopWatch.Start();
            integrationEvents.UpdateInboundmessageSynced(orderPartnerDocument, collectionName, currentProcess_id, true);
            logTime7 += stopWatch.Elapsed.TotalMilliseconds;
            this.stopping.WaitOne(Math.Max((int)stopWatch2.Elapsed.TotalMilliseconds, 50));
            lockTime += (int)stopWatch2.Elapsed.TotalMilliseconds;
        }

        private void SetorderPartnerData(string collectionName, XmlElement orderPartnerElement, BsonDocument orderPartnerDocument, RefToProject refToProject, ObjectId refUser_id, string identifier, string partnerStatus)
        {
            logger.LogDebug("Setting Orderpartner data.");

            orderPartnerDocument.SetExtended(collectionName, "identifier", identifier);
            orderPartnerDocument.SetExtended(collectionName, "status", partnerStatus);
            orderPartnerDocument.SetExtended(collectionName, "modified", DateTime.UtcNow);
            orderPartnerDocument.SetExtended(collectionName, "disabled", false);
            if (refToProject.projectDocument.Contains("duedate"))
                orderPartnerDocument.SetExtended(collectionName, "endtimestamp", refToProject.projectDocument["duedate"]);

            SetDocumentAsArrayItem(orderPartnerDocument, collectionName, (ObjectId)refToProject.projectDocument["_id"], "project");
            SetDocumentAsArrayItem(orderPartnerDocument, collectionName, refUser_id, "user");
        }

        private void ImportProjectCustomerToTro(string messageId, XmlElement orderPartnerElement)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            string collectionName = "project";
            string identifier = orderPartnerElement["ORDERID"].InnerText;
            while (identifier.Substring(0, 1) == "0" && identifier.Length > 1)
                identifier = identifier.Substring(1);
            currentProcess_id = ObjectId.Empty;

            integrationEvents.CollectionName = collectionName;
            integrationEvents.Name = identifier;

            if (string.IsNullOrEmpty(identifier))
            {
                logger.LogError("Orderpartner (customer) identifier empty");
                currentProcessFailed = true;
                currentProcessFailedReason += ";Orderpartner (customer) identifier empty";
                return;
            }

            lock (mongoDatabase)
            {
                logTime1c += stopWatch.Elapsed.TotalMilliseconds;
                stopWatch.Restart();

                MongoCollection<BsonDocument> projectCollection = mongoDatabase.GetCollection(collectionName);
                MongoCursor cursor = projectCollection.Find(Query.EQ("identifier", identifier));

                BsonDocument projectDocument = null;

                if (cursor.Count() == 0)
                {
                    projectDocument = new BsonDocument();
                    currentProcess_id = ObjectId.GenerateNewId();
                    projectDocument.SetExtended(collectionName, DBQuery.Id, currentProcess_id);
                    projectDocument.SetExtended(collectionName, "identifier", identifier);
                    projectDocument.SetExtended(collectionName, "disabled", true);
                    projectDocument.SetExtended(collectionName, "created", DateTime.UtcNow);
                    string externalidentifier = identifier;
                    while (externalidentifier.Length < 12)
                    {
                        externalidentifier = "0" + externalidentifier;
                    }
                    projectDocument.SetExtended(collectionName, "externalidentifier", externalidentifier);

                    logger.LogTrace("Created disabled project", identifier);
                }
                else if (cursor.Count() == 1)
                {
                    foreach (BsonDocument doc in cursor)
                        projectDocument = doc;

                    invalidatedCacheItems.Add(new DataTree(projectDocument[DBQuery.Id].ToString()));
                    currentProcess_id = (ObjectId)projectDocument[DBQuery.Id];
                }
                else
                {
                    currentProcessFailedReason += ";More than one existing project found: ";
                    currentProcessFailed = true;
                    logger.LogError(currentProcessFailedReason, identifier, cursor.Count());
                    return;
                }

                logTime2c += stopWatch.Elapsed.TotalMilliseconds;
                stopWatch.Restart();
                ObjectId refCustomer_id = SetRefToCustomer(orderPartnerElement["PARNR"].InnerText, orderPartnerElement["PARTNER_ROLE"].InnerText);
                SetDocumentAsArrayItem(projectDocument, collectionName, refCustomer_id, "customer");

                projectCollection.Save(projectDocument, WriteConcern.Acknowledged);
                logTime3c += stopWatch.Elapsed.TotalMilliseconds;
                stopWatch.Restart();

                RefreshCache((ObjectId)projectDocument[DBQuery.Id], projectCollection.Name);
                logTime4c += stopWatch.Elapsed.TotalMilliseconds;
                stopWatch.Restart();
                logTime5c += stopWatch.Elapsed.TotalMilliseconds;
                stopWatch.Restart();

                integrationEvents.UpdateInboundmessageSynced(projectDocument, collectionName, currentProcess_id, true);
                logTime6c += stopWatch.Elapsed.TotalMilliseconds;
            }
        }
        #endregion
        
        #region Workers
        // all employers from Are Ax
        private void ImportAllAxEmplTableToTro(string messageId, XmlDocument xmldoc, string messageType)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            int itemsProcessed = 0;

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmldoc.NameTable);
            XmlNodeList nodes = xmldoc.DocumentElement.SelectNodes("//EmplTable/EmplTable", nsmgr);
            foreach (XmlNode node in nodes)
            {
                // integrationevent:
                integrationEvents = new IntegrationEvents("HR", messageId, messageType, (XmlElement)node, "in", logger, mongoDatabase);
                try
                {
                    logger.LogDebug("Process all Employers");
                    ImportAxWorkerToTro(messageId, (XmlElement)node, "");
                }
                catch (Exception ex)
                {
                    currentProcessFailed = true;
                    currentProcessFailedReason += ". " + ex.Message;
                    logger.LogError("Process all Employee:", ex);
                    logger.LogError("Processing all Employee element:", (XmlElement)node);
                }
                finally
                {
                    if (currentProcessFailed)
                    {
                        integrationEvents.Lastfailreason = currentProcessFailedReason;
                        integrationEvents.Failed = true;
                        integrationEvents.Status = "failed";
                    }
                    integrationEvents.createEvent();
                }
            }

            logger.LogDebug("Importing all employee done.", "Elapsed: " + stopWatch.Elapsed, "Items: " + itemsProcessed, "Per item: " + (stopWatch.Elapsed.TotalMilliseconds / itemsProcessed) + " milliseconds");
        }
        private void ImportEmplTableToTro(string messageId, XmlDocument xmldoc, string messageType)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            int itemsProcessed = 0;

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmldoc.NameTable);
            nsmgr.AddNamespace("x", WorkerNameSpaceSQL);
            XmlNodeList nodes = xmldoc.DocumentElement.SelectNodes("//x:EmplTable", nsmgr);
            foreach (XmlNode node in nodes)
            {
                // integrationevent:
                integrationEvents = new IntegrationEvents("HR", messageId, messageType, (XmlElement)node, "in", logger, mongoDatabase);
                try
                {
                    logger.LogDebug("Process Employee");
                    ImportAxWorkerToTro(messageId, (XmlElement)node, "ns0:");
                }
                catch (Exception ex)
                {
                    currentProcessFailed = true;
                    currentProcessFailedReason += ". " + ex.Message;
                    logger.LogError("Process Employee:", ex);
                    logger.LogError("Processing Employee element:", (XmlElement)node);
                }
                finally
                {
                    if (currentProcessFailed)
                    {
                        integrationEvents.Lastfailreason = currentProcessFailedReason;
                        integrationEvents.Failed = true;
                        integrationEvents.Status = "failed";
                    }
                    integrationEvents.createEvent();
                }
            }

            logger.LogDebug("Importing employees done.", "Elapsed: " + stopWatch.Elapsed, "Items: " + itemsProcessed, "Per item: " + (stopWatch.Elapsed.TotalMilliseconds / itemsProcessed) + " milliseconds");
        }
        private void ImportAxWorkerToTro(string messageId, XmlElement userElement, string namespaceName)
        {
            string collectionName = "user";
            string identifier = userElement[namespaceName + "SfiEmplNum"].InnerText;
            currentProcess_id = ObjectId.Empty;

            integrationEvents.CollectionName = collectionName;
            integrationEvents.Name = identifier;

            if (string.IsNullOrEmpty(identifier))
            {
                logger.LogError("User identifier empty");
                currentProcessFailed = true;
                currentProcessFailedReason += ";User identifier empty";
                return;
            }

            // SAP identifier is left padded by "0"
            identifier = identifier.PadLeft(8, '0');

            logger.LogDebug("Fetched User data", identifier);

            lock (mongoDatabase)
            {
                MongoCollection<BsonDocument> userCollection = mongoDatabase.GetCollection(collectionName);

                // Find out if document exists
                MongoCursor cursor = userCollection.Find(Query.EQ("identifier", identifier));

                BsonDocument userDocument = null;
                BsonDocument userDocumentOriginal = null;

                if (cursor.Count() == 0)
                {
                    userDocument = new BsonDocument();
                    currentProcess_id = ObjectId.GenerateNewId();
                    userDocument.SetExtended(collectionName, DBQuery.Id, currentProcess_id);
                    userDocument.SetExtended(collectionName, "created", DateTime.UtcNow);
                }
                else if (cursor.Count() == 1)
                {
                    foreach (BsonDocument doc in cursor)
                        userDocument = doc;

                    userDocumentOriginal = (BsonDocument)userDocument.Clone();
                    currentProcess_id = (ObjectId)userDocument[DBQuery.Id];
                }
                else
                {
                    currentProcessFailedReason += ";More than one existing user found: ";
                    currentProcessFailed = true;
                    logger.LogError(currentProcessFailedReason, identifier, cursor.Count());
                    return;
                }

                string clacontractString = userElement[namespaceName + "SFI_CollectiveLaborAgreement"]?.InnerText;
                if (string.IsNullOrEmpty(clacontractString))
                    clacontractString = userElement[namespaceName + "TES"]?.InnerText;
                // TES codes in Tro are of fixed length of 3, for example "001":
                clacontractString = clacontractString.PadLeft(2, '0');

                ObjectId clacontract = SetRefToClacontract(clacontractString);
                ObjectId refBA_id = SetRefToBA(userElement[namespaceName + "LTA"].InnerText);
                ObjectId refFA_id = SetRefToFunctionalarea(userElement[namespaceName + "Toimintoalue"].InnerText);
                ObjectId refProfitcenter_id = SetRefToProfitcenter(userElement[namespaceName + "Tulosyksikko"].InnerText);
                ObjectId refForeman_id = SetRefToUser(userElement[namespaceName + "FOREMAN"].InnerText);
                string paymentGroupString = userElement[namespaceName + "PaymentGroup"].InnerText;
                if (!(string.IsNullOrEmpty(paymentGroupString)))
                    paymentGroupString = paymentGroupString.PadLeft(3, '0');
                ObjectId refPaymentGroup_id = SetRefToPaymentGroup(paymentGroupString);
                // Todo: Check for required values and other conditions here and throw an exception if not set properly

                SetUserData(collectionName, userElement, userDocument, clacontract, refBA_id, refFA_id, refProfitcenter_id, refPaymentGroup_id, refForeman_id, identifier, paymentGroupString, namespaceName);

                bool dataChanged = true;
                if (cursor.Count() == 1)
                {
                    if (userDocumentOriginal.CompareTo(userDocument) == 0)
                    {
                        dataChanged = false;
                    }
                }

                if (dataChanged)
                {
                    userCollection.Save(userDocument, WriteConcern.Acknowledged);
                    invalidatedCacheItems.Add(new DataTree(userDocument[DBQuery.Id].ToString()));
                    RefreshCache((ObjectId)userDocument[DBQuery.Id], userCollection.Name);

                    logger.LogDebug("Saving userDocument.");
                }

                integrationEvents.UpdateInboundmessageSynced(userDocument, collectionName, currentProcess_id, dataChanged);
            }
        }

        private void SetUserData(string collectionName, XmlElement userElement, BsonDocument userDocument, ObjectId clacontract_id, ObjectId refBA_id, ObjectId refFA_id, ObjectId refProfitcenter_id, ObjectId refPaymentGroup_id, ObjectId refForeman_id, string identifier, string paymentGroupString, string namespaceName)
        {

            logger.LogDebug("Setting User data.");

            userDocument.SetExtended(collectionName, "identifier", identifier);
            userDocument.SetExtended(collectionName, "email", userElement[namespaceName + "Email"].InnerText);
            userDocument.SetExtended(collectionName, "firstname", userElement[namespaceName + "firstName"].InnerText);
            userDocument.SetExtended(collectionName, "lastname", userElement[namespaceName + "lastName"].InnerText);
            userDocument.SetExtended(collectionName, "internalworker", true);
            userDocument.SetExtended(collectionName, "exported_silmu", false);

            EmployerWeeklyHours employerWeeklyHours = new EmployerWeeklyHours(userElement[namespaceName + "WeeklyHoursCode"].InnerText);
            userDocument.SetExtended(collectionName, "weeklyHoursMin", employerWeeklyHours.weeklyHoursMin);
            userDocument.SetExtended(collectionName, "weeklyHoursMax", employerWeeklyHours.weeklyHoursMax);

            // userLevel:
            int userLevel = 1;
            if (userElement[namespaceName + "SFI_HREmplGroup"].InnerText == "Toimihenkilö")
            {
                userLevel = 3; // tj
            }
            userDocument.SetExtended(collectionName, "level", userLevel);

            userDocument.SetExtended(collectionName, "modified", DateTime.UtcNow);

            try
            {
                userDocument.SetExtended(collectionName, "phonenumber", userElement[namespaceName + "CellularPhone"].InnerText);
            }
            catch
            {
                userDocument.RemoveExtended(collectionName, "phonenumber");
            }
            try
            {
                if (string.IsNullOrEmpty(userElement[namespaceName + "SFI_EmploymentStartDate"].InnerText))
                    userDocument.RemoveExtended(collectionName, "employmentstart");
                else
                    userDocument.SetExtended(collectionName, "employmentstart", TroIntegrationCommon.IntegrationHelpers.GetDateFromSAPString(userElement[namespaceName + "SFI_EmploymentStartDate"].InnerText));
            }
            catch
            {
                userDocument.RemoveExtended(collectionName, "employmentstart");
            }
            try
            {
                if (string.IsNullOrEmpty(userElement[namespaceName + "SFI_EmploymentEndDate"].InnerText))
                    userDocument.RemoveExtended(collectionName, "employmentend");
                else
                    userDocument.SetExtended(collectionName, "employmentend", TroIntegrationCommon.IntegrationHelpers.GetDateFromSAPString(userElement[namespaceName + "SFI_EmploymentEndDate"].InnerText));
            }
            catch
            {
                userDocument.RemoveExtended(collectionName, "employmentend");
            }

            SetDocumentAsArrayItem(userDocument, collectionName, clacontract_id, "clacontract");
            SetDocumentAsArrayItem(userDocument, collectionName, refBA_id, "businessarea");
            SetDocumentAsArrayItem(userDocument, collectionName, refFA_id, "functionalarea");
            SetDocumentAsArrayItem(userDocument, collectionName, refProfitcenter_id, "profitcenter");
            SetDocumentAsArrayItem(userDocument, collectionName, refPaymentGroup_id, "paymentgroup");
            SetDocumentAsArrayItem(userDocument, collectionName, refForeman_id, "supervisor");

            string empl_status = "";
            empl_status = userElement[namespaceName + "status"].InnerText.ToLower();
            if (empl_status == "1" || empl_status == "0") // 2 = Päättynyt työsuhde, 1= Voimassa oleva, 0= Ei työsuhteessa
            {
                if (refBA_id == ObjectId.Empty || refFA_id == ObjectId.Empty || refProfitcenter_id == ObjectId.Empty)
                {
                    userDocument.SetExtended(collectionName, "disabled", true);
                }
                else
                {
                    userDocument.SetExtended(collectionName, "disabled", false);
                }
            }
            else
            {
                userDocument.SetExtended(collectionName, "disabled", true);
            }

            logger.LogDebug("Insert User", userElement[namespaceName + "SfiEmplNum"].InnerText, userElement[namespaceName + "Email"].InnerText);
        }

        private static void SetDocumentAsArrayItem(BsonDocument bsonDocument, string collectionName, ObjectId objectId, string fieldName)
        {
            if (objectId != ObjectId.Empty)
            {
                var IdsArray = new BsonArray();
                IdsArray.Add(objectId);
                bsonDocument.SetExtended(collectionName, fieldName, IdsArray);
            }
            else
            {
                bsonDocument.RemoveExtended(collectionName, fieldName);
            }
        }

        private ObjectId SetRefToClacontract(string identifier)
        {
            ObjectId cla_id;
            // set reference to materialGoup
            MongoCollection<BsonDocument> claCollection = mongoDatabase.GetCollection("clacontract");
            MongoCursor cursorCla = claCollection.Find(Query.EQ("identifier", identifier));

            BsonDocument claDocument = null;

            if (cursorCla.Count() == 0)
            {
                claDocument = new BsonDocument();
                cla_id = ObjectId.GenerateNewId();
                claDocument.SetExtended("clacontract", DBQuery.Id, cla_id);
                claDocument.SetExtended("clacontract", "identifier", identifier);
                claDocument.SetExtended("clacontract", "name", "");
                claDocument.SetExtended("clacontract", "created", DateTime.UtcNow);
                claDocument.SetExtended("clacontract", "disabled", true);
                claCollection.Save(claDocument, WriteConcern.Acknowledged);
                logger.LogTrace("Created disabled clacontract", cla_id, identifier);
            }
            else if (cursorCla.Count() == 1)
            {
                foreach (BsonDocument doc in cursorCla)
                    claDocument = doc;

                cla_id = (ObjectId)claDocument[DBQuery.Id];
            }
            else
            {
                cla_id = ObjectId.Empty;
                currentProcessFailedReason += ";More than one existing clacontract found: ";
                currentProcessFailed = true;
                logger.LogError(currentProcessFailedReason, identifier, cursorCla.Count());
            }

            return cla_id;
        }

        private ObjectId SetRefToPaymentGroup(string identifier)
        {
            ObjectId refPaymentGroup_id;
            if (string.IsNullOrEmpty(identifier))
            {
                refPaymentGroup_id = ObjectId.Empty;
                return refPaymentGroup_id;
            }
            // set reference to customer
            MongoCollection<BsonDocument> refPaymentGroupCollection = mongoDatabase.GetCollection("paymentgroup");
            MongoCursor cursorRefPaymentGroup = refPaymentGroupCollection.Find(Query.EQ("identifier", identifier));

            BsonDocument refPaymentGroupDocument = null;

            if (cursorRefPaymentGroup.Count() == 0)
            {
                refPaymentGroupDocument = new BsonDocument();
                refPaymentGroup_id = ObjectId.GenerateNewId();
                refPaymentGroupDocument.SetExtended("paymentgroup", DBQuery.Id, refPaymentGroup_id);
                refPaymentGroupDocument.SetExtended("paymentgroup", "identifier", identifier);
                refPaymentGroupDocument.SetExtended("paymentgroup", "name", "");
                refPaymentGroupDocument.SetExtended("paymentgroup", "disabled", true);
                refPaymentGroupDocument.SetExtended("paymentgroup", "created", DateTime.UtcNow);
                refPaymentGroupCollection.Save(refPaymentGroupDocument, WriteConcern.Acknowledged);
                logger.LogTrace("Created paymentgroup", refPaymentGroup_id, identifier);
            }
            else if (cursorRefPaymentGroup.Count() == 1)
            {
                foreach (BsonDocument doc in cursorRefPaymentGroup)
                    refPaymentGroupDocument = doc;

                refPaymentGroup_id = (ObjectId)refPaymentGroupDocument[DBQuery.Id];
            }
            else
            {
                refPaymentGroup_id = ObjectId.Empty;
                currentProcessFailedReason += ";More than one existing referenced paymentgroups found: ";
                currentProcessFailed = true;
                logger.LogError(currentProcessFailedReason, identifier, cursorRefPaymentGroup.Count());
            }

            return refPaymentGroup_id;
        }

        private void ImportWorkersToTro(string messageId, XmlDocument xmldoc)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmldoc.NameTable);
            nsmgr.AddNamespace("x", WorkerNameSpace);
            nsmgr.AddNamespace("xin", "http://schemas.microsoft.com/dynamics/2006/02/documents/EmplTable");
            XmlNodeList nodes = xmldoc.DocumentElement.SelectNodes("//x:Envelope/PersonData/xin:EmplTable/xin:EmplTable", nsmgr); //("//x:abap/x:values/ARRAY/ZTRO_MATERIAL", nsmgr);
            foreach (XmlNode node in nodes)
            {
                try
                {
                    ImportAxWorkerToTro(messageId, (XmlElement)node, "");
                }
                catch (Exception ex)
                {
                    logger.LogError("Process Worker:", ex);
                    logger.LogError("Processing Worker element:", (XmlElement)node);
                }
            }
        }

        #endregion

        #region Projects

        private void ImportProjectsToTro(string xmlSource)
        {
            throw new NotImplementedException();

        }

        #endregion

        #region Articles

        private void ImportArticlesToTro(string xmlSource)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Cache helpers

        public DataTree GeAndClearInvalidatedCacheItems()
        {
            lock (lockObject)
            {
                var result = (DataTree)invalidatedCacheItems.Clone();
                invalidatedCacheItems.Clear();
                return result;
            }
        }

        #endregion

        #region Helpers

        public class RefToProject
        {
            public BsonDocument projectDocument { get; }
            public bool failed { get; }
            public string failedReason { get; }
            private ObjectId project_id;
            public RefToProject(
                MongoDatabase mongoDatabase,
                ILogger logger,
                string identifier)
            {
                failed = true;
                if (!string.IsNullOrEmpty(identifier))
                {
                    // set reference to project
                    MongoCollection<BsonDocument> projectCollection = mongoDatabase.GetCollection("project");
                    MongoCursor cursorRefProject = projectCollection.Find(Query.EQ("identifier", identifier));


                    if (cursorRefProject.Count() == 0)
                    {
                        projectDocument = new BsonDocument();
                        project_id = ObjectId.GenerateNewId();
                        projectDocument.SetExtended("project", DBQuery.Id, project_id);
                        projectDocument.SetExtended("project", "identifier", identifier);
                        projectDocument.SetExtended("project", "name", "");
                        projectDocument.SetExtended("project", "created", DateTime.UtcNow);
                        projectDocument.SetExtended("project", "disabled", true);

                        string externalidentifier = identifier;
                        while (externalidentifier.Length < 12)
                        {
                            externalidentifier = "0" + externalidentifier;
                        }
                        projectDocument.SetExtended("project", "externalidentifier", externalidentifier);

                        projectCollection.Save(projectDocument, WriteConcern.Acknowledged);
                        failed = false;
                        logger.LogTrace("Created disabled project", project_id, identifier);
                    }
                    else if (cursorRefProject.Count() == 1)
                    {
                        foreach (BsonDocument doc in cursorRefProject)
                            projectDocument = doc;

                        project_id = (ObjectId)projectDocument[DBQuery.Id];
                        failed = false;
                    }
                    else
                    {
                        projectDocument = new BsonDocument();
                        failedReason = "More than one existing referenced project found.";
                        logger.LogError(failedReason, identifier, cursorRefProject.Count());
                    }

                }

            }
        }
        public class EmployerWeeklyHours
        {
            public string weeklyHoursCode { get; set; }
            public string weeklyHoursName { get; set; }
            public double weeklyHoursMin { get; set; }
            public double weeklyHoursMax { get; set; }

            public EmployerWeeklyHours(string weeklyHoursCode)
            {
                this.weeklyHoursCode = weeklyHoursCode;
                switch (weeklyHoursCode)
                {
                    case "15":
                        weeklyHoursName = "15h";
                        weeklyHoursMin = 15;
                        weeklyHoursMax = 15;
                        break;
                    case "20":
                        weeklyHoursName = "20h";
                        weeklyHoursMin = 20;
                        weeklyHoursMax = 20;
                        break;
                    case "225":
                        weeklyHoursName = "22,5h";
                        weeklyHoursMin = 22.5;
                        weeklyHoursMax = 22.5;
                        break;
                    case "25":
                        weeklyHoursName = "25h";
                        weeklyHoursMin = 25;
                        weeklyHoursMax = 25;
                        break;
                    case "275":
                        weeklyHoursName = "27,5h";
                        weeklyHoursMin = 27.5;
                        weeklyHoursMax = 27.5;
                        break;
                    case "30":
                        weeklyHoursName = "30h";
                        weeklyHoursMin = 30;
                        weeklyHoursMax = 30;
                        break;
                    case "375":
                        weeklyHoursName = "37,5h";
                        weeklyHoursMin = 37.5;
                        weeklyHoursMax = 37.5;
                        break;
                    case "40":
                        weeklyHoursName = "40h";
                        weeklyHoursMin = 40;
                        weeklyHoursMax = 40;
                        break;
                    case "0":
                        weeklyHoursName = "0-20h";
                        weeklyHoursMin = 0;
                        weeklyHoursMax = 20;
                        break;
                    case "21":
                        weeklyHoursName = "20-40h";
                        weeklyHoursMin = 20;
                        weeklyHoursMax = 40;
                        break;
                    default:
                        weeklyHoursName = "?";
                        weeklyHoursMin = 0;
                        weeklyHoursMax = 40;
                        break;
                }
            }
        }

        #endregion
    }
}
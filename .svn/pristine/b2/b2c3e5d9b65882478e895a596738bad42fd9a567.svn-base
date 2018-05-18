using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SystemsGarden.mc2.RemoteConnector.Handlers;
using SystemsGarden.mc2.Common;
using SystemsGarden.mc2.Common.Constants;
using SystemsGarden.mc2.RemoteConnector.Handlers.CoreServerHandlers.MongoDBHandler;
using MongoDB.Bson;
using MongoDB.Shared;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Xml;
using System.Xml.Serialization;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace SystemsGarden.mc2.RemoteConnector.Handlers.SapIntegrationHandlerServer
{
    public class TroToSapExport
    {
        #region Members
        private MongoDatabase mongoDatabase;
        private MongoDBHandlerServer mongoDBHandler;
        private string sqlConnectionString;
        private SqlConnection sqlConnection;
        private string troToSapResponseNamespace;

        private ILogger logger;

        private bool currentProcessHandled = false;
        private bool currentProcessFailed = false;
        private bool currentProcessExportedSAP = false;
        private string currentProcessFailedReason = "";
        private int currentProcessPartNr = 0; // for multimessage events

        private IntegrationEvents integrationEvents;

        private bool exportAfterWorkerapproval = false;

        #endregion

        public TroToSapExport(
            ILogger logger,
            MongoDBHandlerServer mongoDBHandler,
            string sqlConnectionString,
            DataTree initializationMessage)
        {
            this.logger = logger.CreateChildLogger("TroToSAPExport");
            this.mongoDatabase = mongoDBHandler.Database;
            this.mongoDBHandler = mongoDBHandler;
            this.sqlConnectionString = sqlConnectionString;
            this.troToSapResponseNamespace = (string)initializationMessage["trotosapresponsenamespace"].GetValueOrDefault("http://schemas.microsoft.com/ado/2007/08/dataservices/metadata");
        }

        internal void ExportDocuments()
        {
            if (!MongoDBHandlerServer.SchemaApplied)
            {
                logger.LogDebug("Waiting for MongoDBHandler to receive schema and configuration before starting TroToSAP integration.");
                return;
            }

            exportAfterWorkerapproval = (bool)MongoDBHandlerServer.ConfigStatic["application"]["features"]["exportafterworkerapproval"];

            Hashtable handledEntries = new Hashtable();

            // to save handled entries:
            MongoCollection<BsonDocument> savedEntryCollection = mongoDatabase.GetCollection("entryupdate_erp_saved");
            ObjectId savedEntry_id;

            // fetch new message entries
            MongoCollection<BsonDocument> entryCollection = mongoDatabase.GetCollection("entryupdate_erp");

            MongoCursor cursor = entryCollection.FindAll().SetSortOrder(SortBy.Ascending("created")).SetLimit(200);

            // Handle all new messages
            foreach (BsonDocument entryDocument in cursor)
            {

                // save entry:
                BsonDocument savedEntryDocument = new BsonDocument();
                savedEntry_id = ObjectId.GenerateNewId();
                savedEntryDocument.Set(DBQuery.Id, savedEntry_id);
                savedEntryDocument.Set("collectionname", (string)entryDocument.GetValue("collectionname", string.Empty));
                savedEntryDocument.Set("objectidstring", (string)entryDocument.GetValue("objectidstring", string.Empty));
                savedEntryDocument.Set("__modifiedby__displayname", (string)entryDocument.GetValue("__modifiedby__displayname", string.Empty));
                savedEntryDocument.Set("created", (DateTime)entryDocument.GetValue("created", DateTime.UtcNow));
                savedEntryDocument.Set("handledTime", DateTime.UtcNow);
                savedEntryDocument.Set("retrycnt", (Int32)entryDocument.GetValue("retrycnt", 0));
                savedEntryCollection.Save(savedEntryDocument, WriteConcern.Acknowledged);
                // handle entry:
                currentProcessHandled = false;
                currentProcessFailed = false;
                currentProcessFailedReason = string.Empty;
                currentProcessExportedSAP = false;
                currentProcessPartNr = 0; // for multimessage entries
                string idstring = string.Empty;
                string collectionname = string.Empty;
                string objectidstring = string.Empty;
                Int32 retryCnt = 0;
                ObjectId objectid = ObjectId.Empty;
                collectionname = (string)entryDocument.GetValue("collectionname", string.Empty);
                objectidstring = (string)entryDocument.GetValue("objectidstring", string.Empty);
                // integrationevent:
                integrationEvents = new IntegrationEvents("SAP", collectionname, objectidstring, "out", logger, mongoDatabase);

                try
                {
                    idstring = entryDocument["_id"].ToString();
                    retryCnt = (Int32)entryDocument.GetValue("retrycnt", 0);
                    objectid = new ObjectId(objectidstring);
                    string handledKey = objectidstring + ' ' + collectionname;

                    // handled the same object already?
                    if (handledEntries.ContainsKey(handledKey))
                    {
                        logger.LogDebug("TroToAx message: entry has been handled already:", collectionname, objectidstring);
                        currentProcessHandled = true;
                        savedEntryDocument.Set("infotext", "entry has been handled already");
                    }
                    else
                    {
                        handledEntries.Add(handledKey, collectionname);

                        ExportDocument(entryDocument, collectionname, objectidstring, objectid, retryCnt);

                        logger.LogDebug("TroToAx message handled:", collectionname, objectidstring, retryCnt);
                    }
                }
                catch (Exception ex)
                {
                    currentProcessFailed = true;
                    currentProcessFailedReason = ex.Message;
                    logger.LogError("Error in message handling:", collectionname, objectidstring, retryCnt, ex);

                    currentProcessHandled = true;
                }
                finally
                {
                    // save handled entry data:
                    savedEntryDocument.Set("processFailed", currentProcessFailed);
                    savedEntryDocument.Set("processFailedReason", currentProcessFailedReason);
                    savedEntryDocument.Set("processHandled", currentProcessHandled);
                    savedEntryDocument.Set("processExportedSAP", currentProcessExportedSAP);
                    savedEntryCollection.Save(savedEntryDocument, WriteConcern.Acknowledged);

                    // remove entry:
                    if (currentProcessHandled)
                        RemoveEntryDocument(idstring, entryCollection, entryDocument, collectionname, objectidstring);

                    // integrationevent:
                    if (currentProcessFailed)
                    {
                        integrationEvents.Lastfailreason = currentProcessFailedReason;
                        integrationEvents.Failed = true;
                        integrationEvents.Status = "failed";
                    }

                    if (integrationEvents.Status != "initialized")
                    {
                        integrationEvents.createEvent();
                    }
                }
            }

            return;
        }

        // get TroIntMessagesOut -messages from SQLDB, which have status like "erp%"
        //   update TroIntMessagesOut message (sql) status retry->new/cancelled
        //   update integrationevent data
        internal void HandleErpProcessedMessages(int retryInterval)
        {
            logger.LogDebug("Start handle troToSAP erp processed messages");

            using (sqlConnection = new SqlConnection(sqlConnectionString))
            {
                SqlCommand sqlCommand = new SqlCommand("dbo.GetTroOutErpMessages", sqlConnection);
                sqlCommand.CommandType = CommandType.StoredProcedure;

                sqlCommand.Parameters.Add("@retryIntervalSec", SqlDbType.Int);
                sqlCommand.Parameters["@retryIntervalSec"].Value = retryInterval / 1000;

                sqlConnection.Open();
                SqlDataReader reader = sqlCommand.ExecuteReader();
                while (reader.Read())
                {
                    Int32 sqlId = (Int32)reader[0];
                    string status = (string)reader[1];
                    string response = (string)reader[2];
                    Int32 processcnt = (Int32)reader[5];
                    string info = (string)reader[6];
                    string responsemessage = (string)reader[2]; // =SAP response. 
                    string responsecode = (string)reader[7];
                    string collectionname = string.Empty;
                    string objectidstring = string.Empty;
                    ObjectId objectid = ObjectId.Empty;

                    MongoCollection<BsonDocument> integrationeventCollection = mongoDatabase.GetCollection("integrationevent");
                    MongoCursor cursor = integrationeventCollection.Find(Query.EQ("sqlid", sqlId));

                    BsonDocument integrationeventDocument = new BsonDocument();
                    foreach (BsonDocument doc in cursor)
                    {
                        integrationeventDocument = doc;
                        if (status == "erperror" || status == "erpretry")
                        {
                            integrationeventDocument.Set("lastfailreason", responsemessage);
                            integrationeventDocument.Set("failed", true);
                        }
                        else
                        {
                            integrationeventDocument.Set("failed", false);
                        }

                        DateTime firstprocesstime = (DateTime)reader[3];
                        if (!System.DBNull.Value.Equals(reader[4]))
                        {
                            DateTime lastprocesstime = (DateTime)reader[4];
                            integrationeventDocument.Set("handledtime", lastprocesstime);
                        }
                        integrationeventDocument.Set("status", status);
                        integrationeventDocument.Set("handledcount", processcnt);
                        integrationeventDocument.Set("firsthandledtime", firstprocesstime);
                        integrationeventDocument.Set("response", response);
                        integrationeventDocument.Set("responsecode", responsecode);
                        integrationeventDocument.Set("responsemessage", responsemessage);
                        integrationeventDocument.Set("info", info);
                        collectionname = (string)integrationeventDocument.GetValue("collectionname", string.Empty);
                        objectid = (ObjectId)integrationeventDocument.GetValue("objectid", ObjectId.Empty);

                        integrationeventCollection.Save(integrationeventDocument, WriteConcern.Acknowledged);
                        logger.LogTrace("Saved integrationevent document.", sqlId, status);

                        // Update infotext to the entity (SG30065 ERP-viennin tiedoissa on näytettävä viennin tila)
                        MongoCollection<BsonDocument> messageSourceCollection = mongoDatabase.GetCollection(collectionname);
                        MongoCursor messageSourcecursor = messageSourceCollection.Find(Query.EQ(DBQuery.Id, objectid));
                        if (messageSourcecursor.Count() == 1)
                        {
                            foreach (BsonDocument messageSourceDocument in messageSourcecursor)
                            {
                                logger.LogTrace("Updating exportnote", collectionname, objectid.ToString(), info);
                                messageSourceDocument.Set("exportnote", info);
                                if (!System.DBNull.Value.Equals(reader[4]))
                                {
                                    DateTime lastprocesstime = (DateTime)reader[4];
                                    messageSourceDocument.Set("exporttimestamp_ax", lastprocesstime);
                                }
                                messageSourceCollection.Save(messageSourceDocument, WriteConcern.Acknowledged);
                            }
                        }
                    }
                }
            }
            logger.LogTrace("End retry troToSAP messages");

            return;
        }

        internal void RemoveEntryDocument(string idstring, MongoCollection entryCollection, BsonDocument entryDocument, string collectionname, string objectidstring)
        {

            try
            {
                if (currentProcessFailed)
                {
                    BsonDocument errorDocument = new BsonDocument();
                    errorDocument.Set("collectionname", collectionname);
                    errorDocument.Set("objectidstring", objectidstring);
                    errorDocument.Set("infotext", currentProcessFailedReason);
                    errorDocument.Set("handlingtime", DateTime.Now);

                    MongoCollection<BsonDocument> errorCollection = mongoDatabase.GetCollection("entryupdate_erp_error");

                    errorCollection.Insert(errorDocument, WriteConcern.Acknowledged);
                }
                // all entries for this document must be removed:
                ObjectId _id = new ObjectId(idstring);
                entryCollection.Remove(Query.EQ("_id", _id));
            }
            catch (Exception ex)
            {
                currentProcessFailed = true;
                currentProcessFailedReason = ex.Message;
                logger.LogError("Error in message removal:", collectionname, objectidstring, ex);
            }

            return;
        }

        internal void CreateOutboundMessage(BsonDocument entryDocument, string messageString, string messageType, string refId1, string refId2)
        {
            using (sqlConnection = new SqlConnection(sqlConnectionString))
            {
                int retryCnt = (Int32)entryDocument.GetValue("retrycnt", 0);
                string collectionname = (string)entryDocument.GetValue("collectionname", "");

                SqlCommand sqlCommand = new SqlCommand("dbo.TroOutNewMessage", sqlConnection);
                sqlCommand.CommandType = CommandType.StoredProcedure;

                sqlCommand.Parameters.Add("@messagetype", SqlDbType.VarChar, 20);
                sqlCommand.Parameters["@messagetype"].Value = messageType;
                sqlCommand.Parameters.Add("@message", SqlDbType.VarChar, -1);
                sqlCommand.Parameters["@message"].Value = messageString;
                sqlCommand.Parameters.Add("@collectionname", SqlDbType.VarChar, 100);
                sqlCommand.Parameters["@collectionname"].Value = collectionname;
                sqlCommand.Parameters.Add("@RefId1", SqlDbType.VarChar, 256);
                sqlCommand.Parameters["@RefId1"].Value = refId1;
                sqlCommand.Parameters.Add("@RefId2", SqlDbType.VarChar, 256);
                sqlCommand.Parameters["@refId2"].Value = refId2;
                sqlCommand.Parameters.Add("@retryCnt", SqlDbType.Int);
                sqlCommand.Parameters["@retryCnt"].Value = retryCnt;
                sqlCommand.Parameters.Add("@errorMessage", SqlDbType.VarChar, -1);
                sqlCommand.Parameters["@errorMessage"].Direction = ParameterDirection.Output;
                currentProcessPartNr += 1;
                sqlCommand.Parameters.Add("@partNr", SqlDbType.Int);
                sqlCommand.Parameters["@partNr"].Value = currentProcessPartNr;
                sqlCommand.Parameters.Add("@return_value", SqlDbType.Int);
                sqlCommand.Parameters["@return_value"].Direction = ParameterDirection.ReturnValue;


                sqlConnection.Open();
                sqlCommand.ExecuteNonQuery();

                if ((int)sqlCommand.Parameters["@return_value"].Value < 1)
                    throw new InvalidOperationException("SQL Error when inserting new outbound message (procedure TroOutNewMessage):" + sqlCommand.Parameters["@errorMessage"].Value);

                currentProcessExportedSAP = true;

                // Integration events:
                if (currentProcessPartNr > 1)
                    integrationEvents.createEvent(); // write integration event for the previous message

                integrationEvents.Messagetype = messageType;
                integrationEvents.Messagecontent = messageString;
                integrationEvents.Status = "new";
                integrationEvents.SqlId = (int)sqlCommand.Parameters["@return_value"].Value;
                integrationEvents.ImportedOrExported = true;
            }

            return;
        }

        internal void ExportDocument(BsonDocument entryDocument, string collectionname, string objectidstring, ObjectId objectid, Int32 retryCnt)
        {
            currentProcessHandled = true;

            // fetch the entity to make an outbound message
            MongoCollection<BsonDocument> messageSourceCollection = mongoDatabase.GetCollection(collectionname);
            MongoCursor messageSourcecursor = messageSourceCollection.Find(Query.EQ(DBQuery.Id, objectid));

            if (messageSourcecursor.Count() == 1)
            {
                foreach (BsonDocument messageSourceDocument in messageSourcecursor)
                {
                    // integration event data:
                    if (messageSourceDocument.Contains("project") && messageSourceDocument["project"].BsonType == BsonType.Array && ((BsonArray)messageSourceDocument["project"]).Count > 0)
                        integrationEvents.Project = (ObjectId)messageSourceDocument["project"][0];
                    else
                        integrationEvents.Project = ObjectId.Empty;

                    if (messageSourceDocument.Contains("user") && messageSourceDocument["user"].BsonType == BsonType.Array && ((BsonArray)messageSourceDocument["user"]).Count > 0)
                        integrationEvents.User = (ObjectId)messageSourceDocument["user"][0];
                    else
                        integrationEvents.User = ObjectId.Empty;

                    if (messageSourceDocument.Contains("profitcenter") && messageSourceDocument["profitcenter"].BsonType == BsonType.Array && ((BsonArray)messageSourceDocument["profitcenter"]).Count > 0)
                        integrationEvents.Profitcenter = (ObjectId)messageSourceDocument["profitcenter"][0];
                    else
                        integrationEvents.Profitcenter = ObjectId.Empty;

                    if (messageSourceDocument.Contains("userprofitcenter") && messageSourceDocument["userprofitcenter"].BsonType == BsonType.Array && ((BsonArray)messageSourceDocument["userprofitcenter"]).Count > 0)
                        integrationEvents.Userprofitcenter = (ObjectId)messageSourceDocument["userprofitcenter"][0];
                    else
                        integrationEvents.Userprofitcenter = ObjectId.Empty;

                    if (messageSourceDocument.Contains("parent") && messageSourceDocument["parent"].BsonType == BsonType.Array && ((BsonArray)messageSourceDocument["parent"]).Count > 0)
                        integrationEvents.Parent = (ObjectId)messageSourceDocument["parent"][0];
                    else
                        integrationEvents.Parent = ObjectId.Empty;

                    integrationEvents.Project__displayname = (string)messageSourceDocument.GetValue("__project__displayname", string.Empty);
                    integrationEvents.User__displayname = (string)messageSourceDocument.GetValue("__user__displayname", string.Empty);
                    integrationEvents.Profitcenter__displayname = (string)messageSourceDocument.GetValue("__profitcenter__displayname", string.Empty);
                    integrationEvents.Userprofitcenter__displayname = (string)messageSourceDocument.GetValue("__userprofitcenter__displayname", string.Empty);
                    integrationEvents.Parent__displayname = (string)messageSourceDocument.GetValue("__parent__displayname", string.Empty);
                    integrationEvents.Identifier = (string)messageSourceDocument.GetValue("identifier", string.Empty);
                    integrationEvents.DisplayName = (string)messageSourceDocument.GetValue("note", string.Empty);

                    if (collectionname == "timesheetentry" || collectionname == "dayentry")
                    {
                        if (entryDocument.Contains("date"))
                        {
                            integrationEvents.Date = ((DateTime)entryDocument["date"]);
                        }
                        else
                        {
                            integrationEvents.Date = null;
                        }
                    }
                    else
                    {
                        integrationEvents.Date = null;
                    }

                    // make a new outbound message
                    if (collectionname == "timesheetentry")
                    {
                        ExportTimesheetentryToSAP(entryDocument, collectionname, objectidstring, objectid, messageSourceDocument);
                    }
                    else if (collectionname == "dayentry")
                    {
                        ExportDayentryToSAP(entryDocument, collectionname, objectidstring, objectid, messageSourceDocument);
                    }
                    else if (collectionname == "articleentry")
                    {
                        ExportArticleentryToSAP(entryDocument, collectionname, objectidstring, objectid, messageSourceDocument);
                    }
                    else if (collectionname == "allocationentry")
                    {
                        ExportAllocationentryToSAP(entryDocument, collectionname, objectidstring, objectid, messageSourceDocument);
                    }
                    else if (collectionname == "TODO")
                    {
                        currentProcessHandled = false; // entry handler is missing 
                    }

                    // update messageSourceDocument handled
                    if (currentProcessExportedSAP && currentProcessHandled)
                    {
                        messageSourceDocument.Set("exported_ax", true);
                        messageSourceCollection.Save(messageSourceDocument, WriteConcern.Acknowledged);
                    }


                }
            }

            return;
        }

        #region AllocationEntry
        internal void ExportAllocationentryToSAP(BsonDocument entryDocument, string collectionname, string objectidstring, ObjectId objectid, BsonDocument allocationEntryDocument)
        {
            logger.LogDebug("ExportAllocationentryToSAP start:", objectidstring);
            string projectexternalidentifier = string.Empty;
            string projecttype = string.Empty;
            ObjectId projectmanager = ObjectId.Empty;
            bool userisManager = false;
            MongoCollection<BsonDocument> collection = mongoDatabase.GetCollection("project");
            MongoCursor cursor = collection.Find(Query.EQ(DBQuery.Id, (ObjectId)allocationEntryDocument["project"][0]));
            if (cursor.Count() == 1)
            {
                foreach (BsonDocument doc in cursor)
                {
                    //projectexternalidentifier = (string)doc["externalidentifier"];
                    string projectidentifier = Convert.ToString(doc.GetValue("identifier", string.Empty));

                    projectexternalidentifier = Convert.ToString(doc.GetValue("externalidentifier", projectexternalidentifier));
                    if (projectexternalidentifier == string.Empty)
                    {
                        projectexternalidentifier = Convert.ToString(doc.GetValue("identifier", string.Empty));
                        while (projectexternalidentifier.Length < 12)
                        {
                            projectexternalidentifier = "0" + projectexternalidentifier;
                        }
                    }
                    projecttype = Convert.ToString(doc.GetValue("projecttype", projecttype));
                    if (doc.Contains("projectmanager"))
                        projectmanager = (ObjectId)doc["projectmanager"][0];
                }
            }
            else
            {
                throw new InvalidDataException("Project not found.");
            }

            if (allocationEntryDocument.Contains("user") && (ObjectId)allocationEntryDocument["user"][0] == projectmanager)
                userisManager = true;


            // check if this timesheetentry is to be sent to SAP
            if (!AllocationEntryIsSendable(projecttype, userisManager))
                return;

            // serialize timesheetentry data
            Hashtable insertedAllocationEntries = new Hashtable(); // need to update sent allocation entries as exported
            string messageString = SerializeAllocationEntry(allocationEntryDocument, objectidstring, projectexternalidentifier, insertedAllocationEntries);
            if (string.IsNullOrEmpty(messageString))
                return;

            // create new timesheetentry message to SAP
            CreateOutboundMessage(entryDocument, messageString, "OrderPartners", objectidstring, "");

            // update sent allocation entries as exported:
            if (currentProcessExportedSAP)
            {
                foreach (DictionaryEntry entry in insertedAllocationEntries)
                {
                    TroIntegrationCommon.IntegrationHelpers.InsertErpOrderPartner((string)entry.Value, (string)entry.Key, mongoDatabase, logger);
                }
            }

            return;
        }
        internal bool AllocationEntryIsSendable(string projecttype, bool userisManager)
        {
            if (!(projecttype == "ZS01" || projecttype == "ZS02"))
                return false;
            if (userisManager)
                return false;

            return true;
        }

        internal string SerializeAllocationEntry(BsonDocument allocationEntryDocument, string objectidstring, string projectexternalidentifier, Hashtable insertedAllocationEntries)
        {
            ObjectId user = (ObjectId)allocationEntryDocument["user"][0];
            logger.LogDebug("Serialize Allocation entry", objectidstring, projectexternalidentifier, user);
            string result = "";
            bool contentFound = false;

            // create xml document
            XmlDocument xmldoc = new XmlDocument();

            XmlElement rootElement = xmldoc.CreateElement("abap");
            rootElement.SetAttribute("xmlns:asx", "http://www.sap.com/abapxml");
            xmldoc.AppendChild(rootElement);

            XmlElement valuesElement = xmldoc.CreateElement("values");
            XmlElement arrayElement = xmldoc.CreateElement("ARRAY");

            // write xml component group for all emloyers to whom an allocationentry exists for this order
            ObjectId tempUserObjectId = ObjectId.Empty;
            int tempUserStatus = -1;
            ObjectId thisUserObjectId = ObjectId.Empty;
            int thisUserStatus = -1;
            MongoCollection<BsonDocument> collection = mongoDatabase.GetCollection("allocationentry");
            //MongoCursor cursor = collection.Find(Query.EQ("project", (ObjectId)allocationEntryDocument["project"][0])).SetSortOrder(SortBy.Ascending("user"));
            MongoCursor cursor = collection.Find(Query.And(Query.EQ("user", user), Query.EQ("project", (ObjectId)allocationEntryDocument["project"][0]))).SetSortOrder(SortBy.Ascending("user"));
            long lkm = cursor.Count();

            foreach (BsonDocument doc in cursor)
            {
                bool disabled = false;
                if (doc.Contains("disabled"))
                    disabled = (bool)doc["disabled"];

                if (doc.Contains("user") && doc.Contains("status") && !disabled)
                {
                    tempUserObjectId = (ObjectId)doc["user"][0];
                    tempUserStatus = TroIntegrationCommon.IntegrationHelpers.EnumerateTroAllocationStatus((string)doc["status"]);
                    if (tempUserObjectId != thisUserObjectId)
                    {
                        if (thisUserObjectId != ObjectId.Empty)
                        {
                            string parNr = getDocumentFieldStringvalueById("user", "identifier", thisUserObjectId);
                            if (BuildAllocationEntry(projectexternalidentifier, parNr, thisUserStatus, xmldoc, arrayElement, mongoDatabase, insertedAllocationEntries))
                                contentFound = true;
                        }
                        thisUserObjectId = tempUserObjectId;
                        thisUserStatus = tempUserStatus;
                    }
                    else if (tempUserStatus < thisUserStatus || thisUserStatus == -1)
                    {
                        thisUserStatus = tempUserStatus;
                    }
                }
            }
            if (thisUserObjectId != ObjectId.Empty)
            {
                string parNr = getDocumentFieldStringvalueById("user", "identifier", thisUserObjectId);
                if (BuildAllocationEntry(projectexternalidentifier, parNr, thisUserStatus, xmldoc, arrayElement, mongoDatabase, insertedAllocationEntries))
                    contentFound = true;
            }

            if (!contentFound)
                return string.Empty;

            valuesElement.AppendChild(arrayElement);
            xmldoc.DocumentElement.AppendChild(valuesElement);

            // convert xml to string:
            using (var stringWriter = new StringWriter())
            using (var xmlTextWriter = XmlWriter.Create(stringWriter))
            {
                xmldoc.WriteTo(xmlTextWriter);
                xmlTextWriter.Flush();
                result = stringWriter.GetStringBuilder().ToString();
            }

            return result;
        }

        private bool BuildAllocationEntry(string projectexternalidentifier, string parNr, int thisUserStatus, XmlDocument xmldoc, XmlElement arrayElement, MongoDatabase mongoDatabase, Hashtable insertedAllocationEntries)
        {
            if (TroIntegrationCommon.IntegrationHelpers.IsQualifiedUserIdentifier(parNr))
            {
                XmlElement ztro_componentElement = xmldoc.CreateElement("ZTRO_COMPONENT");
                XmlElement xeorderidElement = xmldoc.CreateElement("ORDERID");
                xeorderidElement.InnerText = projectexternalidentifier;
                ztro_componentElement.AppendChild(xeorderidElement);

                // collection erporderpartner: orderid in short form:
                string orderId = projectexternalidentifier;
                while (orderId.Substring(0, 1) == "0" && orderId.Length > 1)
                    orderId = orderId.Substring(1);

                XmlElement xeoperationElement = xmldoc.CreateElement("OPERATION");
                // Operation INSERT/UPDATE:
                if (TroIntegrationCommon.IntegrationHelpers.ErpOrderPartnerExists(orderId, parNr, mongoDatabase))
                {
                    xeoperationElement.InnerText = "UPDATE";
                }
                else
                {
                    xeoperationElement.InnerText = "INSERT";
                    insertedAllocationEntries.Add(parNr, orderId);
                }
                ztro_componentElement.AppendChild(xeoperationElement);

                XmlElement xeparnrElement = xmldoc.CreateElement("PARNR");
                xeparnrElement.InnerText = parNr;
                ztro_componentElement.AppendChild(xeparnrElement);
                XmlElement xepartner_roleElement = xmldoc.CreateElement("PARTNER_ROLE");
                xepartner_roleElement.InnerText = TroIntegrationCommon.IntegrationHelpers.GetSAPRoleByTroEnumeration(thisUserStatus); ;
                ztro_componentElement.AppendChild(xepartner_roleElement);
                arrayElement.AppendChild(ztro_componentElement);

                logger.LogDebug("Qualified", projectexternalidentifier, parNr, xmldoc);
                return true;
            }
            logger.LogDebug("Not Qualified", projectexternalidentifier, parNr, xmldoc);
            return false;
        }
        #endregion
        #region ArticleEntry
        internal void ExportArticleentryToSAP(BsonDocument entryDocument, string collectionname, string objectidstring, ObjectId objectid, BsonDocument articleEntryDocument)
        {
            logger.LogDebug("ExportArticleentryToSAP start:", objectidstring);
            ObjectId userID = (ObjectId)articleEntryDocument["user"][0]; // TODO: check is bsonarray, isnullorempty, arraylen
            string userIdentifier = getDocumentFieldStringvalueById("user", "identifier", userID);

            // check if this timesheetentry is to be sent to SAP
            if (!(ArticleEntryIsSendable(articleEntryDocument, userIdentifier)))
                return;


            // serialize timesheetentry data
            string messageString = SerializeArticleEntry(articleEntryDocument, objectidstring, userIdentifier);

            // create new timesheetentry message to SAP
            CreateOutboundMessage(entryDocument, messageString, "MaterialReport", objectidstring, "");

            return;
        }

        internal bool ArticleEntryIsSendable(BsonDocument articleEntryDocument, string userIdentifier)
        {
            if ((bool)articleEntryDocument["exported_ax"])
                return false;


            if (exportAfterWorkerapproval)
            {
                if (!(bool)articleEntryDocument["approvedbyworker"])
                    return false;
            } else
            {
                if (!(bool)articleEntryDocument["approvedbymanager"])
                    return false;
            }

            if (!TroIntegrationCommon.IntegrationHelpers.IsQualifiedUserIdentifier(userIdentifier))
                return false;

            if (Convert.ToDouble(articleEntryDocument["amount"]) == 0)
                return false;

            return true;
        }

        internal string SerializeArticleEntry(BsonDocument articleEntryDocument, string objectidstring, string userIdentifier)
        {
            string result = "";

            // create xml document
            XmlDocument xmldoc = new XmlDocument();

            XmlElement root = xmldoc.CreateElement("abap");
            root.SetAttribute("xmlns:asx", "http://www.sap.com/abapxml");
            xmldoc.AppendChild(root);

            XmlElement values = xmldoc.CreateElement("values");
            XmlElement array = xmldoc.CreateElement("ARRAY");
            XmlElement ztro_articleentry = xmldoc.CreateElement("ZTRO_COMPONENT");

            XmlElement xedboperation = xmldoc.CreateElement("OPERATION");
            xedboperation.InnerText = "INSERT";
            ztro_articleentry.AppendChild(xedboperation);

            XmlElement xereservno = xmldoc.CreateElement("RESERVNO");
            ztro_articleentry.AppendChild(xereservno);

            XmlElement xeresitem = xmldoc.CreateElement("RESITEM");
            ztro_articleentry.AppendChild(xeresitem);

            string materialunit = "";
            XmlElement xematerial = xmldoc.CreateElement("MATERIAL");
            MongoCollection<BsonDocument> collection = mongoDatabase.GetCollection("article");
            MongoCursor cursor = collection.Find(Query.EQ(DBQuery.Id, (ObjectId)articleEntryDocument["article"][0]));
            if (cursor.Count() == 1)
            {
                foreach (BsonDocument doc in cursor)
                {
                    xematerial.InnerText = (string)doc["identifier"];
                    materialunit = (string)doc["unit"];
                }
            }
            ztro_articleentry.AppendChild(xematerial);

            double quantity = Convert.ToDouble(articleEntryDocument["amount"]);
            XmlElement xequantity = xmldoc.CreateElement("REQUIREMENT_QUANTITY");
            xequantity.InnerText = quantity.ToString().Replace(",", ".");
            ztro_articleentry.AppendChild(xequantity);

            XmlElement xequantityunit = xmldoc.CreateElement("REQUIREMENT_QUANTITY_UNIT");
            xequantityunit.InnerText = materialunit;
            ztro_articleentry.AppendChild(xequantityunit);

            XmlElement xeorderid = xmldoc.CreateElement("ORDERID");
            xeorderid.InnerText = getDocumentFieldStringvalueById("project", "externalidentifier", (ObjectId)articleEntryDocument["project"][0]);
            ztro_articleentry.AppendChild(xeorderid);

            XmlElement xedatefrom = xmldoc.CreateElement("REQ_DATE");
            DateTime timeStamp = DateTime.MinValue;
            if (articleEntryDocument.Contains("timestamp"))
                timeStamp = ((DateTime)articleEntryDocument["timestamp"]);
            xedatefrom.InnerText = timeStamp.ToString("yyyy-MM-dd");
            ztro_articleentry.AppendChild(xedatefrom);

            XmlElement xecquantity = xmldoc.CreateElement("COMMITED_QUAN");
            xecquantity.InnerText = "0.0";
            ztro_articleentry.AppendChild(xecquantity);

            XmlElement xecategory = xmldoc.CreateElement("CATEGORY");
            ztro_articleentry.AppendChild(xecategory);

            XmlElement xedesc = xmldoc.CreateElement("DESCRIPTION");
            if (articleEntryDocument.Contains("note"))
                xedesc.InnerText = TroIntegrationCommon.IntegrationHelpers.CutMessageField((string)articleEntryDocument["note"], 128);
            ztro_articleentry.AppendChild(xedesc);

            XmlElement xecreatedby = xmldoc.CreateElement("CREATED_BY");
            xecreatedby.InnerText = userIdentifier;
            ztro_articleentry.AppendChild(xecreatedby);

            XmlElement xeinvoiced = xmldoc.CreateElement("INVOICED");
            ztro_articleentry.AppendChild(xeinvoiced);

            array.AppendChild(ztro_articleentry);
            values.AppendChild(array);
            xmldoc.DocumentElement.AppendChild(values);

            // convert xml to string:
            using (var stringWriter = new StringWriter())
            using (var xmlTextWriter = XmlWriter.Create(stringWriter))
            {
                xmldoc.WriteTo(xmlTextWriter);
                xmlTextWriter.Flush();
                result = stringWriter.GetStringBuilder().ToString();
            }

            return result;
        }
        #endregion

        #region timeSheetRegion
        // Logics for wagetype and projectcategory and the 2:nd HourReport message
        //  1:st HourReport message:
        //      projectcategory is taken from timesheetentry -document if present. It can be user-chosen or a copy from the referenced timesheetentrydetailpaytype
        //          If not present, then projectcategory is taken from timesheetentrydetailpaytype
        //      wagetype is the referenced timesheetentrydetailpaytype.identifier
        //  2:nd HourReport message:
        //      Message is made, if the referenced timesheetentrydetailpaytype contains identifier2 (non-empty string)
        //      projectcategory is taken from timesheetentrydetailpaytype.projectcategory2 if present, otherwise the same  category is used as in the 1:st message
        //      wagetype is the referenced timesheetentrydetailpaytype.identifier2
        internal void ExportTimesheetentryToSAP(BsonDocument entryDocument, string collectionname, string objectidstring, ObjectId objectid, BsonDocument timeSheetEntryDocument)
        {
            logger.LogDebug("ExportTimesheetentryToSAP start:", objectidstring);
            // Get user information
            ObjectId userID = (ObjectId)timeSheetEntryDocument["user"][0]; // TODO: check is bsonarray, isnullorempty, arraylen
            MongoCollection<BsonDocument> collection = mongoDatabase.GetCollection("user");
            MongoCursor cursor = collection.Find(Query.EQ(DBQuery.Id, userID));
            string userIdentifier = string.Empty;
            string userName = "";
            if (cursor.Count() == 1)
            {
                foreach (BsonDocument doc in cursor)
                {
                    if (doc.Contains("identifier"))
                        userIdentifier = (string)doc["identifier"];
                    if (doc.Contains("firstname") && doc.Contains("lastname"))
                        userName = (string)doc["firstname"] + " " + (string)doc["lastname"];
                }
            }

            MongoCollection<BsonDocument> paytypeCollection = mongoDatabase.GetCollection("timesheetentrydetailpaytype");
            MongoCursor cursorPaytype = paytypeCollection.Find(Query.EQ(DBQuery.Id, timeSheetEntryDocument["timesheetentrydetailpaytype"][0]));
            bool exporttoax = false;
            double paytypefactor = 1;
            double paytypefactor2 = 1;
            string projectcategory = string.Empty;
            string projectcategory2 = string.Empty;
            string paytype = string.Empty;
            string paytype2 = string.Empty;

            if (cursorPaytype.Count() == 1)
            {
                foreach (BsonDocument payTypeDocument in cursorPaytype)
                {
                    exporttoax = Convert.ToBoolean(payTypeDocument.GetValue("exporttoax", false));
                    paytype = Convert.ToString(payTypeDocument.GetValue("identifier", string.Empty));
                    paytype2 = Convert.ToString(payTypeDocument.GetValue("identifier2", string.Empty));
                    paytypefactor = Convert.ToDouble(payTypeDocument.GetValue("paytypefactor", paytypefactor));
                    paytypefactor2 = Convert.ToDouble(payTypeDocument.GetValue("paytypefactor2", paytypefactor2));

                    // Project category is taken from timesheet entry and secondarily from paytype if not present in timesheet entry.
                    try
                    {
                        if (timeSheetEntryDocument.Contains("projectcategory"))
                        {
                            projectcategory = getDocumentFieldStringvalueById("projectcategory", "identifier", (ObjectId)timeSheetEntryDocument["projectcategory"][0]);
                            projectcategory2 = getDocumentFieldStringvalueById("projectcategory", "identifier2", (ObjectId)timeSheetEntryDocument["projectcategory"][0]);
                        }
                        else if (payTypeDocument.Contains("projectcategory"))
                        {
                            projectcategory = getDocumentFieldStringvalueById("projectcategory", "identifier", (ObjectId)payTypeDocument["projectcategory"][0]);
                        }
                    }
                    catch
                    {

                    }
                }
            }

            // work duration in millisec:
            double durationDouble = 0;
            durationDouble = Convert.ToDouble(timeSheetEntryDocument.GetValue("duration", 0));
            if ((bool)timeSheetEntryDocument.GetValue("differentdurationforbilling", false))
                durationDouble = Convert.ToDouble(timeSheetEntryDocument.GetValue("billingduration", durationDouble));

            // check if this timesheetentry is to be sent to SAP
            if (!(TimeSheetEntryIsSendable(timeSheetEntryDocument, userIdentifier, exporttoax, durationDouble)))
                return;

            // mandatory fields:
            if (String.IsNullOrEmpty(projectcategory))
                throw new InvalidOperationException("Projectcategory missing");

            // serialize timesheetentry data, projectcategory1
            string messageString = SerializeTimesheetEntry(projectcategory, paytypefactor, paytype, timeSheetEntryDocument, objectidstring, userIdentifier, userName, durationDouble);

            // create new timesheetentry message to SAP
            CreateOutboundMessage(entryDocument, messageString, "HourReport", objectidstring, "");

            // serialize timesheetentry data for second paytype/projectcategory. In case we have both second patype and project cateogyr
            // use second value for each. If we have only one value for either of them, use that value for both exports.
            if (!(string.IsNullOrEmpty(projectcategory2) || projectcategory2 == projectcategory))
            {
                if (string.IsNullOrEmpty(paytype2))
                    paytype2 = paytype;

                // check if paytype2.exporttoax is set to false
                if (paytype2 != paytype)
                {
                    MongoCollection<BsonDocument> paytype2collection = mongoDatabase.GetCollection("timesheetentrydetailpaytype");
                    MongoCursor paytype2cursor = paytype2collection.Find(Query.EQ("identifier", paytype2));
                    if (paytype2cursor.Count() == 1)
                    {
                        foreach (BsonDocument paytype2doc in paytype2cursor)
                            exporttoax = Convert.ToBoolean(paytype2doc.GetValue("exporttoax", false));
                    }
                }

                // check if this timesheetentry is to be sent to SAP
                if (!(TimeSheetEntryIsSendable(timeSheetEntryDocument, userIdentifier, exporttoax, durationDouble)))
                    return;

                messageString = SerializeTimesheetEntry(projectcategory2, paytypefactor2, paytype2, timeSheetEntryDocument, objectidstring, userIdentifier, userName, durationDouble);

                // create new timesheetentry message to SAP
                CreateOutboundMessage(entryDocument, messageString, "HourReport", objectidstring, "");
            }

            return;
        }


        internal string SerializeTimesheetEntry(string projectcategory, double paytypefactor, string paytype, BsonDocument timeSheetEntryDocument, string objectidstring, string userIdentifier, string userName, double durationDouble)
        {
            logger.LogDebug("SerializeTimesheetEntry start:", userIdentifier, userName);
            string result = "";

            string projectID = "";
            string orderID = "";
            ObjectId projectIDObj = (ObjectId)timeSheetEntryDocument["project"][0]; // TODO: check is bsonarray, isnullorempty, arraylen
            MongoCollection<BsonDocument> projCollection = mongoDatabase.GetCollection("project");
            MongoCursor projCursor = projCollection.Find(Query.EQ(DBQuery.Id, projectIDObj));
            if (projCursor.Count() == 1)
            {
                foreach (BsonDocument doc in projCursor)
                    if (doc["projecttype"] == "PROJECT")
                    {
                        projectID = (string)doc["externalidentifier"];
                        orderID = "";
                    }
                    else
                    {
                        orderID = (string)doc["externalidentifier"];
                        projectIDObj = (ObjectId)doc["parentproject"][0]; // TODO: check is bsonarray, isnullorempty, arraylen
                        MongoCursor parentProjCursor = projCollection.Find(Query.EQ(DBQuery.Id, projectIDObj));
                        if (parentProjCursor.Count() == 1)
                        {
                            foreach (BsonDocument parentprojdoc in parentProjCursor)
                            {
                                projectID = (string)parentprojdoc.GetValue("externalidentifier", "");
                                if (projectID == "")
                                {
                                    projectID = (string)parentprojdoc.GetValue("identifier", "");
                                    {
                                        while (projectID.Length < 12)
                                        {
                                            projectID = "0" + projectID;
                                        }
                                    }
                                }
                            }
                        }
                        else if (parentProjCursor.Count() == 0)
                        {
                            throw new InvalidOperationException("Order parent project not found");
                        }
                        else
                        {
                            throw new InvalidOperationException("Multiple order parent projects found, project " + projectID);
                        }
                    }
            }
            else if (projCursor.Count() == 0)
            {
                throw new InvalidOperationException("Project not found " + projectID);
            }
            else
            {
                throw new InvalidOperationException("Multiple order parent projects found " + projectID);
            }


            // create xml document
            XmlDocument xmldoc = new XmlDocument();

            XmlElement root = xmldoc.CreateElement("abap");
            root.SetAttribute("xmlns:asx", "http://www.sap.com/abapxml");
            xmldoc.AppendChild(root);

            XmlElement values = xmldoc.CreateElement("values");
            XmlElement array = xmldoc.CreateElement("ARRAY");
            XmlElement ztro_timeentry = xmldoc.CreateElement("ZTRO_TIMEENTRY");

            XmlElement xedboperation = xmldoc.CreateElement("DBOPERATION");
            xedboperation.InnerText = "INSERT";
            ztro_timeentry.AppendChild(xedboperation);

            XmlElement xepernr = xmldoc.CreateElement("PERNR");
            xepernr.InnerText = userIdentifier;
            ztro_timeentry.AppendChild(xepernr);

            DateTime startdate = ((DateTime)timeSheetEntryDocument["starttimestamp"]);
            string starttimestamp = startdate.ToString("yyyy-MM-dd");
            XmlElement xedatefrom = xmldoc.CreateElement("DATEFROM");
            xedatefrom.InnerText = starttimestamp;
            ztro_timeentry.AppendChild(xedatefrom);

            XmlElement xedateto = xmldoc.CreateElement("DATETO");
            xedateto.InnerText = starttimestamp;
            ztro_timeentry.AppendChild(xedateto);

            XmlElement xeprojectno = xmldoc.CreateElement("PRJECTNO");
            xeprojectno.InnerText = projectID;
            ztro_timeentry.AppendChild(xeprojectno);

            XmlElement xeorderid = xmldoc.CreateElement("ORDERID");
            xeorderid.InnerText = orderID;
            ztro_timeentry.AppendChild(xeorderid);

            XmlElement oper = xmldoc.CreateElement("OPERATION");
            ztro_timeentry.AppendChild(oper);

            XmlElement xesuboper = xmldoc.CreateElement("SUBOPERATION");
            ztro_timeentry.AppendChild(xesuboper);

            // wagetype
            XmlElement xepaytype = xmldoc.CreateElement("WAGETYPE");
            xepaytype.InnerText = paytype;
            ztro_timeentry.AppendChild(xepaytype);

            XmlElement xeunit = xmldoc.CreateElement("UNIT");
            xeunit.InnerText = "H";
            ztro_timeentry.AppendChild(xeunit);

            XmlElement xequantity = xmldoc.CreateElement("QUANTITY");
            durationDouble = paytypefactor * durationDouble;
            int duration = (int)durationDouble;
            int min = (int)Math.Round(durationDouble / (double)60000);
            int tunnit = min / 60;
            min = min - tunnit * 60;
            int tunninosat = min * 100 / 60;
            double tunnitf = (double)tunnit + (double)tunninosat / 100.0;
            string durationStr = tunnitf.ToString();
            xequantity.InnerText = durationStr.Replace(",", ".");
            ztro_timeentry.AppendChild(xequantity);

            XmlElement xetimefrom = xmldoc.CreateElement("TIMEFROM");
            xetimefrom.InnerText = "00:00:00";
            ztro_timeentry.AppendChild(xetimefrom);

            XmlElement xetimeto = xmldoc.CreateElement("TIMETO");
            xetimeto.InnerText = "00:00:00";
            ztro_timeentry.AppendChild(xetimeto);

            XmlElement xedescription = xmldoc.CreateElement("DESCRIPTION");
            DateTime entrydate = ((DateTime)timeSheetEntryDocument["date"]);
            string description = userName + " " + entrydate.ToString("yyyy-MM-dd") + ":";
            if (timeSheetEntryDocument.Contains("note"))
                description = description + (string)timeSheetEntryDocument["note"];
            xedescription.InnerText = description;
            ztro_timeentry.AppendChild(xedescription);

            // activity type
            XmlElement xeacttype = xmldoc.CreateElement("ACTTYPE");
            xeacttype.InnerText = projectcategory;
            ztro_timeentry.AppendChild(xeacttype);

            array.AppendChild(ztro_timeentry);
            values.AppendChild(array);
            xmldoc.DocumentElement.AppendChild(values);

            // convert xml to string:
            using (var stringWriter = new StringWriter())
            using (var xmlTextWriter = XmlWriter.Create(stringWriter))
            {
                xmldoc.WriteTo(xmlTextWriter);
                xmlTextWriter.Flush();
                result = stringWriter.GetStringBuilder().ToString();
            }

            return result;
        }

        internal bool TimeSheetEntryIsSendable(BsonDocument timeSheetEntryDocument, string userIdentifier, bool exporttoax, double durationDouble)
        {
            logger.LogDebug("TimeSheetEntryIsSendable:", exporttoax, (bool)timeSheetEntryDocument["exported_ax"], "conf:", exportAfterWorkerapproval, (bool)timeSheetEntryDocument["approvedbyworker"], (bool)timeSheetEntryDocument["approvedbymanager"], TroIntegrationCommon.IntegrationHelpers.IsQualifiedUserIdentifier(userIdentifier));
            if (!exporttoax)
                return false;

            if ((bool)timeSheetEntryDocument["exported_ax"])
                return false;

            if (exportAfterWorkerapproval)
            {
                if (!(bool)timeSheetEntryDocument["approvedbyworker"])
                    return false;
            }
            else
            {
                if (!(bool)timeSheetEntryDocument["approvedbymanager"])
                    return false;
            }

            if (!TroIntegrationCommon.IntegrationHelpers.IsQualifiedUserIdentifier(userIdentifier))
                return false;

            if (durationDouble == 0)
                return false;

            if (getDocumentFieldStringvalueById("project", "identifier", (ObjectId)timeSheetEntryDocument["project"][0]) == "__socialproject")
                return false;

            return true;
        }
        #endregion
        #region timeSheetRegion
        internal void ExportDayentryToSAP(BsonDocument entryDocument, string collectionname, string objectidstring, ObjectId objectid, BsonDocument dayEntryDocument)
        {

            logger.LogDebug("ExportDayentryToSAP start:", objectidstring);
            // Get user information
            ObjectId userID = (ObjectId)dayEntryDocument["user"][0]; // TODO: check is bsonarray, isnullorempty, arraylen
            MongoCollection<BsonDocument> collection = mongoDatabase.GetCollection("user");
            MongoCursor cursor = collection.Find(Query.EQ(DBQuery.Id, userID));
            string userIdentifier = string.Empty;
            string userName = "";
            if (cursor.Count() == 1)
            {
                foreach (BsonDocument doc in cursor)
                {
                    if (doc.Contains("identifier"))
                        userIdentifier = (string)doc["identifier"];
                    if (doc.Contains("firstname") && doc.Contains("lastname"))
                        userName = (string)doc["firstname"] + " " + (string)doc["lastname"];
                }
            }


            string dayEntryTypeUnit = "";
            string dayEntryTypeIdentifier = "";
            bool exporttoax = false;
            string projectCategoryIdentifier = string.Empty;
            ObjectId projectCategoryId = ObjectId.Empty;
            MongoCollection<BsonDocument> entrytypeCollection = mongoDatabase.GetCollection("dayentrytype");
            MongoCursor cursorEntrytype = entrytypeCollection.Find(Query.EQ(DBQuery.Id, dayEntryDocument["dayentrytype"][0]));

            if (cursorEntrytype.Count() == 1)
            {
                foreach (BsonDocument doc in cursorEntrytype)
                {
                    if (doc.Contains("exporttoerp"))
                        exporttoax = (bool)doc["exporttoerp"];

                    try
                    {
                        if (doc.Contains("projectcategory"))
                            projectCategoryId = (ObjectId)doc["projectcategory"][0];
                    }
                    catch
                    {

                    }
                    if (!(projectCategoryId == ObjectId.Empty))
                        projectCategoryIdentifier = getDocumentFieldStringvalueById("projectcategory", "identifier", projectCategoryId);


                    if (doc.Contains("unit"))
                        dayEntryTypeUnit = (string)doc["unit"];

                    dayEntryTypeIdentifier = (string)doc["identifier"];
                }
            }

            // check if this timesheetentry is to be sent to SAP
            if (!(DayEntryIsSendable(dayEntryDocument, userIdentifier, exporttoax, projectCategoryIdentifier)))
                return;


            // serialize timesheetentry data
            string messageString = SerializeDayEntry(dayEntryDocument, objectidstring, userIdentifier, dayEntryTypeIdentifier, dayEntryTypeUnit, userName, projectCategoryIdentifier);

            // create new timesheetentry message to SAP
            CreateOutboundMessage(entryDocument, messageString, "HourReport", objectidstring, "");

            return;
        }


        internal string SerializeDayEntry(BsonDocument dayEntryDocument, string objectidstring, string userIdentifier, string dayEntryTypeIdentifier, string dayEntryTypeUnit, string userName, string projectCategoryIdentifier)
        {
            logger.LogDebug("SerializeDayEntry start:", objectidstring, userIdentifier, userName, dayEntryDocument);

            string result = "";

            string projectID = "";
            string orderID = "";
            ObjectId projectIDObj = (ObjectId)dayEntryDocument["project"][0]; // TODO: check is bsonarray, isnullorempty, arraylen
            MongoCollection<BsonDocument> projCollection = mongoDatabase.GetCollection("project");
            MongoCursor projCursor = projCollection.Find(Query.EQ(DBQuery.Id, projectIDObj));
            if (projCursor.Count() == 1)
            {
                foreach (BsonDocument doc in projCursor)
                    if (doc["projecttype"] == "PROJECT")
                    {
                        projectID = (string)doc["externalidentifier"];
                        orderID = "";
                    }
                    else
                    {
                        orderID = (string)doc["externalidentifier"];
                        projectIDObj = (ObjectId)doc["parentproject"][0]; // TODO: check is bsonarray, isnullorempty, arraylen
                        MongoCursor parentProjCursor = projCollection.Find(Query.EQ(DBQuery.Id, projectIDObj));
                        if (parentProjCursor.Count() == 1)
                        {
                            foreach (BsonDocument parentprojdoc in parentProjCursor)
                            {
                                projectID = (string)parentprojdoc.GetValue("externalidentifier", "");
                                if (projectID == "")
                                {
                                    projectID = (string)parentprojdoc.GetValue("identifier", "");
                                    {
                                        while (projectID.Length < 12)
                                        {
                                            projectID = "0" + projectID;
                                        }
                                    }
                                }
                            }
                        }
                        else if (parentProjCursor.Count() == 0)
                        {
                            throw new InvalidOperationException("Order parent project not found");
                        }
                        else
                        {
                            throw new InvalidOperationException("Multiple order parent projects found, project " + projectID);
                        }
                    }
            }
            else if (projCursor.Count() == 0)
            {
                throw new InvalidOperationException("Project not found " + projectID);
            }
            else
            {
                throw new InvalidOperationException("Multiple order parent projects found " + projectID);
            }


            logger.LogDebug("SerializeDayEntry create xml document");
            // create xml document
            XmlDocument xmldoc = new XmlDocument();

            XmlElement root = xmldoc.CreateElement("abap");
            root.SetAttribute("xmlns:asx", "http://www.sap.com/abapxml");
            xmldoc.AppendChild(root);

            XmlElement values = xmldoc.CreateElement("values");
            XmlElement array = xmldoc.CreateElement("ARRAY");
            XmlElement ztro_timeentry = xmldoc.CreateElement("ZTRO_TIMEENTRY");

            XmlElement xedboperation = xmldoc.CreateElement("DBOPERATION");
            xedboperation.InnerText = "INSERT";
            ztro_timeentry.AppendChild(xedboperation);

            XmlElement xepernr = xmldoc.CreateElement("PERNR");
            xepernr.InnerText = userIdentifier;
            ztro_timeentry.AppendChild(xepernr);

            DateTime startdate = ((DateTime)dayEntryDocument["date"]);
            string starttimestamp = startdate.ToString("yyyy-MM-dd");
            XmlElement xedatefrom = xmldoc.CreateElement("DATEFROM");
            xedatefrom.InnerText = starttimestamp;
            ztro_timeentry.AppendChild(xedatefrom);

            XmlElement xedateto = xmldoc.CreateElement("DATETO");
            xedateto.InnerText = starttimestamp;
            ztro_timeentry.AppendChild(xedateto);

            XmlElement xeprojectno = xmldoc.CreateElement("PRJECTNO");
            xeprojectno.InnerText = projectID;
            ztro_timeentry.AppendChild(xeprojectno);

            XmlElement xeorderid = xmldoc.CreateElement("ORDERID");
            xeorderid.InnerText = orderID;
            ztro_timeentry.AppendChild(xeorderid);

            XmlElement oper = xmldoc.CreateElement("OPERATION");
            ztro_timeentry.AppendChild(oper);

            XmlElement xesuboper = xmldoc.CreateElement("SUBOPERATION");
            ztro_timeentry.AppendChild(xesuboper);

            // wagetype
            XmlElement xepaytype = xmldoc.CreateElement("WAGETYPE");
            xepaytype.InnerText = dayEntryTypeIdentifier;
            ztro_timeentry.AppendChild(xepaytype);

            XmlElement xeunit = xmldoc.CreateElement("UNIT");
            xeunit.InnerText = dayEntryTypeUnit;
            ztro_timeentry.AppendChild(xeunit);

            XmlElement xequantity = xmldoc.CreateElement("QUANTITY");
            double amount = Convert.ToDouble(dayEntryDocument["amount"]);
            string amountStr = amount.ToString();
            xequantity.InnerText = amountStr.Replace(",", ".");
            ztro_timeentry.AppendChild(xequantity);

            XmlElement xetimefrom = xmldoc.CreateElement("TIMEFROM");
            xetimefrom.InnerText = "00:00:00";
            ztro_timeentry.AppendChild(xetimefrom);

            XmlElement xetimeto = xmldoc.CreateElement("TIMETO");
            xetimeto.InnerText = "00:00:00";
            ztro_timeentry.AppendChild(xetimeto);

            XmlElement xedescription = xmldoc.CreateElement("DESCRIPTION");
            DateTime entrydate = ((DateTime)dayEntryDocument["date"]);
            string description = userName + " " + entrydate.ToString("yyyy-MM-dd") + ":";
            if (dayEntryDocument.Contains("note"))
                description = description + (string)dayEntryDocument["note"];
            xedescription.InnerText = TroIntegrationCommon.IntegrationHelpers.CutMessageField(description, 128);
            ztro_timeentry.AppendChild(xedescription);

            // activity type
            XmlElement xeacttype = xmldoc.CreateElement("ACTTYPE");
            xeacttype.InnerText = projectCategoryIdentifier;
            ztro_timeentry.AppendChild(xeacttype);

            array.AppendChild(ztro_timeentry);
            values.AppendChild(array);
            xmldoc.DocumentElement.AppendChild(values);

            // convert xml to string:
            using (var stringWriter = new StringWriter())
            using (var xmlTextWriter = XmlWriter.Create(stringWriter))
            {
                xmldoc.WriteTo(xmlTextWriter);
                xmlTextWriter.Flush();
                result = stringWriter.GetStringBuilder().ToString();
            }

            return result;
        }

        internal bool DayEntryIsSendable(BsonDocument dayEntryDocument, string userIdentifier, bool exporttoax, string projectCategoryIdentifier)
        {
            if ((bool)dayEntryDocument["exported_ax"])
                return false;

            if (!exporttoax)
                return false;

            if (exportAfterWorkerapproval)
            {
                if (!(bool)dayEntryDocument["approvedbyworker"])
                    return false;
            }
            else
            {
                if (!(bool)dayEntryDocument["approvedbymanager"])
                    return false;
            }

            if (String.IsNullOrEmpty(projectCategoryIdentifier))
                return false;

            if (!TroIntegrationCommon.IntegrationHelpers.IsQualifiedUserIdentifier(userIdentifier))
                return false;

            if (getDocumentFieldStringvalueById("project", "identifier", (ObjectId)dayEntryDocument["project"][0]) == "__socialproject")
                return false;

            return true;
        }
        #endregion


        #region helpers
        private string getDocumentFieldStringvalueById(string collectionName, string fieldName, ObjectId Id)
        {
            MongoCollection<BsonDocument> collection = mongoDatabase.GetCollection(collectionName);
            MongoCursor cursor = collection.Find(Query.EQ(DBQuery.Id, Id));

            if (cursor.Count() == 1)
            {
                foreach (BsonDocument doc in cursor)
                    if (doc.Contains(fieldName))
                        return (string)doc[fieldName];
                    else
                        return string.Empty;
            }
            return string.Empty;
        }
        #endregion
    }
}

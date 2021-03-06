﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using SystemsGarden.mc2.RemoteConnector.Handlers;
using SystemsGarden.mc2.Common;
using SystemsGarden.mc2.Common.Constants;
using SystemsGarden.mc2.RemoteConnector.Handlers.CoreServerHandlers.MongoDBHandler;
using MongoDB.Bson;
using MongoDB.Shared;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace SystemsGarden.mc2.RemoteConnector.Handlers.TroHelpersHandlerServer
{
    public class TroHelpersHandlerServer : BaseHandler
    {
        private MongoDatabase database;
        private static DataTree invalidatedCacheItems = new DataTree();
        private static DataTree frontServerConfiguration = new DataTree();

        private bool autoApproveWork = false;
		private static bool useErpUpdateTable = false;

        public TroHelpersHandlerServer(
            IRemoteConnection remoteConnection, ILogger parentLogger, IHandlerContainer handlerContainer)
            : base(remoteConnection, parentLogger, TroHelpersHandlerServerInfo.HandlerName, handlerContainer)
        {
            _handlerInfo = new TroHelpersHandlerServerInfo();
        }

        public override void HandleMessageInternal()
        {
            switch ((string)Message[RCConstants.action])
            {
                case "tro_gettimesheetentrytotals" :
                    GetTimesheetEntryTotals();
                    break;

                case "tro_autoapprovework":
                    AutoApproveWork();
                    break;

                case "getinvalidatedcacheitems":

                    GetInvalidatedCacheItems();
                    break;

				case "tro_getprojectleadreport":
					GetProjectLeadReport();
					break;

                case "mdbapplyschema":
                    ApplySchemaAndConfiguration();
                    break;

                case "tro_getprojectleads":
                    GetProjectleads();
                    break;
            }

            ForwardMessage();
        }

        private void GetProjectleads()
        {
            ObjectId projectId = new ObjectId(Message["project"]);
            var query = Query.EQ("_id", projectId);
            var project = database.GetCollection("project").FindOne(query);
            if (project.Contains("projectlead"))
            {
                BsonArray projectLeads = (BsonArray)project["projectlead"];
                foreach(ObjectId projectlead in projectLeads)
                {
                    Response["projectleads"][projectlead.ToString()].Create();
                }
            }
        }

        private void GetProjectLeadReport()
		{
			ObjectId project = new ObjectId(Message["project"]);

			ObjectId? payrollPeriod = new ObjectId?();
			if (!string.IsNullOrEmpty(Message["payrollperiod"]))
				payrollPeriod = new ObjectId(Message["payrollperiod"]);

			ObjectId? user =  new ObjectId?();
			if (Message.Contains("user")) user = new ObjectId(Message["user"]);

			ObjectId? userFilter = new ObjectId?();
			if (!string.IsNullOrEmpty(Message["userfilter"])) userFilter = new ObjectId(Message["userfilter"]);

			ObjectId? userList = new ObjectId?();
			if (!string.IsNullOrEmpty(Message["userlist"])) userList = new ObjectId(Message["userlist"]);

			DateTime? startDate = new DateTime?();
			if (!Message["startdate"].Empty) startDate = (DateTime)Message["startdate"];

			DateTime? endDate = new DateTime?();
			if (!Message["enddate"].Empty) endDate = (DateTime)Message["enddate"];

			var projectLeadReport = new ProjectLeadReport(
				logger,
				database,
				project,
				payrollPeriod,
				user,
				startDate,
				endDate,
				userFilter,
				userList);
			DataTree report = projectLeadReport.GenerateProjectLeadReport();
			Response["report"] = report;
		}

		private void ApplySchemaAndConfiguration()
        {
            logger.LogDebug("Applying www-front server configuration.");

            frontServerConfiguration = new DataTree();
            frontServerConfiguration.Merge(Message.Parent[MongoDBHandlerConstants.mongodbhandler]["configuration"]);

			useErpUpdateTable = (bool)frontServerConfiguration["application"]["features"]["useerpupdatetable"];

            logger.LogTrace("Applied www-front server configuration", (frontServerConfiguration.ContentView()));

            VerifySocialProject();
        }

        // Make sure social project exists in projects if it's enabled in configuration
        private void VerifySocialProject()
        {
            if (!(bool)frontServerConfiguration["application"]["features"]["enablesocialproject"])
                return;

            lock(database)
            {
                MongoCollection<BsonDocument> projectsCollection = database.GetCollection("project");

                const string SocialProjectIdentifier = "__socialproject";

                BsonDocument result = projectsCollection.FindOne(Query.EQ("identifier", SocialProjectIdentifier));

                if (result == null)
                {
                    logger.LogInfo("Social project doesn't exist. Adding it.");
                    var socialProject = new BsonDocument();
                    socialProject["identifier"] = SocialProjectIdentifier;
                    socialProject["name"] = "Social project{75FCDB63-82F2-4E0A-B58D-04030A42234D}";
					socialProject["name__fi"] = "Hallinnollinen työ";
					socialProject["name__en-us"] = "Administrative project";
					socialProject["identifier__fi"] = "";
					socialProject["identifier__en-us"] = "";
					socialProject["status"] = "__socialproject";
					socialProject["projecttype"] = "__socialproject";

					projectsCollection.Save(socialProject);
                }
            }
        }

        protected override void Initialize()
        {
            database = ((MongoDBHandlerServer)handlerContainer.GetHandler("mongodbhandler")).Database;
            autoApproveWork = (bool)Message["autoapprovework"].GetValueOrDefault(false);

            // Tuomotesti
            string collName = "mc2db.article";
            if (!database.CollectionExists(collName))
            {
                logger.LogDebug("Collection doesn't exist. ", collName);

                //database.CreateCollection(schemaCollection.Name);
            }
            collName = "article";
            if (!database.CollectionExists(collName))
            {
                logger.LogDebug("Collection doesn't exist. ", collName);

                //database.CreateCollection(schemaCollection.Name);
            }
            collName = "controller";
            if (!database.CollectionExists(collName))
            {
                logger.LogDebug("Collection doesn't exist. ", collName);

                //database.CreateCollection(schemaCollection.Name);
            }

            // Tuomotesti

            SetupEvents();
        }

		private void SetupEvents()
		{
			MongoDBHandlerServer mongoDbHandler = (MongoDBHandlerServer)handlerContainer.GetHandler(MongoDBHandlerConstants.mongodbhandler);

			if (mongoDbHandler == null)
				throw new Exception("Mongo DB Handler not found");

			mongoDbHandler.CreatingDocumentAfter += CreatingOrUpdatingDocumentAfter;
			mongoDbHandler.UpdatingDocumentAfter += CreatingOrUpdatingDocumentAfter;
		}

		private void CreatingOrUpdatingDocumentAfter(BsonDocument document, MongoCollection collection)
		{
			if (!useErpUpdateTable)
				return;

			logger.LogDebug("Saving erp update", collection.Name, document["_id"]);

			var erpUpdate = new BsonDocument();

			erpUpdate["objectidstring"] = document["_id"].ToString();
			erpUpdate["collectionname"] = collection.Name;

			MongoCollection<BsonDocument> erpUpdateCollection =  collection.Database.GetCollection("entryupdate_erp");
			erpUpdateCollection.Save(erpUpdate);

			return;
		}

		private void GetTimesheetEntryTotals()
        {
            string user = Message["user"];
            string project = Message["project"];
            string asset = Message["asset"];
            string profitcenter = Message["profitcenter"];
            string projectmanager = Message["projectmanager"];

            bool approvedByWorker = (bool)Message["approvedbyworker"];
            bool approvedByManager = (bool)Message["approvedbymanager"];
            bool exportedToVisma = (bool)Message["exported_visma"];

            var andQueries = new List<IMongoQuery>();

            if (!string.IsNullOrEmpty(user))
                andQueries.Add(Query.EQ("user", new ObjectId(user)));
            if (!string.IsNullOrEmpty(project))
                andQueries.Add(Query.EQ("project", new ObjectId(project)));
            if (!string.IsNullOrEmpty(asset))
                andQueries.Add(Query.EQ("asset", new ObjectId(asset)));
            if (!string.IsNullOrEmpty(profitcenter))
                andQueries.Add(Query.EQ("profitcenter", new ObjectId(profitcenter)));
            if (!string.IsNullOrEmpty(projectmanager))
                andQueries.Add(Query.EQ("projectmanager", new ObjectId(projectmanager)));

            int duration = 0;
            int overtime50 = 0;
            int overtime100 = 0;
            int lateHours = 0;
            int nightHours = 0;
            int travellingHours = 0;

			if (andQueries.Count > 0)
			{
				if (approvedByWorker)
					andQueries.Add(Query.EQ("approvedbyworker", true));
				else
					andQueries.Add(Query.NE("approvedbyworker", true));

				if (approvedByManager)
					andQueries.Add(Query.EQ("approvedbymanager", true));
				else
                    andQueries.Add(Query.EQ("approvedbymanager", false));// 26062017

                if (exportedToVisma)
					andQueries.Add(Query.EQ("exported_visma", true));
				else
					andQueries.Add(Query.NE("exported_visma", true));

				// Only count regular hours and not any extras.
				andQueries.Add(Query.NE("countsasregularhours", false));


				DateTime startTimestamp = (DateTime)Message["starttimestamp"].GetValueOrDefault(DateTime.MinValue);
				DateTime endTimestamp = (DateTime)Message["endtimestamp"].GetValueOrDefault(DateTime.MaxValue);

				if (Message["starttimestamp"].HasValue)
                    andQueries.Add(Query.GTE("starttimestamp", (DateTime)Message["starttimestamp"]));

                if (Message["endtimestamp"].HasValue)
                    andQueries.Add(Query.LTE("endtimestamp", (DateTime)Message["starttimestamp"]));

                MongoCollection<BsonDocument> mongoCollection = database.GetCollection("timesheetentry");

                MongoCursor<BsonDocument> cursor = mongoCollection.Find(Query.And(andQueries));

                foreach (BsonDocument record in cursor)
                {
                    if (record.Contains("overtime50"))
                        overtime50 += record["overtime50"].AsInt32;

                    if (record.Contains("overtime100"))
                        overtime100 += record["overtime100"].AsInt32;

                    if (record.Contains("latehours"))
                        lateHours += record["latehours"].AsInt32;

                    if (record.Contains("nighthours"))
                        nightHours += record["nighthours"].AsInt32;

                    if (record.Contains("travellinghours"))
                        travellingHours += record["travellinghours"].AsInt32;

                    if (record.Contains("starttimestamp") && record.Contains("endtimestamp"))
                        duration += (int)(record["endtimestamp"].ToUniversalTime() - record["starttimestamp"].ToUniversalTime()).TotalMilliseconds;
                }
            }

            Response["duration"] = duration;
            Response["overtime50"] = overtime50;
            Response["overtime100"] = overtime100;
            Response["latehours"] = lateHours;
            Response["nighthours"] = nightHours;
            Response["travellinghours"] = travellingHours;
        }

        // Automatically accept work for last week. Do this only once a week.
        private void AutoApproveWork()
        {
            if (!autoApproveWork)
            {
                logger.LogTrace("Automatically approving work disabled. Not approving work.");
                return;
            }

            logger.LogTrace("Finding out whether previous work needs to be accepted");
            if (!IsAutomaticWorkAcceptanceDue())
            {
                logger.LogTrace("No need to accept past work yet.");
                return;
            }

            logger.LogInfo("Accepting all work that was logged for last week or earlier.");

            ApproveWork("timesheetentry");
            ApproveWork("dayentry");
            ApproveWork("absenceentry");
            ApproveWork("assetentry");
            ApproveWork("articleentry");

            MarkAutomaticWorkAcceptanceDone();
        }

        private bool IsAutomaticWorkAcceptanceDue()
        {
            var collection = database.GetCollection(TroHelpersHandlerServerInfo.HandlerName);

            BsonDocument lastAccepted = collection.FindOne(Query.EQ("identifier", "automaticworklastaccepted"));

            if (lastAccepted == null)
                return true;

            logger.LogInfo("Accepting all unaccepted work for previous week.");

            DateTime timeStamp = (DateTime)lastAccepted["timestamp"];

            DateTime beginningOfWeek = MC2DateTimeValue.Now().StartOfWeek();

            // Return true if work hasn't been accepted this week.
            return timeStamp.CompareTo(beginningOfWeek) < 0;                      
        }

        private void GetInvalidatedCacheItems()
        {
            lock (invalidatedCacheItems)
            {
                if (Response.Contains("invalidatedcacheitems"))
                    Response["invalidatedcacheitems"].Merge((DataTree)invalidatedCacheItems.Clone());
                else
                    Response["invalidatedcacheitems"] = (DataTree)invalidatedCacheItems.Clone();

                invalidatedCacheItems.Clear();
            }
        }


        private void ApproveWork(string collectionName)
        {
            DateTime beginningOfWeek = MC2DateTimeValue.Now().StartOfWeek();

            MongoCollection<BsonDocument> collection = database.GetCollection(collectionName);

            IMongoQuery query = Query.And(
                Query.LT("starttimestamp", beginningOfWeek),
                Query.NE("approvedbyworker", true)
                );

            MongoCursor<BsonDocument> cursor = collection.Find(query);

            foreach( var document in cursor)
            {
                document["approvedbyworker"] = true;

                invalidatedCacheItems.Add(new DataTree(document[DBQuery.Id].ToString()));

                collection.Save(document, WriteConcern.Acknowledged);
            }
        }

        private void MarkAutomaticWorkAcceptanceDone()
        {
            var collection = database.GetCollection(TroHelpersHandlerServerInfo.HandlerName);

            BsonDocument lastAccepted = collection.FindOne(Query.EQ("identifier", "automaticworklastaccepted"));

            if (lastAccepted == null)
            {
                lastAccepted = new BsonDocument();
                lastAccepted.Set("identifier", "automaticworklastaccepted");
            }

            lastAccepted.Set("timestamp", MC2DateTimeValue.Now());

            collection.Save(lastAccepted, WriteConcern.Acknowledged);
        }
    }
}

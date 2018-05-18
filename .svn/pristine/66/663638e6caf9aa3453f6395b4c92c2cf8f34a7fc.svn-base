using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using SystemsGarden.mc2.RemoteConnector.Handlers;
using SystemsGarden.mc2.Common;
using SystemsGarden.mc2.Common.Constants;
using SystemsGarden.mc2.RemoteConnector.Handlers.CoreServerHandlers.MongoDBHandler;
using System.IO;
using MongoDB.Bson;
using MongoDB.Shared;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace SystemsGarden.mc2.RemoteConnector.Handlers.SQLHandlerServer
{
    public class SQLHandlerServer : BaseHandler
    {
        const int MsPerDay = 24 * 60 * 60 * 1000;
        private static object SQLLock = new object();
        private static bool isInitialized = false;
        private bool disabled;
        private static MongoDBHandlerServer mongoDBHandler;
        private static MongoDatabase database;
        private static int scheduledTimehh;
        private static int scheduledTimemm;
        private static int scheduledMs;
        private static int maxDailyRunMinutes;
        private static Thread cachingThread;

        public SQLHandlerServer(
                IRemoteConnection remoteConnection,
                ILogger parentLogger,
                IHandlerContainer handlerContainer)
                : base(remoteConnection, parentLogger, SQLHandlerServerInfo.HandlerName, handlerContainer)
        {
            _handlerInfo = new SQLHandlerServerInfo();
        }

        public override void HandleMessageInternal()
        {
            ForwardMessage();
        }

        protected override void Initialize()
        {
            lock (SQLLock)
            {
                if (isInitialized) { return; }

                disabled = (bool)Message["disabled"].GetValueOrDefault(false);
                if (disabled)
                {
                    logger.LogInfo("SQL handler is disabled.");
                    return;
                }

                mongoDBHandler = ((MongoDBHandlerServer)handlerContainer.GetHandler("mongodbhandler"));
                database = mongoDBHandler.Database;

                scheduledTimehh = (int)Message["scheduledtime"]["hh"].GetValueOrDefault(01);
                scheduledTimemm = (int)Message["scheduledtime"]["mm"].GetValueOrDefault(15);
                scheduledMs = (scheduledTimehh * 60 + scheduledTimemm) * 60 * 1000;
                maxDailyRunMinutes = (int)Message["maxdailyrunminutes"].GetValueOrDefault(40); // max daily operational time


                isInitialized = true;

                if (cachingThread == null)
                {
                    cachingThread = new Thread(RuncachingDaily);
                    cachingThread.Name = "SQL Handler SQL Thread";
                    cachingThread.Start();
                }
            }
        }

        // run SQL daily.
        // do not run SQL until tomorrow, if time now is past scheduled time. 
        private void RuncachingDaily()
        {
            while (!stopping.WaitOne(0))
            {
                try
                {
                    if (!MongoDBHandlerServer.SchemaApplied)
                    {
                        logger.LogTrace("Schema not yet applied. Waiting for it.");
                        stopping.WaitOne(30000);
                    }
                    else
                    {
                        logger.LogInfo("SQLr will be run as scheduled at ", scheduledTimehh, scheduledTimemm);
                        stopping.WaitOne(GetMillisecondsToScheduledTime());

                        SQL();
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError("Failed to SQL.", ex);
                }
            }
        }

        private void SQL()
        {
            logger.LogInfo("Start caching");

            var endTimer = new Stopwatch();
            endTimer.Start();

            foreach (DataTree schemaCollection in MongoDBHandlerServer.Schema["tro"])
            {
                if ((bool)schemaCollection["collection"]["SQL"])
                {
                    SQLCollection(schemaCollection, endTimer);
                }
            }

            logger.LogInfo("caching done.");
        }

        /// <summary>
        /// SQL documents in collection that meet the conditions specified in schema.
        /// </summary>
        /// <param name="schemaCollection">Collection in schema.</param>
        /// <param name="endTimer">Stopwatch tracking time spent caching documents</param>
        private void SQLCollection(DataTree schemaCollection, Stopwatch endTimer)
        {
            logger.LogInfo("caching collection", schemaCollection.Name);

            IMongoQuery query = GetSQLCollectionConditions(schemaCollection);
            MongoCollection<BsonDocument> sourceCollection = database.GetCollection(schemaCollection.Name);
            MongoCollection<BsonDocument> targetCollection = database.GetCollection(schemaCollection.Name);

            MongoCursor cursorSourceCollection = sourceCollection.Find(query);

            if (cursorSourceCollection == null) return;

            int countSQLd = 0;

            foreach (BsonDocument sourceDocument in cursorSourceCollection)
            {
                if (SQLDocument(sourceDocument, sourceCollection, targetCollection, schemaCollection["collection"]))
                    countSQLd++;

                if (endTimer.Elapsed.TotalMinutes >= maxDailyRunMinutes)
                {
                    logger.LogWarning("caching stopped for today. Total caching time reached.", maxDailyRunMinutes);
                    break;
                }
            }

            logger.LogInfo("SQLd collection entries", schemaCollection.Name, countSQLd);
        }

        private bool SQLDocument(
            BsonDocument sourceDocument,
            MongoCollection<BsonDocument> sourceCollection,
            MongoCollection<BsonDocument> targetCollection,
            DataTree schemaCollection)
        {
            bool documentSQLd = false;

            try
            {
                logger.LogDebug("caching document", sourceCollection.Name, sourceDocument[DBDocument.Id]);

                // Save document to SQL collection
                sourceDocument["__SQLd"] = MC2DateTimeValue.Now();
                targetCollection.Save(sourceDocument);

                // Purge original doucment
                sourceCollection.Remove(Query.EQ(DBDocument.Id, sourceDocument[DBDocument.Id]));

                documentSQLd = true;
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to SQL document", sourceCollection.Name, sourceDocument[DBDocument.Id], ex);
            }

            return documentSQLd;
        }

        /// <summary>
        /// Return query for determining which documents are SQLd.
        /// </summary>
        /// <param name="schemaCollection"></param>
        /// <returns></returns>
        private IMongoQuery GetSQLCollectionConditions(DataTree schemaCollection)
        {
            var andQueries = new List<IMongoQuery>();

            andQueries.Add(GetSQLCollectionConditionQuery(schemaCollection));

            // Get caching limits
            const int DefaultSQLDays = 240;

            // Get SQL limits as UTC timestamp
            var now = DateTime.Now;
            var dateNowUtc = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);

            // Get SQL limits. Use given values if any are specified, if nothing is specified, default to using
            // modified timestamp and 240 days.
            if (schemaCollection.Contains("SQLagedayscreated"))
            {
                int SQLAgeDaysCreated = (int)schemaCollection["SQLagedayscreated"].GetValueOrDefault(DefaultSQLDays);
                DateTime SQLDateCreated = dateNowUtc.AddDays(-SQLAgeDaysCreated);
                andQueries.Add(Query.LT("created", SQLDateCreated));
            }

            if (schemaCollection.Contains("SQLagedaysmodified") || !schemaCollection.Contains("SQLagedayscreated"))
            {
                int SQLAgeDaysModified = (int)schemaCollection["SQLagedaysmodified"].GetValueOrDefault(DefaultSQLDays);
                DateTime SQLDateModified = dateNowUtc.AddDays(-SQLAgeDaysModified);
                andQueries.Add(Query.LT("modified", SQLDateModified));
            }

            return Query.And(andQueries.ToArray());
        }

        private IMongoQuery GetSQLCollectionConditionQuery(DataTree schemaCollection)
        {
            IMongoQuery query = new QueryDocument(BsonDocument.Parse("{}")); ;

            string condition = schemaCollection["collection"]["SQLfilter"];
            if (!string.IsNullOrEmpty(condition))
            {
                try
                {
                    query = new QueryDocument(BsonDocument.Parse(condition));
                }
                catch (Exception ex)
                {
                    logger.LogError("Failed to parse JSON when determining caching query", ex.Message, condition);
                }
            }

            return query;
        }

        private int GetMillisecondsToScheduledTime()
        {
            int msElapsedToday = (DateTime.Now.Hour * 60 + DateTime.Now.Minute) * 60 * 1000;
            if (msElapsedToday >= scheduledMs) { return (scheduledMs - msElapsedToday + MsPerDay); } // tomorrow
            else { return (scheduledMs - msElapsedToday); } // today
        }
    }
}

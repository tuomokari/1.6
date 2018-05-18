using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Shared;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace UtcDateUpdateTool
{
    class UtcDateUpdateTool
    {
        MongoServer server;
        MongoDatabase database;

        static void Main(string[] args)
        {
            if (args.Length < 5)
            {
                Console.Out.WriteLine("Usage: UtcDateUpdateTool server port database collection datefield");
            }

            var program = new UtcDateUpdateTool();

            program.Run(args[0], args[1], args[2], args[3], args[4]);
        }

        public UtcDateUpdateTool()
        {
        }

        public void Run(string serverAddress, string serverPort, string databaseName, string collectionName, string fieldName)
        {
            MongoServerAddress address = new MongoServerAddress(serverAddress, Convert.ToInt32(serverPort));
            var settings = new MongoServerSettings();
            settings.Server = address;

            server = new MongoServer(settings);
            database = server.GetDatabase(databaseName);

            MongoCollection<BsonDocument> collection = database.GetCollection(collectionName);

            MongoCursor<BsonDocument> cursor = collection.FindAll();

            foreach (BsonDocument document in cursor)
            {
                if (document.Contains(fieldName) && document[fieldName].IsValidDateTime)
                {
                    DateTime dtOriginal = (DateTime)document[fieldName];

                    dtOriginal = dtOriginal.AddHours(12);

                    DateTime dtNew = new DateTime(dtOriginal.Year, dtOriginal.Month, dtOriginal.Day, 0, 0, 0, DateTimeKind.Utc);

                    document[fieldName] = dtNew;

                    collection.Save(document);

                    Console.Out.WriteLine("Found and updated datetime. Id: " + document["_id"].ToString() + ". Original: " + dtOriginal.ToString() + ". New: " + dtNew);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SystemsGarden.mc2.RemoteConnector.Handlers.CoreServerHandlers.MongoDBHandler;
using MongoDB.Bson;

namespace SystemsGarden.mc2.RemoteConnector.Handlers.SapIntegrationHandlerServer
{
    public static class BsonDocumentExtensions
    {
        public static void SetExtended(this BsonDocument document, string collectionName, string fieldName, BsonValue fieldValue)
        {
            if ((bool)document.GetValue("disableintegrationchangesfordocument", false) && (bool)MongoDBHandlerServer.Schema.First[collectionName][fieldName]["disableintegrationchanges"])
                    return;


            document.Set(fieldName, fieldValue);
        }

        public static void RemoveExtended(this BsonDocument document, string collectionName, string fieldName)
        {
            if ((bool)document.GetValue("disableintegrationchangesfordocument", false) && (bool)MongoDBHandlerServer.Schema.First[collectionName][fieldName]["disableintegrationchanges"])
                return;

            document.Remove(fieldName);
        }
    }
}

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
    public class TimesheetEntryWithDetails
    {
        public BsonDocument TimesheetEntry { get; set; }
        public List<Tuple<BsonDocument, BsonDocument>> EntryDetailsAndPayTypes = new List<Tuple<BsonDocument, BsonDocument>>();
        public string WorkerCategory { get; set; }
        public string WorkerId { get; set; }
        public string WorkerProfitCenter { get; set; }
        public string ProjectId { get; set; }
        public string Note { get; set; }
    }
}

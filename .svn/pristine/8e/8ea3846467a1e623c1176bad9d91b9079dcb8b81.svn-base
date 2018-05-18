using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Shared;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace SystemsGarden.mc2.RemoteConnector.Handlers.TroIntegrationCommon
{
    public class TimesheetEntryFragment : ICloneable
    {
        public bool IsRootEntry = false;

        public DateTime Start;
        public DateTime End;
        public bool IsTravelTime;

        public BsonDocument Detail;
        public BsonDocument PayType;

        // Base for project category calculation if no direct project category exists.
        // If there is a direct category it can be found in detail type (TimesheetEntryDetailType)
        public string ProjectCategoryBase;
        public string WorkerId;
        public string ProjectId;
        public string WorkerProfitCenter;
        public string Note = string.Empty;

        public object Clone()
        {
            var clone = new TimesheetEntryFragment();

            clone.IsRootEntry = IsRootEntry;
            clone.Start = Start;
            clone.End = End;
            clone.Detail = Detail;
            clone.PayType = PayType;
            clone.ProjectCategoryBase = ProjectCategoryBase;
            clone.ProjectId = ProjectId;
            clone.WorkerId = WorkerId;
            clone.IsTravelTime = IsTravelTime;
            clone.WorkerProfitCenter = WorkerProfitCenter;

            return clone;
        }
    }
}

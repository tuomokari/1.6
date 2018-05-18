using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SystemsGarden.mc2.MC2SiteEnvironment;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.Common;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using MongoDB.Bson;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.Common;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using MongoDB.Bson;
using SystemsGarden.mc2.Core.Runtime;

namespace SystemsGarden.mc2.widgets.workdatawidget
{
	public class workdatawidget : MC2Controller
	{
        #region Actions

        [GrantAccessToGroup("authenticated")]
        public ActionResult getEntryData(DateTime start, DateTime end)
        {
            var entriesQuery = new DBQuery("workdatawidget", "entrydata");
            entriesQuery.AddParameter("user", Runtime.SessionManager.CurrentUser[DBQuery.Id]);
            entriesQuery.AddParameter("start", start);
            entriesQuery.AddParameter("end", end);

            DBResponse response = entriesQuery.Find();
            var allocations = response["allocationentry"];
             
            foreach (DBDocument allocation in allocations)
            {
                allocation["project"] = DBDocument.FindOne("project", allocation["project"][DBDocument.Id]);
            }

            return new AjaxResult((DataTree)response);
        }

        #endregion
    }
}
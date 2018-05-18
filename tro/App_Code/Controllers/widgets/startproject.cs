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

namespace SystemsGarden.mc2.widgets.startproject
{
	public class startproject : MC2Controller
	{
		#region Actions

		[GrantAccessToGroup("authenticated")]
		public ActionResult startworkonproject(string projectid)
		{
			if (!string.IsNullOrEmpty(projectid))
			{
				// When starting work first look for existing allocation without date. If we have one we should
				// start that allocation instead of generating a new one.
				var existingAllocationQuery = new DBQuery("startproject", "unscheduledallocation");
				existingAllocationQuery.AddParameter("user", new ObjectId(Runtime.SessionManager.CurrentUser["_id"]));
				existingAllocationQuery.AddParameter("project", new ObjectId(projectid));

				DBDocument allocationEntry = existingAllocationQuery.FindOne();

				if (allocationEntry == null)
					allocationEntry = new DBDocument("allocationentry");

				allocationEntry.AddRelation("project", projectid);
				allocationEntry.AddRelation("user", Runtime.SessionManager.CurrentUser["_id"]);
				allocationEntry["status"] = "In progress";

				allocationEntry.UpdateDatabaseAsync();

				return new AjaxResult("success");

			}
			else
			{
				return new AjaxResult("No identifier provided. Cannot start work.");
			}
		}

		#endregion
	}
}
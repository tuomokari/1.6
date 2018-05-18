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
using System.Globalization;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.Common;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using MongoDB.Bson;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.tro;

namespace SystemsGarden.mc2.widgets.homescreenprojects
{
	public class projectleadmodule : MC2Controller
	{
		#region Actions

		[GrantAccessToGroup("authenticated")]
		public ActionResult getprojectleadreport(
			string userfilter,
			string userlist,
			string payrollperiod,
			string project,
			MC2DateTimeValue startdate,
			MC2DateTimeValue enddate)
		{
			// Send request to TroHelpsersHandler. Needs to be handled in the server because frontend
			// has no access to audit data.

			var projectLeadReportMessage = new RCMessage("tro_getprojectleadreport");

			DataTree handler = projectLeadReportMessage.Handlers["trohelpershandler"];

			handler["user"] = Runtime.SessionManager.CurrentUser[DBQuery.Id];
			handler["userfilter"] = userfilter;
			handler["userlist"] = userlist;
			handler["payrollperiod"] = payrollperiod;
			handler["startdate"] = startdate;
			handler["enddate"] = enddate;
			handler["project"] = project;

			RCResponse response = Runtime.SendRemoteMessage(projectLeadReportMessage);

			// Return documents as arrays
			string[] documentTypes = { "timesheetentry", "dayentry", "articleentry" };
			foreach (var documentType in documentTypes)
				response["handlers"]["trohelpershandler"]["report"][documentType].JsonSerializationType = JsonSerializationType.ChildrenAsArrays;

			return new AjaxResult(response);
		}


		#endregion
	}
}
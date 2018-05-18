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

namespace SystemsGarden.mc2.widgets.dailyresourcingwidget
{
	public class dailyresourcingwidget : MC2Controller
	{

		#region Members

		private int defaultStartTimeHour;
		private int defaultStartTimeMinute;
		private int defaultEndTimeHour;
		private int defaultEndTimeMinute;

		#endregion

		#region Init


		public override void Init()
		{
		}

		#endregion

		#region Actions

		[GrantAccessToGroup("authenticated")]
		public ActionResult projectdetails(string projectid)
		{
			return View(DBDocument.FindOne("project", projectid));
		}

        #endregion

        [GrantAccessToGroup("authenticated")]
        public AjaxResult allocateuser(
            string user,
            string project,
            DateTime start,
            DateTime end,
            bool ignoreconflicts = false)
        {
            // Get allocations for given date
            string resultValue = "success";

            bool confilictingAllocationFound = false;

            if (!ignoreconflicts)
            {
                DBQuery query = new DBQuery("dailyresourcingwidget", "getuserallocation");
                query.AddParameter("user", new ObjectId(user));
                query.AddParameter("start", start);
                query.AddParameter("end", end);

                DBResponse result = query.FindAsync(new DBCallProperties() { RunWithPrivileges = 5 }).Result;

                if (result["allocationentry"].Count > 0)
                    confilictingAllocationFound = true;
            }

            if (confilictingAllocationFound)
            {
                resultValue = "conflict";
            }
            else
            {
                var newAllocation = new DBDocument("allocationentry");
                newAllocation.AddRelation("user", user);
                newAllocation.AddRelation("project", project);

                newAllocation["starttimestamp"] = start;
                newAllocation["endtimestamp"] = end;

                newAllocation.UpdateDatabase();
            }

            return new AjaxResult(resultValue);
        }

        [GrantAccessToGroup("authenticated")]
        public AjaxResult allocateasset(
            string asset,
            string project,
            DateTime start,
            DateTime end,
            string status,
            bool ignoreconflicts = false)
        {
            // Get allocations for given date
            string resultValue = "success";

            bool confilictingAllocationFound = false;

            if (!ignoreconflicts)
            {
                DBQuery query = new DBQuery("dailyresourcingwidget", "getassetallocation");
                query.AddParameter("asset", new ObjectId(asset));
                query.AddParameter("start", start);
                query.AddParameter("end", end);

                DBResponse result = query.FindAsync(new DBCallProperties() { RunWithPrivileges = 5 }).Result;

                if (result["allocationentry"].Count > 0)
                    confilictingAllocationFound = true;
            }

            if (confilictingAllocationFound)
            {
                resultValue = "conflict";
            }
            else
            {
                var newAllocation = new DBDocument("allocationentry");
                newAllocation.AddRelation("asset", asset);
                newAllocation.AddRelation("project", project);

                newAllocation["starttimestamp"] = start;
                newAllocation["endtimestamp"] = end;

                newAllocation.UpdateDatabase();
            }

            return new AjaxResult(resultValue);
        }

        [GrantAccessToGroup("authenticated")]
		public AjaxResult unallocate(
			string allocationentry)
		{

			var timesheetToRemoveQuery = new DBDocument("allocationentry", allocationentry);

			timesheetToRemoveQuery.RemoveFromDatabase();

			return new AjaxResult("success");
		}
	}
}
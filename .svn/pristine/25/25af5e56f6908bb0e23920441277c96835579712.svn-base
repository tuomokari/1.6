using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SystemsGarden.mc2.MC2SiteEnvironment;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.Common;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using System.Diagnostics;
using MongoDB.Bson;

namespace SystemsGarden.mc2.widgets.totalwork
{
	/// <summary>
	/// Counts hour, expense and absence totals per user
	/// </summary>
	public class totalwork : MC2Controller
	{
		object payTypeLock = new object();
		DBCollection payTypes = null;
		DBCollection expenseTypes = null;
		DBDocument socialProject = null;

		const int PayTypeUpdateIntervalMinutes = 10;

		/// <summary>
		/// This stopwatch tracks cached paytype age.
		/// </summary>
		Stopwatch payTypeUpdateStopwatch = new Stopwatch();

		#region Actions
		[GrantAccess(3, 4, 5, 6, 7)]
		public AjaxResult gettotals(
			string userfilter,
			string projectfilter,
			string assetfilter,
			string profitcenterfilter,
			string resourceprofitcenterfilter,
			string resourcebusinessarea,
			string resourcefunctionalarea,
			string managerprojectsfilter,
			string payrollperiodfilter,
			string favouriteusersfilter,
			bool showonlyentriesnotaccepted,
			MC2DateTimeValue daterangestart = null,
			MC2DateTimeValue daterangeend = null)
		{
			int maxTotalDocuments = (int)Runtime.Config["totalwork"]["maxdocuments"].GetValueOrDefault(1000);

			var timesheetQueries = new List<IMongoQuery>();
			var absenceQueries = new List<IMongoQuery>();
			var expenseQueries = new List<IMongoQuery>();

			SystemsGarden.mc2.tro.approveworklistview.AddFiltersToQuery(
				"timesheetentry",
				userfilter, projectfilter, assetfilter, profitcenterfilter, resourceprofitcenterfilter, 
				resourcebusinessarea, resourcefunctionalarea, managerprojectsfilter, payrollperiodfilter,
				favouriteusersfilter, daterangestart, daterangeend, showonlyentriesnotaccepted, timesheetQueries);

			SystemsGarden.mc2.tro.approveworklistview.AddFiltersToQuery(
				"absenceentry",
				userfilter, projectfilter, assetfilter, profitcenterfilter, resourceprofitcenterfilter,
				resourcebusinessarea, resourcefunctionalarea, managerprojectsfilter, payrollperiodfilter,
				favouriteusersfilter, daterangestart, daterangeend, showonlyentriesnotaccepted, absenceQueries);

			SystemsGarden.mc2.tro.approveworklistview.AddFiltersToQuery(
				"dayentry",
				userfilter, projectfilter, assetfilter, profitcenterfilter, resourceprofitcenterfilter,
				resourcebusinessarea, resourcefunctionalarea, managerprojectsfilter, payrollperiodfilter,
				favouriteusersfilter, daterangestart, daterangeend, showonlyentriesnotaccepted, expenseQueries);

			DBQuery totalsQuery = new DBQuery();

			totalsQuery["timesheetentry"][DBQuery.Condition] = Query.And(timesheetQueries).ToString();
			totalsQuery["timesheetentry"][DBQuery.DocumentsPerPage] = maxTotalDocuments;
			totalsQuery["timesheetentry"][DBQuery.SpecifiedFieldsOnly] = true;
			totalsQuery["timesheetentry"]["duration"].Create();
			totalsQuery["timesheetentry"]["timesheetentrydetailpaytype"].Create();
			totalsQuery["timesheetentry"]["user"].Create();
			totalsQuery["timesheetentry"]["project"].Create();


			totalsQuery["absenceentry"][DBQuery.Condition] = Query.And(absenceQueries).ToString();
			totalsQuery["absenceentry"][DBQuery.DocumentsPerPage] = maxTotalDocuments;
			totalsQuery["absenceentry"][DBQuery.SpecifiedFieldsOnly] = true;
			totalsQuery["absenceentry"]["duration"].Create();
			totalsQuery["absenceentry"]["absenceentrytype"].Create();
			totalsQuery["absenceentry"]["user"].Create();

			totalsQuery["dayentry"][DBQuery.Condition] = Query.And(expenseQueries).ToString();
			totalsQuery["dayentry"][DBQuery.DocumentsPerPage] = maxTotalDocuments;
			totalsQuery["dayentry"][DBQuery.SpecifiedFieldsOnly] = true;
			totalsQuery["dayentry"]["amount"].Create();
			totalsQuery["dayentry"]["dayentrytype"].Create();
			totalsQuery["dayentry"]["user"].Create();

			DBResponse totalsResult = totalsQuery.Find();

			if (totalsResult["timesheetentry"].Count >= maxTotalDocuments ||
				totalsResult["dayentry"].Count >= maxTotalDocuments ||
				totalsResult["absenceentry"].Count >= maxTotalDocuments)
				return new AjaxResult("too many results");

			DataTree userTotals = CountResults(totalsResult);

			return new AjaxResult((DataTree)userTotals);
		}

		private DataTree CountResults(DBResponse totalsResult)
		{
			var userTotals = new DataTree();
			userTotals.JsonSerializationType = JsonSerializationType.ChildrenAsArrays;

			bool useSocialProject = (bool)Runtime.Config["application"]["features"]["enablesocialproject"];
			string socialProjectId = null;

			if (useSocialProject)
				socialProjectId = GetSocialProjectId();

			DBCollection timesheetEntries = totalsResult["timesheetentry"];

			foreach (DBDocument timesheetEntry in timesheetEntries)
			{
				string userId = VerifyUser(timesheetEntry, userTotals);
				if (string.IsNullOrEmpty(userId))
					continue;

				string timsheetEntryId = timesheetEntry[DBQuery.Id];

				DBDocument payType = GetPayType(timesheetEntry["timesheetentrydetailpaytype"][DBQuery.Id]);

				if (payType == null)
					continue;

				string payTypeId = payType[DBQuery.Id];

                if ((bool)payType["countsasregularhours"])
                {
                    userTotals[userId]["totalworkinghours"] =
                    (decimal)userTotals[userId]["totalworkinghours"].GetValueOrDefault(0) +
                    DurationToHours((int)timesheetEntry["duration"].GetValueOrDefault(0));
                }

                if ((bool)payType["isovertime50"] || (bool)payType["isovertime100"] || (bool)payType["isovertime150"] || (bool)payType["isovertime200"])
				{
					userTotals[userId]["overtime"] =
						(decimal)userTotals[userId]["ovetime"].GetValueOrDefault(0) +
						DurationToHours((int)timesheetEntry["duration"].GetValueOrDefault(0));
				}

				if (useSocialProject && socialProjectId != null && timesheetEntry["project"][DBQuery.Id] == socialProjectId) {
					userTotals[userId]["socialproject"] =
						(decimal)userTotals[userId]["ovetime"].GetValueOrDefault(0) +
						DurationToHours((int)timesheetEntry["duration"].GetValueOrDefault(0));
				}
			}

			DBCollection absenceEntries = totalsResult["absenceentry"];

			foreach (DBDocument absenceEntry in absenceEntries)
			{
				string userId = VerifyUser(absenceEntry, userTotals);
				if (string.IsNullOrEmpty(userId))
					continue;

				string absenceId = absenceEntry[DBQuery.Id];

				userTotals[userId]["totalabsences"] =
					(decimal)userTotals[userId]["totalabsences"].GetValueOrDefault(0) +
					DurationToHours((int)absenceEntry["duration"].GetValueOrDefault(0));
			}

			DBCollection expenseEntries = totalsResult["dayentry"];
			foreach (DBDocument expenseEntry in expenseEntries)
			{
				string userId = VerifyUser(expenseEntry, userTotals);
				if (string.IsNullOrEmpty(userId))
					continue;

				string expenseTypeId = expenseEntry["dayentrytype"][DBQuery.Id];

				DBDocument expenseType = GetExpenseType(expenseTypeId);

				if (expenseType == null)
					continue;

				if ((bool)expenseType["totalsgroup"])
				{
					userTotals[userId]["expensetypes"][expenseType["totalsgroup"]] =
						(int)userTotals[userId]["expensetypes"][expenseType["totalsgroup"]].GetValueOrDefault(0) +
						(int)expenseEntry["amount"].GetValueOrDefault(0);
				}
			}

			return userTotals;
		}

		private string VerifyUser(DBDocument document, DataTree userTotals)
		{
			string userId = document["user"];

			if (string.IsNullOrEmpty(userId))
				return null;
			
			if (!userTotals.Contains(userId))
			{
				userTotals[userId]["__displayname"] = document["user"]["__displayname"];
			}

			return userId;
		}

		private decimal DurationToHours(int duration)
		{
			return Math.Round((decimal)duration / 1000 / 6 / 6) / 100;
		}

		private DBDocument GetPayType(string id)
		{
			DBCollection payTypes = GetPayTypes();
			return (payTypes.Contains(id)) ? payTypes[id] : null;
		}

		private DBDocument GetExpenseType(string id)
		{
			DBCollection expenseTypes = GetExpenseTypes();
			return (expenseTypes.Contains(id)) ? expenseTypes[id] : null;
		}

		private DBCollection GetPayTypes()
		{
			UpdatePayTypesIfNeeded();

			return payTypes;
		}

		private DBCollection GetExpenseTypes()
		{
			UpdatePayTypesIfNeeded();

			return expenseTypes;
		}

		private string GetSocialProjectId()
		{
			UpdatePayTypesIfNeeded();

			if (socialProject != null)
				return socialProject[DBQuery.Id];
			else
				return null;
		}

		private void UpdatePayTypesIfNeeded()
		{
			if (payTypeUpdateStopwatch.Elapsed.Minutes > PayTypeUpdateIntervalMinutes || 
				expenseTypes == null || payTypes == null)
			{
				var query = new DBQuery("totalwork", "paytypes");
				var result = query.Find();

				lock (payTypeLock)
				{
					payTypes = result["timesheetentrydetailpaytype"];
					expenseTypes = result["dayentrytype"];

					if (result["socialproject"].Count >= 0)
						socialProject = result["socialproject"][0];
				}

				payTypeUpdateStopwatch.Restart();
			}
		}

		#endregion
	}
}
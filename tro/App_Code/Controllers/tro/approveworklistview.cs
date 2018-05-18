using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SystemsGarden.mc2.MC2SiteEnvironment;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.Core;
using SystemsGarden.mc2.Common.Constants;
using SystemsGarden.mc2.Common;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using MongoDB.Bson;
using SystemsGarden.mc2.MC2Site.App_Code.Controllers.__builtin;

namespace SystemsGarden.mc2.tro
{
	public class approveworklistview : listview
    {
		[GrantAccessToGroup("authenticated")]
		public ActionResult approveworkentry(
            string terms,
            string collection,
            string orderby,
            bool ascending,
            int documentsperpage,
            int page,
            string relation,
            string relationid,
            bool islocalrelation,
            string localcollection,
            string viewcontroller,
            string viewaction,
            MC2DateTimeValue rangestart = null,
            MC2DateTimeValue rangeend = null,
            bool showall = false,
            bool saveAsCsv = false,
            string csvFieldsStr = "")
        {
            string userId = Runtime.SessionManager.CurrentUser[DBQuery.Id];

            var timesheetEntryAndQueries = new List<IMongoQuery>();

			// Entries with parent entries are details for other TSEs and should not be shown.
			timesheetEntryAndQueries.Add(Query.NotExists("parent"));

			if (showall)
			{
				// When showing all get items assigned to or created by current user
				timesheetEntryAndQueries.Add(
					Query.Or(
						Query.EQ("creator", new ObjectId(userId)),
						Query.And(
							Query.EQ("user", new ObjectId(Runtime.SessionManager.CurrentUser["_id"])),
							Query.EQ("approvedbyworker", true)
							)));
			}
			else
			{
				timesheetEntryAndQueries.Add(
						Query.EQ("creator", new ObjectId(userId)));
			}

			if (collection == "timesheetentry")
                timesheetEntryAndQueries.Add(Query.Exists("endtimestamp"));

            if (showall)
            {
                string dateField = string.Empty;

                if (collection == "timesheetentry" || collection == "absenceentry" || collection == "assetentry")
                    dateField = "starttimestamp";
                else if (collection == "dayentry")
                    dateField = "date";
                else if (collection == "articleentry")
                    dateField = "timestamp";

                timesheetEntryAndQueries.Add(Query.GTE(dateField, (DateTime)rangestart));
                timesheetEntryAndQueries.Add(Query.LT(dateField, (DateTime)rangeend));
            }
            else
            {
                timesheetEntryAndQueries.Add(Query.NE("approvedbyworker", true));
            }

            return showfilteredlistview(
                terms,
                collection,
                orderby,
                ascending,
                documentsperpage,
                page,
                relation,
                relationid,
                islocalrelation,
                localcollection,
                Query.And(timesheetEntryAndQueries),
                false,
                viewcontroller,
                viewaction,
                false,
                saveAsCsv,
                csvFieldsStr
                );
        }

        [GrantAccess(3, 4, 5, 6)]
        public ActionResult approveworkmanagerentry(
            string terms,
            string collection,
            string orderby,
            bool ascending,
            int documentsperpage,
            int page,
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
            MC2DateTimeValue rangestart = null,
            MC2DateTimeValue rangeend = null,
            bool showonlyentriesnotaccepted = false,
			bool includetotals = false,
            bool saveAsCsv = false,
            string csvFieldsStr = "")
        {
			var timesheetEntryAndQueries = new List<IMongoQuery>();

			// TSEs with parents are details for other TSEs and should never be shown.
			timesheetEntryAndQueries.Add(Query.NotExists("parent"));

			bool hasFilters = AddFiltersToQuery(
				collection,
				userfilter,
				projectfilter,
				assetfilter,
				profitcenterfilter,
				resourceprofitcenterfilter,
				resourcebusinessarea,
				resourcefunctionalarea,
				managerprojectsfilter,
				payrollperiodfilter,
				favouriteusersfilter,
				rangestart,
				rangeend,
				showonlyentriesnotaccepted,
				timesheetEntryAndQueries);

			return showfilteredlistview(
				terms,
				collection,
				orderby,
				ascending,
				documentsperpage,
				page,
				string.Empty,
				string.Empty,
				false,
				string.Empty,
				Query.And(timesheetEntryAndQueries),
				true,
				"listview",
				"listviewresults",
				includetotals,
                saveAsCsv,
                csvFieldsStr);
        }

        [GrantAccess(4, 5, 6)]
        public ActionResult approveworkhrentry(
            string terms,
            string collection,
            string orderby,
            bool ascending,
            int documentsperpage,
            int page,
            string userfilter,
            string payrollperiodfilter,
            bool showonlyentriesnotacceptedbymanager,
            bool showonlyentriesnotacceptedbyworker,
            bool showonlyexportedentries,
			bool includetotals)
        {

            var timesheetEntryAndQueries = new List<IMongoQuery>();

            // Filter based on user
            if (!string.IsNullOrEmpty(userfilter))
                timesheetEntryAndQueries.Add(Query.EQ("user", new ObjectId(userfilter)));

            AddPayrollFilter(collection, payrollperiodfilter, timesheetEntryAndQueries);

            // Filter based on worker acceptance status
            if (showonlyentriesnotacceptedbyworker)
            {
                timesheetEntryAndQueries.Add(Query.NE("approvedbyworker", true));
            }
            else
            {
                timesheetEntryAndQueries.Add(Query.EQ("approvedbyworker", true));

                // Filter based on manager acceptance status 
                if (showonlyentriesnotacceptedbymanager)
                    timesheetEntryAndQueries.Add(Query.EQ("approvedbymanager", false));// 26062017
                else
                    timesheetEntryAndQueries.Add(Query.EQ("approvedbymanager", true));
            }

            // Filter based on Export status
            if (showonlyexportedentries)
            {
                timesheetEntryAndQueries.Add(Query.EQ("exported_visma", true));
            }
            else
            {
                timesheetEntryAndQueries.Add(Query.NE("exported_visma", true));
            }

            // Filter out external workers
            timesheetEntryAndQueries.Add(Query.EQ("internalworker", true));

            // If nothing is selected show nothing
            if (string.IsNullOrEmpty(userfilter) && string.IsNullOrEmpty(payrollperiodfilter))
            {
                timesheetEntryAndQueries.Add(Query.EQ("creator", ""));
                timesheetEntryAndQueries.Add(Query.NE("creator", ""));
            }

            return showfilteredlistview(
                terms,
                collection,
                orderby,
                ascending,
                documentsperpage,
                page,
                string.Empty,
                string.Empty,
                false,
                string.Empty,
                Query.And(timesheetEntryAndQueries),
				false,
				"listview",
				"listviewresults",
				includetotals);
        }

		// Public access due to being used in totals widget
		public static bool AddFiltersToQuery(
            string collection,
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
            MC2DateTimeValue rangestart,
            MC2DateTimeValue rangeend,
            bool showonlyentriesnotaccepted,
            List<IMongoQuery> timesheetEntryAndQueries)
		{
			bool hasFilters = false;

			hasFilters = AddPayrollFilter(collection, payrollperiodfilter, timesheetEntryAndQueries, hasFilters);

			hasFilters = AddFavouriteUsersFilter(collection, favouriteusersfilter, timesheetEntryAndQueries, hasFilters);

			if (!string.IsNullOrEmpty(userfilter))
			{
				timesheetEntryAndQueries.Add(Query.EQ("user", new ObjectId(userfilter)));
				hasFilters = true;
			}

			if (!string.IsNullOrEmpty(projectfilter))
			{
				timesheetEntryAndQueries.Add(Query.EQ("project", new ObjectId(projectfilter)));
				hasFilters = true;
			}

			if (!string.IsNullOrEmpty(assetfilter))
			{
				timesheetEntryAndQueries.Add(Query.EQ("asset", new ObjectId(assetfilter)));
				hasFilters = true;
			}

			if (!string.IsNullOrEmpty(profitcenterfilter))
			{
				timesheetEntryAndQueries.Add(Query.EQ("profitcenter", new ObjectId(profitcenterfilter)));
				hasFilters = true;
			}

			if (!string.IsNullOrEmpty(resourceprofitcenterfilter))
			{
				timesheetEntryAndQueries.Add(
					Query.Or(
					Query.EQ("assetprofitcenter", new ObjectId(resourceprofitcenterfilter)),
					Query.EQ("userprofitcenter", new ObjectId(resourceprofitcenterfilter))
					));

				hasFilters = true;
			}

			if (!string.IsNullOrEmpty(resourcebusinessarea))
			{
				timesheetEntryAndQueries.Add(
					Query.Or(
					Query.EQ("assetbusinessarea", new ObjectId(resourcebusinessarea)),
					Query.EQ("userbusinessarea", new ObjectId(resourcebusinessarea))
					));

				hasFilters = true;
			}

			if (!string.IsNullOrEmpty(resourcefunctionalarea))
			{
				timesheetEntryAndQueries.Add(
					Query.Or(
					Query.EQ("assetfunctionalarea", new ObjectId(resourcefunctionalarea)),
					Query.EQ("userfunctionalarea", new ObjectId(resourcefunctionalarea))
					));

				hasFilters = true;
			}

			if (!string.IsNullOrEmpty(managerprojectsfilter))
			{
				timesheetEntryAndQueries.Add(Query.EQ("projectmanager", new ObjectId(managerprojectsfilter)));
				hasFilters = true;
			}

			if (!hasFilters)
			{
				// always false
				timesheetEntryAndQueries.Add(Query.EQ("creator", ""));
				timesheetEntryAndQueries.Add(Query.NE("creator", ""));
			}

			if (showonlyentriesnotaccepted)
			{
				timesheetEntryAndQueries.Add(Query.EQ("approvedbymanager", false));// 26062017
            }

			if (rangestart != null && rangeend != null)
			{
				string dateField = string.Empty;

				if (collection == "timesheetentry" || collection == "absenceentry" || collection == "assetentry")
					dateField = "starttimestamp";
				else if (collection == "dayentry")
					dateField = "date";
				else if (collection == "articleentry")
					dateField = "timestamp";

				timesheetEntryAndQueries.Add(Query.GTE(dateField, (DateTime)rangestart));
				timesheetEntryAndQueries.Add(Query.LT(dateField, (DateTime)rangeend));
			}

			timesheetEntryAndQueries.Add(Query.EQ("approvedbyworker", true));
			return hasFilters;
		}

		public static bool AddPayrollFilter(
            string collection,
            string payrollperiodfilter,
            List<IMongoQuery> timesheetEntryAndQueries,
            bool hasFilters = false)
        {
            DataTree payrollPeriod = null;
            if (!string.IsNullOrEmpty(payrollperiodfilter))
            {
                DBQuery payrollPeriodQuery = new DBQuery();
                payrollPeriodQuery["payrollperiod"][DBQuery.Condition] = Query.EQ(DBQuery.Id, new ObjectId(payrollperiodfilter)).ToString();

				payrollPeriod = payrollPeriodQuery.FindOne();

                if (payrollPeriod != null)
                {
                    if (collection == "dayentry")
                    {
                        // Nasty time hack to temporarily fix the issue of date picker not returning utc 00:00 based dates but rather including the time zone.
                        timesheetEntryAndQueries.Add(Query.GT("date", ((DateTime)payrollPeriod["startdate"]).AddHours(-12)));
                        timesheetEntryAndQueries.Add(Query.LTE("date", ((DateTime)payrollPeriod["enddate"]).AddHours(-12)));
                    }
                    else
                    {
                        DateTime startDate = (DateTime)payrollPeriod["startdate"];
                        DateTime endDate = (DateTime)payrollPeriod["enddate"];

                        // Convert dates to what would be the date's timestamp in local timezone. This is to take in to account the fact 
                        // that dates are not timestamps and to compare between date and a datetime we must assign a timezone to the date.
                        startDate = new DateTime(startDate.Year, startDate.Month, startDate.Day, 0, 0, 0, DateTimeKind.Local);
                        endDate = new DateTime(endDate.Year, endDate.Month, endDate.Day, 0, 0, 0, DateTimeKind.Local);

                        // Return time as UTC. The end result is to shift the Date's corresponding timestamp to whatever UTC
                        // time would be at the local timezone.
                        timesheetEntryAndQueries.Add(Query.GT("starttimestamp", startDate.ToUniversalTime()));
                        timesheetEntryAndQueries.Add(Query.LTE("starttimestamp", endDate.ToUniversalTime()));
                    }

                    timesheetEntryAndQueries.Add(Query.EQ("clacontract", new ObjectId(payrollPeriod["clacontract"])));
                    hasFilters = true;
                }
            }

            return hasFilters;
        }

		private static bool AddFavouriteUsersFilter(
		string collection,
		string favouriteusersfilter,
		List<IMongoQuery> timesheetEntryAndQueries,
		bool hasFilters = false)
		{
			if (string.IsNullOrEmpty(favouriteusersfilter))
				return hasFilters;

			DBDocument favouriteUsersDocument = DBDocument.FindOne("favouriteusers", favouriteusersfilter);

			// Fetch all 
			if (favouriteUsersDocument != null)
			{
				var favouriteUserOrQueries = new List<IMongoQuery>();

				foreach(DataTree user in favouriteUsersDocument["user"])
				{
					if (user.Contains(DBQuery.Id))
						favouriteUserOrQueries.Add(Query.EQ("user", new ObjectId(user[DBQuery.Id])));
				}

				timesheetEntryAndQueries.Add(Query.Or(favouriteUserOrQueries));
				hasFilters = true;
			}

			return hasFilters;
		}
	}
}
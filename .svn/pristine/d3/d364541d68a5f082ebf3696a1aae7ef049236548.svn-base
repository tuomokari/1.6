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
using SystemsGarden.mc2.MC2Site.App_Code.Controllers.__builtin;

namespace tro.App_Code.Controllers.tro
{
    public class searchhelpers : searchfilter
    {
		#region Actions

		// Get default project results if no search terms are specified and searched resulsts otherwise
		[GrantAccessToGroup("authenticated")]
		public ActionResult getprojectresults(string terms, string userid)
        {
            var splitters = new char[] { ' ' };
            string[] splitTerms = terms.Split(splitters, StringSplitOptions.RemoveEmptyEntries);

            if (removeWildcards)
                RemoveWildcards(splitTerms);

            DataTree resultData = null;

            if (splitTerms.Length > 0)
            {
                var dbQueries = new List<DBQuery>();

                DataTree schema = Runtime.RunBlock("core", "schemafor", "project");

                DBQuery resultsQuery = QuerySearchFilterTerm(splitTerms, schema, "project");

                if (resultsQuery != null)
                {

                    resultData = (DataTree)resultsQuery.Find().FirstCollection;

                    List<string> nameFields = Schema.GetNameFields(schema);

                    FilterResults(resultData, Schema.GetSearchFields(schema), nameFields);
                }
            }

            if (resultData != null && resultData.Count > 0)
                return NamedView("searchfilter", "searchfilterresult", resultData);
            else
                return NamedView("searchfilter", "searchfilterresult", MC2EmptyValue.EmptyValue);

        }

        internal DataTree GetSuggestedAllocationsForUser(string userId = null, int days = 1)
        {
            DateTime now = MC2DateTimeValue.Now();
            return GetSuggestedAllocationsForUser(userId, now, days);
        }

        internal DataTree GetSuggestedAllocationsForUser(string userId, DateTime dayInitial, int days)
        {
            if (string.IsNullOrEmpty(userId))
                userId = Runtime.SessionManager.CurrentUser[DBQuery.Id];

            // Get all allocation entries
            var allocationEntryQuery = new DBQuery();

            DateTime dayStart = new DateTime(dayInitial.Year, dayInitial.Month, dayInitial.Day);
            DateTime dayEnd;
            
            // From start of today to start of tomorrow
            dayEnd = dayInitial.AddDays(days);
            dayEnd = new DateTime(dayEnd.Year, dayEnd.Month, dayEnd.Day);

            // StartDate is less than end of the day and end date is more than start of the day
            List<IMongoQuery> dateRangeQueries = new List<IMongoQuery>();

            dateRangeQueries.Add(Query.LT("starttimestamp", new BsonDateTime(dayEnd)));
            dateRangeQueries.Add(Query.GT("endtimestamp", new BsonDateTime(dayStart)));

            List<IMongoQuery> searchQueries = new List<IMongoQuery>();

            searchQueries.Add(Query.And(dateRangeQueries));
            searchQueries.Add(Query.EQ("user", new ObjectId(userId)));

            allocationEntryQuery["allocationentry"][DBQuery.Condition] = Query.And(searchQueries).ToString();
            allocationEntryQuery["allocationentry"][DBQuery.OrderBy] = "endtimestamp";

            return (DataTree)allocationEntryQuery.Find().FirstCollection;
		}


        internal DataTree GetSuggestedProjectsForUser(string userId = null)
        {
            DataTree allocationEntries = GetSuggestedAllocationsForUser(userId);

            DataTree projects = new DataTree("project");

            // Get projects from allocations

            foreach (DataTree allocationEntry in allocationEntries)
            {
                DataTree project = allocationEntry["project"];

                projects[(string)allocationEntry["project"]["_id"]] = project;
            }

            return projects;
        }

        #endregion

    }
}
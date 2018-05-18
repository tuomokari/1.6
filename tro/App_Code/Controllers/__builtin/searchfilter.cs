using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.Common;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using MongoDB.Bson;

namespace SystemsGarden.mc2.MC2Site.App_Code.Controllers.__builtin
{
    public class searchfilter : MC2Controller, IMonitorDataSource
    {
        protected bool removeWildcards = false;

		private int searches = 0;

		public string MonitorDataSourceName
		{
			get
			{
				return "searchfilter";
			}
		}

		#region Init

		public override void Init()
        {
            removeWildcards = (bool)Runtime.Config["searchfilter"]["removewildcards"];
			Runtime.Monitoring.RegisterMonitorDataSource(this);
        }

		#endregion

		#region Actions

		/// <summary>
		/// Get results for search filter
		/// </summary>
		/// <param name="terms">Search term</param>
		/// <param name="rootschema">Schema controller name</param>
		/// <param name="collection">Collection "source" collection of the relation to query</param>
		/// <param name="valuename">Name of the relation item in the collection </param>
		/// <returns></returns>
		[GrantAccessToGroup("authenticated")]
		public ActionResult getresultsrelation(
            string terms,
            string rootschema,
            string collection,
            string valuename,
            string relationtarget,
            string itemid = "",
            string filtercontroller = "",
            string filteraction = "")
        {
            var splitters = new char[] { ' ' };
            string[] splitTerms = terms.Split(splitters, StringSplitOptions.RemoveEmptyEntries);

            if (removeWildcards)
                RemoveWildcards(splitTerms);

            // In case there are no search terms, use the default value.
            if (splitTerms.Length == 0)
            {
                DataTree results = 
                    GetDefaultResults(
                        rootschema,
                        collection,
                        valuename,
                        relationtarget,
                        itemid,
                        filtercontroller,
                        filteraction);

                if (results == null)
                    return NamedView("searchfilter", "searchfilterresult", MC2EmptyValue.EmptyValue);
                else
                    return NamedView("searchfilter", "searchfilterresult", results);
            }

            List<IMongoQuery> filterQueries = new List<IMongoQuery>();

            IMongoQuery query = Runtime.Filters.GetFilterQuery(filtercontroller, filteraction);

            if (query != null)
                filterQueries.Add(query);

            string relationTarget = Runtime.Schema.GetRelationTarget(rootschema, collection, valuename);
            DataTree schema = Runtime.RunBlock("core", "schemafor", relationTarget);

            DBQuery resultsQuery = QuerySearchFilterTerm(splitTerms, schema, relationTarget, -1, filterQueries.ToArray());

            DBResponse response = null;

            if (resultsQuery != null)
            {
				response = resultsQuery.Find();

                List<string> nameFields = Schema.GetNameFields(schema);

                FilterResults((DataTree)response.FirstCollection, Schema.GetSearchFields(schema), nameFields);
            }

            if (response != null && response.FirstCollection.Count > 0)
                return NamedView("searchfilter", "searchfilterresult", (MC2Value)response.FirstCollection, response.QueryInfo);
            else
                return NamedView("searchfilter", "searchfilterresult", MC2EmptyValue.EmptyValue);

        }

		[GrantAccessToGroup("authenticated")]
		public ActionResult getresultscollection(
            string terms,
            string rootschema,
            string collection,
            int documentperpage = -1,
            string filtercontroller = "",
            string filteraction = "")
        {
            var splitters = new char[] { ' ' };
            string[] splitTerms = terms.Split(splitters, StringSplitOptions.RemoveEmptyEntries);

            if (removeWildcards)
                RemoveWildcards(splitTerms);

            DataTree schema = Runtime.RunBlock("core", "schemafor", collection);

            var filterQueries = new List<IMongoQuery>();
            IMongoQuery query = Runtime.Filters.GetFilterQuery(filtercontroller, filteraction);
            if (query != null)
                filterQueries.Add(query);

            DBQuery resultsQuery = QuerySearchFilterTerm(splitTerms, schema, collection, documentperpage, filterQueries.ToArray());

            DBResponse response = null;

            if (resultsQuery != null)
            {
				response = resultsQuery.Find();

				List<string> nameFields = Schema.GetNameFields(schema);

				// Todo: consider not casting to datatree and using the collection.
                FilterResults((DataTree)response.FirstCollection, Schema.GetSearchFields(schema), nameFields);
            }

            if (response != null && response.FirstCollection.Count > 0)
                return NamedView("searchfilter", "searchfilterresult", (MC2Value)response.FirstCollection, response.QueryInfo);
            else
                return NamedView("searchfilter", "searchfilterresult", MC2EmptyValue.EmptyValue);

        }

        protected DBQuery QuerySearchFilterTerm(string[] splitTerms, DataTree schema, string relationTarget, int documentperpage = -1, IMongoQuery[] filters = null)
        {
            const int MinTermLength = 1;
            const int DefaultMaxSearchfilterResults = 15;

            var resultsQuery = new DBQuery();

            var searchFields = Schema.GetSearchFields(schema);
            if (searchFields.Count == 0)
                return null;

            var andQueries = new List<IMongoQuery>();

            if (filters != null)
                andQueries.AddRange(filters);
            
            IMongoQuery defaultQuery = listview.GetDefaultFilter(Runtime, relationTarget, "searchfilter");
    
            if (defaultQuery != null)
                andQueries.Add(defaultQuery);

            foreach (string term in splitTerms)
            {
                if (term.Length < MinTermLength)
                    continue;

                var orQueries = new List<IMongoQuery>();
                foreach (string searchField in searchFields)
                    orQueries.Add(Query.Matches(searchField, new BsonRegularExpression(term, "i")));

                if (orQueries.Count > 0)
                    andQueries.Add(Query.Or(orQueries));
            }

            if (andQueries.Count > 0)
            {
                resultsQuery[relationTarget][DBQuery.Condition] = Query.And(andQueries)
                    .ToString();

                if (documentperpage == -1)
                    documentperpage =  (int)Runtime.Config["searchfilter"]["documentsperpage"].GetValueOrDefault(DefaultMaxSearchfilterResults);

                resultsQuery[relationTarget][DBQuery.DocumentsPerPage] = documentperpage;

				Interlocked.Increment(ref searches);

				return resultsQuery;
            }
            else
            {
                return null;
            }
        }

        protected void FilterResults(
            DataTree results,
            List<string> searchFields,
            List<string> nameFields)
        {
            const string NameField = "__namefield";

            foreach (DataTree row in results)
            {
                for (int i = row.Length - 1; i >= 0; i--)
                {
                    var column = row[i];
                    bool remove = true;

                    if (nameFields.Contains(column.Name))
                    {
                        column[NameField] = true;
                        remove = false;
                    }

                    if (searchFields.Contains(column.Name))
                        remove = false;

                    if (column.Name == DBQuery.Id)
                        remove = false;

                    if (remove)
                        row[i].Remove();
                }
            }
        }

        #endregion

        protected void RemoveWildcards(string[] terms)
        {
            for (int i = 0; i < terms.Length; i++)
                terms[i] = terms[i].Replace("*", "");

        }

        // Use Nullable to distinguish from other ActionResults that get interpreted as 
        // MC2 actions
        protected DataTree GetDefaultResults(
            string rootschema,
            string collection,
            string valuename,
            string relationtarget,
            string itemid,
            string filtercontroller,
            string filteraction)
        {
            string defaultValueController = Runtime.Schema.First[collection][valuename]["defaultresultscontroller"];
            string defaultValueBlock = Runtime.Schema.First[collection][valuename]["defaultresultsblock"];
            string relation = Runtime.Schema.First[collection][valuename]["relation"];

            if (string.IsNullOrEmpty(defaultValueController) || string.IsNullOrEmpty(defaultValueBlock))
                return null;

            MC2Value result = Runtime.RunBlock(
                defaultValueController,
                defaultValueBlock,
                collection,
                valuename,
                relationtarget,
                itemid,
                filtercontroller,
                filteraction);

            if (result is MC2DataTreeValue)
            {
                // MC2 datatree value has no name and we use the relation's target
                DataTree dtResult =  ((MC2DataTreeValue)result).DataTreeValue;
                dtResult.Name = relation;
                return dtResult;
            }
            else
            {
                return null;
            }
        }

		public DataTree GetMonitorData()
		{
			var result = new DataTree();
			result["searches"] = searches;
			return result;
        }
	}
}
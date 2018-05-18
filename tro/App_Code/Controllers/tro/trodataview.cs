using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SystemsGarden.mc2.MC2SiteEnvironment;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.Common;
using System.Text;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using MongoDB.Bson;
using mc2.Controllers;

namespace mc2.Controllers.tro
{
    public class trodataview : MC2Controller
    {
        #region Actions
        [GrantAccess(3)]
        public ActionResult getprojects(
            string projectmanager,
            string searchterms,
            string orderby,
            bool ascending,
            int documentsperpage,
            int page)
        {
            var splitters = new char[] { ' ' };
            string[] splitTerms = searchterms.Split(splitters, StringSplitOptions.RemoveEmptyEntries);

            DBQuery query = QueryGetProjects(
                splitTerms,
                orderby,
                ascending,
                documentsperpage,
                page,
                projectmanager);

            if (query != null)
            {

				DBResponse response = query.Find();
				DataTree qi = response.QueryInfo;
				return new AjaxResult(GetJsonFromProjectsCollection((DataTree)response.FirstCollection));


                int totalRecords = (int)qi["totalrecords"];

                // Max page to refer to is number of pages -1
                int maxPage = (int)Math.Ceiling((decimal)totalRecords / (decimal)documentsperpage) - 1;

                totalRecords = (int)qi["totalrecords"];
				return new AjaxResult(GetJsonFromProjectsCollection((DataTree)query.Find().FirstCollection));
			}
			else
			{
				return new AjaxResult("");
			}

		}

        [GrantAccess(3)]
        public ActionResult getsubprojects(
            string parentproject)
        {
            int MaxSubProjects = (int)Runtime.Config["tro"]["dataview"]["maxsubprojects"].GetValueOrDefault(300);
            
            DBQuery query = QueryGetProjects(
                null,
                "name",
                false,
                MaxSubProjects,
                0,
                string.Empty);

            if (query != null)
            {
				DBResponse response = query.Find();
                DataTree qi = response.QueryInfo;
				return new AjaxResult(GetJsonFromProjectsCollection((DataTree)response.FirstCollection));
			}
			else
			{
				return new AjaxResult("");
			}
		}

        [GrantAccess(3)]
        public ActionResult getmultipleuserallocations(
            string projectmanager,
            string searchterms,
            string orderby,
            bool ascending,
            int documentsperpage,
            int page)
        {
            return new AjaxResult("");
        }

        [GrantAccess(3)]
        public ActionResult getsingleuserallocations(
            string userid)
        {
            return new AjaxResult("");
        }

        private DBQuery QueryGetProjects(
            string[] splitTerms,
            string orderby,
            bool ascending,
            int documentsperpage,
            int page,
            string projectmanager)
        {
            const int MinTermLength = 2;

            var resultsQuery = new DBQuery();

            var searchFields = Schema.GetSearchFields(Runtime.Schema["tro"]["project"]);

            var andQueries = new List<IMongoQuery>();

            // If threre are search terms
            //      - Get element that matches both search term and project manager (if present)
            //      - Repeat for each search term
            //
            // If there are no search terms
            //      - Get element that matches project manager. If no relation is present get all items.
            //
            if (searchFields.Count > 0 && splitTerms != null && splitTerms.Length > 0)
            {
                foreach (string term in splitTerms)
                {
                    if (term.Length < MinTermLength)
                        continue;

                    var orQueries = new List<IMongoQuery>();
                    foreach (string searchField in searchFields)
                    {
                        var relationAndFilterQueries = new List<IMongoQuery>();

                        relationAndFilterQueries.Add(Query.Matches(searchField, new BsonRegularExpression(term, "i")));

                        if (!string.IsNullOrEmpty(projectmanager))
                        {
                            relationAndFilterQueries.Add(
                                Query.EQ("projectmanager", new ObjectId(projectmanager)));
                        }

                        orQueries.Add(Query.And(relationAndFilterQueries));
                    }

                    andQueries.Add(Query.Or(orQueries));
                }
            }

            // Add project manager to query
            if (!string.IsNullOrEmpty(projectmanager))
            {
                andQueries.Add(Query.EQ("projectmanager", new ObjectId(projectmanager)));
            }

            // Add project end date to query
            andQueries.Add(Query.GT("projectend", new BsonDateTime(MC2DateTimeValue.Now())));

            resultsQuery["project"][DBQuery.Condition] = Query.And(andQueries).ToString();

            // Apply sorting and paging
            if (!resultsQuery["project"][DBQuery.Condition].Empty)
            {
                resultsQuery["project"][DBQuery.OrderBy] = orderby;
                resultsQuery["project"][DBQuery.Ascending] = ascending;
                resultsQuery["project"][DBQuery.DocumentsPerPage] = documentsperpage;
                resultsQuery["project"][DBQuery.Page] = page;
				resultsQuery["project"][DBQuery.IncludeTotals] = true;

				return resultsQuery;
            }
            else
            {
                return null;
            }
        }

        private string GetJsonFromProjectsCollection(DataTree projects)
        {
            var sb = new StringBuilder();


            foreach(DataTree project in projects)
            {
                sb.Append("<p>");
                sb.Append((string)project["identifier"]);
                sb.Append((string)project["name"]);
                sb.Append((string)project["projectstart"]);
                sb.Append((string)project["projectend"]);
                sb.Append((string)project[""]);
                sb.Append("</p>");
            }

            return sb.ToString();
        }


        #endregion
    }
}
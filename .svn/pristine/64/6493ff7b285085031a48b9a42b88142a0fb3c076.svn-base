using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.Common;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using MongoDB.Bson;

namespace SystemsGarden.mc2.MC2Site.App_Code.Controllers.__builtin
{
    public class form : MC2Controller
    {
		#region Constants

		private const int DropdownMaxDocuments = 1000;

		#endregion

		#region Blocks

		public MC2Value getdatetimecontrolyears(int startYear = -1, int endYear = -1)
        {
            var result = new DataTree();

            var now = MC2DateTimeValue.Now();

            int currentYear = now.Year;

            // Display previous year for january and february.
            if (startYear == -1)
                startYear = (now.Month > 2) ? currentYear : currentYear - 1;

            // Default to showing next year to future.
            if (endYear == -1)
                endYear = currentYear + 1;

            for (int i = startYear; i <= endYear; i++)
            {
                var yearNode = result.AddNodeWithIndex();
                yearNode.Value = i.ToString();
                if (i == currentYear)
                    yearNode["selected"] = true;

            }

            return result;
        }


        #endregion

        #region Actions

        [GrantAccessToGroup("defaultformaccess")]
        public ActionResult relationdropdown(
            string collection,
            string filtercontroller = "",
            string filteraction = "")
        {
            DataTree schema = Runtime.Schema.First[collection];
            string filter = schema["collection"]["listfilter"];

            string orderBy = schema["collection"]["orderby"];
            bool ascending = (bool)schema["collection"]["ascending"].GetValueOrDefault(false);

            // Combine given filter and filter from schema.
            if (!string.IsNullOrEmpty(filtercontroller) && !string.IsNullOrEmpty(filteraction))
            {
                IMongoQuery customFilter = this.Runtime.Filters.GetFilterQuery(filtercontroller, filteraction);

                if (string.IsNullOrEmpty(filter))
                {

                }
                else
                {
                    BsonDocument doc = MongoDB.Bson.Serialization
                       .BsonSerializer.Deserialize<BsonDocument>(filter);

                    var combinedFilter = Query.And(new QueryDocument(doc), customFilter);
                    filter = combinedFilter.ToString();
                }
            }

            var query = new DBQuery();

            if (!string.IsNullOrEmpty(orderBy))
            {
                query[collection][DBQuery.OrderBy] = orderBy;
                query[collection][DBQuery.Ascending] = ascending;
            }

            if (string.IsNullOrEmpty(filter))
                query[collection][DBQuery.Condition] = DBQuery.All;
            else
                query[collection][DBQuery.Condition] = filter;

			query[collection][DBQuery.DocumentsPerPage] = DropdownMaxDocuments;

			DBResponse response = query.Find();

            DataTree filteredResults = new DataTree("results");
            foreach (DataTree result in response.FirstCollection)
            {
                DataTree filteredResult = filteredResults.Add(result.Name);

                List<string> nameFields = Schema.GetNameFields(Runtime.Schema.First[collection]);

                string displayName = "";

                bool first = false;
                foreach (string nameField in nameFields)
                {
                    if (first)
                        first = false;
                    else if (!displayName.EndsWith(" ") && !string.IsNullOrEmpty(displayName))
                        displayName += " ";

                    displayName += result[nameField];
                }

                filteredResult["name"] = displayName;
            }

            return new AjaxResult(filteredResults);
        }

        #endregion
    }
}
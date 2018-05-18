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

namespace SystemsGarden.mc2.widgets.approveworkhelper
{
	public class approveworkhelper : MC2Controller
	{
		#region Actions

		[GrantAccessToGroup("authenticated")]
		/// <summary>
		/// Generated code for MC2 widget "approveworkhelper".
		/// </summary>
		public ActionResult getdetailsdata(string detailids)
		{
			string[] identifiers = detailids.Split(',');

			var query = new DBQuery();
			query["details"][DBQuery.CollectionName] = "timesheetentry";

			var orQueries = new List<IMongoQuery>();

			foreach(string strId in identifiers)
			{
				var id = new ObjectId(strId);
				orQueries.Add(Query.EQ("parent", id));
			}

			query["details"][DBQuery.Condition] = Query.Or(orQueries).ToString();

			DBCollection results = query.Find().FirstCollection;

			return new AjaxResult((DataTree)results);
		}

		#endregion
	}
}
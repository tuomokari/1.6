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
    public class myplaces : listview
    {
        #region blocks

        public MC2Value myplacesdropdown()
        {
            return Query.EQ("owner", new ObjectId(Runtime.SessionManager.CurrentUser["_id"])).ToString();
        }

        #endregion

		[GrantAccessToGroup("authenticated")]
        public ActionResult myplaceslistview(
            string terms,
            string collection,
            string orderby,
            bool ascending,
            int documentsperpage,
            int page,
            string relation,
            string relationid,
            bool islocalrelation,
            string localcollection)
        {
            IMongoQuery filter = null;

            if (string.IsNullOrEmpty(terms.Trim()))
                filter = Query.EQ("owner", new ObjectId(Runtime.SessionManager.CurrentUser[DBQuery.Id]));

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
                filter);
        }
    }
}
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
    public class userdata : MC2Controller
    {
        #region Constants

        #endregion

        #region Blocks

        #endregion

        #region Actions

        [GrantAccessToGroup("authenticated")]
        public ActionResult getuserdata(string key)
        {

            var query = new DBQuery("core", "userdata");

            query.AddParameter("user", Runtime.SessionManager.CurrentUser["_id"]);
            query.AddParameter("key", key);

            DBResponse result = query.FindAsync().Result;

            return new AjaxResult((DataTree)result.FirstCollection);
        }

        [GrantAccessToGroup("authenticated")]
        public ActionResult setuserdata(string key, string value)
        {
            var userdata = new DBDocument("userdata");
            userdata["user"] = Runtime.SessionManager.CurrentUser["_id"];
            userdata["key"] = key;
            userdata["value"] = value;

            userdata.UpdateDatabase();

            return new AjaxResult("");
        }

        #endregion
    }
}
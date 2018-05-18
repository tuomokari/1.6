using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.Common;
using SystemsGarden.mc2.Common.Constants;
using SystemsGarden.mc2.Core;

namespace SystemsGarden.mc2.MC2Site.App_Code.Controllers.__builtin
{
    public class monitoring : MC2Controller
    {
        #region Constants

        const string DefaultAuthenticationToken = "9A48AB98-2660-4F90-B549-CD0C636E4B1A";

        #endregion

        #region Actions

        [History(false)]
        [GrantAccessToGroup("anonymous")]
        public AjaxResult getdata(string authenticationtoken)
        {
            if (authenticationtoken != (string)Runtime.Config["monitoring"]["authenticationtoken"].GetValueOrDefault(DefaultAuthenticationToken))
                return new AjaxResult("forbidden", System.Net.HttpStatusCode.Forbidden);

            var monitorData = Runtime.Monitoring.GetMonitorData();

            return new AjaxResult(monitorData);
        }

        [GrantAccessToGroup("anonymous")]
        public ActionResult getstatus()
        {
            var monitorQuery = new DBQuery();
            monitorQuery["monitorinfo"][DBQuery.Condition] = DBQuery.All;

            string result = "";
            try
            {
                DBDocument resultDoc = monitorQuery.FindOne(new DBCallProperties() { RunWithPrivileges = 5 });

                if (resultDoc == null)
                {
                    return new AjaxResult("ERROR. No result document found. To configure monitoring add monitorinfo collection to schema and insert one document with the desired response.");
                }

                result = resultDoc["info"];
            }
            catch (Exception ex)
            {
                result = "error: " + ex;
            }

            return new AjaxResult(result);
        }


        #endregion

        #region Private

        #endregion
    }
}
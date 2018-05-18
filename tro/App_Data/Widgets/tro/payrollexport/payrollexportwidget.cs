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
using SystemsGarden.mc2.MC2Site.App_Code.Controllers.__builtin;

namespace SystemsGarden.mc2.widgets.payrollexportwidget
{
	public class payrollexportwidget : MC2Controller
    {
		#region Actions

		private const string ExporTargetCsv = "csv";
		private const string ExporTargetExcel = "excel";

		[GrantAccess(4, 5, 6)]
        public ActionResult export(
			string payrollperiod,
			string user = null,
			string target = ExporTargetCsv,
			bool onlyentriesnotacceptedbymanager = false,
			bool onlyentriesnotacceptedbyworker = false,
			bool onlyexportedentries = false)
        {
            logger.LogInfo("Received a request to export data", target, payrollperiod, user);

            if (string.IsNullOrEmpty(payrollperiod) && string.IsNullOrEmpty(user))
            {
                logger.LogError("Payroll and user are missing from payroll export request");
                return new AjaxResult("error: No payroll or user period specified.");
            }

            var export = new DBDocument("payrollexport");
            export["status"] = "WaitingToStart";
			export["target"] = target;
			export["user"] = user;
			export["onlyentriesnotacceptedbymanager"] = onlyentriesnotacceptedbymanager;
			export["onlyentriesnotacceptedbyworker"] = onlyentriesnotacceptedbyworker;
			export["onlyexportedentries"] = onlyexportedentries;

			if (!string.IsNullOrEmpty(payrollperiod))
				export["payrollperiod"] = payrollperiod;

			if (!string.IsNullOrEmpty(user))
				export["user"] = user;

			export.UpdateDatabaseAsync();

            return new AjaxResult("");
        }

        [GrantAccess(4, 5, 6)]
        public ActionResult revert(string payrollperiod, string user = null)
        {
			logger.LogInfo("Received a request to revert exported payroll items to unexported status.", payrollperiod);

			if (string.IsNullOrEmpty(payrollperiod) && string.IsNullOrEmpty(user))
			{
				logger.LogError("Payroll period and user are missing from payroll revert request");
				return new AjaxResult("error: No payroll period or user specified.");
			}

			var revertPayrollData = new RCMessage("payrollrevertexport");
            DataTree payrollHandler = revertPayrollData.Handlers["payrollintegrationhandler"];

			if (!string.IsNullOrEmpty(payrollperiod))
				payrollHandler["payrollperiod"] = payrollperiod;

			if (!string.IsNullOrEmpty(user))
				payrollHandler["user"] = user;

			Runtime.SendRemoteMessage(revertPayrollData);
            return new AjaxResult("");
        }

        #endregion
    }
}
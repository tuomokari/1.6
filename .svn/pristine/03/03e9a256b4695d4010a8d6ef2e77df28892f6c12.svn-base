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
    public class admin : MC2Controller
    {
        #region Actions

        [GrantAccessToGroup("sysadmin")]
        public ActionResult adminactions()
        {
            return View();
        }


		[GrantAccessToGroup("sysadmin")]
		public ActionResult runtimeinfo()
        {
            return View();
        }


		/// <summary>
		/// Restores database from dump with a given name. The database dump is stored in server inside
		/// data\dbdumps folder. Success is not verified.
		/// </summary>
		/// <param name="dumpname"></param>
		/// <returns></returns>
		[GrantAccessToGroup("sysadmin")]
		public ActionResult restoredatabase(string dumpname)
        {
            var restoreMessage = new RCMessage("restoredb");

            restoreMessage.Handlers[MongoDBHandlerConstants.mongodbhandler]["dumpname"] = dumpname;
            RCResponse response = Runtime.RemoteConnection.ProcessMessage(restoreMessage);

            return View();
        }

		[GrantAccessToGroup("sysadmin")]
		public ActionResult setnowfortesting(string now)
        {
            var setNowMessage = new RCMessage("setnowfortesting");

            MC2DateTimeValue dtNow = (MC2DateTimeValue)MC2DateTimeValue.TryConvertValueFromString(now);

            MC2DateTimeValue.OverrideNow(dtNow);

            setNowMessage.Handlers[MongoDBHandlerConstants.mongodbhandler]["now"] = dtNow;
            RCResponse response = Runtime.RemoteConnection.ProcessMessage(setNowMessage);

            return View();
        }

        #endregion
    }
}
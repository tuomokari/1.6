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
    public class navigation : MC2Controller
    {
        #region Actions

        [History(false)]
        [GrantAccessToGroup("authenticated")]
        public ActionResult previous()
        {
            return Redirect(Runtime.HistoryManager.GetPreviousAddress());
        }

        [History(false)]
		[GrantAccessToGroup("authenticated")]
		public ActionResult current()
        {
            return Redirect(Runtime.HistoryManager.GetCurrentAddress());
        }

        [History(false)]
		[GrantAccessToGroup("authenticated")]
		public ActionResult addhistoryentry(string historyaddress, string redirectaddress, string token)
        {
            var historyEntry = new DataTree();

            historyEntry["ispost"] = false;
            historyEntry["address"] = historyaddress;
            historyEntry["timestamp"] = MC2DateTimeValue.Now();
            historyEntry["addedprogrammatically"] = true;

            Runtime.HistoryManager.AddHistoryEntry(historyEntry, token);
            return Redirect(redirectaddress);
        }

        #endregion
    }
}
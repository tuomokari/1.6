using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.Common;
using System.Web.Script.Serialization;

namespace SystemsGarden.mc2.MC2Site.App_Code.Controllers.__builtin
{
    public class history : MC2Controller
    {
        #region Actions

        /// <summary>
        /// Sets a value in given history state
        /// </summary>
        /// <returns></returns>
        [GrantAccessToGroup("authenticated")]
        [History(false)]
        [HttpPost]
        public AjaxResult setstate(DataTree json, string identifier, string historytoken)
        {

			if (string.IsNullOrEmpty(identifier))
				return new AjaxResult("No identifier", System.Net.HttpStatusCode.InternalServerError);

            if (json == null)
                return new AjaxResult("No history date", System.Net.HttpStatusCode.InternalServerError);

			DataTree historyEntry;

			if (string.IsNullOrEmpty(historytoken))
				historyEntry = Runtime.HistoryManager.GetCurrentHistoryEntry();
			else
				historyEntry = Runtime.HistoryManager.GetUserHistory().GetHistoryEntryForToken(historytoken, false);

            if (historyEntry == null)
                return new AjaxResult("No history entry found.", System.Net.HttpStatusCode.InternalServerError);

			historyEntry["state"][identifier].Clear();
            historyEntry["state"][identifier] = json;

            return new AjaxResult("success");
        }

        #endregion

        #region Blocks

        /// <summary>
        /// Gets a value in given history state
        /// </summary>
        /// <returns></returns>
        [GrantAccessToGroup("authenticated")]
        public MC2Value getstate(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return MC2EmptyValue.EmptyValue;

            DataTree historyEntry = Runtime.HistoryManager.GetCurrentHistoryEntry();

            if (historyEntry == null)
                return MC2EmptyValue.EmptyValue;

            if (historyEntry["state"].Contains(identifier))
                return historyEntry["state"][identifier];
            else
                return MC2EmptyValue.EmptyValue;
        }

        /// <summary>
        /// Sets a value in given session variablwe. All values set from script live under scriptedvalues section for security
        /// </summary>
        /// <returns></returns>
        [GrantAccessToGroup("authenticated")]
        [History(false)]
        [HttpPost]
        public AjaxResult setsessionvariable(DataTree json, string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return new AjaxResult("");

            if (json == null)
                return new AjaxResult("");


            Session["scriptedvalues"][identifier].Clear();

            Session["scriptedvalues"][identifier] = json;

            return new AjaxResult("");
        }

        /// <summary>
        /// Gets a value in given session variable. All values set from script live under scriptedvalues section for security
        /// </summary>
        /// <returns></returns>
        [GrantAccessToGroup("authenticated")]
        public MC2Value getsessionvariable(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return MC2EmptyValue.EmptyValue;

            if (Session["scriptedvalues"].Contains(identifier))
                return Session["scriptedvalues"][identifier];
            else
                return MC2EmptyValue.EmptyValue;
        }

        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.Common;
using System.Text;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using MongoDB.Bson;

namespace SystemsGarden.mc2.tro.App_Code.Controllers.tro
{
    public class trofilters : MC2Controller
    {

        public MC2Value timesheetentrydetailpaytype()
        {
            DataTree historyEntry = Runtime.HistoryManager.GetCurrentHistoryEntry();

            string text = historyEntry.ContentView();

            if (!string.IsNullOrEmpty(Runtime.SessionManager.Session["scriptedvalues"]["addtimesheetentryform"]["selecteduser"]))
            {
                // Get cla contract for selected user and figure out which paytypes to show
            }

            return "";
        }
    }
}
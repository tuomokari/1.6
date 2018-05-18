using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SystemsGarden.mc2.RemoteConnector.Handlers.PayrollIntegrationHandlerServer
{
    /// <summary>
    /// Wrapper for collection("timesheetentry")
    /// </summary>
    internal class Timesheet : EntryToPayroll
    {
        /// <summary>
        ///<para>Projectnumber consists of max 3 part, since there is a 8char limit / part
        ///from config["projectnumber"]["fieldspan"] and config["projectnumber"]["length"] </para> 
        ///<para>("class_entry").project from collection("project").poski
        ///from config["projectnumber"]["identifier"] </para>
        ///<para>Projekti </para>
        /// </summary>
        public string[] ProjectNumber { get; set; }

        /// <summary>
        ///<para>("timesheetentry").project from collection ("project").projecttype
        ///if !=="PROJECT" this get's a value and the ProjectNumber get's
        ///value from ("project").parentproject</para>
        /// max 8 chars
        /// Työmääräin
        /// </summary>
        public string WorkOrder { get; set; }

        /// <summary>
        /// ("timesheetentry").price has value only if ("timesheetentry").hasprice==true
        /// </summary>
        public double Price { get; set; }

        /// <summary>
        /// Creates CSV -line based on this type
        /// </summary>
        /// <returns>CSV-line</returns>
        public override string CreateCsvLine()
        {
            return CreateCsvLine("d.M.yyyy");
        }

        public override string CreateCsvLine(string format)
        {
            var config = EntriesToPayroll<Timesheet>.ParsedConfig;
            var indexOfEntryType = 2; //needed in
           return EntriesToPayroll<Timesheet>.CreateCsvLineWithReflection(this,indexOfEntryType);
        }
    }
}

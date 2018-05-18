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
    /// Wrapper for collection("dayentry")
    /// Kulu
    /// </summary>
    internal class Day : EntryToPayroll
    {
        /// <summary>
        ///<para>Projectnumber consists of max 3 part, since there is a 8char limit / part
        ///from config["projectnumber"]["fieldspan"] and config["projectnumber"]["length"] </para> 
        ///<para>("class_entry").project from collection("project").poski
        ///from config["projectnumber"]["identifier"] </para>
        ///<para>Projekti </para>
        /// </summary>
        public virtual string[] ProjectNumber { get; set; }

        /// <summary>
        ///<para>("timesheetentry").project from collection ("project").projecttype
        ///if !=="PROJECT" this get's a value and the ProjectNumber get's
        ///value from ("project").parentproject</para>
        /// max 8 chars
        /// Työmääräin
        /// </summary>
        public string WorkOrder { get; set; }


        //TODO: Move to base class, need to make sure that works with ARE also
        ///// <summary>
        ///// ("dayentry").amount, if not specified Hours, value goes here
        ///// <para>Määrä</para>
        ///// </summary>
        //public double Amount { get; set; }

        ///// <summary>
        ///// If different units to separate in different columns in csv
        ///// </summary>
        //public string AmountExtra1 { get; set; }

        ///// <summary>
        ///// If different units to separate in different columns in csv
        ///// </summary>
        //public string AmountExtra2 { get; set; }

        /// <summary>
        /// Implemented on 2015-10-30, some types have HourlyRate, like "event guarding"?
        /// ("dayentrytype").hastime==true 
        /// <para>Tuntihinta</para>
        /// </summary>
        //public double HourlyRate { get; set; }

        /// <summary>
        /// Implemented on 2015-10-30, some types have QuantityRate, like buss-ticket
        /// ("dayentrytype").hasprice==true 
        /// <para>Määrähinta</para>
        /// </summary>
        public double QuantityRate { get; set; }




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
            var config = EntriesToPayroll<Day>.ParsedConfig;
            var indexOfEntryType = 1; //needed from configuration "config.tree" validtypescountandtypes
            return EntriesToPayroll<Day>.CreateCsvLineWithReflection(this, indexOfEntryType);
        }
    }
}

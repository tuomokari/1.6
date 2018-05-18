using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace BatchProcessEntriesToExcel
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
        public string ProjectNumber { get; set; }
        public double TravelTime { get; set; }


        /// <summary>
        /// Creates CSV -line based on this type
        /// </summary>
        /// <returns>CSV-line</returns>
        public override string CreateCsvLine()
        {
            return CreateCsvLine("d.M.yyyy");
        }

        public bool IsTimesheetEntryDetail { get; set; }
        public override string CreateCsvLine(string format)
        {
            return format;
        }


    }
}

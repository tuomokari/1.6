using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BatchProcessEntriesToExcel
{
    /// <summary>
    /// Wrapper for collection("absenceentry")
    /// </summary>
    class Absence : EntryToPayroll
    {
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
            return format;
        }

    }
}

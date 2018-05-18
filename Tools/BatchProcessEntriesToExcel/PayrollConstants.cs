using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SystemsGarden.mc2.Common;

namespace BatchProcessEntriesToExcel
{
    /// <summary>
    /// Different statuses for payroll. Export updated in collection("payrollexport").status
    /// </summary>
    public static class PayrollConstants
    {
        #region status in generating Payroll
        /// <summary>
        /// Message loop to check for starting Export
        /// </summary>
        public static readonly string WaitingToStart = "WaitingToStart";
        /// <summary>
        /// After WaitingToStart
        /// </summary>
        public static readonly string Starting = "Starting";
        /// <summary>
        /// Pretty much self explanitory - ProcessingData
        /// </summary>
        public static readonly string ProcessingData = "Processing Data";
        /// <summary>
        /// Pretty much self explanitory - GeneratingCsv
        /// </summary>
        public static readonly string GeneratingCsv = "Generating Document";
        /// <summary>
        /// If not Csv then creating Excel data
        /// </summary>
        public static readonly string GeneratingExcel = "Generating ExcelWorkbook";
        /// <summary>
        /// Generate audit files for human readable text
        /// </summary>
        [Obsolete("Not in used any longer, since excel data does the job")]
        public static readonly string GeneratingAudit = "Generating Audit";
        /// <summary>
        /// Pretty much self explanitory - Finalizing the export
        /// </summary>
        public static readonly string Finalizing = "Finalizing";
        /// <summary>
        /// All is well an export without no failures
        /// </summary>
        public static readonly string Completed = "Completed";
        /// <summary>
        /// Automatically completed
        /// </summary>
        public static readonly string CompletedAutomatically = "Automatically completed ";
        /// <summary>
        /// No export data is done, see the log-file
        /// </summary>
        public static readonly string Failed = "Failed";
        /// <summary>
        /// If no payrollperiod selected, not the case atm(2015-12-18)
        /// </summary>
        public static readonly string FailedMissingPayrollPeriod = "Failed due to missing payrollperiod";
        /// <summary>
        /// Generates errors file to show which are not exported to use with manual corrction in payroll export
        /// </summary>
        public static readonly string CompletedWithErrors = "Completed With Errors";
        #endregion

        #region constants for amount or hour
        /// <summary>
        /// Check for amount for any dayetries
        /// </summary>
        public static readonly string Amount = "amount";
        /// <summary>
        /// Check for hours for any dayetries
        /// </summary>
        public static readonly string Hours = "hours";
        #endregion

        #region for payrollrevert
        /// <summary>
        /// Not in used atm(2015-12-18)
        /// </summary>
        public static readonly string PayrollPeriod = "payrollperiod";
        /// <summary>
        /// Not in used atm(2015-12-18)
        /// </summary>
        public static readonly string User = "user";

        #endregion
    }
}

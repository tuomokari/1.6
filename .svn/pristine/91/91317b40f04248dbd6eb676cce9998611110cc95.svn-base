using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace SystemsGarden.mc2.RemoteConnector.Handlers.PayrollIntegrationHandlerServer
{
	/// <summary>
	/// Type of the payroll export.
	/// </summary>
	public enum ExportType
	{
		/// <summary>
		/// CSV export for work entries.
		/// </summary>
		Csv,
        /// <summary>
        /// Automatically exported payroll
        /// </summary>
        CsvAutomatic,
		/// <summary>
		/// Excel export for work entries.
		/// </summary>
		Excel
	}

    /// <summary>
    /// Class to pass information for the PayrollExport
    /// </summary>
	public class PayrollExportTask
	{
		private const ExportType DefaultExportType = ExportType.Csv;
		private const string DefaultLanguage = "en-US";

		/// <summary>
		/// _id field of the export task
		/// </summary>
		public ObjectId ExportId { get; set; } = ObjectId.Empty;

		/// <summary>
		/// _id field of the payroll for export task
		/// </summary>
		public ObjectId PayrollPeriodId { get; set; } = ObjectId.Empty;

		/// <summary>
		/// Name of the payroll period to export
		/// </summary>
		public string PayrollPeriodName { get; set; }

		/// <summary>
		/// This export task's payroll period's document
		/// </summary>
		public BsonDocument PayrollPeriod { get; set; } = null;

		/// <summary>
		/// _id field of the user for export task
		/// </summary>
		public ObjectId UserId { get; set; } = ObjectId.Empty;

		/// <summary>
		/// Name of the user for export task
		/// </summary>
		public string UserName { get; set; } = "";

		/// <summary>
		/// This export task's user's document
		/// </summary>
		public BsonDocument User { get; set; } = null;

		/// <summary>
		/// Type of the export
		/// </summary>
		public ExportType ExportType { get; set; } = DefaultExportType;

		/// <summary>
		/// Language of the payroll export task
		/// </summary>
		public string Language { get; set; } = DefaultLanguage;

		/// <summary>
		/// If set, only export entries not accepted by manager
		/// </summary>
		public bool onlyExportEntriesNotAcceptedByManager { get; set; } = false;

		/// <summary>
		/// If set, only export entries not accepted by worker
		/// </summary>
		public bool onlyExportEntriesNotAcceptedByWorker { get; set; } = false;

		/// <summary>
		/// If set, only export exported entries
		/// </summary>
		public bool onlyExportExportedEntries { get; set; } = false;

        /// <summary>
        /// If Export is completed automatically (timed job)
        /// </summary>
        public bool IsAutomatic { get; set; }

    }
}

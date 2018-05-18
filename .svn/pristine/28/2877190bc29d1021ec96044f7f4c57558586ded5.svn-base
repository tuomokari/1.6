using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchProcessEntriesToExcel
{
    /// <summary>
    /// Base class for any entrytype atm timesheet, absence, day
    /// </summary>
    internal abstract class EntryToPayroll
    {
        /// <summary>
        /// Abstract method to create CSV-line
        /// </summary>
        /// <returns>CSV line</returns>
        public abstract string CreateCsvLine();
        public abstract string CreateCsvLine(string format);
        
        /// <summary>
        /// Entry document
        /// </summary>
        public virtual BsonDocument Document { get; set; }
        
        /// <summary>
        /// Not available yet collection("paymentgroup")
        /// Maksuryhmä
        /// </summary>
        public virtual string PaymentGroup { get; set; }

        /// <summary>
        ///("class_entry").user from collection("user").identifier
        /// Henkilönumero 
        /// </summary>
        public virtual string UserIdentifier { get; set; }

        public virtual string UserName { get; set; }


        /// <summary>
        ///("class_entry").date
        /// Alkupäivä
        /// </summary>
        public virtual DateTime StartDate { get; set; }

        /// <summary>
        /// ("class_entry").date --> same as startdate atm (2015-09-17)
        /// Loppupäivä
        /// </summary>
        public virtual DateTime EndDate { get; set; }

        /// <summary>
        /// Pay type for entry.
        /// </summary>
        public virtual BsonDocument PayType { get; set; }

        /// <summary>
        /// ("class_entry").class_entry_detailpaytype from collection ("class_entry_detailpaytype").identifier
        /// Palkkalaji
        /// </summary>
        public virtual string PayTypeId { get; set; }

        public virtual string PayTypeName { get; set; }

        /// <summary>
        /// ("class_entry").duration(ms)
        /// </summary>
        public virtual double Hours { get; set; }


		/// <summary>
		///CLA (TES) group maybe not needed atm (2015-09-17) 
		///("class_entry").clacontract from collection("clacontract").identifier
		/// </summary>
		public virtual string ClaGroup { get; set; }

        /// ("class_entry").class_entry_profitcenter from collection ("class_entry_profitcenter").identifier
        /// Tulosyksikkö
        public virtual string ProfitCenter { get; set; }

        /// <summary>
        /// ("dayentry").amount, if not specified Hours, value goes here
        /// <para>Määrä</para>
        /// </summary>
        public double Amount { get; set; }
        
        /// <summary>
        /// If different units to separate in different columns in csv
        /// </summary>
        public double AmountExtra1 { get; set; }
        /// <summary>
        /// If different units to separate in different columns in csv
        /// </summary>

        public double AmountExtra2 { get; set; }


        public virtual bool ApprovedByWorker { get; set; } = true;
        public virtual bool ApprovedByManager { get; set; } = true;
        public ObjectId _id { get; set; } = ObjectId.Empty;

    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml;
using SystemsGarden.mc2.Common;
using MongoDB.Bson;
using System.Reflection;

namespace BatchProcessEntriesToExcel
{
    /// <summary>
    /// Collection class for payrollEntries
    /// </summary>
    /// <typeparam name="T">class based on EntryToPayroll</typeparam>
    class EntriesToPayroll<T> : List<T> where T : EntryToPayroll
    {
        #region fields and properties
        private const int DateExcelColumn = 1;
        private const int StartTimeExcelColumn = 2;
        private const int EndTimeExcelColumn = 3;
        private const int NameExcelColumn = 4;
        private const int UserIdExcelColumn = 5;
        private const int HoursExcelColumn = 6;
        private const int PayTypeNameExcelColumn = 7;
        private const int PayTypeExcelColumn = 8;
        private const int ProjectExcelColumn = 9;
        private const int ApprovedByWorkerExcelColumn = 10;
        private const int ApprovedByManagerExcelColumn = 11;
        private const int ObjectIdExcelColumn = 12;
        private const int ProfitCenterExcelColumn = 13;

        //This is the same as HoursExcelColumn but only used in Day entries, not in Timesheet or Absence entries
        private const int AmountExcelColumn = 4;

        private const string ExcelDateFormat = "d/M/yyyy";
        private const string ExcelTimeFormat = "HH:mm";

        /// <summary>
        /// Helper list to create CSV and Audit files, assigned here or in constructor
        /// </summary>

        #endregion


        #region constructors
        public EntriesToPayroll()
        { }

        #endregion


        #region public methods
        /// <summary>
        /// Generate Excel file with absence and timesheet and also day entries that count for.
        /// </summary>
        /// <typeparam name="EntryType">Absence, Day or Timehseet</typeparam>
        /// <param name="worksheet">Worksheet to add hours or amounts</param>
        /// <param name="startingRow">Row position where to start write data</param>
        /// <param name="writeHeaderRow">Prevents writing header info again</param>
        /// <returns>Row position where to start next data to be written, usefull to add timesheet entries and abcense entries otherwise starts at 2</returns>
        public int AppendToExcelWorksheet<EntryType>(ExcelWorksheet worksheet, int startingRow, bool writeHeaderRow = true) where EntryType : EntryToPayroll
        {
            if (writeHeaderRow)
            {
                if (Properties.Settings.Default.Language == "fi")
                {
                    worksheet.Cells[1, DateExcelColumn].Value = "Kirjaus päivämäärä";
                    worksheet.Cells[1, StartTimeExcelColumn].Value = "Aloitus";
                    worksheet.Cells[1, EndTimeExcelColumn].Value = "Lopetus";
                    worksheet.Cells[1, NameExcelColumn].Value = "Nimi";
                    worksheet.Cells[1, UserIdExcelColumn].Value = "Tunnus";
                    worksheet.Cells[1, HoursExcelColumn].Value = "Tunnit";
                    worksheet.Cells[1, PayTypeNameExcelColumn].Value = "Kirjaustyyppi";
                    worksheet.Cells[1, PayTypeExcelColumn].Value = "Kirjaus tunnus";
                    worksheet.Cells[1, ProjectExcelColumn].Value = "Projekti";
                    worksheet.Cells[1, ApprovedByWorkerExcelColumn].Value = "Työntekijän hyväksyntä";
                    worksheet.Cells[1, ApprovedByManagerExcelColumn].Value = "Työnjohtajan hyväksyntä";
                    worksheet.Cells[1, ObjectIdExcelColumn].Value = "Yksilöllinen tunnus";
                    worksheet.Cells[1, ProfitCenterExcelColumn].Value = "Tulosyksikkö";
                }
                else
                {
                    worksheet.Cells[1, DateExcelColumn].Value = "Date";
                    worksheet.Cells[1, StartTimeExcelColumn].Value = "Start";
                    worksheet.Cells[1, EndTimeExcelColumn].Value = "End";
                    worksheet.Cells[1, NameExcelColumn].Value = "Name";
                    worksheet.Cells[1, UserIdExcelColumn].Value = "PersonId";
                    worksheet.Cells[1, HoursExcelColumn].Value = "Hours";
                    worksheet.Cells[1, PayTypeNameExcelColumn].Value = "Paytype";
                    worksheet.Cells[1, PayTypeExcelColumn].Value = "Paytype identifier";
                    worksheet.Cells[1, ProjectExcelColumn].Value = "Project";
                    worksheet.Cells[1, ApprovedByWorkerExcelColumn].Value = "Approved by worker";
                    worksheet.Cells[1, ApprovedByManagerExcelColumn].Value = "Approved by Manager";
                    worksheet.Cells[1, ObjectIdExcelColumn].Value = "UniqueID";
                    worksheet.Cells[1, ProfitCenterExcelColumn].Value = "Profit center";

                }

                worksheet.Column(DateExcelColumn).Style.Numberformat.Format = ExcelDateFormat;
                worksheet.Column(StartTimeExcelColumn).Style.Numberformat.Format = ExcelTimeFormat;
                worksheet.Column(EndTimeExcelColumn).Style.Numberformat.Format = ExcelTimeFormat;
            }

            return AppendTimesheetAndAbcenceAndDayDataToWorksheet<T>(worksheet, startingRow);
        }

        #endregion

        #region private methods
        /// <summary>
        /// Appends entrytype hours or amounts to a worksheet
        /// </summary>
        /// <typeparam name="EntryType">Abcence, Day or Timesheet</typeparam>
        /// <param name="worksheet">Worksheet to append work hours or amounts</param>
        /// <param name="row">Row position where to start</param>
        /// <returns>Row position where to start next data to be written</returns>
        private int AppendTimesheetAndAbcenceAndDayDataToWorksheet<EntryType>(ExcelWorksheet worksheet, int row) where EntryType : EntryToPayroll
        {
            var collection = this as EntriesToPayroll<EntryType>;
            foreach (EntryType item in collection)
            {

                worksheet.Cells[row, DateExcelColumn].Value = Convert.ToDateTime(item.StartDate).ToString(ExcelDateFormat);

                if (typeof(EntryType) == typeof(Timesheet))
                {
                    if ((item as Timesheet).IsTimesheetEntryDetail == false)
                    {
                        worksheet.Cells[row, StartTimeExcelColumn].Value = Convert.ToDateTime(item.StartDate).ToString(ExcelTimeFormat);
                        worksheet.Cells[row, EndTimeExcelColumn].Value = Convert.ToDateTime(item.EndDate).ToString(ExcelTimeFormat);
                    }
                }
                else
                {
                    worksheet.Cells[row, StartTimeExcelColumn].Value = Convert.ToDateTime(item.StartDate).ToString(ExcelTimeFormat);
                    worksheet.Cells[row, EndTimeExcelColumn].Value = Convert.ToDateTime(item.EndDate).ToString(ExcelTimeFormat);
                }
                worksheet.Cells[row, NameExcelColumn].Value = item.UserName;
                worksheet.Cells[row, UserIdExcelColumn].Value = item.UserIdentifier;
                worksheet.Cells[row, HoursExcelColumn].Value = PayrollExport.MillisecondsToHours((int)item.Hours);
                worksheet.Cells[row, PayTypeNameExcelColumn].Value = item.PayTypeName;
                worksheet.Cells[row, PayTypeExcelColumn].Value = item.PayTypeId;
                if (typeof(EntryType) == typeof(Absence) == false)
                {
                    worksheet.Cells[row, ProjectExcelColumn].Value = (item as Timesheet).ProjectNumber;
                }
                worksheet.Cells[row, ApprovedByWorkerExcelColumn].Value = item.ApprovedByWorker;
                worksheet.Cells[row, ApprovedByManagerExcelColumn].Value = item.ApprovedByManager;
                worksheet.Cells[row, ObjectIdExcelColumn].Value = item._id;
                //if (typeof(EntryType) == typeof(Absence) == false)
                {
                    worksheet.Cells[row, ProfitCenterExcelColumn].Value = item.ProfitCenter;
                }
                row++;
            }
            return row;
        }

        #endregion

    }
}

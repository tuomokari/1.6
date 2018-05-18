using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml;
using OfficeOpenXml.Table.PivotTable;
using OfficeOpenXml.Table;


namespace BatchProcessEntriesToExcel
{
    /// <summary>
    /// Class for creating pivot tables with EPPlus. Based on http://stackoverflow.com/questions/11650080/epplus-pivot-tables-charts
    /// </summary>
    public class ExportPivotTable
	{
		private List<string> groupByColumns;
		private List<string> summaryColumns;

        /// <summary>
        /// Constructor for SimplePivotTable
        /// </summary>
        /// <param name="groupByColumns">Column names</param>
        /// <param name="summaryColumns">Summary column names</param>
        public ExportPivotTable(string[] groupByColumns, string[] summaryColumns)
		{
			this.groupByColumns = new List<string>(groupByColumns);
			this.summaryColumns = new List<string>(summaryColumns);
		}

        /// <summary>
        /// Parametless constructor to create assiosation for payroll and for future realease
        /// </summary>
        public ExportPivotTable()
        {}

        /// <summary>
        /// Call-back handler that builds simple PivotTable in Excel
        /// </summary>
        public void CreatePivotTable(OfficeOpenXml.ExcelPackage excelPackage, ExcelWorksheet worksheet, string pivotRangeName)
		{
			string pivotWorksheetName = "Pivot-" + worksheet.Name.Replace(" ", "");
			var wsPivot = excelPackage.Workbook.Worksheets.Add(pivotWorksheetName);

			excelPackage.Workbook.Worksheets.MoveBefore(pivotWorksheetName, worksheet.Name);

			ExcelRange dataRange = worksheet.Cells["A1:" + worksheet.Dimension.End.Address];

			var pivotTable = wsPivot.PivotTables.Add(wsPivot.Cells[1,1], dataRange, pivotWorksheetName);

			pivotTable.ShowHeaders = true;
			pivotTable.UseAutoFormatting = true;
			pivotTable.ApplyWidthHeightFormats = true;
			pivotTable.ShowDrill = true;
			pivotTable.FirstHeaderRow = 1;  // first row has headers
			pivotTable.FirstDataCol = 1;    // first col of data
			pivotTable.FirstDataRow = 2;    // first row of data

			foreach (string row in groupByColumns)
			{
				var field = pivotTable.Fields[row];
				pivotTable.RowFields.Add(field);
				field.Sort = eSortType.Ascending;
			}

			foreach (string column in summaryColumns)
			{
				var field = pivotTable.Fields[column];
				ExcelPivotTableDataField result = pivotTable.DataFields.Add(field);
			}

			pivotTable.DataOnRows = false;
		}
	}
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;


namespace TroToVismaCSVHelper
{
    /// <summary>
    /// A Simple helper for diagnosing Visma CSV export files. 
    /// 
    /// Note that file handling, memory efficiency etc. are not production quality. Do not copy your
    /// production CSV handling code from here!
    /// </summary>
    public partial class CSVHelperForm : Form
    {
        public CSVHelperForm()
        {
            InitializeComponent();
        }

        private void LoadButton_Click(object sender, EventArgs e)
        {
            OpenCsvFileDialgog.ShowDialog();
        }

        private void OpenCsvFileDialgog_FileOk(object sender, CancelEventArgs e)
        {
            try
            {
                string fileName = OpenCsvFileDialgog.FileName;

                string messageText = File.ReadAllText(fileName, Encoding.UTF8);

                ShowCsvData(messageText);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save message: " + ex.Message);
            }
        }

        private void ShowCsvData(string data)
        {
            DataGridCsv.Rows.Clear();

            using (StringReader sr = new StringReader(data))
            {
                string[] csvLine = ReadCsvLine(sr);

                while (csvLine != null)
                {

                    var row = new DataGridViewRow();
                    row.CreateCells(DataGridCsv, "", "", "", "", "", "", "", "","", "");
                    for (int i = 0; i < csvLine.Length; i++)
                    {
                        row.Cells[i].Value = csvLine[i];
                    }

                    DataGridCsv.Rows.Add(row);

                    csvLine = ReadCsvLine(sr);
                }
            }
        }

        private string[] ReadCsvLine(StringReader sr)
        {
            const char CsvSeparator = ';';

            while (true)
            {
                string line = sr.ReadLine();

                if (line == string.Empty)
                    continue;

                if (line == null)
                    return null;
                else
                    return line.Split(CsvSeparator);
            }
        }



    }
}

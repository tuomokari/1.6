using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using System.IO;
using SystemsGarden.mc2.Common;

namespace BatchProcessEntriesToExcel
{
    class Program
    {
        static void Main(string[] args)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            var settings = new MongoDB.Driver.MongoServerSettings();

            var address = new MongoDB.Driver.MongoServerAddress(Properties.Settings.Default.Server, Properties.Settings.Default.Port);
            settings.Server = address;

            MongoDB.Driver.MongoServer server = new MongoDB.Driver.MongoServer(settings);


            var client = new MongoDB.Driver.MongoClient();
            MongoDatabase database = server.GetDatabase("mc2db");

            Console.WriteLine("Aloitetaan...");
            var now = MC2DateTimeValue.Now().ToLocalTime();
            var nowStr = string.Format("{0:0000}-{1:00}-{2:00}-{3:00}-{4:00}-{5:00}",
                now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);

            var logFile = Path.Combine(string.Format("{0}_{1}.txt", Properties.Settings.Default.ExcelPath + "\\Log", nowStr  ));
            if (!Directory.Exists(Path.GetDirectoryName(logFile)))
                Directory.CreateDirectory(Path.GetDirectoryName(logFile));
            var x = new PayrollExport(logFile, database);
            x.ExportDocuments();
            Console.WriteLine("Valmis...aikaa kului {0} minuuttia", sw.Elapsed.TotalMinutes);
        }
    }
}

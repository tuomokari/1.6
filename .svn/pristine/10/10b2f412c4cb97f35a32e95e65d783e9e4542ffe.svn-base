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
using System.Xml;

namespace XmlUpdateTool
{

    /// <summary>
    /// This is a single purprose tool for converting exported AX entries to have max 60 characters in their description (txt field).
    /// 
    /// </summary>
    public partial class Form1 : Form
    {
        private XmlDocument xmlDocument;

        public Form1()
        {
            InitializeComponent();
        }

        private void OpenXmlFileDialgog_FileOk(object sender, CancelEventArgs e)
        {
            string fileName = OpenXmlFileDialgog.FileName;
            try
            {
                xmlDocument = new XmlDocument();
                xmlDocument.Load(fileName);

                var nodeList = xmlDocument.GetElementsByTagName("Txt");
                int converted = 0;
                int total = 0;
                foreach (XmlNode node in nodeList)
                {
                   total++;
                    if(node.InnerText.Length > 60)
                    {
                        converted++;
                        string convertedText = node.InnerText.Substring(0, Math.Min(60, node.InnerText.Length));
                        node.InnerText = convertedText;
                    }
                }

                MessageBox.Show("Conversion done. " + converted + " items updated out of total " + total + ".");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exeption: " + ex, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

        }

        private void LoadButton_Click(object sender, EventArgs e)
        {
            OpenXmlFileDialgog.ShowDialog();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            saveXMLDialog.ShowDialog();
        }

        private void saveXMLDialog_FileOk(object sender, CancelEventArgs e)
        {
            string fileName = saveXMLDialog.FileName;

            string docStr;

            using (var stringWriter = new StringWriter())
            {
                var writerSettings = new XmlWriterSettings();
                writerSettings.Indent = true;
                writerSettings.NamespaceHandling = NamespaceHandling.Default;
                writerSettings.Encoding = Encoding.UTF8;
                writerSettings.NewLineHandling = NewLineHandling.Entitize;

                using (var xmlTextWriter = XmlWriter.Create(stringWriter, writerSettings))
                {
                    xmlDocument.WriteTo(xmlTextWriter);
                }

                docStr = stringWriter.GetStringBuilder().ToString();
                docStr = docStr.Replace(" xmlns=\"\"", "");
            }

            File.WriteAllText(fileName, docStr);

        }
    }
}

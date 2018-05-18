using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using SystemsGarden.mc2.Common;

namespace SystemsGarden.mc2.Tools.DevelopmentServer
{
    public partial class DevelopmentServerForm : Form
    {
        private DevelopmentServerHost developmentServerHost;

        public DevelopmentServerForm()
        {
            InitializeComponent();

            SetupServerHost();
        }

        internal void ShowErrorMessage(string message)
        {
            MessageBox.Show(message);
        }

        internal void UpdateConfigurationView()
        {
            if (developmentServerHost != null)
                DisplayRegisterOnTreeView(developmentServerHost.Configuration, TreeViewConfiguration);
        }

        private void SetupServerHost()
        {
            
            developmentServerHost = new DevelopmentServerHost(this);
            developmentServerHost.StartServer();

            UpdateConfigurationView();

        }

        private void ButtonStart_Click(object sender, EventArgs e)
        {
            developmentServerHost.StartServer();

            ButtonStop.Enabled = true;
            ButtonStart.Enabled = false;
        }

        private void ButtonStop_Click(object sender, EventArgs e)
        {
            developmentServerHost.StopServer();

            ButtonStop.Enabled = false;
            ButtonStart.Enabled = true;
        }

        private void ButtonEditConfiguration_Click(object sender, EventArgs e)
        {
            var editConfiguration = new FormEditConfiguration();
            editConfiguration.TextBoxValue.Text = developmentServerHost.ConfigurationText;

            DialogResult dialogResult = editConfiguration.ShowDialog();

            if (DialogResult == DialogResult.OK)
            {
                developmentServerHost.SetConfiguration(editConfiguration.TextBoxValue.Text);
            }
        }

        private void ButtonLoadConfiguration_Click(object sender, EventArgs e)
        {
            OpenFileDialogConfigMessage.ShowDialog();
        }

        private void ButtonSaveConfiguration_Click(object sender, EventArgs e)
        {
            SaveFileDialogConfigMessage.ShowDialog();
        }

        private static void DisplayRegisterOnTreeView(DataTree message, TreeView tree, string rootName = "")
        {
            tree.Nodes.Clear();

            dynamic root = new MessageTreeNode(rootName);
            tree.Nodes.Add(root);

            RecursivelyDisplayMessageAsTreeNodes(message, root);
            tree.ExpandAll();
        }

        private static void RecursivelyDisplayMessageAsTreeNodes(DataTree message, TreeNode parentNode)
        {
            foreach (DataTree dataTreeNode in message)
            {
                string itemString = dataTreeNode.Name;
                if ( !string.IsNullOrEmpty( Convert.ToString(dataTreeNode) ) )
                {
                    itemString += ": " + dataTreeNode.Value;
                }

                var displayTreeNode = new MessageTreeNode(itemString);
                displayTreeNode.TreeName = dataTreeNode.Name;

                parentNode.Nodes.Add(displayTreeNode);
                RecursivelyDisplayMessageAsTreeNodes(dataTreeNode, displayTreeNode);
            }
        }

        internal sealed class MessageTreeNode : TreeNode
        {
            public string TreeName = string.Empty;

            internal MessageTreeNode(string name)
                : base(name)
            {
            }
        }

        private void SaveFileDialogConfigMessage_FileOk(object sender, CancelEventArgs e)
        {
            try
            {
                string fileName = SaveFileDialogConfigMessage.FileName;
                
                File.WriteAllText(
                    fileName,
                    developmentServerHost.ConfigurationText,
                    Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save message: " + ex.Message);
            }
        }

        private void OpenFileDialogConfigMessage_FileOk(object sender, CancelEventArgs e)
        {
            string fileName = OpenFileDialogConfigMessage.FileName;
            
            string messageText = File.ReadAllText(fileName, Encoding.UTF8);

            developmentServerHost.SetConfiguration(messageText);
        }

        private void DevelopmentServerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (developmentServerHost.Running)
                developmentServerHost.StopServer();

            developmentServerHost.Dispose();
        }
    }
}

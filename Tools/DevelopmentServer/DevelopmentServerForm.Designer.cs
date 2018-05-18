namespace SystemsGarden.mc2.Tools.DevelopmentServer
{
    partial class DevelopmentServerForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.TreeNode treeNode1 = new System.Windows.Forms.TreeNode("Message");
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DevelopmentServerForm));
            this.TabControlConnections = new System.Windows.Forms.TabControl();
            this.TabPageServer = new System.Windows.Forms.TabPage();
            this.GroupBox3 = new System.Windows.Forms.GroupBox();
            this.ButtonEditConfiguration = new System.Windows.Forms.Button();
            this.ButtonSaveConfiguration = new System.Windows.Forms.Button();
            this.ButtonLoadConfiguration = new System.Windows.Forms.Button();
            this.TreeViewConfiguration = new System.Windows.Forms.TreeView();
            this.ButtonStop = new System.Windows.Forms.Button();
            this.ButtonStart = new System.Windows.Forms.Button();
            this.SaveFileDialogConfigMessage = new System.Windows.Forms.SaveFileDialog();
            this.OpenFileDialogConfigMessage = new System.Windows.Forms.OpenFileDialog();
            this.TabControlConnections.SuspendLayout();
            this.TabPageServer.SuspendLayout();
            this.GroupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // TabControlConnections
            // 
            this.TabControlConnections.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TabControlConnections.Controls.Add(this.TabPageServer);
            this.TabControlConnections.Location = new System.Drawing.Point(12, 12);
            this.TabControlConnections.Name = "TabControlConnections";
            this.TabControlConnections.SelectedIndex = 0;
            this.TabControlConnections.Size = new System.Drawing.Size(531, 392);
            this.TabControlConnections.TabIndex = 15;
            // 
            // TabPageServer
            // 
            this.TabPageServer.Controls.Add(this.GroupBox3);
            this.TabPageServer.Controls.Add(this.ButtonStop);
            this.TabPageServer.Controls.Add(this.ButtonStart);
            this.TabPageServer.Location = new System.Drawing.Point(4, 22);
            this.TabPageServer.Name = "TabPageServer";
            this.TabPageServer.Padding = new System.Windows.Forms.Padding(3);
            this.TabPageServer.Size = new System.Drawing.Size(523, 366);
            this.TabPageServer.TabIndex = 0;
            this.TabPageServer.Text = "Server";
            this.TabPageServer.UseVisualStyleBackColor = true;
            // 
            // GroupBox3
            // 
            this.GroupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.GroupBox3.Controls.Add(this.ButtonEditConfiguration);
            this.GroupBox3.Controls.Add(this.ButtonSaveConfiguration);
            this.GroupBox3.Controls.Add(this.ButtonLoadConfiguration);
            this.GroupBox3.Controls.Add(this.TreeViewConfiguration);
            this.GroupBox3.Location = new System.Drawing.Point(91, 6);
            this.GroupBox3.Name = "GroupBox3";
            this.GroupBox3.Size = new System.Drawing.Size(422, 354);
            this.GroupBox3.TabIndex = 16;
            this.GroupBox3.TabStop = false;
            this.GroupBox3.Text = "Configuration";
            // 
            // ButtonEditConfiguration
            // 
            this.ButtonEditConfiguration.AllowDrop = true;
            this.ButtonEditConfiguration.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ButtonEditConfiguration.Location = new System.Drawing.Point(6, 325);
            this.ButtonEditConfiguration.Name = "ButtonEditConfiguration";
            this.ButtonEditConfiguration.Size = new System.Drawing.Size(75, 23);
            this.ButtonEditConfiguration.TabIndex = 10;
            this.ButtonEditConfiguration.Text = "Edit";
            this.ButtonEditConfiguration.UseVisualStyleBackColor = true;
            this.ButtonEditConfiguration.Click += new System.EventHandler(this.ButtonEditConfiguration_Click);
            // 
            // ButtonSaveConfiguration
            // 
            this.ButtonSaveConfiguration.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonSaveConfiguration.Location = new System.Drawing.Point(341, 325);
            this.ButtonSaveConfiguration.Name = "ButtonSaveConfiguration";
            this.ButtonSaveConfiguration.Size = new System.Drawing.Size(75, 23);
            this.ButtonSaveConfiguration.TabIndex = 9;
            this.ButtonSaveConfiguration.Text = "Save";
            this.ButtonSaveConfiguration.UseVisualStyleBackColor = true;
            this.ButtonSaveConfiguration.Click += new System.EventHandler(this.ButtonSaveConfiguration_Click);
            // 
            // ButtonLoadConfiguration
            // 
            this.ButtonLoadConfiguration.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonLoadConfiguration.Location = new System.Drawing.Point(260, 325);
            this.ButtonLoadConfiguration.Name = "ButtonLoadConfiguration";
            this.ButtonLoadConfiguration.Size = new System.Drawing.Size(75, 23);
            this.ButtonLoadConfiguration.TabIndex = 8;
            this.ButtonLoadConfiguration.Text = "Load";
            this.ButtonLoadConfiguration.UseVisualStyleBackColor = true;
            this.ButtonLoadConfiguration.Click += new System.EventHandler(this.ButtonLoadConfiguration_Click);
            // 
            // TreeViewConfiguration
            // 
            this.TreeViewConfiguration.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TreeViewConfiguration.Location = new System.Drawing.Point(6, 19);
            this.TreeViewConfiguration.Name = "TreeViewConfiguration";
            treeNode1.Name = "Message";
            treeNode1.Text = "Message";
            this.TreeViewConfiguration.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
            treeNode1});
            this.TreeViewConfiguration.Size = new System.Drawing.Size(410, 300);
            this.TreeViewConfiguration.TabIndex = 0;
            // 
            // ButtonStop
            // 
            this.ButtonStop.Cursor = System.Windows.Forms.Cursors.Default;
            this.ButtonStop.Location = new System.Drawing.Point(10, 43);
            this.ButtonStop.Name = "ButtonStop";
            this.ButtonStop.Size = new System.Drawing.Size(75, 23);
            this.ButtonStop.TabIndex = 14;
            this.ButtonStop.Text = "Stop";
            this.ButtonStop.UseVisualStyleBackColor = true;
            this.ButtonStop.Click += new System.EventHandler(this.ButtonStop_Click);
            // 
            // ButtonStart
            // 
            this.ButtonStart.Enabled = false;
            this.ButtonStart.Location = new System.Drawing.Point(10, 14);
            this.ButtonStart.Name = "ButtonStart";
            this.ButtonStart.Size = new System.Drawing.Size(75, 23);
            this.ButtonStart.TabIndex = 13;
            this.ButtonStart.Text = "Start";
            this.ButtonStart.UseVisualStyleBackColor = true;
            this.ButtonStart.Click += new System.EventHandler(this.ButtonStart_Click);
            // 
            // SaveFileDialogConfigMessage
            // 
            this.SaveFileDialogConfigMessage.DefaultExt = "tree";
            this.SaveFileDialogConfigMessage.FileName = "config.tree";
            this.SaveFileDialogConfigMessage.FileOk += new System.ComponentModel.CancelEventHandler(this.SaveFileDialogConfigMessage_FileOk);
            // 
            // OpenFileDialogConfigMessage
            // 
            this.OpenFileDialogConfigMessage.FileOk += new System.ComponentModel.CancelEventHandler(this.OpenFileDialogConfigMessage_FileOk);
            // 
            // DevelopmentServerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(554, 414);
            this.Controls.Add(this.TabControlConnections);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "DevelopmentServerForm";
            this.Text = "Development Server";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.DevelopmentServerForm_FormClosing);
            this.TabControlConnections.ResumeLayout(false);
            this.TabPageServer.ResumeLayout(false);
            this.GroupBox3.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        internal System.Windows.Forms.TabControl TabControlConnections;
        internal System.Windows.Forms.TabPage TabPageServer;
        internal System.Windows.Forms.GroupBox GroupBox3;
        internal System.Windows.Forms.Button ButtonEditConfiguration;
        internal System.Windows.Forms.Button ButtonSaveConfiguration;
        internal System.Windows.Forms.Button ButtonLoadConfiguration;
        internal System.Windows.Forms.TreeView TreeViewConfiguration;
        internal System.Windows.Forms.Button ButtonStop;
        internal System.Windows.Forms.Button ButtonStart;
        internal System.Windows.Forms.SaveFileDialog SaveFileDialogConfigMessage;
        internal System.Windows.Forms.OpenFileDialog OpenFileDialogConfigMessage;
    }
}


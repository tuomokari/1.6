namespace XmlUpdateTool
{
    partial class Form1
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
            this.LoadButton = new System.Windows.Forms.Button();
            this.OpenXmlFileDialgog = new System.Windows.Forms.OpenFileDialog();
            this.button1 = new System.Windows.Forms.Button();
            this.saveXMLDialog = new System.Windows.Forms.SaveFileDialog();
            this.SuspendLayout();
            // 
            // LoadButton
            // 
            this.LoadButton.Location = new System.Drawing.Point(133, 12);
            this.LoadButton.Name = "LoadButton";
            this.LoadButton.Size = new System.Drawing.Size(138, 23);
            this.LoadButton.TabIndex = 28;
            this.LoadButton.Text = "Load and patch XML file";
            this.LoadButton.UseVisualStyleBackColor = true;
            this.LoadButton.Click += new System.EventHandler(this.LoadButton_Click);
            // 
            // OpenXmlFileDialgog
            // 
            this.OpenXmlFileDialgog.FileName = "openFileDialog1";
            this.OpenXmlFileDialgog.Title = "Open XML";
            this.OpenXmlFileDialgog.FileOk += new System.ComponentModel.CancelEventHandler(this.OpenXmlFileDialgog_FileOk);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(133, 41);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(138, 23);
            this.button1.TabIndex = 29;
            this.button1.Text = "Save XML file";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // saveXMLDialog
            // 
            this.saveXMLDialog.Title = "Save XML";
            this.saveXMLDialog.FileOk += new System.ComponentModel.CancelEventHandler(this.saveXMLDialog_FileOk);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(402, 87);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.LoadButton);
            this.Name = "Form1";
            this.Text = "XML note (txt) field to 60 chars";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button LoadButton;
        private System.Windows.Forms.OpenFileDialog OpenXmlFileDialgog;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.SaveFileDialog saveXMLDialog;
    }
}


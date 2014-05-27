// Copyright 2010 Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License"); 
// You may not use this file except in compliance with the License. 
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 

// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, 
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR 
// CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, 
// MERCHANTABLITY OR NON-INFRINGEMENT. 

// See the Apache 2 License for the specific language governing 
// permissions and limitations under the License.

namespace SyncSvcUtilUI.CodegenWizardPages
{
    partial class Step1_PickConfigOrCSDLModelPage
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.openBtn = new System.Windows.Forms.Button();
            this.openCfgTextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.configPanel = new System.Windows.Forms.Panel();
            this.dbsBox = new System.Windows.Forms.ComboBox();
            this.scopesBox = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.csdlPanel = new System.Windows.Forms.Panel();
            this.errorLbl = new System.Windows.Forms.Label();
            this.infoLbl = new System.Windows.Forms.Label();
            this.browseBtn = new System.Windows.Forms.Button();
            this.csdlUrl = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.csdlScopesBox = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.useConfigOption = new System.Windows.Forms.RadioButton();
            this.csdlOption = new System.Windows.Forms.RadioButton();
            this.label6 = new System.Windows.Forms.Label();
            this.configPanel.SuspendLayout();
            this.csdlPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // openBtn
            // 
            this.openBtn.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.openBtn.Location = new System.Drawing.Point(427, 6);
            this.openBtn.Name = "openBtn";
            this.openBtn.Size = new System.Drawing.Size(61, 23);
            this.openBtn.TabIndex = 6;
            this.openBtn.Text = "Open";
            this.openBtn.UseVisualStyleBackColor = true;
            this.openBtn.Click += new System.EventHandler(this.openBtn_Click);
            // 
            // openCfgTextBox
            // 
            this.openCfgTextBox.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.openCfgTextBox.Location = new System.Drawing.Point(153, 7);
            this.openCfgTextBox.Name = "openCfgTextBox";
            this.openCfgTextBox.Size = new System.Drawing.Size(227, 22);
            this.openCfgTextBox.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(3, 11);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(139, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Specify the config file: ";
            // 
            // openFileDialog
            // 
            this.openFileDialog.FileName = "openFileDialog1";
            // 
            // configPanel
            // 
            this.configPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.configPanel.Controls.Add(this.dbsBox);
            this.configPanel.Controls.Add(this.scopesBox);
            this.configPanel.Controls.Add(this.label4);
            this.configPanel.Controls.Add(this.label3);
            this.configPanel.Controls.Add(this.openBtn);
            this.configPanel.Controls.Add(this.label2);
            this.configPanel.Controls.Add(this.openCfgTextBox);
            this.configPanel.Location = new System.Drawing.Point(13, 63);
            this.configPanel.Name = "configPanel";
            this.configPanel.Size = new System.Drawing.Size(504, 120);
            this.configPanel.TabIndex = 11;
            // 
            // dbsBox
            // 
            this.dbsBox.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dbsBox.FormattingEnabled = true;
            this.dbsBox.Location = new System.Drawing.Point(154, 88);
            this.dbsBox.Name = "dbsBox";
            this.dbsBox.Size = new System.Drawing.Size(226, 22);
            this.dbsBox.TabIndex = 10;
            // 
            // scopesBox
            // 
            this.scopesBox.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.scopesBox.FormattingEnabled = true;
            this.scopesBox.Location = new System.Drawing.Point(153, 47);
            this.scopesBox.Name = "scopesBox";
            this.scopesBox.Size = new System.Drawing.Size(226, 22);
            this.scopesBox.TabIndex = 9;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(6, 92);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(141, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "Select Target Database";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(34, 51);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(113, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "Select Sync Scope";
            // 
            // csdlPanel
            // 
            this.csdlPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.csdlPanel.Controls.Add(this.errorLbl);
            this.csdlPanel.Controls.Add(this.infoLbl);
            this.csdlPanel.Controls.Add(this.browseBtn);
            this.csdlPanel.Controls.Add(this.csdlUrl);
            this.csdlPanel.Controls.Add(this.label1);
            this.csdlPanel.Controls.Add(this.csdlScopesBox);
            this.csdlPanel.Controls.Add(this.label5);
            this.csdlPanel.Enabled = false;
            this.csdlPanel.Location = new System.Drawing.Point(13, 212);
            this.csdlPanel.Name = "csdlPanel";
            this.csdlPanel.Size = new System.Drawing.Size(504, 135);
            this.csdlPanel.TabIndex = 12;
            // 
            // errorLbl
            // 
            this.errorLbl.AutoSize = true;
            this.errorLbl.ForeColor = System.Drawing.Color.Red;
            this.errorLbl.Location = new System.Drawing.Point(69, 105);
            this.errorLbl.Name = "errorLbl";
            this.errorLbl.Size = new System.Drawing.Size(331, 13);
            this.errorLbl.TabIndex = 15;
            this.errorLbl.Text = "Unable to find a Sync Service Metadata document at specified URL.";
            this.errorLbl.Visible = false;
            // 
            // infoLbl
            // 
            this.infoLbl.AutoSize = true;
            this.infoLbl.ForeColor = System.Drawing.Color.Black;
            this.infoLbl.Location = new System.Drawing.Point(65, 105);
            this.infoLbl.Name = "infoLbl";
            this.infoLbl.Size = new System.Drawing.Size(326, 13);
            this.infoLbl.TabIndex = 14;
            this.infoLbl.Text = "Trynig to discover Sync Service metadata document....Please wait..";
            this.infoLbl.Visible = false;
            // 
            // browseBtn
            // 
            this.browseBtn.Enabled = false;
            this.browseBtn.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.browseBtn.Location = new System.Drawing.Point(423, 7);
            this.browseBtn.Name = "browseBtn";
            this.browseBtn.Size = new System.Drawing.Size(62, 23);
            this.browseBtn.TabIndex = 13;
            this.browseBtn.Text = "Browse";
            this.browseBtn.UseVisualStyleBackColor = true;
            this.browseBtn.Click += new System.EventHandler(this.browseBtn_Click);
            // 
            // csdlUrl
            // 
            this.csdlUrl.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.csdlUrl.Location = new System.Drawing.Point(150, 8);
            this.csdlUrl.Name = "csdlUrl";
            this.csdlUrl.Size = new System.Drawing.Size(250, 22);
            this.csdlUrl.TabIndex = 12;
            this.csdlUrl.TextChanged += new System.EventHandler(this.csdlUrl_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(7, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(144, 13);
            this.label1.TabIndex = 10;
            this.label1.Text = "Specify the Service URL";
            // 
            // csdlScopesBox
            // 
            this.csdlScopesBox.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.csdlScopesBox.FormattingEnabled = true;
            this.csdlScopesBox.Location = new System.Drawing.Point(150, 53);
            this.csdlScopesBox.Name = "csdlScopesBox";
            this.csdlScopesBox.Size = new System.Drawing.Size(226, 22);
            this.csdlScopesBox.TabIndex = 11;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(38, 62);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(113, 13);
            this.label5.TabIndex = 9;
            this.label5.Text = "Select Sync Scope";
            // 
            // useConfigOption
            // 
            this.useConfigOption.AutoSize = true;
            this.useConfigOption.Checked = true;
            this.useConfigOption.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.useConfigOption.Location = new System.Drawing.Point(14, 42);
            this.useConfigOption.Name = "useConfigOption";
            this.useConfigOption.Size = new System.Drawing.Size(148, 17);
            this.useConfigOption.TabIndex = 13;
            this.useConfigOption.TabStop = true;
            this.useConfigOption.Text = "From Sync Config File";
            this.useConfigOption.UseVisualStyleBackColor = true;
            this.useConfigOption.CheckedChanged += new System.EventHandler(this.useConfigOption_CheckedChanged);
            // 
            // csdlOption
            // 
            this.csdlOption.AutoSize = true;
            this.csdlOption.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.csdlOption.Location = new System.Drawing.Point(13, 189);
            this.csdlOption.Name = "csdlOption";
            this.csdlOption.Size = new System.Drawing.Size(143, 17);
            this.csdlOption.TabIndex = 14;
            this.csdlOption.Text = "From A Sync Service";
            this.csdlOption.UseVisualStyleBackColor = true;
            this.csdlOption.CheckedChanged += new System.EventHandler(this.csdlOption_CheckedChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Verdana", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(17, 15);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(285, 16);
            this.label6.TabIndex = 15;
            this.label6.Text = "Step1 - Select code generation source";
            // 
            // Step1_PickConfigOrCSDLModelPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label6);
            this.Controls.Add(this.csdlOption);
            this.Controls.Add(this.useConfigOption);
            this.Controls.Add(this.csdlPanel);
            this.Controls.Add(this.configPanel);
            this.Name = "Step1_PickConfigOrCSDLModelPage";
            this.Size = new System.Drawing.Size(560, 352);
            this.configPanel.ResumeLayout(false);
            this.configPanel.PerformLayout();
            this.csdlPanel.ResumeLayout(false);
            this.csdlPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button openBtn;
        private System.Windows.Forms.TextBox openCfgTextBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.Panel configPanel;
        private System.Windows.Forms.ComboBox dbsBox;
        private System.Windows.Forms.ComboBox scopesBox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Panel csdlPanel;
        private System.Windows.Forms.Label errorLbl;
        private System.Windows.Forms.Label infoLbl;
        private System.Windows.Forms.Button browseBtn;
        private System.Windows.Forms.TextBox csdlUrl;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox csdlScopesBox;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.RadioButton useConfigOption;
        private System.Windows.Forms.RadioButton csdlOption;
        private System.Windows.Forms.Label label6;
    }
}

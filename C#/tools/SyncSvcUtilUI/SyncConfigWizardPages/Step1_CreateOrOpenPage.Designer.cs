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

namespace SyncSvcUtilUI.SyncConfigWizardPages
{
    partial class Step1_CreateOrOpenPage
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
            this.label1 = new System.Windows.Forms.Label();
            this.createNewRadioBtn = new System.Windows.Forms.RadioButton();
            this.editRadioBtn = new System.Windows.Forms.RadioButton();
            this.label2 = new System.Windows.Forms.Label();
            this.createPanel = new System.Windows.Forms.Panel();
            this.createBtn = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.newCfgTxtBox = new System.Windows.Forms.TextBox();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.openPanel = new System.Windows.Forms.Panel();
            this.openBtn = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.openCfgTextBox = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.createPanel.SuspendLayout();
            this.openPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Verdana", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(17, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(330, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "Step1 - Create or edit Sync configuration file";
            // 
            // createNewRadioBtn
            // 
            this.createNewRadioBtn.AutoSize = true;
            this.createNewRadioBtn.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.createNewRadioBtn.Location = new System.Drawing.Point(42, 37);
            this.createNewRadioBtn.Name = "createNewRadioBtn";
            this.createNewRadioBtn.Size = new System.Drawing.Size(252, 18);
            this.createNewRadioBtn.TabIndex = 1;
            this.createNewRadioBtn.TabStop = true;
            this.createNewRadioBtn.Text = "Create a new Sync configuration file";
            this.createNewRadioBtn.UseVisualStyleBackColor = true;
            this.createNewRadioBtn.CheckedChanged += new System.EventHandler(this.createNewRadioBtn_CheckedChanged);
            // 
            // editRadioBtn
            // 
            this.editRadioBtn.AutoSize = true;
            this.editRadioBtn.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.editRadioBtn.Location = new System.Drawing.Point(41, 162);
            this.editRadioBtn.Name = "editRadioBtn";
            this.editRadioBtn.Size = new System.Drawing.Size(263, 18);
            this.editRadioBtn.TabIndex = 2;
            this.editRadioBtn.TabStop = true;
            this.editRadioBtn.Text = "Edit an existing Sync configuration file";
            this.editRadioBtn.UseVisualStyleBackColor = true;
            this.editRadioBtn.CheckedChanged += new System.EventHandler(this.editRadioBtn_CheckedChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(18, 12);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(109, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Specify the path: ";
            // 
            // createPanel
            // 
            this.createPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.createPanel.Controls.Add(this.createBtn);
            this.createPanel.Controls.Add(this.label3);
            this.createPanel.Controls.Add(this.newCfgTxtBox);
            this.createPanel.Controls.Add(this.label2);
            this.createPanel.Location = new System.Drawing.Point(20, 63);
            this.createPanel.Name = "createPanel";
            this.createPanel.Size = new System.Drawing.Size(522, 76);
            this.createPanel.TabIndex = 4;
            // 
            // createBtn
            // 
            this.createBtn.Location = new System.Drawing.Point(388, 10);
            this.createBtn.Name = "createBtn";
            this.createBtn.Size = new System.Drawing.Size(75, 23);
            this.createBtn.TabIndex = 6;
            this.createBtn.Text = "Browse";
            this.createBtn.UseVisualStyleBackColor = true;
            this.createBtn.Click += new System.EventHandler(this.createBtn_Click);
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(131, 39);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(355, 23);
            this.label3.TabIndex = 5;
            this.label3.Text = "A new sync configuration file will be created in the location you specified.";
            // 
            // newCfgTxtBox
            // 
            this.newCfgTxtBox.Location = new System.Drawing.Point(134, 12);
            this.newCfgTxtBox.Name = "newCfgTxtBox";
            this.newCfgTxtBox.Size = new System.Drawing.Size(247, 20);
            this.newCfgTxtBox.TabIndex = 4;
            // 
            // openFileDialog
            // 
            this.openFileDialog.FileName = "openFileDialog1";
            // 
            // openPanel
            // 
            this.openPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.openPanel.Controls.Add(this.openBtn);
            this.openPanel.Controls.Add(this.label4);
            this.openPanel.Controls.Add(this.openCfgTextBox);
            this.openPanel.Controls.Add(this.label5);
            this.openPanel.Location = new System.Drawing.Point(20, 189);
            this.openPanel.Name = "openPanel";
            this.openPanel.Size = new System.Drawing.Size(522, 76);
            this.openPanel.TabIndex = 5;
            // 
            // openBtn
            // 
            this.openBtn.Location = new System.Drawing.Point(388, 10);
            this.openBtn.Name = "openBtn";
            this.openBtn.Size = new System.Drawing.Size(75, 23);
            this.openBtn.TabIndex = 6;
            this.openBtn.Text = "Open";
            this.openBtn.UseVisualStyleBackColor = true;
            this.openBtn.Click += new System.EventHandler(this.openBtn_Click);
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(131, 39);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(355, 23);
            this.label4.TabIndex = 5;
            this.label4.Text = "Load sync configuration from an existing file.";
            // 
            // openCfgTextBox
            // 
            this.openCfgTextBox.Location = new System.Drawing.Point(134, 12);
            this.openCfgTextBox.Name = "openCfgTextBox";
            this.openCfgTextBox.Size = new System.Drawing.Size(247, 20);
            this.openCfgTextBox.TabIndex = 4;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(18, 12);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(101, 13);
            this.label5.TabIndex = 3;
            this.label5.Text = "Specify the file: ";
            // 
            // Step1_CreateOrOpenPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.openPanel);
            this.Controls.Add(this.createPanel);
            this.Controls.Add(this.editRadioBtn);
            this.Controls.Add(this.createNewRadioBtn);
            this.Controls.Add(this.label1);
            this.Name = "Step1_CreateOrOpenPage";
            this.Size = new System.Drawing.Size(559, 288);
            this.createPanel.ResumeLayout(false);
            this.createPanel.PerformLayout();
            this.openPanel.ResumeLayout(false);
            this.openPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RadioButton createNewRadioBtn;
        private System.Windows.Forms.RadioButton editRadioBtn;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel createPanel;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.TextBox newCfgTxtBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button createBtn;
        private System.Windows.Forms.Panel openPanel;
        private System.Windows.Forms.Button openBtn;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox openCfgTextBox;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
    }
}

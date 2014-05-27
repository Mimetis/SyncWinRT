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

namespace SyncSvcUtilUI.SyncProvisionWizardPages
{
    partial class Setp1_GetAndOpenConfigFile
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
            this.openPanel = new System.Windows.Forms.Panel();
            this.openCfgTextBox = new System.Windows.Forms.TextBox();
            this.openBtn = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.settingsGrp = new System.Windows.Forms.GroupBox();
            this.deprovstoreOption = new System.Windows.Forms.RadioButton();
            this.deprovOption = new System.Windows.Forms.RadioButton();
            this.provisionOption = new System.Windows.Forms.RadioButton();
            this.dbsBox = new System.Windows.Forms.ComboBox();
            this.scopesBox = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.openPanel.SuspendLayout();
            this.settingsGrp.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Verdana", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(17, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(68, 16);
            this.label1.TabIndex = 1;
            this.label1.Text = "Settings";
            // 
            // openPanel
            // 
            this.openPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.openPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.openPanel.Controls.Add(this.openCfgTextBox);
            this.openPanel.Controls.Add(this.openBtn);
            this.openPanel.Controls.Add(this.label5);
            this.openPanel.Location = new System.Drawing.Point(20, 51);
            this.openPanel.Name = "openPanel";
            this.openPanel.Size = new System.Drawing.Size(500, 67);
            this.openPanel.TabIndex = 6;
            // 
            // openCfgTextBox
            // 
            this.openCfgTextBox.Location = new System.Drawing.Point(148, 11);
            this.openCfgTextBox.Name = "openCfgTextBox";
            this.openCfgTextBox.Size = new System.Drawing.Size(247, 21);
            this.openCfgTextBox.TabIndex = 4;
            // 
            // openBtn
            // 
            this.openBtn.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.openBtn.Location = new System.Drawing.Point(411, 11);
            this.openBtn.Name = "openBtn";
            this.openBtn.Size = new System.Drawing.Size(75, 23);
            this.openBtn.TabIndex = 6;
            this.openBtn.Text = "Open";
            this.openBtn.UseVisualStyleBackColor = true;
            this.openBtn.Click += new System.EventHandler(this.openBtn_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(3, 15);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(139, 13);
            this.label5.TabIndex = 3;
            this.label5.Text = "Specify the config file: ";
            // 
            // openFileDialog
            // 
            this.openFileDialog.FileName = "openFileDialog1";
            // 
            // settingsGrp
            // 
            this.settingsGrp.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.settingsGrp.Controls.Add(this.deprovstoreOption);
            this.settingsGrp.Controls.Add(this.deprovOption);
            this.settingsGrp.Controls.Add(this.provisionOption);
            this.settingsGrp.Controls.Add(this.dbsBox);
            this.settingsGrp.Controls.Add(this.scopesBox);
            this.settingsGrp.Controls.Add(this.label6);
            this.settingsGrp.Controls.Add(this.label4);
            this.settingsGrp.Controls.Add(this.label3);
            this.settingsGrp.Enabled = false;
            this.settingsGrp.Location = new System.Drawing.Point(20, 124);
            this.settingsGrp.Name = "settingsGrp";
            this.settingsGrp.Size = new System.Drawing.Size(500, 183);
            this.settingsGrp.TabIndex = 7;
            this.settingsGrp.TabStop = false;
            this.settingsGrp.Text = "Settings";
            // 
            // deprovstoreOption
            // 
            this.deprovstoreOption.AutoSize = true;
            this.deprovstoreOption.Location = new System.Drawing.Point(170, 130);
            this.deprovstoreOption.Name = "deprovstoreOption";
            this.deprovstoreOption.Size = new System.Drawing.Size(182, 17);
            this.deprovstoreOption.TabIndex = 7;
            this.deprovstoreOption.Text = "Deprovision complete store";
            this.deprovstoreOption.UseVisualStyleBackColor = true;
            // 
            // deprovOption
            // 
            this.deprovOption.AutoSize = true;
            this.deprovOption.Location = new System.Drawing.Point(277, 107);
            this.deprovOption.Name = "deprovOption";
            this.deprovOption.Size = new System.Drawing.Size(93, 17);
            this.deprovOption.TabIndex = 6;
            this.deprovOption.Text = "Deprovision";
            this.deprovOption.UseVisualStyleBackColor = true;
            // 
            // provisionOption
            // 
            this.provisionOption.AutoSize = true;
            this.provisionOption.Checked = true;
            this.provisionOption.Location = new System.Drawing.Point(170, 107);
            this.provisionOption.Name = "provisionOption";
            this.provisionOption.Size = new System.Drawing.Size(77, 17);
            this.provisionOption.TabIndex = 5;
            this.provisionOption.TabStop = true;
            this.provisionOption.Text = "Provision";
            this.provisionOption.UseVisualStyleBackColor = true;
            // 
            // dbsBox
            // 
            this.dbsBox.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dbsBox.FormattingEnabled = true;
            this.dbsBox.Location = new System.Drawing.Point(170, 68);
            this.dbsBox.Name = "dbsBox";
            this.dbsBox.Size = new System.Drawing.Size(226, 22);
            this.dbsBox.TabIndex = 4;
            // 
            // scopesBox
            // 
            this.scopesBox.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.scopesBox.FormattingEnabled = true;
            this.scopesBox.Location = new System.Drawing.Point(170, 27);
            this.scopesBox.Name = "scopesBox";
            this.scopesBox.Size = new System.Drawing.Size(226, 22);
            this.scopesBox.TabIndex = 3;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(78, 109);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(85, 13);
            this.label6.TabIndex = 2;
            this.label6.Text = "Select Mode";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(4, 71);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(159, 13);
            this.label4.TabIndex = 1;
            this.label4.Text = "Select Target Database";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(38, 30);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(125, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "Select Sync Scope";
            // 
            // Setp1_GetAndOpenConfigFile
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.settingsGrp);
            this.Controls.Add(this.openPanel);
            this.Controls.Add(this.label1);
            this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "Setp1_GetAndOpenConfigFile";
            this.Size = new System.Drawing.Size(560, 340);
            this.openPanel.ResumeLayout(false);
            this.openPanel.PerformLayout();
            this.settingsGrp.ResumeLayout(false);
            this.settingsGrp.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel openPanel;
        private System.Windows.Forms.Button openBtn;
        private System.Windows.Forms.TextBox openCfgTextBox;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.GroupBox settingsGrp;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox dbsBox;
        private System.Windows.Forms.ComboBox scopesBox;
        private System.Windows.Forms.RadioButton provisionOption;
        private System.Windows.Forms.RadioButton deprovOption;
        private System.Windows.Forms.RadioButton deprovstoreOption;
    }
}

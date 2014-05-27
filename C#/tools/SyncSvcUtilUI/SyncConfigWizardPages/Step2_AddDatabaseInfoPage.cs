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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Synchronization.ClientServices.Configuration;
using System.Data.SqlClient;

namespace SyncSvcUtilUI.SyncConfigWizardPages
{

    public partial class Step2_AddDatabaseInfoPage : UserControl, IWizardPage
    {
        bool addMode = false;

        public Step2_AddDatabaseInfoPage()
        {
            InitializeComponent();
        }

        private void ReadAndBindData()
        {
            this.dbListBox.Items.Clear();
            foreach (TargetDatabaseConfigElement dbs in WizardHelper.Instance.SyncConfigSection.Databases)
            {
                this.dbListBox.Items.Add(dbs.Name);
            }
            this.dbListBox.SelectedIndex = -1;
            this.removeBtn.Enabled = true;
            this.editBtn.Enabled = true;
        }

        private void useIntAuthRadioBtn_CheckedChanged(object sender, EventArgs e)
        {
            SetVisibilityForSqlLoginFields();
        }

        private void SetVisibilityForSqlLoginFields()
        {
            this.unameTxtBox.Enabled = this.useSqlAuthRadioBtn.Checked;
            this.pwdTextBox.Enabled = this.useSqlAuthRadioBtn.Checked;
        }

        private void editBtn_Click(object sender, EventArgs e)
        {
            this.dbSettingsGrp.Enabled = true;
        }

        private void useSqlAuthRadioBtn_CheckedChanged(object sender, EventArgs e)
        {
            SetVisibilityForSqlLoginFields();
        }

        private void dbListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.dbListBox.SelectedIndex != -1)
            {
                // When a TargetDatabase node is selected populate the values
                this.addMode = false;

                this.removeBtn.Enabled = true;
                this.editBtn.Enabled = true;

                TargetDatabaseConfigElement db = WizardHelper.Instance.SyncConfigSection.Databases.GetElementAt(this.dbListBox.SelectedIndex);
                this.useIntAuthRadioBtn.Checked = db.UseIntegratedAuth;
                this.useSqlAuthRadioBtn.Checked = !db.UseIntegratedAuth;
                this.cfgNameTxtBox.Text = db.Name;
                this.useIntAuthRadioBtn.Checked = db.UseIntegratedAuth;
                this.unameTxtBox.Text = db.UserName;
                this.pwdTextBox.Text = db.Password;
                this.dbServerTxtBox.Text = db.DbServer;
                this.dbNameTxtBox.Text = db.DbName;
            }
        }

        private void addBtn_Click(object sender, EventArgs e)
        {
            this.dbSettingsGrp.Enabled = true;
            this.addMode = true;

            // Set all values to empty
            this.dbServerTxtBox.Text = this.dbNameTxtBox.Text =
                this.unameTxtBox.Text = this.pwdTextBox.Text = string.Empty;
            this.cfgNameTxtBox.Text = "[Enter New Target Database]";
            this.useIntAuthRadioBtn.Checked = true;
        }

        private void testBtn_Click(object sender, EventArgs e)
        {
            if (TestConnection())
            {
                MessageBox.Show("Connection successful.", "Connection test.", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void saveBtn_Click(object sender, EventArgs e)
        {
            // First test connection
            if (this.TestConnection())
            {
                // Now add this to the Target database list
                if (string.IsNullOrEmpty(this.cfgNameTxtBox.Text))
                {
                    MessageBox.Show("Please enter a valid name for Config Name.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.cfgNameTxtBox.Focus();
                }
                if (addMode)
                {
                    TargetDatabaseConfigElement db = new TargetDatabaseConfigElement()
                    {
                        Name = this.cfgNameTxtBox.Text,
                        UseIntegratedAuth = this.useIntAuthRadioBtn.Checked,
                        UserName = this.unameTxtBox.Text,
                        Password = this.pwdTextBox.Text,
                        DbServer = this.dbServerTxtBox.Text,
                        DbName = this.dbNameTxtBox.Text
                    };
                    WizardHelper.Instance.SyncConfigSection.Databases.Add(db);
                    ReadAndBindData();
                }
                else
                {
                    TargetDatabaseConfigElement db = WizardHelper.Instance.SyncConfigSection.Databases.GetElementAt(this.dbListBox.SelectedIndex);
                    db.Name = this.cfgNameTxtBox.Text;
                    db.UseIntegratedAuth = this.useIntAuthRadioBtn.Checked;
                    db.UserName = this.unameTxtBox.Text;
                    db.Password = this.pwdTextBox.Text;
                    db.DbServer = this.dbServerTxtBox.Text;
                    db.DbName = this.dbNameTxtBox.Text;
                }
                this.dbSettingsGrp.Enabled = false;
            }
        }

        private bool TestConnection()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder.DataSource = this.dbServerTxtBox.Text;
            builder.InitialCatalog = this.dbNameTxtBox.Text;
            builder.IntegratedSecurity = this.useIntAuthRadioBtn.Checked;
            if (!builder.IntegratedSecurity)
            {
                builder.UserID = this.unameTxtBox.Text;
                builder.Password = this.pwdTextBox.Text;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(builder.ConnectionString))
                {
                    conn.Open();
                }
                return true;
            }
            catch (SqlException sqlE)
            {
                MessageBox.Show("Connection to database failed. " + sqlE.Message, "Connection test.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void removeBtn_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Do you want to remove the Database config info?", "Remove Database Config", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                WizardHelper.Instance.SyncConfigSection.Databases.Remove(this.dbListBox.SelectedItem.ToString());
                ReadAndBindData();
            }
        }

        #region IWizardPage Members

        public bool OnMovingNext()
        {
            return true;
        }

        public void OnFinish()
        {
            // Noop for Finish
        }

        public bool OnMovingBack()
        {
             return true;
            // no-op
        }

        public void OnFocus()
        {
            ReadAndBindData();
            SetVisibilityForSqlLoginFields();
            dbListBox.SelectedIndex = -1;
            dbSettingsGrp.Enabled = false;
        }
        #endregion
    }
}

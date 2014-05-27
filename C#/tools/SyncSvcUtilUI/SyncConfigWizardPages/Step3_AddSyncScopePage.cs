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

    public partial class Step3_AddSyncScopePage : UserControl, IWizardPage
    {
        bool addMode = false;

        public Step3_AddSyncScopePage()
        {
            InitializeComponent();
        }

        private void ReadAndBindData()
        {
            this.syncScopeBox.Items.Clear();
            foreach (SyncScopeConfigElement scope in WizardHelper.Instance.SyncConfigSection.SyncScopes)
            {
                this.syncScopeBox.Items.Add(scope.Name);
            }
            this.syncScopeBox.SelectedIndex = -1;
            this.removeBtn.Enabled = true;
            this.editBtn.Enabled = true;
        }

        private void editBtn_Click(object sender, EventArgs e)
        {
            this.scopeSettingsGrp.Enabled = true;
        }

        private void syncScopeBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.syncScopeBox.SelectedIndex != -1)
            {
                // When a SyncScope node is selected populate the values
                this.addMode = false;

                this.removeBtn.Enabled = true;
                this.editBtn.Enabled = true;

                SyncScopeConfigElement scope = WizardHelper.Instance.SyncConfigSection.SyncScopes.GetElementAt(this.syncScopeBox.SelectedIndex);
                this.scopeNameTxtBox.Text = scope.Name;
                this.schemaNameTxtBox.Text = scope.SchemaName;
                this.isTempScopeOption.Checked = scope.IsTemplateScope;
                this.enableBulkProcsOption.Checked = scope.EnableBulkApplyProcedures;
            }
        }

        private void addBtn_Click(object sender, EventArgs e)
        {
            this.scopeSettingsGrp.Enabled = true;
            this.addMode = true;

            // Set all values to empty
            this.schemaNameTxtBox.Text = string.Empty;
            this.scopeNameTxtBox.Text = "[Enter New SyncScope Name]";
        }

        private void saveBtn_Click(object sender, EventArgs e)
        {
            // Now add this to the SyncScopes list
            if (string.IsNullOrEmpty(this.scopeNameTxtBox.Text))
            {
                MessageBox.Show("Please enter a valid name for sync scope Name.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.scopeNameTxtBox.Focus();
            }
            if (addMode)
            {
                SyncScopeConfigElement scope = new SyncScopeConfigElement()
                {
                    Name = this.scopeNameTxtBox.Text,
                    IsTemplateScope = this.isTempScopeOption.Checked,
                    EnableBulkApplyProcedures = this.enableBulkProcsOption.Checked,
                };
                if (!string.IsNullOrEmpty(this.schemaNameTxtBox.Text))
                {
                    scope.SchemaName = this.schemaNameTxtBox.Text;
                }

                WizardHelper.Instance.SyncConfigSection.SyncScopes.Add(scope);
                ReadAndBindData();
            }
            else
            {
                SyncScopeConfigElement scope = WizardHelper.Instance.SyncConfigSection.SyncScopes.GetElementAt(this.syncScopeBox.SelectedIndex);
                scope.Name = this.scopeNameTxtBox.Text;
                if (!string.IsNullOrEmpty(this.schemaNameTxtBox.Text))
                {
                    scope.SchemaName = this.schemaNameTxtBox.Text;
                }
                scope.IsTemplateScope = this.isTempScopeOption.Checked;
                scope.EnableBulkApplyProcedures = this.enableBulkProcsOption.Checked;
            }
            this.scopeSettingsGrp.Enabled = false;
        }

        private void removeBtn_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Do you want to remove the selected SyncScope?", "Remove SyncScope Config", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                WizardHelper.Instance.SyncConfigSection.Databases.Remove(this.syncScopeBox.SelectedItem.ToString());
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
            syncScopeBox.SelectedIndex = -1;
            scopeSettingsGrp.Enabled = false;
        }
        #endregion
    }
}

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

    public partial class Step4_AddSyncTablesPage : UserControl, IWizardPage
    {
        public Step4_AddSyncTablesPage()
        {
            InitializeComponent();
        }

        private void ReadAndBindData()
        {
            this.syncTablesBox.Items.Clear();
            this.scopeComboBox.Items.Clear();
            foreach (SyncScopeConfigElement scope in WizardHelper.Instance.SyncConfigSection.SyncScopes)
            {
                this.scopeComboBox.Items.Add(scope.Name);
            }
            this.scopeComboBox.SelectedIndex = -1;
            this.selectTablesGrp.Enabled = false;
            this.upBtn.Enabled = false;
            this.downBtn.Enabled = false;
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
        }
        #endregion

        private void delBtn_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Do you want to remove the selected SyncTable?", "Remove SyncTable Config", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                WizardHelper.Instance.SyncConfigSection.SyncScopes.GetElementAt(this.scopeComboBox.SelectedIndex)
                    .SyncTables.Remove(this.syncTablesBox.SelectedItem.ToString());
                this.ReadAndBindTablesData();
            }
        }

        private void scopeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.scopeComboBox.SelectedIndex > -1)
            {
                ReadAndBindTablesData();
                this.selectTablesGrp.Enabled = true;
            }
        }

        private void ReadAndBindTablesData()
        {
            SyncScopeConfigElement selectedScope = WizardHelper.Instance.SyncConfigSection.SyncScopes.GetElementAt(this.scopeComboBox.SelectedIndex);
            this.syncTablesBox.Items.Clear();
            foreach (SyncTableConfigElement table in selectedScope.SyncTables)
            {
                this.syncTablesBox.Items.Add(table.Name);
            }
            this.syncTablesBox.SelectedIndex = -1;
            this.delBtn.Enabled = this.syncTablesBox.Items.Count > 0;
        }

        private void syncTablesBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.syncTablesBox.SelectedIndex > -1)
            {
                SyncScopeConfigElement selectedScope = WizardHelper.Instance.SyncConfigSection.SyncScopes.GetElementAt(this.scopeComboBox.SelectedIndex);

                SyncTableConfigElement table = selectedScope.SyncTables.GetElementAt(this.syncTablesBox.SelectedIndex);
                if (string.IsNullOrEmpty(table.FilterClause))
                {
                    this.filterColTxtBox.Text = table.FilterClause;
                }

                this.allColsIncludedOption.Checked = table.IncludeAllColumns;
                this.filterColTxtBox.Text = table.FilterClause;
            }
            // Allow reordering the SyncTable orders.
            upBtn.Enabled = this.syncTablesBox.SelectedIndex > 0;
            downBtn.Enabled = this.syncTablesBox.SelectedIndex != this.syncTablesBox.Items.Count - 1;
        }

        private void upBtn_Click(object sender, EventArgs e)
        {
            MoveTable(this.syncTablesBox.SelectedIndex, this.syncTablesBox.SelectedIndex - 1);
        }

        private void downBtn_Click(object sender, EventArgs e)
        {
            this.MoveTable(this.syncTablesBox.SelectedIndex, this.syncTablesBox.SelectedIndex + 1);
        }

        private void MoveTable(int currentIndex, int newIndex)
        {
            SyncScopeConfigElement selectedScope = WizardHelper.Instance.SyncConfigSection.SyncScopes.GetElementAt(this.scopeComboBox.SelectedIndex);
            SyncTableConfigElement table = selectedScope.SyncTables.GetElementAt(currentIndex);

            // Remove element from current index
            selectedScope.SyncTables.RemoveAt(this.syncTablesBox.SelectedIndex);
            if (newIndex == selectedScope.SyncTables.Count)
            {
                // It means the element should go to the end of the list. Just call add
                selectedScope.SyncTables.Add(table);
                newIndex = selectedScope.SyncTables.Count - 1;
            }
            else
            {
                // Re-Add the element at the new index
                selectedScope.SyncTables.Add(newIndex, table);
            }

            // Rebind
            this.ReadAndBindTablesData();
            this.syncTablesBox.SelectedIndex = newIndex;
        }

        private void addBtn_Click(object sender, EventArgs e)
        {
            using (Step4a_ReadTableInfoFromDbPage dialog = new Step4a_ReadTableInfoFromDbPage(this.scopeComboBox.SelectedIndex))
            {
                dialog.ShowDialog(this);
            }
            this.ReadAndBindTablesData();
        }
    }
}

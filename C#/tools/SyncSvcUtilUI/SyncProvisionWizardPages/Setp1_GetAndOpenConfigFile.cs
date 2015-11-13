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
using System.Configuration;
using Microsoft.Synchronization.ClientServices.Configuration;

namespace SyncSvcUtilUI.SyncProvisionWizardPages
{
    public partial class Setp1_GetAndOpenConfigFile : UserControl, IWizardPage
    {
        public Setp1_GetAndOpenConfigFile()
        {
            InitializeComponent();

            // Set dialog settings
            openFileDialog.Filter = "Sync Config|*.config|All Files(*.*)|*.*";
            openFileDialog.Title = "Open Sync Configuration file";
            openFileDialog.CheckFileExists = true;
            openFileDialog.DefaultExt = ".config";
            openFileDialog.InitialDirectory = System.IO.Directory.GetCurrentDirectory();
        }

        private void openBtn_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                this.openCfgTextBox.Text = openFileDialog.FileName;
                WizardHelper.Instance.ProvisioningWizardHelper[WizardHelper.CONFIG_FILE_NAME] = openFileDialog.FileName;
                this.settingsGrp.Enabled = true;

                // Try to read it as a config file
                Configuration config = ConfigurationManager.OpenMappedExeConfiguration
                        (new ExeConfigurationFileMap()
                        {
                            ExeConfigFilename = WizardHelper.Instance.ProvisioningWizardHelper[WizardHelper.CONFIG_FILE_NAME]
                        }, ConfigurationUserLevel.None);

                WizardHelper.Instance.SyncConfigSection = (SyncConfigurationSection)((SyncConfigurationSection)config.GetSection("SyncConfiguration")).Clone();

                scopesBox.Items.Clear();
                dbsBox.Items.Clear();

                // Populate the names of sync scopes
                foreach (SyncScopeConfigElement scope in WizardHelper.Instance.SyncConfigSection.SyncScopes)
                {
                    this.scopesBox.Items.Add(scope.Name);
                }

                // Populate target dbs name
                foreach (TargetDatabaseConfigElement db in WizardHelper.Instance.SyncConfigSection.Databases)
                {
                    this.dbsBox.Items.Add(db.Name);
                }

                this.scopesBox.SelectedIndex = (this.scopesBox.Items.Count == 1) ? 0 : -1;
                this.dbsBox.SelectedIndex = (this.dbsBox.Items.Count == 1) ? 0 : -1;
            }
        }

        #region IWizardPage Members

        public void OnFocus()
        {
            this.openCfgTextBox.Focus();

        }

        public bool OnMovingNext()
        {
            // Check to see that file has been selected
            if (!WizardHelper.Instance.ProvisioningWizardHelper.ContainsKey(WizardHelper.CONFIG_FILE_NAME))
            {
                MessageBox.Show("Please specify a SyncConfig file name.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // Check that atleast on Scope has been selected
            if (this.scopesBox.SelectedIndex == -1)
            {
                MessageBox.Show("Please specify a SyncScope before proceeding.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // Check that atleast one target database has been selected
            if (this.dbsBox.SelectedIndex == -1)
            {
                MessageBox.Show("Please specify a Target Database before proceeding.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            WizardHelper.Instance.ProvisioningWizardHelper[WizardHelper.SELECTED_DB_NAME] = this.dbsBox.SelectedItem.ToString();
            WizardHelper.Instance.ProvisioningWizardHelper[WizardHelper.SELECTED_CONFIG_NAME] = this.scopesBox.SelectedItem.ToString();

            string provisionMode = "provision";
            if (deprovOption.Checked) provisionMode = "deprovision";
            if (deprovstoreOption.Checked) provisionMode = "deprovisionstore";
            if (provisionScriptOption.Checked) provisionMode = "provisionscript";
            if (deprovScriptOption.Checked) provisionMode = "deprovisionscript";
            if (deprovstoreScriptOption.Checked) provisionMode = "deprovisionstorescript";
            WizardHelper.Instance.ProvisioningWizardHelper[WizardHelper.SELECTED_PROV_MODE] = provisionMode;

            WizardHelper.Instance.CodeGenWizardHelper[WizardHelper.CODEGEN_OUTDIRECTORY] = this.outputDirTxtBox.Text;


            return true;
        }

        public void OnFinish()
        {
            // no-op
        }
        public bool OnMovingBack()
        {
             return false;
            // no-op
        }
        #endregion

        private void button1_Click(object sender, EventArgs e)
        {
            if (this.folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                this.outputDirTxtBox.Text = this.folderBrowserDialog1.SelectedPath;
            }
        }
    }
}

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
using System.Net;
using System.Xml.Linq;

namespace SyncSvcUtilUI.CodegenWizardPages
{
    public partial class Step1_PickConfigOrCSDLModelPage : UserControl, IWizardPage
    {
        public Step1_PickConfigOrCSDLModelPage()
        {
            InitializeComponent();

            // Set dialog settings
            openFileDialog.Filter = "Sync Config|*.config|All Files(*.*)|*.*";
            openFileDialog.Title = "Oepn SyncConfiguration file";
            openFileDialog.CheckFileExists = true;
            openFileDialog.DefaultExt = ".config";
            openFileDialog.InitialDirectory = System.IO.Directory.GetCurrentDirectory();
        }

        private void openBtn_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                this.openCfgTextBox.Text = openFileDialog.FileName;
                WizardHelper.Instance.CodeGenWizardHelper[WizardHelper.CONFIG_FILE_NAME] = openFileDialog.FileName ;

                // Try to read it as a config file
                Configuration config = ConfigurationManager.OpenMappedExeConfiguration
                        (new ExeConfigurationFileMap()
                        {
                            ExeConfigFilename = WizardHelper.Instance.CodeGenWizardHelper[WizardHelper.CONFIG_FILE_NAME]
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
            if (this.useConfigOption.Checked)
            {
                // Check to see that file has been selected
                if (!WizardHelper.Instance.CodeGenWizardHelper.ContainsKey(WizardHelper.CONFIG_FILE_NAME))
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

                WizardHelper.Instance.CodeGenWizardHelper[WizardHelper.SELECTED_DB_NAME] = this.dbsBox.SelectedItem.ToString();
                WizardHelper.Instance.CodeGenWizardHelper[WizardHelper.SELECTED_CONFIG_NAME] = this.scopesBox.SelectedItem.ToString();
                WizardHelper.Instance.CodeGenWizardHelper[WizardHelper.SELECTED_CODEGEN_SOURCE] = WizardHelper.CONFIG_FILE_CODEGEN_SOURCE;
            }
            else
            {
                // Check that atleast on Scope has been selected
                if (this.csdlScopesBox.SelectedIndex == -1)
                {
                    MessageBox.Show("Please specify a SyncScope before proceeding.", "Error", MessageBoxButtons.OK);
                    return false;
                }

                WizardHelper.Instance.CodeGenWizardHelper[WizardHelper.SELECTED_CONFIG_NAME] = this.csdlScopesBox.SelectedItem.ToString();
                WizardHelper.Instance.CodeGenWizardHelper[WizardHelper.SELECTED_CODEGEN_SOURCE] = WizardHelper.CSDL_CODEGEN_SOURCE;
                WizardHelper.Instance.CodeGenWizardHelper[WizardHelper.CSDL_CODEGEN_URL] =
                    string.Format(WizardHelper.CSDL_CODEGEN_URL_FORMAT, this.csdlUrl.Text, this.csdlScopesBox.SelectedItem.ToString());
            }

            return true;
        }

        public bool OnMovingBack()
        {
             return true;
            // no-op
        }
        public void OnFinish()
        {
            // no-op
        }

        #endregion

        private void browseBtn_Click(object sender, EventArgs e)
        {
            this.errorLbl.Visible = false;
            this.infoLbl.Visible = false;
            this.csdlScopesBox.Items.Clear();

            try
            {
                WebClient wc = new WebClient();
                wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_DownloadStringCompleted);
                wc.DownloadStringAsync(new Uri(this.csdlUrl.Text));
                this.openBtn.Enabled = false;
                this.csdlUrl.Enabled = false;
                this.infoLbl.Visible = true;
            }
            catch (Exception)
            {
                this.errorLbl.Visible = true;
            }
        }

        void wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Cancelled || e.Error != null)
            {
                this.errorLbl.Visible = true;
            }
            else
            {
                this.infoLbl.Visible = false;
            }

            this.openBtn.Enabled = true;
            this.csdlUrl.Enabled = true;
            XNamespace ns = "http://www.w3.org/2007/app";
            // Success
            XDocument document = XDocument.Parse(e.Result);
            XElement workspaceElem = document.Root.Element(ns + "workspace");

            if (workspaceElem != null)
            {
                foreach (XElement elem in workspaceElem.Elements(ns + "collection"))
                {
                    if (elem.Attribute("href") != null)
                    {
                        this.csdlScopesBox.Items.Add(elem.Attribute("href").Value);
                    }
                }
            }

            if (csdlScopesBox.Items.Count == 1)
            {
                csdlScopesBox.SelectedIndex = 0;
            }
        }

        private void csdlUrl_TextChanged(object sender, EventArgs e)
        {
            this.browseBtn.Enabled = this.csdlUrl.Text.Length > "http://".Length;
        }

        private void useConfigOption_CheckedChanged(object sender, EventArgs e)
        {
            this.configPanel.Enabled = this.useConfigOption.Checked;
            this.csdlPanel.Enabled = this.csdlOption.Checked;
        }

        private void csdlOption_CheckedChanged(object sender, EventArgs e)
        {
            this.configPanel.Enabled = this.useConfigOption.Checked;
            this.csdlPanel.Enabled = this.csdlOption.Checked;
        }
    }
}

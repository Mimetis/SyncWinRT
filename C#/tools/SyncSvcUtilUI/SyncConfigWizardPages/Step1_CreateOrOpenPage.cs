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
using System.IO;
using System.Configuration;
using Microsoft.Synchronization.ClientServices.Configuration;

namespace SyncSvcUtilUI.SyncConfigWizardPages
{
    public partial class Step1_CreateOrOpenPage : UserControl, IWizardPage
    {
        public Step1_CreateOrOpenPage()
        {
            InitializeComponent();

            createPanel.Enabled = createNewRadioBtn.Checked;
            openPanel.Enabled = editRadioBtn.Checked;

            // Set Open and Save FileDialog settings
            saveFileDialog.Filter = "Sync Config|*.config";
            saveFileDialog.CheckPathExists = true;
            saveFileDialog.DefaultExt = ".config";
            saveFileDialog.AddExtension = true;
            saveFileDialog.Title = "New SyncConfiguration file";
            saveFileDialog.InitialDirectory = System.IO.Directory.GetCurrentDirectory();

            openFileDialog.Filter = "Sync Config|*.config|All Files(*.*)|*.*";
            openFileDialog.Title = "Oepn SyncConfiguration file";
            openFileDialog.CheckFileExists = true;
            openFileDialog.DefaultExt = ".config";
            openFileDialog.InitialDirectory = System.IO.Directory.GetCurrentDirectory();
        }

        private void createNewRadioBtn_CheckedChanged(object sender, EventArgs e)
        {
            createPanel.Enabled = createNewRadioBtn.Checked;
        }

        private void editRadioBtn_CheckedChanged(object sender, EventArgs e)
        {
            openPanel.Enabled = editRadioBtn.Checked;
        }

        private void createBtn_Click(object sender, EventArgs e)
        {
            if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                this.newCfgTxtBox.Text = saveFileDialog.FileName;
                WizardHelper.Instance.ConfigFileWizardHelper[WizardHelper.CONFIG_FILE_NAME] =saveFileDialog.FileName ;
            }
        }

        private void openBtn_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                this.openCfgTextBox.Text = openFileDialog.FileName;
                WizardHelper.Instance.ConfigFileWizardHelper[WizardHelper.CONFIG_FILE_NAME] =  openFileDialog.FileName;
            }
        }

        #region IWizardPage Members

        public bool OnMovingNext()
        {
            if (!WizardHelper.Instance.ConfigFileWizardHelper.ContainsKey(WizardHelper.CONFIG_FILE_NAME))
            {
                MessageBox.Show("Please specify a config file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // Try to read the config file
            if (File.Exists(WizardHelper.Instance.ConfigFileWizardHelper[WizardHelper.CONFIG_FILE_NAME]))
            {
                // Try to read it as a config file
                Configuration config = ConfigurationManager.OpenMappedExeConfiguration
                        (new ExeConfigurationFileMap()
                        {
                            ExeConfigFilename = WizardHelper.Instance.ConfigFileWizardHelper[WizardHelper.CONFIG_FILE_NAME]
                        }, ConfigurationUserLevel.None);

                WizardHelper.Instance.SyncConfigSection = (SyncConfigurationSection)((SyncConfigurationSection)config.GetSection("SyncConfiguration")).Clone();
            }

            if (WizardHelper.Instance.SyncConfigSection == null)
            {
                // Create a new one if no config is found
                WizardHelper.Instance.SyncConfigSection = new SyncConfigurationSection();
            }

            return true;
        }

        public void OnFinish()
        {
            // Noop for Finish
        }

        public void OnFocus()
        {
            // Noop
        }

        public bool OnMovingBack()
        {
             return true;
            // no-op
        }
        #endregion
    }
}

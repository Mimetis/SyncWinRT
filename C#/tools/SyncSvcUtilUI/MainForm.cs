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
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SyncSvcUtilUI
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void exitBtn_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void genOrEdirSyncCfgLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Launch the wizard for Sync Config generation/creator
            SyncMasterWizard wizard = new SyncMasterWizard();
            
            // Add pages
            List<UserControl> pages = new List<UserControl>();
            pages.Add(new SyncConfigWizardPages.Step1_CreateOrOpenPage());
            pages.Add(new SyncConfigWizardPages.Step2_AddDatabaseInfoPage());
            pages.Add(new SyncConfigWizardPages.Step3_AddSyncScopePage());
            pages.Add(new SyncConfigWizardPages.Step4_AddSyncTablesPage());
            pages.Add(new SyncConfigWizardPages.Step5_SummaryAndFinishPage());
            
            // Set wizard pages
            wizard.SetPages(pages.ToArray());

            // Show wizard
            wizard.ShowDialog(this);
        }

        private void provLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Launch the wizard for Provision/Deprovision link
            SyncMasterWizard wizard = new SyncMasterWizard();

            // Add pages
            List<UserControl> pages = new List<UserControl>();
            pages.Add(new SyncProvisionWizardPages.Setp1_GetAndOpenConfigFile());
            pages.Add(new SyncProvisionWizardPages.Step2_SummaryOfProvDeProvPage());

            // Set wizard pages
            wizard.SetPages(pages.ToArray());

            // Show wizard
            wizard.ShowDialog(this);
        }

        private void codeGenLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Launch the wizard for Provision/Deprovision link
            SyncMasterWizard wizard = new SyncMasterWizard();

            // Add pages
            List<UserControl> pages = new List<UserControl>();
            pages.Add(new CodegenWizardPages.Step1_PickConfigOrCSDLModelPage());
            pages.Add(new CodegenWizardPages.Step2_SelectCodeGenPrams());
            pages.Add(new CodegenWizardPages.Step3_SummaryOfCodegenPage());

            // Set wizard pages
            wizard.SetPages(pages.ToArray());

            // Show wizard
            wizard.ShowDialog(this);
        }
    }
}

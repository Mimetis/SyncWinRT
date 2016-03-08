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
using System.Diagnostics;
using Microsoft.Synchronization.ClientServices;

namespace SyncSvcUtilUI.SyncProvisionWizardPages
{
    public partial class Step2_SummaryOfProvDeProvPage : UserControl, IWizardPage
    {
        public Step2_SummaryOfProvDeProvPage()
        {
            InitializeComponent();
        }

        #region IWizardPage Members

        public void OnFocus()
        {

            // Display the contents of the current SyncScope for review
            this.displayBox.Text = "Running SyncSvcUtil command...\n";
            var tArgs = new List<String>();

            tArgs.Add("/scopeconfig:" +  WizardHelper.Instance.ProvisioningWizardHelper[WizardHelper.CONFIG_FILE_NAME]);
            tArgs.Add("/mode:" + WizardHelper.Instance.ProvisioningWizardHelper[WizardHelper.SELECTED_PROV_MODE]);
            tArgs.Add("/scopename:" + WizardHelper.Instance.ProvisioningWizardHelper[WizardHelper.SELECTED_CONFIG_NAME]);

            if (WizardHelper.Instance.CodeGenWizardHelper.ContainsKey(WizardHelper.CODEGEN_OUTDIRECTORY))
            {
                tArgs.Add("/directory:" + WizardHelper.Instance.CodeGenWizardHelper[WizardHelper.CODEGEN_OUTDIRECTORY]);
            }

            try
            {

                this.displayBox.Text += "Command line :" + Environment.NewLine;
                this.displayBox.Text += " --------------------------------------------- " + Environment.NewLine;
                this.displayBox.Text += tArgs.Aggregate("syncsvcutil.exe ", (current, tArg) => current + tArg + " ") + Environment.NewLine;
                this.displayBox.Text += " --------------------------------------------- " + Environment.NewLine;
                this.displayBox.Text += Environment.NewLine;

                SyncSvcUtil.LogOccured += SyncSvcUtilOnLogOccured;
                SyncSvcUtil.Main(tArgs.ToArray());


            }
            catch (Exception ex)
            {

                this.displayBox.Text += ex.Message;
            }

       


            //this.displayBox.Text += WizardHelper.ExecuteProcessAndReturnLog(
            //    string.Format(WizardHelper.PROV_PARAM_FORMAT,
            //                  "\"" + WizardHelper.Instance.ProvisioningWizardHelper[WizardHelper.CONFIG_FILE_NAME] + "\"",
            //                  WizardHelper.Instance.ProvisioningWizardHelper[WizardHelper.SELECTED_PROV_MODE],
            //                  WizardHelper.Instance.ProvisioningWizardHelper[WizardHelper.SELECTED_CONFIG_NAME],
            //                  WizardHelper.Instance.ProvisioningWizardHelper[WizardHelper.SELECTED_DB_NAME]));
        }

        private void SyncSvcUtilOnLogOccured(object sender, StringEventArgs args)
        {
            this.displayBox.Text += args.LogMessage + System.Environment.NewLine;
        }

        public bool OnMovingNext()
        {
            SyncSvcUtil.LogOccured -= SyncSvcUtilOnLogOccured;
            return false;
            // no-op
        }
        public bool OnMovingBack()
        {
            SyncSvcUtil.LogOccured -= SyncSvcUtilOnLogOccured;
            return true;
            // no-op
        }

        public void OnFinish()
        {
            SyncSvcUtil.LogOccured -= SyncSvcUtilOnLogOccured;
            // no-op
        }

        #endregion
    }
}

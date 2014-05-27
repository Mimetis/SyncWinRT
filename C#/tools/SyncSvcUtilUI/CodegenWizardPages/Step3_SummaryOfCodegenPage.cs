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

namespace SyncSvcUtilUI.CodegenWizardPages
{
    public partial class Step3_SummaryOfCodegenPage : UserControl, IWizardPage
    {
        public Step3_SummaryOfCodegenPage()
        {
            InitializeComponent();
        }

        #region IWizardPage Members

        public void OnFocus()
        {
            //string sourceSpecificParams = null;
            //string commonParams = null;

            SyncSvcUtil.LogOccured += SyncSvcUtilOnLogOccured;

            var tArgs = new List<String>();


            if (WizardHelper.Instance.CodeGenWizardHelper[WizardHelper.SELECTED_CODEGEN_SOURCE] ==
                WizardHelper.CONFIG_FILE_CODEGEN_SOURCE)
            {
                // Display the contents of the current SyncScope for review
                this.displayBox.Text = "Running SyncSvcUtil command...\n";

                tArgs.Add("/scopeconfig:" + WizardHelper.Instance.CodeGenWizardHelper[WizardHelper.CONFIG_FILE_NAME]);
                tArgs.Add("/scopename:" + WizardHelper.Instance.CodeGenWizardHelper[WizardHelper.SELECTED_CONFIG_NAME]);
                tArgs.Add("/database:" + WizardHelper.Instance.CodeGenWizardHelper[WizardHelper.SELECTED_DB_NAME]);

                //sourceSpecificParams = string.Format(WizardHelper.CONFIG_CODEGEN_PARAM_FORMAT,
                //                  WizardHelper.Instance.CodeGenWizardHelper[WizardHelper.CONFIG_FILE_NAME],
                //                  WizardHelper.Instance.CodeGenWizardHelper[WizardHelper.SELECTED_CONFIG_NAME],
                //                  WizardHelper.Instance.CodeGenWizardHelper[WizardHelper.SELECTED_DB_NAME]);

            }
            //else
            //{
            //    sourceSpecificParams = string.Format(WizardHelper.CSDL_CODEGEN_PARAM_FORMAT,
            //                      WizardHelper.Instance.CodeGenWizardHelper[WizardHelper.CSDL_CODEGEN_URL],
            //                      WizardHelper.Instance.CodeGenWizardHelper[WizardHelper.SELECTED_CONFIG_NAME]);
            //}

            tArgs.Add("/language:" + WizardHelper.Instance.CodeGenWizardHelper[WizardHelper.CODEGEN_LANGUAGE]);
            tArgs.Add("/namespace:" + WizardHelper.Instance.CodeGenWizardHelper[WizardHelper.CODEGEN_NAMESPACE]);
            tArgs.Add("/mode:codegen");
            tArgs.Add("/target:" + WizardHelper.Instance.CodeGenWizardHelper[WizardHelper.CODEGEN_TARGET]);
            tArgs.Add("/directory:" + WizardHelper.Instance.CodeGenWizardHelper[WizardHelper.CODEGEN_OUTDIRECTORY]);
          

            //commonParams = string.Format(WizardHelper.CODEGEN_COMMON_PARAMS_FORMAT,
            //                  WizardHelper.Instance.CodeGenWizardHelper[WizardHelper.CODEGEN_LANGUAGE],
            //                  WizardHelper.Instance.CodeGenWizardHelper[WizardHelper.CODEGEN_NAMESPACE],                              
            //                  WizardHelper.Instance.CodeGenWizardHelper[WizardHelper.CODEGEN_TARGET],
            //                  WizardHelper.Instance.CodeGenWizardHelper[WizardHelper.CODEGEN_OUTDIRECTORY]);

            if (!string.IsNullOrEmpty(WizardHelper.Instance.CodeGenWizardHelper[WizardHelper.CODEGEN_OUTPREFIX]))
            {
                tArgs.Add("/outprefix:" + WizardHelper.Instance.CodeGenWizardHelper[WizardHelper.CODEGEN_OUTPREFIX]);
                
                //commonParams = string.Format(WizardHelper.CODEGEN_OUTPREFIX_PARAM_FORMAT,
                //    commonParams,
                //    WizardHelper.Instance.CodeGenWizardHelper[WizardHelper.CODEGEN_OUTPREFIX]);
            }

            this.displayBox.Text += "Command line :" + Environment.NewLine;
            this.displayBox.Text += " --------------------------------------------- " + Environment.NewLine;
            this.displayBox.Text += tArgs.Aggregate("syncsvcutil.exe ", (current, tArg) => current + tArg + " ") + Environment.NewLine;
            this.displayBox.Text += " --------------------------------------------- " + Environment.NewLine;
            this.displayBox.Text += Environment.NewLine;

 
             SyncSvcUtil.Main(tArgs.ToArray());

           // this.displayBox.Text += WizardHelper.ExecuteProcessAndReturnLog(sourceSpecificParams + commonParams);

        }

        private void SyncSvcUtilOnLogOccured(object sender, StringEventArgs args)
        {
            this.displayBox.Text += args.LogMessage + System.Environment.NewLine;
        }

        public bool OnMovingBack()
        {
            SyncSvcUtil.LogOccured -= SyncSvcUtilOnLogOccured;
            return true;
            // no-op
        }
        public bool OnMovingNext()
        {
            SyncSvcUtil.LogOccured -= SyncSvcUtilOnLogOccured;
            return false;
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

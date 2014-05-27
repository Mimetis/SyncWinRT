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
    public partial class Step5_SummaryAndFinishPage : UserControl, IWizardPage
    {
        public Step5_SummaryAndFinishPage()
        {
            InitializeComponent();
        }

        #region IWizardPage Members

        public void OnFocus()
        {
            // Display the contents of the current SyncScope for review
            this.displayBox.Text = WizardHelper.Instance.SyncConfigSection.ToString();
        }

        public bool OnMovingNext()
        {
            return false;
            // no-op
        }

        public bool OnMovingBack()
        {
             return true;
        }
        public void OnFinish()
        {
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration
                                    (new ExeConfigurationFileMap()
                                    {
                                        ExeConfigFilename = WizardHelper.Instance.ConfigFileWizardHelper[WizardHelper.CONFIG_FILE_NAME]
                                    }, ConfigurationUserLevel.None);
            config.Sections.Clear();
            if (File.Exists(WizardHelper.Instance.ConfigFileWizardHelper[WizardHelper.CONFIG_FILE_NAME]))
            {
                config.Sections.Remove("SyncConfiguration");
            }
            config.Sections.Add("SyncConfiguration", WizardHelper.Instance.SyncConfigSection);
            config.Save(ConfigurationSaveMode.Full);
        }

        #endregion
    }
}

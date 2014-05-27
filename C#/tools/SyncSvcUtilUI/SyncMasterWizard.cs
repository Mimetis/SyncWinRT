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
    public partial class SyncMasterWizard : Form
    {
        const string FinishText = "&Finish";
        const string NextText = "&Next";

        // Represents the pages that makes this wizard
        UserControl[] wizardPages;

        // Represents the current page the wizard is on.
        int curPage = -1;

        public SyncMasterWizard()
        {
            InitializeComponent();
            wizardPages = new UserControl[0];
        }

        public object WizardHelper { get; set; }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this.MoveForward();
        }

        private void exitBtn_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void backBtn_Click(object sender, EventArgs e)
        {
            this.MoveBack();
        }

        private void nextBtn_Click(object sender, EventArgs e)
        {
            this.MoveForward();
        }

        private void MoveForward()
        {
            if (this.nextBtn.Text.Equals(NextText))
            {
                if (curPage >= 0)
                {
                    // Invoke the OnMovingNext callback
                    if (!((IWizardPage)this.wizardPages[curPage]).OnMovingNext())
                    {
                        return;
                    }
                }

                curPage++;

                if (this.wizardPages.Length > curPage)
                {
                    this.ShowWizardPageAtIndex();
                }
            }
            else
            {
                try
                {
                    // Call the finish callback
                    ((IWizardPage)this.wizardPages[curPage]).OnFinish();
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception exp)
                {
                    // If exception then show it to user.
                    MessageBox.Show(exp.Message, "Error in finishing wizard workflow.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            this.SetButtonLabels();
        }

        private void ShowWizardPageAtIndex()
        {
            // Remove and add current page to the wizard
            this.wizardPagePanel.Controls.Clear();
            this.wizardPagePanel.Controls.Add(this.wizardPages[curPage]);
            this.wizardPagePanel.Controls[0].Dock = DockStyle.Fill;
            ((IWizardPage)this.wizardPages[curPage]).OnFocus();
        }

        private void MoveBack()
        {
            // Invoke the OnMovingNext callback
            if (!((IWizardPage)this.wizardPages[curPage]).OnMovingBack())
            {
                return;
            }

            if (curPage != 0)
            {
                curPage--;
            }

            if (curPage >= 0)
            {
 
                this.ShowWizardPageAtIndex();
            }

            this.SetButtonLabels();
        }

        private void SetButtonLabels()
        {
            this.backBtn.Enabled = this.curPage > 0;
            this.nextBtn.Text = (this.curPage >= this.wizardPages.Length - 1) ? SyncMasterWizard.FinishText : SyncMasterWizard.NextText;
        }

        internal void SetPages(UserControl[] pages)
        {
            if (pages == null)
            {
                throw new ArgumentNullException("pages");
            }
            this.wizardPages = pages;
        }
    }
}

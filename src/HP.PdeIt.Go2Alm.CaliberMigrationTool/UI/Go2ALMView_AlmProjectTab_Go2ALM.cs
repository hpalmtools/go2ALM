// go2ALM: migrate from various tools to QC/ALM
// Copyright (C) 2012 Hewlett Packard Company
// Authors: 
//      Gustavo Mejia
//        from Hewlett Packard Company
//      
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.

namespace HP.PdeIt.Go2Alm.CaliberMigrationTool.UI
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Windows.Forms;

    public partial class Go2ALMView
    {
        private Thread CaliberLoginThread;
        private Thread LoadCaliberBaselinesThread;
        private Thread LoadCaliberRequirementTypesThread;
        private Thread AlmLoginThread;
        private Thread LoadAlmProjectsThread;
        private Thread LoadAlmProjectListValuesThread;
        private Thread MigrateRequirementsThread;
        private Thread LoadAlmReleasesThread;
        private Thread LoadAlmFoldersThread;
        private Thread LoadAlmCustomFieldsThread;
        private Thread ListFieldsThread;

        private void EnableMigrateRequirementsButton()
        {
            if (CaliberProjectList.Enabled     && CaliberProjectList.SelectedIndex > -1
                && CaliberBaselineList.Enabled && CaliberBaselineList.SelectedIndex > -1
                && CaliberReqTypesCheckedList.Enabled && CaliberReqTypesCheckedList.CheckedIndices.Count > 0
                && AlmDomainList.Enabled       && AlmDomainList.SelectedIndex > -1
                && AlmProjectList.Enabled      && AlmProjectList.SelectedIndex > -1
                && AlmFolderList.Enabled       && AlmFolderList.SelectedNode != null
                && AlmReleaseList.Enabled      && AlmReleaseList.SelectedIndex > -1
                && AlmRequiredCustomFieldsValidation()
                && (MigrateTracesCheckBox.Checked == false
                                               || (!string.IsNullOrEmpty(ISVNUser.Text.Trim())
                                                    && !string.IsNullOrEmpty(ISVNPassword.Text.Trim())
                                                    && !string.IsNullOrEmpty(ISVNRepository.Text.Trim())))
            ) {
                MigrateRequirementsButton.Enabled = true;
                ListFieldsButton.Enabled = true;
            }
            else
            {
                MigrateRequirementsButton.Enabled = false;
                ListFieldsButton.Enabled = false;
            }
        }

        private void MigrateRequirementsButton_Click(object sender, EventArgs e)
        {
            if (CaliberProjectList.Enabled     && CaliberProjectList.SelectedIndex > -1
                && CaliberBaselineList.Enabled && CaliberBaselineList.SelectedIndex > -1
                && CaliberReqTypesCheckedList.Enabled && CaliberReqTypesCheckedList.CheckedIndices.Count > 0
                && AlmDomainList.Enabled       && AlmDomainList.SelectedIndex > -1
                && AlmProjectList.Enabled      && AlmProjectList.SelectedIndex > -1
                && AlmFolderList.Enabled       && AlmFolderList.SelectedNode != null
                && AlmReleaseList.Enabled      && AlmReleaseList.SelectedIndex > -1
                && AlmRequiredCustomFieldsValidation()
                && (MigrateTracesCheckBox.Checked == false
                                               || (!string.IsNullOrEmpty(ISVNUser.Text.Trim())
                                                    && !string.IsNullOrEmpty(ISVNPassword.Text.Trim())
                                                    && !string.IsNullOrEmpty(ISVNRepository.Text.Trim())))
            ) {
                MigrateRequirementsThread = new Thread(new ThreadStart(MigrateRequirementsThreadJob));
                MigrateRequirementsThread.Name = "MigrateRequirements";
                MigrateRequirementsThread.IsBackground = true;
                MigrateRequirementsThread.Start();
            }
            else
            {
                EnableMigrateRequirementsButton();
                MessageBox.Show("Please make sure all data is selected before trying to migrate the requirements.",
                    "Make sure all data is selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ListFieldsButton_Click(object sender, EventArgs e)
        {
            if (CaliberProjectList.Enabled && CaliberProjectList.SelectedIndex > -1
                && CaliberBaselineList.Enabled && CaliberBaselineList.SelectedIndex > -1
                && CaliberReqTypesCheckedList.Enabled && CaliberReqTypesCheckedList.CheckedIndices.Count > 0
                && AlmDomainList.Enabled && AlmDomainList.SelectedIndex > -1
                && AlmProjectList.Enabled && AlmProjectList.SelectedIndex > -1
                && AlmFolderList.Enabled && AlmFolderList.SelectedNode != null
                && AlmReleaseList.Enabled && AlmReleaseList.SelectedIndex > -1
                && AlmRequiredCustomFieldsValidation()
                && (MigrateTracesCheckBox.Checked == false
                                               || (!string.IsNullOrEmpty(ISVNUser.Text.Trim())
                                                    && !string.IsNullOrEmpty(ISVNPassword.Text.Trim())
                                                    && !string.IsNullOrEmpty(ISVNRepository.Text.Trim())))
            )
            {
                ListFieldsThread = new Thread(new ThreadStart(ListFieldsThreadJob));
                ListFieldsThread.Name = "ListFields";
                ListFieldsThread.IsBackground = true;
                ListFieldsThread.Start();
            }
            else
            {
                EnableMigrateRequirementsButton();
                MessageBox.Show("Please make sure all data is selected before trying to List the fields.",
                    "Make sure all data is selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ListFieldsThreadJob()
        {
            MigrateRequirementsSetup();
            Go2ALMBusinessLogic.ListFields();
            MigrateRequirementsSuccessful();
        }

        /// <summary>
        ///  Displays a confirmation message, and in case the user confirms starts migrating the requirements.
        /// </summary>
        private void MigrateRequirementsThreadJob()
        {
                bool confirmMigrateRequirements = MessageBox.Show("Are you sure you want to migrate the Caliber Requirements List?"
                + Environment.NewLine + "This will take a few minutes. No other actions can be done while migrating."
                + Environment.NewLine + "A message will be displayed when migration is complete, please be patient... ",
                "Please confirm the migration", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
                if (confirmMigrateRequirements)
                {
                    MigrateRequirements();
                    MessageBox.Show("Requirements migration completed, please check the Log for detailed status.",
                        "Requirements migration completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
        }

        /// <summary>
        ///  Loads the selected Caliber and ALM projects and loads the requirements that will be migrated.
        /// </summary>
        private void MigrateRequirements()
        {
            //if (ValidateCaliberProjectAndBaseline())
            //{
                MigrateRequirementsSetup();
                Go2ALMBusinessLogic.MigrateRequirements();
                MigrateRequirementsSuccessful();
            //}
        }

        private void MigrateRequirementsSetup()
        {
            MigrateRequirementsButton.Invoke(new MethodInvoker(delegate
            {
                StatusProgressBar.MarqueeAnimationSpeed += 30;
                MigrateRequirementsButton.Enabled = false;
                CaliberLogOffButton.Enabled = false;
                AlmLogOffButton.Enabled = false;
                CaliberProjectGroup.Enabled = false;
                AlmProjectGroup.Enabled = false;
                SetupTracesGroup.Enabled = false;

                Go2ALMBusinessLogic.CaliberServerName = CaliberServerList.SelectedItem.ToString();
                Go2ALMBusinessLogic.CaliberUser = CaliberUser.Text;
                Go2ALMBusinessLogic.CaliberPassword = CaliberPassword.Text;

                Go2ALMBusinessLogic.AlmServerName = AlmServerList.SelectedItem.ToString();
                Go2ALMBusinessLogic.AlmUser = AlmUser.Text; ;
                Go2ALMBusinessLogic.AlmPassword = AlmPassword.Text;

                Go2ALMBusinessLogic.MigrateTraces = MigrateTracesCheckBox.Checked;
                Go2ALMBusinessLogic.ISVNUser = ISVNUser.Text.Trim();
                Go2ALMBusinessLogic.ISVNPassword = ISVNPassword.Text.Trim();
                Go2ALMBusinessLogic.ISVNRepository = ISVNRepository.Text.Trim();

                Go2ALMBusinessLogic.CaliberProjectName = CaliberProjectList.SelectedItem.ToString();
                Go2ALMBusinessLogic.CaliberBaselineName = CaliberBaselineList.SelectedItem.ToString();
                string[] SelectedCaliberRequirementTypes = new string[CaliberReqTypesCheckedList.CheckedItems.Count];
                for (int i = 0; i < CaliberReqTypesCheckedList.CheckedItems.Count; i++)
                {
                    SelectedCaliberRequirementTypes[i] = CaliberReqTypesCheckedList.CheckedItems[i].ToString();
                }
                Go2ALMBusinessLogic.CaliberRequirementTypes = SelectedCaliberRequirementTypes;

                Go2ALMBusinessLogic.AlmDomainName = AlmDomainList.SelectedItem.ToString();
                Go2ALMBusinessLogic.AlmProjectName = AlmProjectList.SelectedItem.ToString();
                Go2ALMBusinessLogic.AlmReleaseName = AlmReleaseList.SelectedItem.ToString();
                Go2ALMBusinessLogic.AlmFolderPath = AlmFolderList.SelectedNode.FullPath;
                Go2ALMBusinessLogic.AlmCustomFieldsValues = GetAlmCustomFieldsValues();

            }));
        }

        private void MigrateRequirementsSuccessful()
        {
            MigrateRequirementsButton.Invoke(new MethodInvoker(delegate
            {
                MigrateRequirementsButton.Enabled = true;
                StatusProgressBar.MarqueeAnimationSpeed -= 30;
                CaliberLogOffButton.Enabled = true;
                AlmLogOffButton.Enabled = true;
                CaliberProjectGroup.Enabled = true;
                AlmProjectGroup.Enabled = true;
                SetupTracesGroup.Enabled = true;
            }));
        }

        private bool ValidateCaliberProjectAndBaseline()
        {
            MigrateRequirementsButton.Invoke(new MethodInvoker(delegate {
                LogMessage("    Loading selected CaliberRM project and baseline... ");
                SelectedCaliberProjectName = CaliberProjectList.SelectedItem.ToString();
                SelectedCaliberBaselineName = CaliberBaselineList.SelectedItem.ToString();
            }));
            if (Go2ALMBusinessLogic.GetCaliberBaseline(SelectedCaliberProjectName, SelectedCaliberBaselineName) == null)
            {
                MigrateRequirementsButton.Invoke(new MethodInvoker(delegate {
                    MigrateRequirementsButton.Enabled = true;
                    LogMessage("    ERROR: Failed loading CaliberRM project and baseline. Not found.");
                }));
                return false;
            }
            MigrateRequirementsButton.Invoke(new MethodInvoker(delegate {
                LogMessage("    Finished loading CaliberRM project and baseline.");
            }));
            return true;
        }

    }
}

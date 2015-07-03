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
        private void CaliberReqTypeList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (CaliberReqTypesCheckedList.CheckedIndices.Count > 0)
            {
                EnableMigrateRequirementsButton();
            }
        }

        private void LoadCaliberReqTypesThreadJob()
        {
            try
            {
                LoadCaliberReqTypesSetup();
                List<string> caliberReqTypeNameList = Go2ALMBusinessLogic.GetCaliberReqTypeList(SelectedCaliberProjectName, SelectedCaliberBaselineName);
                LoadCaliberReqTypesSuccessful(caliberReqTypeNameList);
            }
            catch (Exception ex)
            {
                LoadCaliberReqTypesFailed(ex.Message);
            }
        }

        private void LoadCaliberReqTypesSetup()
        {
            CaliberProjectGroup.Invoke(new MethodInvoker(delegate
            {
                SelectedCaliberProjectName = CaliberProjectList.SelectedItem.ToString();
                LogMessage("Loading list of ReqTypes in the selected project (" + SelectedCaliberProjectName + ")... ");
                CaliberReqTypesCheckedList.Items.Clear();
                CaliberReqTypesCheckedList.Items.Add("Loading list, this may take a few");
                CaliberReqTypesCheckedList.Items.Add("minutes please be patient...");
                CaliberReqTypesCheckedList.Enabled = false;
                MigrateRequirementsButton.Enabled = false;
            }));
        }

        private void LoadCaliberReqTypesSuccessful(List<string> caliberReqTypeNameList)
        {
            CaliberProjectGroup.Invoke(new MethodInvoker(delegate
            {
                CaliberReqTypesCheckedList.Items.Clear();
                if (caliberReqTypeNameList != null && caliberReqTypeNameList.Count > 0 && MigrateReqByTypeRadioButton.Checked)
                {
                    foreach (string caliberReqTypeName in caliberReqTypeNameList)
                    {
                        CaliberReqTypesCheckedList.Items.Add(caliberReqTypeName, true);
                    }
                    LogMessage("Finished loading Caliber ReqTypes.");
                    CaliberReqTypesCheckedList.Enabled = true;
                }
                else
                {
                    CaliberReqTypesCheckedList.Items.Add("Nothing found, please select ", false);
                    CaliberReqTypesCheckedList.Items.Add("another Project/Baseline.", false);
                    CaliberReqTypesCheckedList.Enabled = false;
                }
                EnableMigrateRequirementsButton();
            }));
        }

        private void LoadCaliberReqTypesFailed(String ErrorMessage)
        {
            CaliberProjectGroup.Invoke(new MethodInvoker(delegate
            {
                LogMessage("FAILED loading Caliber RM ReqTypes.");
                LogMessage(ErrorMessage);
                CaliberReqTypesCheckedList.Items.Clear();
                CaliberReqTypesCheckedList.Focus();
                EnableMigrateRequirementsButton();
            }));
        }

        private void MigrateAllRequirementsRadioButton_CheckedChanged(object sender, EventArgs e)
        {

            MigrateReqByTypeRadioButton.Checked = !MigrateAllRequirementsRadioButton.Checked;
            if (MigrateAllRequirementsRadioButton.Checked)
            {
                CaliberReqTypesCheckedList.Enabled = false;
                CaliberReqTypesCheckedList.Items.Clear();
                CaliberReqTypesCheckedList.Items.Add("Migrate all requirements");
                CaliberReqTypesCheckedList.SetItemChecked(0, true);
                CaliberReqTypesCheckedList.Enabled = true;
            }
            EnableMigrateRequirementsButton();
        }

        private void MigrateReqByTypeRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            MigrateAllRequirementsRadioButton.Checked = !MigrateReqByTypeRadioButton.Checked;
            if (MigrateReqByTypeRadioButton.Checked)
            {
                try
                {
                    LoadCaliberRequirementTypesThread = new Thread(new ThreadStart(LoadCaliberReqTypesThreadJob));
                    LoadCaliberRequirementTypesThread.Name = "LoadCaliberRequirementTypes";
                    LoadCaliberRequirementTypesThread.IsBackground = true;
                    LoadCaliberRequirementTypesThread.Start();
                }
                catch (Exception ex)
                {
                    LogMessage("User Logged off from Caliber RM session. Load Requirement Types aborted. " + ex.Message);
                }
                EnableMigrateRequirementsButton();
            }
        }

        private void CaliberRequirementTypesCheckedList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (CaliberReqTypesCheckedList.CheckedIndices.Count > 0)
            {
                EnableMigrateRequirementsButton();
            }
        }
    }
}

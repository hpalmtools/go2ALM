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
        private void CaliberBaselineList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (CaliberBaselineList.SelectedIndex >= 0)
            {
                SelectedCaliberBaselineName = CaliberBaselineList.SelectedItem.ToString();
                EnableMigrateRequirementsButton();
                MigrateCheckBoxGroup.Enabled = true;
                RequirementTypesGroup.Enabled = true;
                MigrateAllRequirementsRadioButton.Checked = true;
            }
        }

        private void LoadCaliberBaselinesThreadJob()
        {
            try
            {
                LoadCaliberBaselinesSetup();
                List<string> caliberBaselineNameList = Go2ALMBusinessLogic.GetCaliberBaselineList(SelectedCaliberProjectName);
                LoadCaliberBaselinesSuccessful(caliberBaselineNameList);
            }
            catch (Exception ex)
            {
                LoadCaliberBaselinesFailed(ex.Message);
            }
        }

        private void LoadCaliberBaselinesSetup()
        {
            CaliberProjectGroup.Invoke(new MethodInvoker(delegate
            {
                SelectedCaliberProjectName = CaliberProjectList.SelectedItem.ToString();
                LogMessage("Loading list of baselines in the selected project (" + SelectedCaliberProjectName + ")... ");
                CaliberBaselineList.Items.Clear();
                CaliberBaselineList.Items.Add("Loading list of baselines in the selected project (" + SelectedCaliberProjectName + ")... ");
                CaliberBaselineList.Items.Add("This may take a few minutes please be patient... ");
                CaliberBaselineList.Enabled = false;
                MigrateRequirementsButton.Enabled = false;
            }));
        }

        private void LoadCaliberBaselinesSuccessful(List<string> caliberBaselineNameList)
        {
            CaliberProjectGroup.Invoke(new MethodInvoker(delegate
            {
                CaliberBaselineList.Items.Clear();
                if (caliberBaselineNameList != null && caliberBaselineNameList.Count > 0)
                {
                    CaliberBaselineList.Items.AddRange(caliberBaselineNameList.ToArray());
                    CaliberBaselineList.SelectedItem = caliberBaselineNameList[0];
                }
                LogMessage("Finished loading Caliber baselines.");
                CaliberBaselineList.Enabled = true;
                EnableMigrateRequirementsButton();
            }));
        }

        private void LoadCaliberBaselinesFailed(String ErrorMessage)
        {
            CaliberProjectGroup.Invoke(new MethodInvoker(delegate
            {
                LogMessage("FAILED loading Caliber RM Baselines.");
                LogMessage(ErrorMessage);
                CaliberBaselineList.Items.Clear();
                CaliberBaselineList.Items.Add("FAILED loading Caliber RM Baselines.");
                CaliberBaselineList.Items.Add(ErrorMessage);
                CaliberBaselineList.Focus();
                EnableMigrateRequirementsButton();
            }));
        }
    }
}

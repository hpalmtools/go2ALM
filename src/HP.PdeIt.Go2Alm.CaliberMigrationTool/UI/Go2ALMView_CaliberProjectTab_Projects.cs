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
        private void CaliberProjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (CaliberProjectList.SelectedIndex >= 0)
            {
                try
                {
                    LoadCaliberBaselinesThread = new Thread(new ThreadStart(LoadCaliberBaselinesThreadJob));
                    LoadCaliberBaselinesThread.Name = "LoadCaliberBaselines";
                    LoadCaliberBaselinesThread.IsBackground = true;
                    LoadCaliberBaselinesThread.Start();
                }
                catch (Exception ex)
                {
                    LogMessage("User Logged off from Caliber RM session. Load Baselines aborted. " + ex.Message);
                }
            }
        }

        private void LoadCaliberProjects()
        {
            try
            {
                LoadCaliberProjectsSetup();
                List<string> projectNameList = Go2ALMBusinessLogic.GetCaliberProjectList();
                LoadCaliberProjectsSuccessful(projectNameList);
            }
            catch (Exception ex)
            {
                LoadCaliberProjectsFailed(ex.Message);
            }
        }

        private void LoadCaliberProjectsSetup()
        {
            CaliberProjectGroup.Invoke(new MethodInvoker(delegate
            {
                LogMessage("Loading list of Caliber RM projects... ");
                LogMessage("This may take a few minutes please be patient... ");
                CaliberProjectList.Items.Clear();
                CaliberProjectList.Items.Add("Loading list of Caliber RM projects... ");
                CaliberProjectList.Items.Add("This may take a few minutes please be patient... ");
                //MessageBox.Show("Loading list of Caliber RM projects..." + Environment.NewLine
                //+ "This may take a few minutes please be patient..." + Environment.NewLine
                //+ "You can continue logging in to ALM and selecting the project in the mean time.",
                //"Please wait while Caliber projects are loaded", MessageBoxButtons.OK, MessageBoxIcon.Information);
                TabControl.SelectedTab = AlmLoginTab;
                AlmUser.Focus();
                AlmUser.SelectAll();
            }));
        }

        private void LoadCaliberProjectsSuccessful(List<string> projectNameList)
        {
            CaliberProjectGroup.Invoke(new MethodInvoker(delegate
            {
                CaliberProjectList.Items.Clear();
                CaliberProjectList.Items.AddRange(projectNameList.ToArray());
                CaliberProjectList.Focus();
                LogMessage("Finished loading Caliber RM projects.");
                //MessageBox.Show("Finished loading Caliber RM projects.",
                //"Finished loading Caliber RM projects.", MessageBoxButtons.OK, MessageBoxIcon.Information);
                CaliberProjectGroup.Enabled = true;
                StatusProgressBar.MarqueeAnimationSpeed -= 30;
                CaliberProjectList.Focus();
                EnableMigrateRequirementsButton();
            }));
        }

        private void LoadCaliberProjectsFailed(String ErrorMessage)
        {
            CaliberLoginGroup.Invoke(new MethodInvoker(delegate
            {
                LogMessage("FAILED loading Caliber RM projects.");
                LogMessage(ErrorMessage);
                CaliberUser.Enabled = true;
                CaliberPassword.Enabled = true;
                CaliberLoginButton.Enabled = true;
                StatusProgressBar.MarqueeAnimationSpeed -= 30;
                EnableMigrateRequirementsButton();
            }));
        }

        private void CaliberProjectNextTabButton_Click(object sender, EventArgs e)
        {
            TabControl.SelectedTab = AlmProjectTab;
        }

    }
}

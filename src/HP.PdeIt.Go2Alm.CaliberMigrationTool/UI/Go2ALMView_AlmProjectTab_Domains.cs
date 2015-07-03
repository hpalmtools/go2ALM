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
        private void AlmDomainList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (AlmDomainList.SelectedIndex >= 0)
            {
                try
                {
                    LoadAlmProjectsThread = new Thread(new ThreadStart(LoadAlmProjectsThreadJob));
                    LoadAlmProjectsThread.Name = "LoadAlmProjects";
                    LoadAlmProjectsThread.IsBackground = true;
                    LoadAlmProjectsThread.Start();
                }
                catch (Exception ex)
                {
                    LogMessage("User Logged off from ALM session. Load projects aborted. " + ex.Message);
                }
            }
        }

        private void LoadAlmDomains()
        {
            try
            {
                LoadAlmDomainsSetup();
                List<string> AlmDomainNameList = Go2ALMBusinessLogic.GetAlmDomainList();
                LoadAlmDomainsSuccessful(AlmDomainNameList);
            }
            catch (Exception ex)
            {
                LoadAlmDomainsFailed(ex.Message);
            }
        }

        private void LoadAlmDomainsSetup()
        {
            AlmProjectGroup.Invoke(new MethodInvoker(delegate
            {
                LogMessage("Loading list of ALM domains... ");
                AlmDomainList.Items.Clear();
                AlmDomainList.Items.Add("Loading list of ALM domains.");
                AlmDomainList.Items.Add("This may take a few minutes please be patient... ");
            }));
        }

        private void LoadAlmDomainsSuccessful(List<string> AlmDomainNameList)
        {
            AlmProjectGroup.Invoke(new MethodInvoker(delegate
            {
                AlmDomainList.Items.Clear();
                AlmDomainList.Items.AddRange(AlmDomainNameList.ToArray());
                LogMessage("Finished loading ALM domains.");
                AlmProjectGroup.Enabled = true;
                StatusProgressBar.MarqueeAnimationSpeed -= 30;
            }));
        }

        private void LoadAlmDomainsFailed(String ErrorMessage)
        {
            AlmLoginGroup.Invoke(new MethodInvoker(delegate
            {
                LogMessage("FAILED loading ALM domains.");
                LogMessage(ErrorMessage);
                AlmUser.Enabled = true;
                AlmPassword.Enabled = true;
                AlmLoginButton.Enabled = true;
                StatusProgressBar.MarqueeAnimationSpeed -= 30;
            }));
        }
    }
}

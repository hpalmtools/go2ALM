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
        private void AlmReleaseList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (AlmReleaseList.SelectedIndex >= 0)
            {
                EnableMigrateRequirementsButton();
            }
        }

        private void LoadAlmReleases()
        {
            try
            {
                LoadAlmReleasesSetup();
                List<string> releaseList = Go2ALMBusinessLogic.GetAlmReleaseList();
                LoadAlmReleasesSuccessful(releaseList);
            }
            catch (Exception ex)
            {
                LoadAlmReleasesFailed(ex.Message);
            }
        }

        private void LoadAlmReleasesSetup()
        {
            AlmProjectGroup.Invoke(new MethodInvoker(delegate
            {
                AlmReleaseList.Enabled = false;
                LogMessage("Loading list of Releases in the selected project (" + SelectedAlmProjectName + ")... ");
                AlmReleaseList.Items.Clear();
            }));
        }

        private void LoadAlmReleasesSuccessful(List<string> releaseList)
        {
            AlmProjectGroup.Invoke(new MethodInvoker(delegate
            {
                AlmReleaseList.Items.Clear();
                AlmReleaseList.Items.AddRange(releaseList.ToArray());
                LogMessage("Finished loading Releases.");
                AlmReleaseList.Enabled = true;
                AlmReleaseList.Focus();
            }));
        }

        private void LoadAlmReleasesFailed(String ErrorMessage)
        {
            AlmLoginGroup.Invoke(new MethodInvoker(delegate
            {
                LogMessage("FAILED loading ALM releases.");
                LogMessage(ErrorMessage);
                AlmReleaseList.Items.Clear();
                AlmReleaseList.Items.Add("FAILED loading ALM releases.");
                AlmReleaseList.Items.Add(ErrorMessage);
            }));
        }
    }
}

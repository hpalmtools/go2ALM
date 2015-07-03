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
        private void AlmProjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (AlmProjectList.SelectedIndex >= 0)
            {
                try
                {
                    LoadAlmProjectListValuesThread = new Thread(new ThreadStart(LoadAlmProjectListValuesThreadJob));
                    LoadAlmProjectListValuesThread.Name = "LoadAlmProjectListValues";
                    LoadAlmProjectListValuesThread.IsBackground = true;
                    LoadAlmProjectListValuesThread.Start();
                }
                catch (Exception ex)
                {
                    LogMessage("User Logged off from ALM session. Load folders aborted. " + ex.Message);
                }
            }
        }

        private void LoadAlmProjectsThreadJob()
        {
            try
            {
                LoadAlmProjectsSetup();
                List<string> AlmProjectNameList = Go2ALMBusinessLogic.GetAlmProjectList(SelectedAlmDomainName);
                LoadAlmProjectsSuccessful(AlmProjectNameList);
            }
            catch (Exception ex)
            {
                LoadAlmProjectsFailed(ex.Message);
            }
        }

        private void LoadAlmProjectsSetup()
        {
            AlmProjectGroup.Invoke(new MethodInvoker(delegate
            {
                SelectedAlmDomainName = AlmDomainList.SelectedItem.ToString();
                LogMessage("Loading list of ALM projects in the selected domain (" + SelectedAlmDomainName + ")... ");
                AlmProjectList.Items.Clear();
                AlmProjectList.Items.Add("Loading list of ALM projects in the selected domain (" + SelectedAlmDomainName + ")... ");
                AlmProjectList.Items.Add("This may take a few minutes please be patient... ");
                AlmReleaseList.Items.Clear();
                AlmFolderList.Nodes.Clear();
                AlmCustomFieldsClear();
                AlmProjectList.Enabled = false;
                AlmReleaseList.Enabled = false;
                AlmFolderList.Enabled = false;
                AlmCustomFieldsEnabled(false);
                MigrateRequirementsButton.Enabled = false;
            }));
        }

        private void LoadAlmProjectsSuccessful(List<string> AlmProjectNameList)
        {
            AlmProjectGroup.Invoke(new MethodInvoker(delegate
            {
                AlmProjectList.Items.Clear();
                AlmProjectList.Items.AddRange(AlmProjectNameList.ToArray());
                LogMessage("Finished loading ALM projects.");
                AlmProjectList.Enabled = true;
                AlmProjectList.Focus();
            }));
        }

        private void LoadAlmProjectsFailed(String ErrorMessage)
        {
            AlmProjectGroup.Invoke(new MethodInvoker(delegate
            {
                LogMessage("FAILED loading ALM projects.");
                LogMessage(ErrorMessage);
                AlmProjectList.Items.Clear();
                AlmProjectList.Items.Add("FAILED loading ALM projects.");
                AlmProjectList.Items.Add(ErrorMessage);
            }));
        }

        private void LoadAlmProjectListValuesThreadJob()
        {
            LoadAlmProjectListValuesSetup();
            Go2ALMBusinessLogic.PerformAlmProjectConnection(SelectedAlmDomainName, SelectedAlmProjectName);
            try
            {
                LoadAlmFoldersThread = new Thread(new ThreadStart(LoadAlmFolders));
                LoadAlmFoldersThread.Name = "LoadAlmFolders";
                LoadAlmFoldersThread.IsBackground = true;
                LoadAlmFoldersThread.Start();
            }
            catch (Exception ex)
            {
                LogMessage("User Logged off from ALM session. Load folders aborted. " + ex.Message);
            }
            try
            {
                LoadAlmReleasesThread = new Thread(new ThreadStart(LoadAlmReleases));
                LoadAlmReleasesThread.Name = "LoadAlmReleases";
                LoadAlmReleasesThread.IsBackground = true;
                LoadAlmReleasesThread.Start();
            }
            catch (Exception ex)
            {
                LogMessage("User Logged off from ALM session. Load releases aborted. " + ex.Message);
            }
            try
            {
                LoadAlmCustomFieldsThread = new Thread(new ThreadStart(LoadAlmCustomFields));
                LoadAlmCustomFieldsThread.Name = "LoadAlmCustomFields";
                LoadAlmCustomFieldsThread.IsBackground = true;
                LoadAlmCustomFieldsThread.Start();
            }
            catch (Exception ex)
            {
                LogMessage("User Logged off from ALM session. Load custom fields aborted. " + ex.Message);
            }
            LoadAlmReleasesThread.Join();
            LoadAlmCustomFieldsThread.Join();
            LoadAlmFoldersThread.Join();
            LoadAlmProjectListValuesSuccessful();
        }

        private void LoadAlmProjectListValuesSetup()
        {
            AlmProjectGroup.Invoke(new MethodInvoker(delegate
            {
                SelectedAlmDomainName = AlmDomainList.SelectedItem.ToString();
                SelectedAlmProjectName = AlmProjectList.SelectedItem.ToString();
                AlmReleaseList.Items.Clear();
                AlmFolderList.Nodes.Clear();
                AlmCustomFieldsClear();
                AlmReleaseList.Enabled = false;
                AlmFolderList.Enabled = false;
                AlmCustomFieldsEnabled(false);
                MigrateRequirementsButton.Enabled = false;
                LogMessage("Connecting to the selected Domain/Project (" + SelectedAlmDomainName + "/" + SelectedAlmProjectName + ")...");
            }));
        }

        private void LoadAlmProjectListValuesSuccessful()
        {
            AlmProjectGroup.Invoke(new MethodInvoker(delegate
            {
                AlmReleaseList.Enabled = true;
                AlmFolderList.Enabled = true;
                AlmCustomFieldsEnabled(true);
            }));
        }

    }
}

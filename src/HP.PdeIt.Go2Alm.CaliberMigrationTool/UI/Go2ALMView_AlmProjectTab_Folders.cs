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
        private void AlmFolderList_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (AlmFolderList.SelectedNode != null)
            {
                EnableMigrateRequirementsButton();
            }
        }

        private void LoadAlmFolders()
        {
            try
            {
                LoadAlmFoldersSetup();
                System.Windows.Forms.TreeNode TopNode = Go2ALMBusinessLogic.GetAlmFolderList();
                LoadAlmFoldersSuccessful(TopNode);
            }
            catch (Exception ex)
            {
                LoadAlmFoldersFailed(ex.Message);
            }
        }

        private void LoadAlmFoldersSetup()
        {
            AlmProjectGroup.Invoke(new MethodInvoker(delegate
            {
                AlmFolderList.Enabled = false;
                LogMessage("Loading list of ALM folders in the selected project (" + SelectedAlmProjectName + ")... ");
                AlmFolderList.Nodes.Add("Loading list of folders....").Nodes.Add("This may take a few minutes.");
                AlmFolderList.ImageList = new ImageList();
                AlmFolderList.ImageList.Images.Add(HP.PdeIt.Properties.Resources.Folder);
                AlmFolderList.TopNode.Expand();
            }));
        }

        private void LoadAlmFoldersSuccessful(TreeNode TopNode)
        {
            AlmProjectGroup.Invoke(new MethodInvoker(delegate
            {
                AlmFolderList.BeginUpdate();
                AlmFolderList.Nodes.Clear();
                AlmFolderList.Nodes.Add(TopNode);
                AlmFolderList.Sort();
                AlmFolderList.TopNode.Expand();
                AlmFolderList.EndUpdate();
                AlmFolderList.Enabled = true;
                LogMessage("Finished loading ALM folders.");
                AlmFolderList.Focus();
            }));
        }

        private void LoadAlmFoldersFailed(String ErrorMessage)
        {
            AlmLoginGroup.Invoke(new MethodInvoker(delegate
            {
                LogMessage("FAILED loading ALM folders.");
                LogMessage(ErrorMessage);
                AlmFolderList.Nodes.Clear();
                AlmFolderList.Nodes.Add("FAILED loading ALM folders.");
                AlmFolderList.Nodes.Add(ErrorMessage);
            }));
        }
    }
}

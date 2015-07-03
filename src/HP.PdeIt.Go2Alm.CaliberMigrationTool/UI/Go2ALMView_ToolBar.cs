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

    public partial class Go2ALMView
    {

        /// <summary>
        /// When the OpenLogFolderButton is clicked:
        ///    Open the log folder
        /// </summary>
        private void OpenLogFolderButton_Click(object sender, EventArgs e)
        {
            try
            {
                string logsPath = ".\\logs";
                System.Diagnostics.Process.Start(logsPath);
            }
            catch (Exception ex)
            {
                LogMessage("Could not open the logs folder. " + ex.Message);
            }
        }

        /// <summary>
        /// When the OpenLogFileButton is clicked:
        ///    Open the log file.
        /// </summary>
        private void OpenLogFileButton_Click(object sender, EventArgs e)
        {
            try
            {
                string logFileName = ".\\logs\\Go2ALM_CaliberMigrationTool.Log";
                System.Diagnostics.Process.Start("notepad.exe", logFileName);
            }
            catch (Exception ex)
            {
                LogMessage("Could not open the Log file. " + ex.Message);
            }
        }

        /// <summary>
        /// When the CopyTextButton is clicked:
        ///    Select all the text and copy it, so it is available for pasting anywhere.
        /// </summary>
        private void CopyTextButton_Click(object sender, EventArgs e)
        {
            LogTextBox.SelectAll();
            LogTextBox.Copy();
        }

        /// <summary>
        /// When the HelpLinkButton is clicked:
        ///    Open the Help Link, this is configured in the app config file.
        /// </summary>
        private void HelpLinkButton_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(HelpLink);
            }
            catch (Exception ex)
            {
                LogMessage("Could not open the help SharePoint. " + ex.Message);
            }
        }

        
    }
}

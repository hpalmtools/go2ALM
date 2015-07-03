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
    using System.Windows.Forms;

    public partial class Go2ALMView
    {

        /// <summary>
        /// When the MigrateTracesCheckBox value is changed:
        ///    Enable/Disable the iSVN fields according to the MigrateTracesCheckBox state.
        ///    Select the next control to improve user experience.
        ///    Enable/Disable the Migrate Requirements Button.
        /// </summary>
        private void MigrateTracesCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            //Disable/Enable the iSVN fields according to the MigrateTracesCheckBox state
            ISVNUser.Enabled = MigrateTracesCheckBox.Checked;
            ISVNPassword.Enabled = MigrateTracesCheckBox.Checked;
            ISVNRepository.Enabled = MigrateTracesCheckBox.Checked;
            
            if (MigrateTracesCheckBox.Checked)
            {
                ISVNUser.Focus();
                ISVNUser.SelectAll();
            }
            else
            {
                SetupTracesNextTabButton.Select();
            }
            EnableMigrateRequirementsButton();
        }

        /// <summary>
        /// When a key is pressed on the ISVNUser TextBox:
        ///    If that key is Enter, select the next control to improve user experience.
        /// </summary>
        private void ISVNUser_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ISVNPassword.Focus();
                ISVNPassword.SelectAll();
            }
        }

        /// <summary>
        /// When a key is pressed on the ISVNPassword TextBox:
        ///    If that key is Enter, select the next control to improve user experience.
        /// </summary>
        private void ISVNPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ISVNRepository.Focus();
                ISVNRepository.SelectAll();
            }
        }

        /// <summary>
        /// When a key is pressed on the ISVNRepository TextBox:
        ///    If that key is Enter, select the next tab to improve user experience.
        /// </summary>
        private void ISVNRepository_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                TabControl.SelectedTab = CaliberProjectTab;
            }
        }

        /// <summary>
        /// When the ISVNUser TextBox text changes:
        ///    Enable/Disable the Migrate Requirements Button.
        /// </summary>
        private void ISVNUser_TextChanged(object sender, EventArgs e)
        {
            EnableMigrateRequirementsButton();
        }

        /// <summary>
        /// When the ISVNPassword TextBox text changes:
        ///    Enable/Disable the Migrate Requirements Button.
        /// </summary>
        private void ISVNPassword_TextChanged(object sender, EventArgs e)
        {
            EnableMigrateRequirementsButton();
        }

        /// <summary>
        /// When the ISVNRepository TextBox text changes:
        ///    Enable/Disable the Migrate Requirements Button.
        /// </summary>
        private void ISVNRepository_TextChanged(object sender, EventArgs e)
        {
            EnableMigrateRequirementsButton();
        }
        
        /// <summary>
        /// When the NextTabButton is clicked:
        ///    Select the next tab to improve user experience.
        /// </summary>
        private void SetupTracesNextTabButton_Click(object sender, EventArgs e)
        {
            TabControl.SelectedTab = CaliberProjectTab;
        }

    }
}

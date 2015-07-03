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
    using System.Drawing;
    using System.Threading;
    using System.Windows.Forms;
    using System.Configuration;

    public partial class Go2ALMView
    {

        private void CaliberServerList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (CaliberServerList.SelectedIndex >= 0)
            {
                SelectedCaliberServerName = CaliberServerList.SelectedItem.ToString();
            }
        }

        /// <summary>
        /// When a key is pressed on the CaliberUser TextBox:
        ///    If that key is Enter, select the next control to improve user experience.
        /// </summary>
        private void CaliberUser_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                CaliberPassword.Focus();
                CaliberPassword.SelectAll();
            }
        }

        /// <summary>
        /// When a key is pressed on the CaliberPassword TextBox:
        ///    If that key is Enter, select and perform click on the Login button.
        /// </summary>
        private void CaliberPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                CaliberLoginButton.Focus();
                CaliberLoginButton.PerformClick();
            }
        }

        /// <summary>
        /// When the CaliberLoginButton is clicked:
        ///    Create a new thread to perform the login in the background.
        /// </summary>
        private void CaliberLoginButton_Click(object sender, EventArgs e)
        {
            try
            {
                CaliberLoginThread = new Thread(new ThreadStart(CaliberLoginThreadJob));
                CaliberLoginThread.Name = "CaliberLogin";
                CaliberLoginThread.IsBackground = true;
                CaliberLoginThread.Start();
            }
            catch (Exception ex)
            {
                LogMessage("User Logged off from Caliber session. Login action aborted. " + ex.Message);
            }
        }

        private void CaliberLoginThreadJob()
        {
            bool LoginSuccessful = false;
            try
            {
                CaliberLoginSetup();
                Go2ALMBusinessLogic.PerformCaliberLogin(SelectedCaliberServerName, CaliberUser.Text, CaliberPassword.Text);
                CaliberLoginSuccessful();
                LoginSuccessful = true;
            }
            catch (Exception ex)
            {
                CaliberLoginFailed(ex.Message);
            }
            if (LoginSuccessful)
            {
                LoadCaliberProjects();
            }
        }

        private void CaliberLoginSetup()
        {
            CaliberLoginGroup.Invoke(new MethodInvoker(delegate
            {
                StatusProgressBar.MarqueeAnimationSpeed += 30;
                CaliberLoginSubtitleLabel.ForeColor = Color.FromArgb(0, 150, 214);
                CaliberLoginSubtitleLabel.Text = "1) Logging in to Caliber RM... ";
                LogMessage("Logging in to Caliber RM... ");
                CaliberLoginButton.Enabled = false;
            }));
        }

        private void CaliberLoginSuccessful()
        {
            CaliberLoginGroup.Invoke(new MethodInvoker(delegate
            {
                LogMessage("Finished Caliber RM Login");
                CaliberServerList.Enabled = false;
                CaliberUser.Enabled = false;
                CaliberPassword.Enabled = false;
                CaliberLoginSubtitleLabel.ForeColor = Color.FromArgb(0, 150, 214);
                CaliberLoginSubtitleLabel.Text = "1) Logged in as " + CaliberUser.Text;
                CaliberLoginSubtitleLabel.Enabled = true;
                CaliberLogOffButton.Enabled = true;
                TabControl.SelectedTab = AlmLoginTab;
                AlmUser.Focus();
            }));
        }

        private void CaliberLoginFailed(String ErrorMessage)
        {
            CaliberLoginGroup.Invoke(new MethodInvoker(delegate
            {
                LogMessage("Caliber RM login FAILED.");
                string errorMessage = "Invalid user or password, please try to Log in again... ";
                if (ErrorMessage.Contains("Retrieving the COM class factory for component with CLSID"))
                {
                    errorMessage = "Please install CaliberRM, It is a prerequisite for using this tool.";
                }
                else if (ErrorMessage.Contains("Unable to cast COM object of type"))
                {
                    errorMessage = "Please install CaliberRM, It is a prerequisite for using this tool.";
                }
                LogMessage(errorMessage + " For more info check the user guide. " + ErrorMessage);
                CaliberLoginSubtitleLabel.ForeColor = Color.Red;
                CaliberLoginSubtitleLabel.Text = errorMessage;
                CaliberPassword.Focus();
                CaliberPassword.SelectAll();
                CaliberLoginButton.Enabled = true;
                StatusProgressBar.MarqueeAnimationSpeed -= 30;
            }));
        }

        private void CaliberLogOffButton_Click(object sender, EventArgs e)
        {
            CaliberLogOffSetup();
            CaliberLogOff();
            CaliberLogOffSuccessful();
        }

        private void CaliberLogOffSetup()
        {
            LogMessage("User Logged off from Caliber RM session. Aborting sessions in the middle of a transaction may cause some exceptions, please ignore any exceptions in the following seconds.");
            CaliberLoginSubtitleLabel.ForeColor = Color.FromArgb(0, 150, 214);
            CaliberLoginSubtitleLabel.Text = "1) Please Log in to Caliber RM...";
            CaliberPassword.Text = string.Empty;
            CaliberServerList.Enabled = true;
            CaliberUser.Enabled = true;
            CaliberPassword.Enabled = true;
            CaliberLoginButton.Enabled = true;
            CaliberLogOffButton.Enabled = false;
            CaliberProjectGroup.Enabled = false;
        }

        private void CaliberLogOff()
        {
            if (CaliberLoginThread != null && CaliberLoginThread.IsAlive)
            {
                CaliberLoginThread.Interrupt();
            }

            if (LoadCaliberBaselinesThread != null && LoadCaliberBaselinesThread.IsAlive)
            {
                LoadCaliberBaselinesThread.Interrupt();
            }

            Go2ALMBusinessLogic.CaliberLogOff();
        }

        private void CaliberLogOffSuccessful()
        {
            MigrateAllRequirementsRadioButton.Checked = true;
            CaliberProjectList.Items.Clear();
            CaliberBaselineList.Items.Clear();
            CaliberReqTypesCheckedList.Items.Clear();
            CaliberPassword.Focus();
            CaliberPassword.SelectAll();
        }

        /// <summary>
        /// Loads the ComboBox with all the Caliber servers at caliberServerNames configuration section in app.config
        /// </summary>
        private void LoadCaliberServers()
        {
            try
            {
                ConfigManager configManager = (ConfigManager)ConfigurationManager.GetSection("serverConfiguration");
                if (configManager != null && configManager.CaliberServerNames.Count > 0)
                {
                    for (int i = 0; i < configManager.CaliberServerNames.Count; i++)
                    {
                        CaliberServerList.Items.Add(configManager.CaliberServerNames[i].Name);
                    }
                    CaliberServerList.SelectedIndex = 0;
                    SelectedCaliberServerName = CaliberServerList.SelectedItem.ToString();
                }
                else
                {
                    LogMessage("Please add at least one server in the caliberServerNames configuration section.");
                }
            }
            catch (Exception e)
            {
                LogMessage("ERROR: While loading Caliber server names from the config file. " + e.Message);
            }
        }
    }
}

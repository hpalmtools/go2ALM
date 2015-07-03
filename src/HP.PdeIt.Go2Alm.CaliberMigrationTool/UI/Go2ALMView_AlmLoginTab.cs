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

        private void AlmServerList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (AlmServerList.SelectedIndex >= 0)
            {
                AlmShortServerName = AlmServerList.SelectedItem.ToString();
                SelectedAlmServerName = "http://" + AlmShortServerName + "/qcbin";
            }
        }

        /// <summary>
        /// When a key is pressed on the AlmUser TextBox:
        ///    If that key is Enter, select the next control to improve user experience.
        /// </summary>
        private void AlmUser_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                AlmPassword.Focus();
                AlmPassword.SelectAll();
            }
        }

        /// <summary>
        /// When a key is pressed on the AlmPassword TextBox:
        ///    If that key is Enter, select and perform click on the Login button.
        /// </summary>
        private void AlmPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                AlmLoginButton.Focus();
                AlmLoginButton.PerformClick();
            }
        }

        /// <summary>
        /// When the AlmLoginButton is clicked:
        ///    Create a new thread to perform the login in the background.
        /// </summary>
        private void AlmLoginButton_Click(object sender, EventArgs e)
        {
            try
            {
                AlmLoginThread = new Thread(new ThreadStart(AlmLoginThreadJob));
                AlmLoginThread.Name = "AlmLogin";
                AlmLoginThread.IsBackground = true;
                AlmLoginThread.Start();
            }
            catch (Exception ex)
            {
                LogMessage("User Logged off from ALM session. Login action aborted. " + ex.Message);
            }
        }

        private void AlmLoginThreadJob()
        {
            bool LoginSuccessful = false;
            try
            {
                AlmLoginSetup();
                Go2ALMBusinessLogic.PerformAlmLogin(SelectedAlmServerName, AlmUser.Text, AlmPassword.Text, AppSettings);
                AlmLoginSuccessful();
                LoginSuccessful = true;
            }
            catch (Exception ex)
            {
                AlmLoginFailed(ex.Message);
            }
            if (LoginSuccessful)
            {
                LoadAlmDomains();
            }
        }

        private void AlmLoginSetup()
        {
            AlmLoginGroup.Invoke(new MethodInvoker(delegate
            {
                StatusProgressBar.MarqueeAnimationSpeed += 30;
                AlmLoginSubtitleLabel.ForeColor = Color.FromArgb(0, 150, 214);
                AlmLoginSubtitleLabel.Text = "2) Logging in to ALM... ";
                LogMessage("Logging in to ALM... ");
                AlmLoginButton.Enabled = false;
            }));
        }

        private void AlmLoginSuccessful()
        {
            AlmLoginGroup.Invoke(new MethodInvoker(delegate
            {
                LogMessage("Finished ALM login");
                AlmServerList.Enabled = false;
                AlmUser.Enabled = false;
                AlmPassword.Enabled = false;
                AlmLoginSubtitleLabel.ForeColor = Color.FromArgb(0, 150, 214);
                AlmLoginSubtitleLabel.Text = "2) Logged in as " + AlmUser.Text;
                AlmLoginSubtitleLabel.Enabled = true;
                AlmLogOffButton.Enabled = true;
                TabControl.SelectedTab = SetupTracesTab;
                ISVNUser.Focus();
            }));
        }

        private void AlmLoginFailed(String ErrorMessage)
        {
            AlmLoginGroup.Invoke(new MethodInvoker(delegate
            {
                LogMessage("ALM login FAILED.");
                string errorMessage = "Invalid user or password, please try to Log in again... ";
                if (ErrorMessage.Contains("Retrieving the COM class factory for component with CLSID"))
                {
                    errorMessage = "Please install ALM, It is a prerequisite for using this tool.";
                }
                else if (ErrorMessage.Contains("Unable to cast COM object of type"))
                {
                    errorMessage = "Please install ALM Connectivity addin, It is a prerequisite for using this tool.";
                }
                LogMessage(errorMessage + " For more info check the user guide. " + ErrorMessage);
                AlmLoginSubtitleLabel.ForeColor = Color.Red;
                AlmLoginSubtitleLabel.Text = errorMessage;
                AlmPassword.Focus();
                AlmPassword.SelectAll();
                AlmLoginButton.Enabled = true;
                StatusProgressBar.MarqueeAnimationSpeed -= 30;
            }));
        }

        private void AlmLogOffButton_Click(object sender, EventArgs e)
        {
            AlmLogOffSetup();
            AlmLogOff();
            AlmLogOffSuccessful();
        }

        private void AlmLogOffSetup()
        {
            LogMessage("User Logged off from ALM session. Aborting sessions in the middle of a transaction may cause some exceptions, please ignore any exceptions in the following seconds.");
            AlmLoginSubtitleLabel.ForeColor = Color.FromArgb(0, 150, 214);
            AlmLoginSubtitleLabel.Text = "2) Please Log in to ALM...";
            AlmPassword.Text = string.Empty;
            AlmServerList.Enabled = true;
            AlmUser.Enabled = true;
            AlmPassword.Enabled = true;
            AlmLoginButton.Enabled = true;
            AlmLogOffButton.Enabled = false;
            AlmProjectList.Enabled = false;
            AlmReleaseList.Enabled = false;
            AlmFolderList.Enabled = false;
            AlmCustomFieldsEnabled(false);
            AlmProjectGroup.Enabled = false;
        }

        private void AlmLogOff()
        {
            if (AlmLoginThread != null && AlmLoginThread.IsAlive)
            {
                AlmLoginThread.Interrupt();
            }

            if (LoadAlmProjectsThread != null && LoadAlmProjectsThread.IsAlive)
            {
                LoadAlmProjectsThread.Interrupt();
            }

            if (LoadAlmProjectListValuesThread != null && LoadAlmProjectListValuesThread.IsAlive)
            {
                LoadAlmProjectListValuesThread.Interrupt();
            }

            Go2ALMBusinessLogic.AlmLogOff();
        }

        private void AlmLogOffSuccessful()
        {
            AlmDomainList.Items.Clear();
            AlmProjectList.Items.Clear();
            AlmReleaseList.Items.Clear();
            AlmFolderList.Nodes.Clear();
            AlmCustomFieldsClear();
            AlmPassword.Focus();
            AlmPassword.SelectAll();
        }

        /// <summary>
        /// Loads the ComboBox with all the ALM servers at almServerNames configuration section in app.config
        /// </summary>
        private void LoadAlmServers()
        {
            try
            {
                ConfigManager configManager = (ConfigManager)ConfigurationManager.GetSection("serverConfiguration");
                if (configManager != null && configManager.ALMServerNames.Count > 0)
                {
                    for (int i = 0; i < configManager.ALMServerNames.Count; i++)
                    {
                        AlmServerList.Items.Add(configManager.ALMServerNames[i].Name);
                    }
                    AlmServerList.SelectedIndex = 0;
                    AlmShortServerName = AlmServerList.SelectedItem.ToString();
                    SelectedAlmServerName = "http://" + AlmShortServerName + "/qcbin";
                }
                else
                {
                    LogMessage("Please add at least one server in the almServerNames configuration section.");
                }
            }
            catch (Exception e)
            {
                LogMessage("ERROR: While loading ALM server names from the config file. " + e.Message);
            }
        }

        private void AlmUser_TextChanged(object sender, EventArgs e)
        {
            ISVNUser.Text = AlmUser.Text;

            if (AlmUser.Text.Length > 0 && AlmUser.Text.Contains("_"))
            {
                int index = AlmUser.Text.LastIndexOf("_");
                string lastPart = AlmUser.Text.Substring(index);
                ISVNUser.Text = AlmUser.Text.Substring(0, index) + lastPart.Replace("_", "@");
            }
        }
    }
}

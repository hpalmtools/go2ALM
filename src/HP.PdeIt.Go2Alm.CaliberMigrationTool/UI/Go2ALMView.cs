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
    using System.Configuration;
    using System.Threading;
    using System.Windows.Forms;
    using log4net;
    using Microsoft.Win32;
    using HP.PdeIt.Go2Alm.CaliberMigrationTool;

    public partial class Go2ALMView : Form
    {
        public readonly ILog Log = LogManager.GetLogger("CaliberMigrationTool");
        public string AlmShortServerName { get; set; }
        private AppSettingsSection AppSettings;
        private string SelectedAlmServerName;
        private string SelectedCaliberServerName;
        private string SelectedCaliberProjectName;
        private string SelectedCaliberBaselineName;
        private string SelectedAlmDomainName;
        private string SelectedAlmProjectName;
        private string SelectedAlmReleaseName;
        private string SelectedAlmFolderName;

        private string HelpLink;

        private Go2ALMBusinessLogic Go2ALMBusinessLogic;

        /// <summary>
        ///  Class constructor.
        ///     Initializes all components.
        ///     Loads application settings.
        ///     Checks for go2ALM updates.
        ///     Initializes Caliber and ALM APIs.
        /// </summary>
        public Go2ALMView()
        {
            InitializeComponent();
            Thread.CurrentThread.Name = "MainThread";
            AutoUpdate AutoUpdate = new AutoUpdate();
            LogMessage("Starting CaliberRM to ALM Migration Tool Version: " + AutoUpdate.VERSION);
            LogMessage("Please keep in mind that all dates are based on UTC time zone...");

            Go2ALMBusinessLogic = new Go2ALMBusinessLogic(this);

            LogMessage("Loading Application Settings...");
            LoadApplicationSettings();

            LogMessage("Checking for updates...");
            AutoUpdate.CheckForUpdates();

            int height = SystemInformation.PrimaryMonitorSize.Height;
            if (height <= 768)
            {
                this.MinimumSize = new System.Drawing.Size(866, height);
                this.Size = new System.Drawing.Size(866, height);
            }

            LoadCaliberServers();
            LoadAlmServers();
            SetupCustomFields();

            LogMessage("CaliberRM server: " + SelectedCaliberServerName);
            LogMessage("ALM server: " + SelectedAlmServerName);
            LogMessage("Finished starting up");
            CaliberUser.Focus();
            CaliberUser.Select();
        }

        /// <summary>
        ///     Loads application settings.
        /// </summary>
        private void LoadApplicationSettings()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            AppSettings = (System.Configuration.AppSettingsSection)config.GetSection("appSettings");
            HelpLink = AppSettings.Settings["HelpLink"].Value;
        }

        /// <summary>
        /// When a key is pressed on the LogTextBox:
        ///    If that key is Control + A, select all text in the Log Text Box.
        /// </summary>
        private void LogTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (((Control.ModifierKeys & Keys.Control) == Keys.Control) && e.KeyValue == 'A')
            {
                LogTextBox.SelectAll();
            }
        }

        /// <summary>
        /// When the tab changes:
        ///    Select the next control to improve user experience..
        /// </summary>
        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (TabControl.SelectedTab.Equals(CaliberLoginTab))
            {
                CaliberUser.Focus();
                CaliberUser.SelectAll();
            }
            else if (TabControl.SelectedTab.Equals(AlmLoginTab))
            {
                AlmUser.Focus();
                AlmUser.SelectAll();
            }
            else if (TabControl.SelectedTab.Equals(SetupTracesTab))
            {
                MigrateTracesCheckBox.Focus();
                //Enable/Disable the iSVN fields according to the MigrateTracesCheckBox state.
                ISVNUser.Enabled = MigrateTracesCheckBox.Checked;
                ISVNPassword.Enabled = MigrateTracesCheckBox.Checked;
                ISVNRepository.Enabled = MigrateTracesCheckBox.Checked;
            }
            else if (TabControl.SelectedTab.Equals(CaliberProjectTab))
            {
                CaliberProjectList.Focus();
            }
        }

        /// <summary>
        /// Utility method to log the messages into the log files and the log text box.
        /// </summary>
        /// <param name="status">The message that will be logged</param>
        public void LogMessage(string status)
        {
            Log.Info(status);
            LogTextBox.Text += string.Format("{0:yyyy-MM-dd  HH:mm:ss}", DateTime.Now.ToUniversalTime()) + "\t" + status + Environment.NewLine;
            LogTextBox.SelectionStart = LogTextBox.Text.Length;
            LogTextBox.ScrollToCaret();
            StatusLabel.Text = status;
        }

    }
}

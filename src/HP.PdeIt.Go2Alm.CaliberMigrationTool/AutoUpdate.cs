// go2ALM: migrate from various tools to QC/ALM
// Copyright (C) 2012 Hewlett Packard Company
// Authors: 
//      Olivier Jacques
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

namespace HP.PdeIt.Go2Alm.CaliberMigrationTool
{
    using System;
    using System.Windows.Forms;
    using Microsoft.Win32;

    /// <summary>
    ///  Data structure to map requirements between the old caliber ID and the new ALM ID.
    /// </summary>
    public class AutoUpdate
    {
        public const long VERSION = 20121009;  // Do not forget to update the version string to force updates
        const string DOWNLOAD_LINK = "https://sourceforge.net/projects/almtools/files/go2ALM/";
        const string VERSION_URI = "http://almtools.sourceforge.net/go2alm-latest.txt";

        /// <summary>
        ///     AutoUpdate constructor.
        /// </summary>
        public AutoUpdate()
        {

        }

        /// <summary>
        ///     Checks for go2ALM updates.
        /// </summary>
        public void CheckForUpdates()
        {
            // Check for an updated version of go2ALM
            string strLastCheck = RegistryGet("LastVersionCheck");
            if (String.Compare(strLastCheck, DateTime.Now.AddDays(-7).ToString("yyyyMMdd")) < 0)
            {
                // We checked more than 7 days ago: check again
                long webVersion;
                string strVersionMessage = getLatestVersion(VERSION_URI, out webVersion);
                RegistrySet("LastVersionCheck", DateTime.Now.ToString("yyyyMMdd"));

                if ((!strVersionMessage.Contains("ERROR")) && (webVersion > VERSION))
                {
                    if ((MessageBox.Show("There is a new version of go2ALM.\r\nCurrent version: " + VERSION.ToString() +
                        ". Latest version: " + webVersion.ToString() +
                        ".\r\nDo you want to download the latest version?",
                        "Version outdated", MessageBoxButtons.YesNo) == DialogResult.Yes))
                    {
                        System.Diagnostics.Process.Start(DOWNLOAD_LINK);
                    }
                }
                else
                {
                    // Silently ignore
                }
            }
        }

        /// <summary>
        /// Check the latest version
        /// </summary>
        /// <param name="strUrl">URL to check</param>
        /// <param name="result">Version</param>
        /// <returns>Error if any</returns>
        private string getLatestVersion(string strUrl, out long result)
        {
            // Check if an update of the tool is available
            Cursor.Current = Cursors.WaitCursor;
            System.Net.WebClient wc = new System.Net.WebClient();
            string myVersion;
            result = 0;
            try
            {
                wc.Credentials = System.Net.CredentialCache.DefaultCredentials;
                System.IO.Stream str;
                str = wc.OpenRead(strUrl);
                System.IO.StreamReader sr = new System.IO.StreamReader(str);
                myVersion = sr.ReadToEnd();
                sr.Close();
                long.TryParse(myVersion, out result);
                return "";
            }
            catch (Exception ex)
            {
                return "ERROR: " + ex.Message.ToString();
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        /// <summary>
        /// Get registry value
        /// </summary>
        /// <param name="strKeyName">Name of the key to get</param>
        /// <returns>Registry value</returns>
        private string RegistryGet(string strKeyName)
        {
            RegistryKey regkey;
            string strValue = "";
            regkey = Registry.CurrentUser.OpenSubKey(@"Software\go2ALM");

            if (!(regkey == null))
                strValue = (string)regkey.GetValue(strKeyName, "");
            return strValue;
        }

        /// <summary>
        /// Set registry value
        /// </summary>
        /// <param name="strKeyName">Registry key to set</param>
        /// <param name="strKeyValue">Registry value</param>
        private void RegistrySet(string strKeyName, string strKeyValue)
        {
            RegistryKey regkey;
            regkey = Registry.CurrentUser.CreateSubKey(@"Software\go2ALM");
            regkey.SetValue(strKeyName, strKeyValue);
        }

    }
}

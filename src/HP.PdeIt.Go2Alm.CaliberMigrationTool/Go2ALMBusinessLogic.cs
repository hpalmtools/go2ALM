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

namespace HP.PdeIt.Go2Alm.CaliberMigrationTool
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;
    using HP.PdeIt.Go2Alm.CaliberMigrationTool.Mapping;
    using HP.PdeIt.Go2Alm.CaliberMigrationTool.UI;
    using HP.PdeIt.SSR;
    using Starbase.CaliberRM.Interop;
    using StarTeam;
    using TDAPIOLELib;
    using System.Collections;

    public partial class Go2ALMBusinessLogic
    {
        public string CaliberServerName { get; set; }
        public string CaliberUser { get; set; }
        public string CaliberPassword { get; set; }

        public string AlmServerName { get; set; }
        public string AlmUser { get; set; }
        public string AlmPassword { get; set; }

        public bool MigrateTraces { get; set; }
        public string ISVNUser { get; set; }
        public string ISVNPassword { get; set; }
        public string ISVNRepository { get; set; }

        public string CaliberProjectName { get; set; }
        public string CaliberBaselineName { get; set; }
        public string[] CaliberRequirementTypes { get; set; }

        public string AlmDomainName { get; set; }
        public string AlmProjectName { get; set; }
        public string AlmReleaseName { get; set; }
        public string AlmFolderPath { get; set; }
        public string[] AlmCustomFieldsValues { get; set; }

        private bool EnableALMSSRWebServices { get; set; }
        private string AlmSSRWebServices { get; set; }

        private iSelfServiceRouterClient SSRClient;
        private Customization Customization;
        private AppSettingsSection AppSettings;

        private Dictionary<string, string> AlmRequirementFieldsDictionary;
        private Dictionary<string, int> AlmReleasesDataDictionary;
        private Dictionary<string, int> AlmParentIdDataDictionary;
        private Dictionary<string, HashSet<string>> AlmCustomListDictionary;
        private Dictionary<string, int> CaliberProjectDataDictionary;
        private Dictionary<string, string> CaliberBaselinesDataDictionary;
        private Dictionary<string, Project> CaliberProjectNamesDataDictionary;
        private Dictionary<int, RequirementData> RequirementDataDictionary;
        private HashSet<string> UserList;
        
        private Session CaliberSession;
        private Collection CaliberProjects;
        private Starbase.CaliberRM.Interop.Baseline SelectedCaliberBaseline;
        public AlmMapping mapping;

        private TDConnection AlmConnection;
        private int AlmRequirementID;
        
        private Go2ALMView View;

        public Go2ALMBusinessLogic(Go2ALMView CaliberMigrationTool)
        {
            View = CaliberMigrationTool;

            AlmRequirementFieldsDictionary = new Dictionary<string, string>();
            AlmReleasesDataDictionary = new Dictionary<string, int>();
            AlmParentIdDataDictionary = new Dictionary<string, int>();
            AlmCustomListDictionary = new Dictionary<string, HashSet<string>>();
            CaliberProjectDataDictionary = new Dictionary<string, int>();
            CaliberBaselinesDataDictionary = new Dictionary<string, string>();
            CaliberProjectNamesDataDictionary = new Dictionary<string, Project>();
            RequirementDataDictionary = new Dictionary<int, RequirementData>();

            string mappingsConfigurationPath = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + @"\alm_mapping.xml";
            
            UserList = new HashSet<string>();
            try
            {
                if (System.IO.File.Exists(mappingsConfigurationPath))
                {
                    mapping = AlmMapping.Load(mappingsConfigurationPath);
                }
                else
                {
                    try
                    {
                        AlmMapping.Save(mappingsConfigurationPath, new AlmMapping());
                        throw new Exception("An empty mappings configuration file was created, please make sure to fill it, check the user guide for help.");
                    }
                    catch (System.IO.IOException ex)
                    {
                        throw new System.IO.IOException("An error occurred while saving empty mappings configuration file.  Exception details: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                View.Log.Error("Error while loading mappings configuration file, please make sure that the file \"" + mappingsConfigurationPath + "\" exists. Exception details: " + ex.Message);
                MessageBox.Show("Error while loading mappings configuration file, please make sure that the file \"" + mappingsConfigurationPath + "\" exists. Exception details: " + ex.Message, 
                    "Error while loading mappings configuration file.",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        /// <summary>
        ///  Gets the value of the given Caliber AttributeValue object as a string
        /// </summary>
        /// <returns>The string value of the Caliber AttributeValue object</returns>
        public string GetListValue(AttributeValue currentAttributeValue, string type)
        {
            string Value = string.Empty;
            UDAListValue currentListValue = (UDAListValue)currentAttributeValue;
            if (currentListValue.SelectedValue != null && ! (currentListValue.SelectedValue is System.DBNull))
            {
                try
                {
                    Value = currentListValue.SelectedValue.ToString();
                    if (type.Equals(currentAttributeValue.Attribute.UI_NAME_MSL)
                    || type.Equals(currentAttributeValue.Attribute.UI_NAME_SSL))
                    {
                        Value = currentListValue.SelectedValue.ToString();
                    }
                    else if (type.Equals(currentAttributeValue.Attribute.UI_NAME_MSUL)
                    || type.Equals(currentAttributeValue.Attribute.UI_NAME_SSUL))
                    {
                        Value = ((User)currentListValue.SelectedValue).EmailAddress;
                    }
                    else if (type.Equals(currentAttributeValue.Attribute.UI_NAME_SSGL)
                    || type.Equals(currentAttributeValue.Attribute.UI_NAME_MSGL))
                    {
                        Value = ((Starbase.CaliberRM.Interop.Group)currentListValue.SelectedValue).Name;
                    }
                }
                catch (Exception ex)
                {
                    Value = null;
                }
            }

            return Value;
        }

        /// <summary>
        ///  Strips the emails address from another string to remove other information such as full name.
        /// </summary>
        /// <returns>The emails address</returns>
        private string GetSimplifiedEmails(string emails)
        {
            StringBuilder simplifiedEmails = new StringBuilder();
            foreach (string currentEmail in emails.Split(','))
            {
                if (!string.IsNullOrEmpty(currentEmail) && currentEmail.Contains("<"))
                {
                    simplifiedEmails.Append(currentEmail.Substring(currentEmail.IndexOf('<') + 1)).Append(",");
                }
                else
                {
                    simplifiedEmails.Append(currentEmail).Append(","); ;
                }
            }
            simplifiedEmails.Replace(">", string.Empty);
            return simplifiedEmails.ToString();
        }

        /// <summary>
        /// Returns a dictionary of ALM fields. key = field Label, value = database field name
        /// </summary>
        private void LoadAlmRequiremetFieldsDictionary(List fieldList)
        {
            AlmRequirementFieldsDictionary = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
            foreach (TDField Field in fieldList)
            {
                FieldProperty fieldProp = (FieldProperty)Field.Property;
                if (!string.IsNullOrEmpty(fieldProp.UserLabel))
                {
                    AlmRequirementFieldsDictionary.Add(fieldProp.UserLabel, Field.Name);
                }
            }
        }

        /// <summary>
        /// Gets the corresponding  database field name based on the provided field Label
        /// LoadAlmRequiremetFieldsDictionary() should be called before using this method
        /// </summary>
        /// <param name="fieldName">The field Label name</param>
        /// <returns>the database field name that is mapped to the field name</returns>
        private string GetAlmDBFieldName(string fieldName)
        {
            string AlmDBFieldName = string.Empty;
            if (AlmRequirementFieldsDictionary.ContainsKey(fieldName))
            {
                AlmDBFieldName = AlmRequirementFieldsDictionary[fieldName];
            }

            return AlmDBFieldName;
        }

        /// <summary>
        /// Replaces invalid characters that are not accepted in ALM project names with an underscore
        /// </summary>
        /// <returns>A new string with replaced invalid characters</returns>
        private string Escape(string originalValue)
        {
            // Escape the following chars:      \/:"?'<>|*%
            string newChar = "_";
            StringBuilder sb = new StringBuilder(originalValue);
            sb.Replace("\\", newChar);
            sb.Replace("/", newChar);
            sb.Replace(":", newChar);
            sb.Replace("\"", newChar);
            sb.Replace("?", newChar);
            sb.Replace("'", newChar);
            sb.Replace("<", newChar);
            sb.Replace(">", newChar);
            sb.Replace("|", newChar);
            sb.Replace("*", newChar);
            sb.Replace("%", newChar);
            return sb.ToString();
        }

        /// <summary>
        ///  Gets the ALM requirement ID based on the name of the given requirement folder.
        /// </summary>
        /// <returns>The ALM Requirement ID of the given ALM Folder</returns>
        private int GetSelectedAlmFolderID(string AlmFolderPath, ReqFactory reqFactory)
        {
            int SelectedAlmFolderID = 0;
            List AlmRequirementList = reqFactory.NewList("select * from REQ where RQ_TYPE_ID in (select TPR_TYPE_ID from REQ_TYPE where TPR_NAME = 'Folder')");
            foreach (Req currentRequirement in AlmRequirementList)
            {
                if (AlmFolderPath.Equals(currentRequirement.Path))
                {
                    SelectedAlmFolderID = (int)currentRequirement.ID;
                    break;
                }
            }

            return SelectedAlmFolderID;
        }

        /// <summary>
        ///  Gets the Caliber Baseline object based on the name of the given Baseline.
        /// </summary>
        /// <returns>The Caliber Baseline object</returns>
        public Starbase.CaliberRM.Interop.Baseline GetCaliberBaseline(string CaliberProjectName, string CaliberBaselineName)
        {
            LogMessage("    Connecting to the selected CaliberRM Project... ");
            Project CurrentProject = GetCaliberProject(CaliberProjectName);
            LogMessage("    Finished CaliberRM project connection successfully.");
            LogMessage("    Loading selected CaliberRM Baseline...");
            Starbase.CaliberRM.Interop.Collection caliberBaselines = CurrentProject.Baselines;
            Starbase.CaliberRM.Interop.Baseline Baseline = null;
            foreach (Starbase.CaliberRM.Interop.Baseline currentCaliberBaseline in caliberBaselines)
            {
                if (currentCaliberBaseline.Name.Equals(CaliberBaselineName))
                {
                    Baseline = currentCaliberBaseline;
                    LogMessage("    Finished loading CaliberRM Baseline.");
                    break;
                }
            }
            SelectedCaliberBaseline = Baseline;
            return Baseline;
        }

        private Project GetCaliberProject(string CaliberProjectName)
        {
            Project currentCaliberProject = null;
            if (CaliberProjectNamesDataDictionary.ContainsKey(CaliberProjectName))
            {
                currentCaliberProject = CaliberProjectNamesDataDictionary[CaliberProjectName];
            }
            else
            {
                ProjectIDFactory ProjectIDFactory = new ProjectIDFactory();
                ProjectID currentProjectID = ProjectIDFactory.Create(CaliberProjectDataDictionary[CaliberProjectName]);
                currentCaliberProject = (Project)CaliberSession.getProject(currentProjectID);
                CaliberProjectNamesDataDictionary.Add(CaliberProjectName, currentCaliberProject);
            }
            return currentCaliberProject;
        }

        /// <summary>
        ///  Uploads an Attachment to ALM and its linked to the given Requirement
        /// </summary>
        private void UploadFileAttachment(Req AlmRequirement, string attachmentPath)
        {
            AttachmentFactory attachFact = (AttachmentFactory)AlmRequirement.Attachments;
            attachmentPath = attachmentPath.Replace("file:///", string.Empty);
            attachmentPath = attachmentPath.Replace("/", "\\");
            string path = attachmentPath.Substring(0, attachmentPath.LastIndexOf("\\"));
            string name = attachmentPath.Substring(attachmentPath.LastIndexOf("\\") + 1);
            Attachment attachObj = (Attachment)attachFact.AddItem(System.DBNull.Value);
            attachObj.Description = attachmentPath;
            attachObj.FileName = attachmentPath;
            attachObj.Type = 1; // Attachment type, 2 URL, 1 File

            // Update the attachment record in the project database.
            attachObj.Post();
            IExtendedStorage extendedStorage = (IExtendedStorage)attachObj.AttachmentStorage;

            // Specify the location of the file to upload.
            extendedStorage.ClientPath = path;

            // Use IExtendedStorage.Save to upload the file.
            extendedStorage.Save(name, true);
            extendedStorage.GetLastError();
        }

        /// <summary>
        ///  Uploads a URL Attachment to ALM
        /// </summary>
        private void UploadUrlAttachment(Req AlmRequirement, string attachmentPath, string comment)
        {
            AttachmentFactory attachFact = (AttachmentFactory)AlmRequirement.Attachments;
            attachmentPath = attachmentPath.Replace("file:///", string.Empty);
            attachmentPath = attachmentPath.Replace("/", "\\");
            Attachment attachObj = (Attachment)attachFact.AddItem(System.DBNull.Value);
            attachObj.Description = comment;
            attachObj.FileName = attachmentPath;
            attachObj.Type = 2; // Attachment type, 2 URL, 1 File

            // Update the attachment record in the project database.
            attachObj.Post();
        }

        /// <summary>
        ///  Uploads an Image to ALM and its linked to the given Requirement
        /// </summary>
        private void UploadImage(Req AlmRequirement, string attachmentPath)
        {
            AttachmentFactory attachFact = (AttachmentFactory)AlmRequirement.Attachments;
            attachmentPath = attachmentPath.Replace("file:///", string.Empty);
            attachmentPath = attachmentPath.Replace("/", "\\");
            string path = attachmentPath.Substring(0, attachmentPath.LastIndexOf("\\"));
            string name = attachmentPath.Substring(attachmentPath.LastIndexOf("\\") + 1);
            System.Drawing.Image image = System.Drawing.Image.FromFile(attachmentPath);
            object[] args = new object[4];
            args[0] = attachmentPath;
            args[1] = 1; // Attachment type, 0 URL, 1 File
            args[2] = "rich content"; // Description
            args[3] = 1; // Attachment subtype, 0 TDATT_NONE Empty subtype. 1 TDATT_RICH_CONTENT Rich content attachment.
            Attachment attachObj = (Attachment)attachFact.AddItem(args);

            // Update the attachment record in the project database.
            attachObj.Post();
            IExtendedStorage extendedStorage = (IExtendedStorage)attachObj.AttachmentStorage;

            // Specify the location of the file to upload.
            extendedStorage.ClientPath = path;

            // Use IExtendedStorage.Save to upload the file.
            extendedStorage.Save(name, true);
            extendedStorage.GetLastError();
            attachObj.Refresh();
            AlmRequirement.Comment = AlmRequirement.Comment.Replace(name + "\"", attachObj.Name + string.Format("\" height=\"{0}\" width=\"{1}\" border=\"0\" ", image.Height, image.Width));
        }

        /// <summary>
        ///  Adds a user to the ALM site and project using the Self Service Router Web services
        /// </summary>
        private void AddUser(string AlmServerName, string AlmDomainName, string AlmProjectName, string caliberUsers)
        {
            string[] args = new string[2];
            args[0] = AlmServerName;
            string email = GetSimplifiedEmails(caliberUsers);
            args[1] = email;
            if (SSRClient == null)
            {
                SSRClient = new iSelfServiceRouterClient("basicSoapEndPoint");
            }
            SSRResult ssrResult = SSRClient.SelfService("go2ALM@hp.com", "QC", "addQCUsers", args);
            if (ssrResult.ReturnCode.Equals("200"))
            {
                Customization.Commit();
                Customization.Load();
                args = new string[4];
                args[0] = AlmServerName;
                args[1] = AlmDomainName;
                args[2] = AlmProjectName;
                args[3] = email;
                ssrResult = SSRClient.SelfService("go2ALM@hp.com", "QC", "QCAddUsersToProject", args);
                if (ssrResult.ReturnCode.Equals("200"))
                {
                    Customization.Commit();
                    Customization.Load();
                }
            }
        }

        /// <summary>
        ///  Adds a value to the specified custom list.
        /// </summary>
        private bool AddValueToCustomizationList(CustomizationListNode listRoot, string value)
        {
            bool valueAdded = false;
            if (!string.IsNullOrEmpty(value))
            {
                if (listRoot.CanAddChild && listRoot.get_Child(value) == null)
                {
                    listRoot.AddChild(value);
                    valueAdded = true;
                }
            }
            return valueAdded;
        }

        private void LoadCustomization()
        {
            Customization = (Customization)AlmConnection.Customization;
            Customization.Load();
        }

        public void PerformCaliberLogin(string CaliberServerName, string CaliberUser, string CaliberPassword)
        {
            this.CaliberServerName = CaliberServerName;
            this.CaliberUser = CaliberUser;
            this.CaliberPassword = CaliberPassword;

            CaliberProjectDataDictionary = new Dictionary<string, int>();
            CaliberBaselinesDataDictionary = new Dictionary<string, string>();
            CaliberProjectNamesDataDictionary = new Dictionary<string, Project>();
            RequirementDataDictionary = new Dictionary<int, RequirementData>();

            /*The Initializer object is responsible for loading the Java VM.
            Get the current VM from the initializer and set the max memory to 512 MB...*/
            new Initializer().JavaConfiguration.CurrentJavaVM.Options = "-Xmx512M";
            CaliberServerFactory caliberServerFactory = new CaliberServerFactory();
            CaliberServer caliberServer = caliberServerFactory.Create(CaliberServerName);
            CaliberSession = caliberServer.login(CaliberUser, CaliberPassword);
        }

        public List<string> GetCaliberProjectList()
        {
            List<string> projectNameList = new List<string>();
            string JavaVersion = GetJavaVersion();
            if (JavaVersion.Contains("java version"))
            {
                //Get the Caliber Projects with the fast Java Caliber API
                string[] CaliberProjectList = null;
                string FileName = CaliberUser + "_projects_at_" + CaliberServerName + ".temp";
                System.Diagnostics.Process CaliberProjectsProcess = new System.Diagnostics.Process();
                CaliberProjectsProcess.StartInfo.FileName = "java";
                CaliberProjectsProcess.StartInfo.Arguments = "-jar ./lib/CaliberProjects.jar " + CaliberServerName + " " + CaliberUser + " " + CaliberPassword + " " + FileName;
                CaliberProjectsProcess.StartInfo.CreateNoWindow = true;
                CaliberProjectsProcess.StartInfo.UseShellExecute = false;
                CaliberProjectsProcess.Start();
                CaliberProjectsProcess.WaitForExit();

                if (System.IO.File.Exists(FileName))
                {
                    CaliberProjectList = System.IO.File.ReadAllLines(FileName);
                }
                foreach(string CurrentProject in CaliberProjectList)
                {
                    string[] ProjectData = CurrentProject.Split('|');
                    CaliberProjectDataDictionary.Add(ProjectData[1], Convert.ToInt32(ProjectData[0]));
                    CaliberBaselinesDataDictionary.Add(ProjectData[1], ProjectData[2]);
                    projectNameList.Add(ProjectData[1]);
                }
            }
            else
            {
                //Get the Caliber Projects with the slow Caliber API
                CollectionFactory CollectionFactory = new CollectionFactory();
                CaliberProjects = CollectionFactory.Create();
                Collection allProjectIDs = CaliberSession.AllProjectIDs;
                foreach (ProjectID currentProjectID in allProjectIDs)
                {
                    Project project = (Project)CaliberSession.getProject(currentProjectID);
                    CaliberProjects.Add(project);
                    View.Controls[0].Invoke(new MethodInvoker(delegate
                    {
                        View.CaliberProjectList.Items.Add(project.Name);
                    }));
                }

                for (int i = 0; i < CaliberProjects.Count; i++)
                {
                    Project currentCaliberProject = (Project)CaliberProjects[i];
                    CaliberProjectDataDictionary.Add(currentCaliberProject.Name, currentCaliberProject.ID.IDNumber);
                    StringBuilder baselines = new StringBuilder();
                    for (int j = 0; j < currentCaliberProject.Baselines.Count; j++)
                    {
                        Starbase.CaliberRM.Interop.Baseline currentBaseline = ((Starbase.CaliberRM.Interop.Baseline) currentCaliberProject.Baselines[j]);
                        baselines.Append(currentBaseline.Name).Append(",");
                    }
                    baselines.Remove(baselines.Length - 1, 1);
                    CaliberBaselinesDataDictionary.Add(currentCaliberProject.Name, baselines.ToString());
                    projectNameList.Add(currentCaliberProject.Name);
                }
            }
            projectNameList.Sort();
            return projectNameList;
        }

        private string GetJavaVersion()
        {
            string JavaVersion = string.Empty;

            System.Diagnostics.Process JavaVersionProcess = new System.Diagnostics.Process();
            JavaVersionProcess.StartInfo.FileName = "java";
            JavaVersionProcess.StartInfo.Arguments = "-version";
            JavaVersionProcess.StartInfo.UseShellExecute = false;
            JavaVersionProcess.StartInfo.RedirectStandardOutput = true;
            JavaVersionProcess.StartInfo.RedirectStandardError = true;
            JavaVersionProcess.Start();

            JavaVersion = JavaVersionProcess.StandardOutput.ReadToEnd();
            if (string.IsNullOrEmpty(JavaVersion))
            {
                JavaVersion = JavaVersionProcess.StandardError.ReadToEnd();
            }
            JavaVersionProcess.WaitForExit();
            JavaVersionProcess.Close();
            return JavaVersion;
        }

        public List<string> GetCaliberBaselineList(string CaliberProjectName)
        {
            List<string> caliberBaselineNameList = new List<string>();
            caliberBaselineNameList.AddRange(CaliberBaselinesDataDictionary[CaliberProjectName].Split(','));
            caliberBaselineNameList.Remove("Deleted View");
            caliberBaselineNameList.Sort();
            return caliberBaselineNameList;
        }

        public List<string> GetCaliberReqTypeList(string CaliberProjectName, string CaliberBaselineName)
        {
            List<string> caliberBaselineNameList = new List<string>();
            Starbase.CaliberRM.Interop.Baseline Baseline = GetCaliberBaseline(CaliberProjectName, CaliberBaselineName);
            LogMessage("    Loading list of CaliberRM Requirements...");
            Collection RequirementTypes = Baseline.RequirementTypes;
            foreach (RequirementType currentType in RequirementTypes)
            {
                caliberBaselineNameList.Add(currentType.Name);
            }
            return caliberBaselineNameList;
        }

        public List<string> GetAlmDomainList()
        {
            List<string> AlmDomainNameList = new List<string>();
            List AlmDomainList = AlmConnection.VisibleDomains;
            foreach (string currentAlmDomain in AlmDomainList)
            {
                AlmDomainNameList.Add(currentAlmDomain);
            }

            AlmDomainNameList.Sort();
            return AlmDomainNameList;
        }

        public List<string> GetAlmProjectList(string AlmDomainName)
        {
            this.AlmDomainName = AlmDomainName;
            List<string> AlmProjectNameList = new List<string>();
            List AlmProjectList = AlmConnection.get_VisibleProjects(AlmDomainName);

            foreach (string currentAlmProjectName in AlmProjectList)
            {
                AlmProjectNameList.Add(currentAlmProjectName);
            }

            AlmProjectNameList.Sort();
            return AlmProjectNameList;
        }

        public List<string> GetAlmReleaseList()
        {
            List<string> releaseList = new List<string>();
            AlmReleasesDataDictionary.Clear();
            ReleaseFactory releaseFactory = (ReleaseFactory)AlmConnection.ReleaseFactory;
            List AlmReleases = releaseFactory.NewList(string.Empty);
            foreach (Release CurrentRelease in AlmReleases)
            {
                releaseList.Add(CurrentRelease.Name);
                AlmReleasesDataDictionary.Add(CurrentRelease.Name, (int)CurrentRelease.ID);
            }

            releaseList.Sort();
            return releaseList;
        }

        public List<string> GetAlmCustomFieldList(string AlmListName)
        {
            List<string> AlmCustomFieldList = new List<string>();
            if (((CustomizationLists)Customization.Lists).IsListExist[AlmListName])
            {
                CustomizationList custList = (CustomizationList)((CustomizationLists)Customization.Lists).get_List(AlmListName);
                CustomizationListNode listRoot = (CustomizationListNode)custList.RootNode;
                List Values = listRoot.Children;
                foreach (CustomizationListNode CurrentValue in Values)
                {
                    AlmCustomFieldList.Add(CurrentValue.Name);
                }
                AlmCustomFieldList.Sort();
            }
            return AlmCustomFieldList;
        }

        public void CaliberLogOff()
        {
            if (CaliberSession != null && CaliberSession.LoggedIn)
            {
                try
                {
                    CaliberSession.logout();
                }
                catch (Exception ex)
                {
                }
            }
        }

        public void AlmLogOff()
        {
            if (AlmConnection != null)
            {
                try
                {
                    if (AlmConnection.Connected)
                    {
                        AlmConnection.Disconnect();
                    }

                    if (AlmConnection.LoggedIn)
                    {
                        AlmConnection.Logout();
                    }

                    AlmConnection.ReleaseConnection();
                }
                catch (Exception ex)
                {
                }
            }
        }

        public System.Windows.Forms.TreeNode GetAlmFolderList()
        {
            System.Windows.Forms.TreeNode AlmFolderList = new System.Windows.Forms.TreeNode();
            ReqFactory ReqFactory = (ReqFactory)AlmConnection.ReqFactory;
            //apply filter to loop only over Folder Requirements
            List AlmRequirementList = ReqFactory.NewList("select * from REQ where RQ_TYPE_ID in (select TPR_TYPE_ID from REQ_TYPE where TPR_NAME = 'Folder')");
            foreach (Req currentRequirement in AlmRequirementList)
            {
                string path = currentRequirement.Path;
                string[] folderNames = path.Split(new char[] { '\\' });
                string[] fullPathArray = new string[folderNames.Count()];
                string tempFullPath = folderNames[0];
                System.Windows.Forms.TreeNode[] nodeArray = AlmFolderList.Nodes.Find(folderNames[0], true);
                System.Windows.Forms.TreeNode parentNode = null;
                if (nodeArray != null && nodeArray.Count() > 0)
                {
                    parentNode = nodeArray[0];
                }

                if (parentNode == null)
                {
                    parentNode = AlmFolderList.Nodes.Add(folderNames[0], folderNames[0]);
                }

                for (int i = 1; i < folderNames.Count(); i++)
                {
                    tempFullPath += folderNames[i];
                    fullPathArray[i] = tempFullPath;
                    nodeArray = AlmFolderList.Nodes.Find(fullPathArray[i], true);
                    if (nodeArray == null || nodeArray.Count() == 0)
                    {
                        parentNode = parentNode.Nodes.Add(fullPathArray[i], folderNames[i]);
                    }
                    else
                    {
                        parentNode = nodeArray[0];
                    }
                }
            }
            return AlmFolderList.FirstNode;
        }

        /// <summary>
        ///  Calls the MigrateRequirementDetails method on the root requirement.
        /// </summary>
        private void MigrateAllRequirementDetails()
        {
            Collection RequirementTypes = SelectedCaliberBaseline.RequirementTypes;
            foreach (RequirementType currentType in RequirementTypes)
            {
                try
                {
                    Req AlmRequirement = (Req)((ReqFactory)AlmConnection.ReqFactory).AddItem(System.DBNull.Value);
                    AlmRequirement.ParentId = AlmRequirementID;
                    AlmRequirement[GetAlmDBFieldName("BP Filter")] = "HP";
                    AlmRequirement[GetAlmDBFieldName("Requirement Type")] = 1; // Folder
                    AlmRequirement.Name = currentType.Tag;
                    AlmRequirement.Post();
                    int reqID = (int)AlmRequirement.ID;
                    AlmParentIdDataDictionary.Add(currentType.Name, reqID);
                }
                catch (Exception ex)
                {
                    List reqList = ((ReqFactory)AlmConnection.ReqFactory).NewList("select * from REQ "
                        + "where RQ_REQ_NAME = '" + currentType.Tag + "' and RQ_FATHER_ID = " + AlmRequirementID);
                    foreach (Req currentReq in reqList)
                    {
                        AlmParentIdDataDictionary.Add(currentType.Name, (int)currentReq.ID);
                    }
                    if (!ex.Message.Contains("Duplicate requirement name"))
                    {
                        LogMessage(ex.Message);
                    }
                }
            }
            MigrateRequirementDetails((RequirementTreeNode)SelectedCaliberBaseline.RequirementTree.Root, AlmRequirementID);
        }

        /// <summary>
        ///  Calls the MigrateTraces method on the root requirement.
        /// </summary>
        private void MigrateAllTraces()
        {
            LogMessage("        Migrating requirement traces... (Only traces between requirements from the same project are Migrateed, traces to other projects or external objects will not be Migrated.)");
            try
            {
                MigrateReqTraces((RequirementTreeNode)SelectedCaliberBaseline.RequirementTree.Root);
            }
            catch (Exception ex)
            {
                LogMessage("            Failed migrating traces. " + ex.Message);
            }
            LogMessage("        Finished migrating requirement traces.");
        }

        /// <summary>
        ///  Migrates all the missing custom list values of each requirement recursively.
        /// </summary>
        private void LoadListValues(RequirementTreeNode currentNode)
        {
            LogMessage("    Loading Caliber Requirement...");
            CaliberObjectID id = currentNode.AssociatedObjectID;
            LogMessage("        Caliber Requirement ID is: " + id.IDNumber);
            CaliberObject CaliberObject = CaliberSession.get(id);
            LogMessage("    Finished loading Caliber Requirement: " + CaliberObject.Name);
            if (CaliberObject is Requirement)
            {
                Requirement CaliberRequirement = (Requirement)CaliberObject;
                try
                {
                    LoadRequirementListValues(CaliberRequirement);
                }
                catch (Exception ex)
                {
                    LogMessage("    Failed migrating requirement list values... " + CaliberRequirement.Name + ". " + ex.Message);
                }
            }

            Collection childrenNodes = currentNode.Children;
            foreach (RequirementTreeNode children in childrenNodes)
            {
                LoadListValues(children);
            }
        }

        /// <summary>
        ///  Migrates the details of each requirement recursively, handling the logic of the parent ID to preserve hierarchy.
        /// </summary>
        private void MigrateRequirementDetails(RequirementTreeNode currentNode, int parentNodeID)
        {
            int childRequirementID = AlmRequirementID;
            CaliberObjectID id = currentNode.AssociatedObjectID;
            CaliberObject CaliberObject = CaliberSession.get(id);
            if (CaliberObject is Requirement)
            {
                Requirement CaliberRequirement = (Requirement)CaliberObject;
                int reqID = AlmRequirementID;
                try
                {
                    reqID = MigrateCaliberRequirement(CaliberRequirement, parentNodeID);
                    childRequirementID = reqID;
                }
                catch (Exception ex)
                {
                    LogMessage("    Failed migrating requirement... " + CaliberRequirement.Name + ". " + ex.Message);
                    reqID = AlmRequirementID;
                }
            }

            Collection childrenNodes = currentNode.Children;
            foreach (RequirementTreeNode children in childrenNodes)
            {
                MigrateRequirementDetails(children, childRequirementID);
            }
        }

        /// <summary>
        ///  Migrates the traces of each requirement recursively.
        /// </summary>
        private void MigrateReqTraces(RequirementTreeNode currentNode)
        {
            CaliberObjectID id = currentNode.AssociatedObjectID;
            CaliberObject CaliberObject = CaliberSession.get(id);
            if (CaliberObject is Requirement)
            {
                Requirement CaliberRequirement = (Requirement)CaliberObject;
                MigrateCaliberRequirementTraces(CaliberRequirement);
            }

            Collection childrenNodes = currentNode.Children;
            foreach (RequirementTreeNode children in childrenNodes)
            {
                MigrateReqTraces(children);
            }
        }

        /// <summary>
        ///  Migrates the custom list values of the given Requirement.
        /// </summary>
        private void LoadRequirementListValues(Requirement CaliberRequirement)
        {
            AttributeValue currentAttributeValue = null;
            string currentAttributeName = null;
            string value = null;

            UserList.Add(CaliberRequirement.Owner.EmailAddress);
            LogMessage("        Loading list of values from requirement " + CaliberRequirement.Name + "...");
            for (int i = 0; i < CaliberRequirement.AttributeValues.Count; i++)
            {
                currentAttributeValue = (AttributeValue)CaliberRequirement.AttributeValues[i];
                currentAttributeName = currentAttributeValue.Attribute.Name;

                string type = currentAttributeValue.Attribute.UITypeName;
                if (type.Equals(currentAttributeValue.Attribute.UI_NAME_MLTF) || type.Equals(currentAttributeValue.Attribute.UI_NAME_STL))
                {
                    value = ((UDATextValue)currentAttributeValue).Value;
                }
                else if (type.Equals(currentAttributeValue.Attribute.UI_NAME_LONG_INTEGER))
                {
                    value = ((UDAIntegerValue)currentAttributeValue).Value.ToString();
                }
                else if (currentAttributeValue is IUDAListValue)
                {
                    value = GetListValue(currentAttributeValue, type);
                }

                if (!string.IsNullOrEmpty(value))
                {
                    string listValue = GetSimplifiedEmails(value).Replace("@", "_");
                    string listName = string.Empty;
                    switch (currentAttributeName)
                    {
                        case "Programmer":
                            listName = "_RQ_PROGRAMMER";
                            break;
                        case "Review A":
                            listName = "_RQ_REVIEW";
                            break;
                        case "Review B":
                            listName = "_RQ_REVIEW";
                            break;
                        case "Reviewed By":
                            listName = "_RQ_REVIEWED_BY";
                            break;
                        case "Sign off":
                            listName = "_RQ_SIGNOFF";
                            break;
                        case "Impacted product interface":
                            listName = "_SH_PRODUCT";
                            listValue = value;
                            break;
                        default:
                            continue;
                    }

                    if (!AlmCustomListDictionary.ContainsKey(listName))
                    {
                        AlmCustomListDictionary.Add(listName, new HashSet<string>());
                    }

                    AlmCustomListDictionary[listName].Add(listValue);
                }
            }

            if (!AlmCustomListDictionary.ContainsKey("_SH_PPM"))
            {
                AlmCustomListDictionary.Add("_SH_PPM", new HashSet<string>());
            }
            AlmCustomListDictionary["_SH_PPM"].Add(AlmCustomFieldsValues[0]);

            if (!AlmCustomListDictionary.ContainsKey("_SH_PRODUCT"))
            {
                AlmCustomListDictionary.Add("_SH_PRODUCT", new HashSet<string>());
            }
            AlmCustomListDictionary["_SH_PRODUCT"].Add(AlmCustomFieldsValues[1]);

            if (!AlmCustomListDictionary.ContainsKey("_SH_PRODUCT_WS"))
            {
                AlmCustomListDictionary.Add("_SH_PRODUCT_WS", new HashSet<string>());
            }
            AlmCustomListDictionary["_SH_PRODUCT_WS"].Add(AlmCustomFieldsValues[1]);
            LogMessage("        Finished loading list of values from requirement: " + CaliberRequirement.Name);
        }

        /// <summary>
        ///  Migrates all data of an specified requirement.
        /// </summary>
        /// <returns>The newly created ALM Requierement ID </returns>
        private int MigrateCaliberRequirement(Requirement CaliberRequirement, int parentAlmRequirementID)
        {
            Req AlmRequirement = null;
            RequirementData requirementData = new RequirementData();
            AttributeValue currentAttributeValue = null;
            int reqID = 0;
            string CRID = string.Empty;
            string CRDomain = string.Empty;
            string CRProject = string.Empty;
            LogMessage("    Started  migrating requirement: " + CaliberRequirement.Name);
            requirementData.CaliberName = CaliberRequirement.Name;
            requirementData.CaliberID = CaliberRequirement.IDNumber;
            AlmRequirement = (Req)((ReqFactory)AlmConnection.ReqFactory).AddItem(System.DBNull.Value);
            int parentID = parentAlmRequirementID;
            try
            {
                if (parentID == AlmRequirementID)
                {
                    parentID = AlmParentIdDataDictionary[CaliberRequirement.RequirementType.Name];
                }
            }
            catch (Exception ex)
            {
                LogMessage(ex.Message);
                parentID = parentAlmRequirementID;
            }

            AlmRequirement.ParentId = parentID;
            HTMLHelperFactory HTMLHelperFactory = new HTMLHelperFactory();
            HTMLHelper HTMLHelper = HTMLHelperFactory.Create();
            string correctedDescription = CaliberRequirement.Description.Text.Replace(@"xmlns=""http://www.w3.org/1999/xhtml""", string.Empty);
            correctedDescription = HTMLHelper.tidy(correctedDescription);
            correctedDescription = Regex.Replace(correctedDescription, "(<head.+/head>)|(<head.*?/>)", string.Empty);
            correctedDescription = Regex.Replace(correctedDescription, "<s*img(.+?)src=\"file://[^\"]+\"(.+?)src_original=\"([^\"]+)\"(.+?)/>", "<img$1src=\"file://[IMAGE_BASE_PATH_PLACEHOLDER]$3\"$2$4/>");
            requirementData.Description = correctedDescription;
            AlmRequirement.Comment = requirementData.Description;

            MappingField field = null;
            string requirementType = CaliberRequirement.RequirementType.Tag;

            /*
            // Rich Text Field
            try
            {
                field = mapping.FindSystemMapping("description", CaliberRequirement.RequirementType.Tag);
                AlmRequirement[GetAlmDBFieldName(field.AlmField)] = field.ApplyTransformToValue(requirementData.Description);
            }
            catch (MappingException ex)
            {
                //TODO: Handle Mapping warnings
                LogMessage("        " + ex.Message);
            }
            */

            // BP Filter Field
            try
            {
                field = mapping.FindSystemMapping("bp_filter", requirementType);
                AlmRequirement[GetAlmDBFieldName(field.AlmField)] = field.ApplyTransformToValue(null);
            }
            catch (MappingException ex)
            {
                //TODO: Handle Mapping warnings
                LogMessage("        " + ex.Message);
            }

            // Required Type Field
            try
            {
                //TODO: Fix the Requirement Types
                field = mapping.FindSystemMapping("requirement_type", requirementType);
                AlmRequirement[GetAlmDBFieldName(field.AlmField)] = field.ApplyTransformToValue(null);
            }
            catch (MappingException ex)
            {
                //TODO: Handle Mapping warnings
                LogMessage("        " + ex.Message);
            }

            string name = Escape(CaliberRequirement.Name);
            AlmRequirement.Name = name;
            requirementData.AlmName = name;

            /*
            //TODO: HANDLE N CUSTOM FIELDS AND USE CUSTOM FIELD INSTEAD
            //foreach custom field
            // PPM ID Field
            try
            {
                field = mapping.FindCustomMapping("PPM id", requirementType);
                if (!string.IsNullOrEmpty(requirementType) && !requirementType.Equals("TR"))
                {
                    AlmRequirement[GetAlmDBFieldName(field.AlmField)] = field.ApplyTransformToValue(AlmCustomFieldsValues[0]);
                }
            }
            catch (MappingException ex)
            {
                //TODO: Handle Mapping warnings
                LogMessage("        " + ex.Message);
            }
            catch (Exception ex)
            {
                LogMessage("        Failed while trying to set the PPM ID. Requirement: " + CaliberRequirement.Name + ". PPM ID: " + AlmCustomFieldsValues[0] + ex.Message);
            }

            // Product Field
            try
            {
                field = mapping.FindCustomMapping("product", requirementType);
                AlmRequirement[GetAlmDBFieldName(field.AlmField)] = field.ApplyTransformToValue(AlmCustomFieldsValues[1]);
            }
            catch (MappingException ex)
            {
                //TODO: Handle Mapping warnings
                LogMessage("        " + ex.Message);
            }
            catch (Exception ex)
            {
                LogMessage("        Failed while trying to set the Product. Requirement: " + CaliberRequirement.Name + ". Product: " + AlmCustomFieldsValues[1] + ex.Message);
            }

            */

            // Target Release Field
            try
            {
                field = mapping.FindSystemMapping("target_release", requirementType);
                AlmRequirement[GetAlmDBFieldName(field.AlmField)] = field.ApplyTransformToValue(AlmReleasesDataDictionary[AlmReleaseName].ToString());
            }
            catch (MappingException ex)
            {
                //TODO: Handle Mapping warnings
                LogMessage("        " + ex.Message);
            }
            catch (Exception ex)
            {
                LogMessage("        Failed while trying to set the Target Release. Requirement: " + CaliberRequirement.Name + ". Target Release: " + AlmReleaseName + ex.Message);
            }

            // Author Field
            try
            {
                field = mapping.FindSystemMapping("author", requirementType);
                AlmRequirement[GetAlmDBFieldName(field.AlmField)] = field.ApplyTransformToValue(AlmConnection.UserName);
            }
            catch (MappingException ex)
            {
                //TODO: Handle Mapping warnings
                LogMessage("        " + ex.Message);
            }
            catch (Exception ex)
            {
                LogMessage("        Failed while trying to set the Author. Requirement: " + CaliberRequirement.Name + ". Author: " + AlmConnection.UserName + ex.Message);
            }

            // Status Field
            try
            {
                field = mapping.FindSystemMapping("status", requirementType);
                AlmRequirement[GetAlmDBFieldName(field.AlmField)] = field.ApplyTransformToValue(CaliberRequirement.Status.SelectedValue.ToString());
            }
            catch (MappingException ex)
            {
                //TODO: Handle Mapping warnings
                LogMessage("        " + ex.Message);
            }

            // Priority Field
            try
            {
                field = mapping.FindSystemMapping("priority", requirementType);
                AlmRequirement[GetAlmDBFieldName(field.AlmField)] = field.ApplyTransformToValue(CaliberRequirement.Priority.SelectedValue.ToString());
            }
            catch (MappingException ex)
            {
                //TODO: Handle Mapping warnings
                LogMessage("        " + ex.Message);
            }

            // Owner Field
            try
            {
                field = mapping.FindSystemMapping("owner", requirementType);
                AlmRequirement[GetAlmDBFieldName(field.AlmField)] = field.ApplyTransformToValue(CaliberRequirement.Owner.EmailAddress);
            }
            catch (MappingException ex)
            {
                //TODO: Handle Mapping warnings
                LogMessage("        " + ex.Message);
            }
            catch (Exception ex)
            {
                LogMessage("        Failed while trying to set the Owner, The user in Caliber Requirement does not exist in your ALM project.");
                LogMessage("        You will need to add the user into your ALM project and set the Owner value manually. Requirement: " + CaliberRequirement.Name + ". Owner: " + CaliberRequirement.Owner.EmailAddress + ex.Message);
            }

            // Legacy ID
            try
            {
                field = mapping.FindSystemMapping("legacy_id", requirementType);
                AlmRequirement[GetAlmDBFieldName(field.AlmField)] = field.ApplyTransformToValue(requirementType + CaliberRequirement.RequirementID.IDNumber);
            }
            catch (MappingException ex)
            {
                //TODO: Handle Mapping warnings
                LogMessage("        " + ex.Message);
            }

            // Legacy Project
            try
            {
                field = mapping.FindSystemMapping("legacy_project", requirementType);
                AlmRequirement[GetAlmDBFieldName(field.AlmField)] = field.ApplyTransformToValue(CaliberRequirement.Project.Name);
            }
            catch (MappingException ex)
            {
                //TODO: Handle Mapping warnings
                LogMessage("        " + ex.Message);
            }

            // Comment Variable
            StringBuilder AlmComments = new StringBuilder("<html><body>");

            // Attributes
            for (int i = 0; i < CaliberRequirement.AttributeValues.Count; i++)
            {
                currentAttributeValue = (AttributeValue)CaliberRequirement.AttributeValues[i];
                string value = null;
                string attributeName = currentAttributeValue.Attribute.Name;

                // string type = ((AttributeValue)CaliberRequirement.AttributeValues[i]).Attribute.UITypeName;
                value = currentAttributeValue.GetAttributeValue(this);

                if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(attributeName))
                {
                    try
                    {
                        switch (attributeName)
                        {
                            case "Comments":
                                AlmComments.Append(value);//HTMLHelper.htmlToPlainText(value));
                                break;
                            case "CR ID":
                                CRID = "CR ID: " + value + Environment.NewLine;
                                break;
                            case "CR ALM Domain":
                                CRDomain = "CR ALM Domain: " + value + Environment.NewLine;
                                break;
                            case "CR ALM Project":
                                CRProject = "CR ALM Project: " + value + Environment.NewLine;
                                break;
                            default:
                                field = mapping.FindCustomMapping(attributeName, currentAttributeValue.Requirement.RequirementType.Tag);
                                if (string.IsNullOrEmpty(field.Skip) || !field.Skip.Equals("True"))
                                {
                                    AlmRequirement[GetAlmDBFieldName(field.AlmField)] = field.ApplyTransformToValue(value);
                                }
                                break;
                        }
                    }
                    catch (MappingException ex)
                    {
                        //TODO: Handle Mapping warnings
                        LogMessage("        " + ex.Message);
                    }
                    catch (Exception ex)
                    {
                        LogMessage("        Failed while trying to set the " + attributeName + ". Requirement: " + CaliberRequirement.Name + ". " + attributeName + " value: " + value + ". Exception details: " + ex.Message);
                    }
                }
            }

            if (!string.IsNullOrEmpty(CRID) || !string.IsNullOrEmpty(CRDomain) || !string.IsNullOrEmpty(CRProject))
            {
                AlmComments.Append("<span style=\"font-size:8pt;color:#000080\"><b>"
                    + "<br><br>________________________________________________________________________________________________"
                    + "<br>The Followong information was used to reference approved RCRs in ALM:</b></span>"
                    + "<br>&nbsp;&nbsp;&nbsp;&nbsp;CR ID:      " + CRID
                    + "<br>&nbsp;&nbsp;&nbsp;&nbsp;CR Domain:  " + CRDomain
                    + "<br>&nbsp;&nbsp;&nbsp;&nbsp;CR Project: " + CRProject
                    + "<br>________________________________________________________________________________________________<br><br>");
            }

            if (CaliberRequirement.Discussion != null)
            {
                Collection messages = CaliberRequirement.Discussion.Messages;
                if (messages.Count > 0)
                {
                    StringBuilder discussionMessagesText = new StringBuilder();
                    discussionMessagesText.Append("<span style=\"font-size:8pt;color:#000080\"><b>"
                        + "<br><br>____________________________________________________________________________________________________"
                        + "<br>Requirement Discussions:</b></span>");
                    foreach (DiscussionMessage currentMessage in messages)
                    {
                        int tabSize = currentMessage.Depth * 4;
                        string indent = string.Empty.PadLeft(tabSize);
                        indent.Replace(" ", "&nbsp;");
                        discussionMessagesText.Append("<br>" + indent + "________________________________________________________________________________________________");
                        discussionMessagesText.Append("<br>" + indent + "Subject: " + currentMessage.Subject);
                        discussionMessagesText.Append("<br>" + indent + "Date: " + string.Format("{0:yyyy-MM-dd\tHH:mm:ss}", DateTime.FromOADate(currentMessage.OLEDate)));
                        discussionMessagesText.Append("<br>" + indent + "Body: " + currentMessage.Body);
                        discussionMessagesText.Append("<br>" + indent + "________________________________________________________________________________________________");
                    }

                    discussionMessagesText.Append("<br>____________________________________________________________________________________________________<br><br>");
                    AlmComments.Append(discussionMessagesText);
                }
            }

            AlmRequirement.Post();
            if (CaliberRequirement.DocumentReferences != null)
            {
                Collection references = CaliberRequirement.DocumentReferences;
                if (references.Count > 0)
                {
                    StringBuilder requirementReferencesText = new StringBuilder();
                    requirementReferencesText.Append("<span style=\"font-size:8pt;color:#000080\"><b>"
                        + "<br><br><br><br>____________________________________________________________________________________________________"
                        + "<br>Requirement References:</b></span>");
                    foreach (DocumentReference currentReference in references)
                    {
                        requirementReferencesText.Append("<br>&nbsp;&nbsp;&nbsp;&nbsp;________________________________________________________________________________________________");
                        if (currentReference is TextReference)
                        {
                            TextReference textReference = (TextReference)currentReference;
                            requirementReferencesText.Append("<br>&nbsp;&nbsp;&nbsp;&nbsp;Text Reference: " + textReference.Text);
                        }
                        else if (currentReference is WebReference)
                        {
                            WebReference webReference = (WebReference)currentReference;
                            requirementReferencesText.Append("<br>&nbsp;&nbsp;&nbsp;&nbsp;Web  Reference: " + webReference.URL);
                            try
                            {
                                LogMessage("        Uploading URL attachment with Path: " + webReference.URL);
                                UploadUrlAttachment(AlmRequirement, webReference.URL, webReference.URL);
                            }
                            catch (Exception ex)
                            {
                                LogMessage("        Failed to upload URL attachment: " + webReference.URL + ". Exception Description: " + ex.Message);
                            }
                        }
                        else if (currentReference is FileReference)
                        {
                            FileReference fileReference = (FileReference)currentReference;
                            requirementReferencesText.Append("<br>&nbsp;&nbsp;&nbsp;&nbsp;File Reference: " + fileReference.Path);
                            try
                            {
                                LogMessage("        Uploading attachment with Path: " + fileReference.Path);
                                UploadFileAttachment(AlmRequirement, fileReference.Path);
                            }
                            catch (Exception ex)
                            {
                                LogMessage("        Failed to upload attachment: " + fileReference.Path + ". Exception Description: " + ex.Message);
                            }
                        }
                        else
                        {
                            requirementReferencesText.Append("<br>&nbsp;&nbsp;&nbsp;&nbsp;Reference: " + currentReference.ToString());
                        }

                        requirementReferencesText.Append("<br>&nbsp;&nbsp;&nbsp;&nbsp;________________________________________________________________________________________________");
                    }

                    requirementReferencesText.Append("<br>____________________________________________________________________________________________________<br><br>");
                    AlmComments.Append(requirementReferencesText);
                }
            }
            AlmComments.Append("</body></html>");

            // Comments
            try
            {
                field = mapping.FindSystemMapping("comments", requirementType);
                AlmRequirement[GetAlmDBFieldName(field.AlmField)] = field.ApplyTransformToValue(AlmComments.ToString());
            }
            catch (MappingException ex)
            {
                //TODO: Handle Mapping warnings
                LogMessage("        " + ex.Message);
            }

            // Images
            CaliberSession.ImageManager.populateCache3(CaliberRequirement.Description);
            Collection imagePaths = HTMLHelper.getImagePaths(CaliberRequirement.Description.Text);
            foreach (string imagePath in imagePaths)
            {
                try
                {
                    LogMessage("        Uploading image with Path: " + imagePath);
                    UploadImage(AlmRequirement, imagePath);
                }
                catch (Exception ex)
                {
                    LogMessage("        Failed to upload image: " + imagePath + ". Exception Description: " + ex.Message);
                }
            }

            // Rich Text
            try
            {
                field = mapping.FindSystemMapping("rich_text", requirementType);
                AlmRequirement[GetAlmDBFieldName(field.AlmField)] = field.ApplyTransformToValue(requirementData.Description);
            }
            catch (MappingException ex)
            {
                //TODO: Handle Mapping warnings
                LogMessage("        " + ex.Message);
            }
            AlmRequirement.Post();
            reqID = (int)AlmRequirement.ID;
            requirementData.AlmID = reqID;
            RequirementDataDictionary.Add(requirementData.CaliberID, requirementData);
            LogMessage("    Finished migrating requirement successfully: " + CaliberRequirement.Name);
            return reqID;
        }

        /// <summary>
        ///  Migrates all traces of an specified requirement.
        /// </summary>
        private void MigrateCaliberRequirementTraces(Requirement CaliberRequirement)
        {
            int AlmFromReqID;
            Req AlmFromRequirement;
            int AlmToReqID = 0;
            ReqTraceFactory AlmReqTraceFactoryFrom;
            Collection caliberRequirementTracesTo;
            try
            {
                AlmFromReqID = RequirementDataDictionary[CaliberRequirement.IDNumber].AlmID;
                AlmFromRequirement = (Req)((ReqFactory)AlmConnection.ReqFactory)[AlmFromReqID];

                // TDOLE_TRACED_FROM 0    -    TDOLE_TRACED_TO 1
                AlmReqTraceFactoryFrom = (ReqTraceFactory)AlmFromRequirement.ReqTraceFactory[1];
                caliberRequirementTracesTo = CaliberRequirement.TracesTo;
                foreach (trace currentCaliberRequirementTracedTo in caliberRequirementTracesTo)
                {
                    try
                    {
                        CaliberObjectID toId = currentCaliberRequirementTracedTo.TraceToID;
                        CaliberObject to = CaliberSession.IntegrationManager.get(toId);
                        if (to == null)
                        {
                            to = currentCaliberRequirementTracedTo.ToObject;
                        }

                        if (to is IntegrationObject && ((IntegrationObject)to).IntegrationName.Contains("StarTeam") && this.MigrateTraces)
                        {
                            StarTeam.StInitializer StInit = new StarTeam.StInitializer();
                            StarTeam.IStJavaConfiguration StJavaConfig = StInit.JavaConfiguration;
                            StJavaConfig.CurrentJavaVM.Options = "-Xmx128M";

                            IntegrationObject extObject = (IntegrationObject)to;
                            string[] values = extObject.IntegrationContextID.Split(':');
                            string stServerUrl = values[0];
                            int stPort = Convert.ToInt32(values[1]);
                            int stProjectId = Convert.ToInt32(values[2]);
                            int stViewId = Convert.ToInt32(values[3]);
                            string stType = values[4];
                            int stFileId = Convert.ToInt32(values[5]);
                            string stUser = CaliberSession.User.Name;
                            string stPassword = AlmPassword;
                            StServerFactory starTeamServerFactory = new StServerFactory();
                            StServer stServer = starTeamServerFactory.Create(stServerUrl, stPort);
                            stServer.connect();
                            stServer.logOn(stUser, stPassword);
                            StCollection starTeamProjects = stServer.Projects;
                            foreach (StProject currentProject in starTeamProjects)
                            {
                                if (currentProject.id == stProjectId)
                                {
                                    StCollection starTeamViews = currentProject.Views;
                                    foreach (StView currentView in starTeamViews)
                                    {
                                        if (currentView.id == stViewId)
                                        {
                                            string isvnRepository = ISVNRepository;
                                            if (!isvnRepository.EndsWith("/"))
                                            {
                                                isvnRepository += "/";
                                            }

                                            StFile stFile = (StFile)currentView.findItem(stServer.typeForName(stType), stFileId);
                                            if (stFile != null)
                                            {
                                                string isvnPath = isvnRepository + stFile.ParentFolderHierarchy.Replace('\\', '/') + stFile.Name;
                                                if (UrlExists(isvnPath))
                                                {
                                                    string comment = "iSVN     file  path : " + isvnPath
                                                        + Environment.NewLine + "StarTeam trace path : starteam://" + stServerUrl + ":" + stPort + "/" + currentProject.Name + "/" + currentView.Name + "/" + stFile.ParentFolderHierarchy.Replace('\\', '/') + stFile.Name
                                                        + Environment.NewLine + "StarTeam trace Revision : " + stFile.RevisionNumber;
                                                    UploadUrlAttachment(AlmFromRequirement, isvnPath, comment);
                                                    AlmFromRequirement[GetAlmDBFieldName("Comments")] +=
                                                        "<span style=\"font-size:8pt;color:#000080\"><b>_________________________________________________________________________________________________________<br></b></span>"
                                                        + "<span style=\"font-size:8pt;color:#000080\"><b>Go2Alm - Caliber Migration Tool. SVN automated comment on behalf of " + AlmUser + ", " + string.Format("{0:yyyy-MM-dd  HH:mm:ss}", DateTime.Now.ToUniversalTime() + "<br></b></span>")
                                                        + "<span style=\"font-size:8pt;color:#000080\"><b>Trace Migration messages: </b></span>"
                                                        + "The following traces from Caliber RM to StarTeam migrated successfully as a new URL Attachment to an iSVN file.<br>"
                                                        + "<span style=\"font-size:8pt;color:#000080\"><b>iSVN     file  path : </b></span>" + isvnPath
                                                        + "<span style=\"font-size:8pt;color:#000080\"><br><b>_________________________________________________________________________________________________________<br></b></span>";
                                                    AlmFromRequirement.Post();
                                                }
                                                else
                                                {
                                                    LogMessage("            Failed migrating trace from Requirement: " + CaliberRequirement.Name + " to iSVN Path: " + isvnPath + ". The iSVN file does not exist yet, make sure you have migrated all the source code to iSVN.");
                                                }
                                            }
                                            else
                                            {
                                                LogMessage("            Failed migrating trace from Requirement: " + CaliberRequirement.Name + " to iSVN View Path: " + currentView.Path + ". File ID: " + stFileId + ". The StarTeam file does not exist anymore, It may have moved or deleted after the trace was created.");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else if (to is IntegrationObject && !((IntegrationObject)to).IntegrationName.Contains("StarTeam"))
                        {
                            // trace to IntegrationObject but not StarTeam trace.
                            //IGNORE <Quality Center> but warn about others
                            //TODO: Migrate <Quality Center> traces
                            if (!((IntegrationObject)to).IntegrationName.Contains("Quality Center"))
                            {
                                LogMessage("            Trace from Requirement: " + CaliberRequirement.Name + " to external <" + ((IntegrationObject)to).IntegrationName + "> object will not be Migrateed.");
                            }
                        }
                        else if (! (to is IntegrationObject))
                        {
                            if (to is Starbase.CaliberRM.Interop.ExternalObject)
                            {
                                LogMessage("--------Trace is an ExternalObject");
                            }
                            if (to is Starbase.CaliberRM.Interop.File)
                            {
                                LogMessage("--------Trace is a File");
                            }
                            if (to is Starbase.CaliberRM.Interop.SCMFile)
                            {
                                LogMessage("--------Trace is a SCMFile");
                            }
                            if (to is Starbase.CaliberRM.Interop.XGenericObject)
                            {
                                LogMessage("--------Trace is a XGenericObject");
                            }
                            
                            try
                            {
                                AlmToReqID = currentCaliberRequirementTracedTo.TraceToID.IDNumber;
                                AlmToReqID = RequirementDataDictionary[currentCaliberRequirementTracedTo.TraceToID.IDNumber].AlmID;
                                Req AlmToRequirement = (Req)((ReqFactory)AlmConnection.ReqFactory)[AlmToReqID];
                                Trace trace = (Trace)AlmReqTraceFactoryFrom.AddItem(AlmToRequirement);
                                AlmFromRequirement.Post();
                                trace.Post();
                                trace.Refresh();
                            }
                            catch (Exception ex)
                            {
                                if (ex.Message.Contains("The given key was not present in the dictionary"))
                                {
                                    if (to is Starbase.CaliberRM.Interop.Requirement)
                                    {
                                        MigrateCaliberRequirement((Starbase.CaliberRM.Interop.Requirement) to, AlmFromReqID);
                                        try
                                        {
                                            AlmToReqID = currentCaliberRequirementTracedTo.TraceToID.IDNumber;
                                            AlmToReqID = RequirementDataDictionary[currentCaliberRequirementTracedTo.TraceToID.IDNumber].AlmID;
                                            Req AlmToRequirement = (Req)((ReqFactory)AlmConnection.ReqFactory)[AlmToReqID];
                                            Trace trace = (Trace)AlmReqTraceFactoryFrom.AddItem(AlmToRequirement);
                                            AlmFromRequirement.Post();
                                            trace.Post();
                                            trace.Refresh();
                                        }
                                        catch (Exception ex2)
                                        {
                                            LogMessage("            Failed migrating trace from requirement: " + CaliberRequirement.Name + ". Exception details: " + ex2.Message);
                                        }
                                    }
                                }
                                else
                                {
                                    LogMessage("            Failed migrating trace from requirement: " + CaliberRequirement.Name + ". Exception details: " + ex.Message);
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        //if (!ex.Message.Contains("The given key was not present in the dictionary"))
                        //{
                            LogMessage("            Failed migrating trace from requirement: " + CaliberRequirement.Name + " to [ID]: " + AlmToReqID + ". Exception details: " + ex.Message);
                        //}
                    }
                }

            }
            catch (Exception ex)
            {
                LogMessage("            Failed migrating traces for requirement " + CaliberRequirement.Name + ". " + ex.Message);
            }
        }

        public void MigrateRequirements()
        {
            SelectedCaliberBaseline = GetCaliberBaseline(CaliberProjectName, CaliberBaselineName);
            LogMessage("    Loading CaliberRM Requirements...");
            Collection CaliberRequirementsList = SelectedCaliberBaseline.RequirementTypes;
            LogMessage("    Finished loading CaliberRM Requirements.");
            if (CaliberRequirementsList.Count > 0)
            {
                PerformAlmProjectConnection(AlmDomainName, AlmProjectName);
                List fieldList = ((ReqFactory)AlmConnection.ReqFactory).Fields;
                LoadAlmRequiremetFieldsDictionary(fieldList);
                AlmRequirementID = GetSelectedAlmFolderID(AlmFolderPath, (ReqFactory)AlmConnection.ReqFactory);

                RequirementDataDictionary.Clear();
                AlmParentIdDataDictionary.Clear();

                UserList.Clear();
                AlmCustomListDictionary.Clear();

                LoopBaselineRequirements();
                LoopBaselineRequirementsByType();
                LoopBaselineRequirementsByTree();

                //MigrateAllListValues();
                //MigrateAllRequirementDetails();
                //MigrateAllTraces();
            }
        }

        private string GetDataTypeName(int Type)
        {
            string TypeName = string.Empty;
            switch ((TDAPI_DATATYPES)Type)
            {
                case TDAPI_DATATYPES.TDOLE_LONG:
                    TypeName = "Long integer";
                    break;
                case TDAPI_DATATYPES.TDOLE_ULONG:
                    TypeName = "Unsigned long integer";
                    break;
                case TDAPI_DATATYPES.TDOLE_FLOAT:
                    TypeName = "Floating point number";
                    break;
                case TDAPI_DATATYPES.TDOLE_STRING:
                    TypeName = "String";
                    break;
                case TDAPI_DATATYPES.TDOLE_MEMO:
                    TypeName = "Memo";
                    break;
                case TDAPI_DATATYPES.TDOLE_DATE:
                    TypeName = "Date";
                    break;
                case TDAPI_DATATYPES.TDOLE_TIMESTAMP:
                    TypeName = "Time stamp";
                    break;
                case TDAPI_DATATYPES.TDOLE_TREENODE:
                    TypeName = "List of values (General tree node)";
                    break;
                case TDAPI_DATATYPES.TDOLE_USER_LIST:
                    TypeName = "List of users";
                    break;
                case TDAPI_DATATYPES.TDOLE_TESTSET_LIST:
                    TypeName = "List of test sets";
                    break;
                case TDAPI_DATATYPES.TDOLE_HOST_LIST:
                    TypeName = "List of hosts";
                    break;
                case TDAPI_DATATYPES.TDOLE_SUBJECT_TREENODE:
                    TypeName = "Subject tree node";
                    break;
                case TDAPI_DATATYPES.TDOLE_TESTSET_FOLDER:
                    TypeName = "Test set folders tree node";
                    break;
                case TDAPI_DATATYPES.TDOLE_REQUIREMENT_TREENODE:
                    TypeName = "Requirements tree node";
                    break;
                case TDAPI_DATATYPES.TDOLE_REQUIREMENT_TYPE_ID:
                    TypeName = "(Number) Requirement type ID";
                    break;
                case TDAPI_DATATYPES.TDOLE_RELEASE_SINGLE_TREENODE:
                    TypeName = "Release single-value tree node";
                    break;
                case TDAPI_DATATYPES.TDOLE_RELEASE_MULTI_TREENODE:
                    TypeName = "Release multi-value tree node";
                    break;
                case TDAPI_DATATYPES.TDOLE_RELEASECYCLE_SINGLE_TREENODE:
                    TypeName = "ReleaseCycle single-value tree node";
                    break;
                case TDAPI_DATATYPES.TDOLE_RELEASECYCLE_MULTI_TREENODE:
                    TypeName = "ReleaseCycle multi-value tree node";
                    break;
                case TDAPI_DATATYPES.TDOLE_APPLICATION_ENTITY_FOLDER:
                    TypeName = "Application entity folders tree node";
                    break;
                case TDAPI_DATATYPES.TDOLE_CHANGE_ENTITY_FOLDER:
                    TypeName = "Change entity folders tree node";
                    break;
                case TDAPI_DATATYPES.TDOLE_DataType_SINGLE_TREENODE:
                    TypeName = "DataType single-value tree node";
                    break;
                case TDAPI_DATATYPES.TDOLE_DataType_MULTI_TREENODE:
                    TypeName = "DataType multi-value tree node";
                    break;
                case TDAPI_DATATYPES.TDOLE_BASELINES:
                    TypeName = "DataType Baselines";
                    break;
                case TDAPI_DATATYPES.TDOLE_TEST_CLASS:
                    TypeName = "DataType Test Class";
                    break;
                case TDAPI_DATATYPES.TDOLE_DATETIME:
                    TypeName = "Date and Time";
                    break;
                case TDAPI_DATATYPES.TDOLE_RESOURCE_FOLDER:
                    TypeName = "Resources tree node";
                    break;
                case TDAPI_DATATYPES.TDOLE_ENCRYPTED_STRING:
                    TypeName = "Encrypted String";
                    break;
            }
            return TypeName;
        }

        /// <summary>
        ///  Migrates the custom list values that are missing in ALM based in the Caliber requirements values.
        /// </summary>
        private void MigrateAllListValues()
        {
            try
            {
                LogMessage("    Started  migrating custom list values...");
                LoadListValues((RequirementTreeNode)SelectedCaliberBaseline.RequirementTree.Root);
                AddListValuesToCustomLists();
                LogMessage("    Finished migrating custom list values.");
            }
            catch (Exception ex)
            {
                LogMessage("    Failed migrating custom list values. Exception details: " + ex.Message);
            }
        }

        private void LoopBaselineRequirements()
        {
            LogMessage("    LoopBaselineRequirements...");
            Collection topRequirements = SelectedCaliberBaseline.Requirements;
            for (int i = 0; i < topRequirements.Count; i++)
            {
                PrintRequirement((Requirement) topRequirements[i]);
            }
            LogMessage("    LoopBaselineRequirements Finished.");
        }

        private void LoopBaselineRequirementsByType()
        {
            LogMessage("    LoopBaselineRequirementsByType...");
            Collection topRequirementTypes = SelectedCaliberBaseline.RequirementTypes;
            for (int i = 0; i < topRequirementTypes.Count; i++)
            {
                RequirementType currentRequirementType = (RequirementType)topRequirementTypes[i];
                Collection topRequirements = currentRequirementType.getRequirements2(SelectedCaliberBaseline);
                for (int j = 0; j < topRequirements.Count; j++)
                {
                    PrintRequirement((Requirement)topRequirements[j]);
                }
            }
            LogMessage("    LoopBaselineRequirementsByType Finished.");
        }

        private void LoopBaselineRequirementsByTree()
        {
            LogMessage("    LoopBaselineRequirementsByTree...");
            Starbase.CaliberRM.Interop.TreeNode RootNode = SelectedCaliberBaseline.RequirementTree.Root;
            PrintNode(RootNode);
            LogMessage("    LoopBaselineRequirementsByTree Finished.");
        }

        private void PrintNode(Starbase.CaliberRM.Interop.TreeNode node)
        {
            if (node.RequirementNode)
            {
                PrintRequirement((Requirement)CaliberSession.get(node.AssociatedObjectID));
            }
            Collection children = node.Children;
            for (int i = 0; i < children.Count; i++)
            {
                PrintNode((Starbase.CaliberRM.Interop.TreeNode)children[i]);
            }
        }

        private void PrintRequirement(Requirement req)
        {
            LogMessage("        Req ID: " + req.IDNumber + ". Name:" + req.Name + ".");
            Collection children = req.ChildRequirements;
            for (int i = 0; i < children.Count; i++)
            {
                PrintRequirement((Requirement) children[i]);
            }
        }

        private void AddListValuesToCustomLists()
        {
            foreach (string listName in AlmCustomListDictionary.Keys)
            {
                if (((CustomizationLists)Customization.Lists).IsListExist[listName])
                {
                    CustomizationList custList = (CustomizationList)((CustomizationLists)Customization.Lists).get_List(listName);
                    CustomizationListNode listRoot = (CustomizationListNode)custList.RootNode;
                    List<string> listValues = AlmCustomListDictionary[listName].ToList();
                    foreach (string listValue in listValues)
                    {
                        try
                        {
                            if (AddValueToCustomizationList(listRoot, listValue))
                            {
                                LogMessage("        Added [list]-[value]: [" + listName + "]-[" + listValue + "]");
                            }
                        }
                        catch (Exception ex)
                        {
                            LogMessage("        Failed while trying to add value to " + listName + " list. " + listName + ": " + listValue + ". Exception details: " + ex.Message);
                        }
                    }
                }
            }
            Customization.Commit();
            if (EnableALMSSRWebServices) 
            {
                StringBuilder users = new StringBuilder();
                foreach (string currentUser in UserList)
                {
                    users.Append(currentUser + ",");
                }
                users.Remove(users.Length - 1, 1);
                try
                {
                    AddUser(AlmServerName, AlmDomainName, AlmProjectName, users.ToString());
                }
                catch (Exception ex)
                {
                    LogMessage("        Failed while trying to add value to Owner list. Owner: " + users.ToString() + ". Exception details: " + ex.Message);
                }
            }
        }

        public void PerformAlmLogin(string AlmServerName, string AlmUser, string AlmPassword, AppSettingsSection AppSettings)
        {
            AlmRequirementFieldsDictionary = new Dictionary<string, string>();
            AlmReleasesDataDictionary = new Dictionary<string, int>();
            AlmParentIdDataDictionary = new Dictionary<string, int>();
            AlmCustomListDictionary = new Dictionary<string, HashSet<string>>();
            RequirementDataDictionary = new Dictionary<int, RequirementData>();

            AlmConnection = new TDConnection();
            AlmConnection.InitConnectionEx(AlmServerName);
            AlmConnection.Login(AlmUser, AlmPassword);
            this.AlmServerName = AlmServerName;
            this.EnableALMSSRWebServices = Convert.ToBoolean(AppSettings.Settings["EnableALMSSRWebServices"].Value);
            this.AlmSSRWebServices = "http://" + AppSettings.Settings["AlmSSRWebServices"].Value + "/qcbin";
            this.AlmUser = AlmUser;
            this.AlmPassword = AlmPassword;
            this.AppSettings = AppSettings;
        }

        public void PerformAlmProjectConnection(string AlmDomainName, string AlmProjectName)
        {
            this.AlmDomainName = AlmDomainName;
            this.AlmProjectName = AlmProjectName;
            LogMessage("    Connecting to the selected ALM Project...");
            AlmConnection.Connect(AlmDomainName, AlmProjectName);
            LoadCustomization();
            LogMessage("    Finished ALM project connection successfully.");
        }

        private void LogMessage(string status)
        {
            View.Controls[0].Invoke(new MethodInvoker(delegate {
                View.LogMessage(status);
            }));
        }

        private bool UrlExists(string url)
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.Credentials = new NetworkCredential(ISVNUser, ISVNPassword);
                request.Method = "HEAD";
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                return response.StatusCode == HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                // Any exception will return false.
                return false;
            }
        }

        public void ListFields()
        {
            CustomizationFields fields = (CustomizationFields)Customization.Fields;

            //Prepare list of all user-defined and available Req-fields
            List fieldsList = fields.get_Fields("REQ");
            ArrayList attributesList = new ArrayList();

            LogMessage("ALM Domain/Project: " + AlmDomainName + "/" + AlmProjectName);
            LogMessage("Field\tType");
            foreach (CustomizationField attribute in fieldsList)
            {
                if (!string.IsNullOrEmpty(attribute.UserLabel)) //attribute.ColumnName.Contains("RQ_USER_")
                {
                    LogMessage(attribute.UserLabel + "\t" + GetDataTypeName(attribute.Type));
                    attributesList.Add(attribute.ColumnName);
                }
            }

            LogMessage("Caliber Project/Baseline: " + CaliberProjectName + "/" + CaliberBaselineName);
            LogMessage("Field\tType");
            foreach (RequirementType CurrentCaliberRequirementType in GetCaliberBaseline(CaliberProjectName, CaliberBaselineName).RequirementTypes)
            {
                LogMessage("    Requirement Type: " + CurrentCaliberRequirementType.Name + ". Tag: " + CurrentCaliberRequirementType.Tag);

                Requirement CaliberRequirement = (Requirement) CurrentCaliberRequirementType.getRequirements2(GetCaliberBaseline(CaliberProjectName, CaliberBaselineName))[0];
                LogMessage("        Description");
                LogMessage("        Discussion");
                LogMessage("        IDNumber");
                LogMessage("        Metadata");
                LogMessage("        Name");
                LogMessage("        Owner");
                LogMessage("        Priority");
                LogMessage("        Project");
                LogMessage("        Status");

                if (CurrentCaliberRequirementType.Attributes.Count > 0)
                {
                    foreach (Starbase.CaliberRM.Interop.Attribute CurrentAttribute in CurrentCaliberRequirementType.Attributes)
                    {
                        LogMessage("        " + CurrentAttribute.Name + "\t" + CurrentAttribute.UITypeName);
                    }
                }
                else
                {
                    LogMessage("        No custom fields found.");
                }
            }


            /*
            LogMessage("ALM Domain/Project: " + AlmDomainName + "/" + AlmProjectName);
            PerformAlmProjectConnection(AlmDomainName, AlmProjectName);
            foreach (CustomizationReqType CurrentALMRequirementType in Customization.Types.GetEntityCustomizationTypes())
            {
                LogMessage("Requirement Type: " + CurrentALMRequirementType.Name);
                LogMessage("Listing " + CurrentALMRequirementType.Fields.Count + " Fields.");
                foreach (CustomizationTypedField CurrentAlmField in CurrentALMRequirementType.Fields)
                {
                    LogMessage("        " + CurrentAlmField.Field.Name + ". UIType: " + CurrentAlmField.Field.Name);
                }
                LogMessage("_________________________________________________");
            }
            LogMessage("___________________________________________________________________________________________");
            */

            /*
            //Get the RequirementsTypes objects one after another
            CustomizationTypes custTypes = (CustomizationTypes)Customization.Types;
            CustomizationReqType reqType = (CustomizationReqType)custTypes.GetEntityCustomizationType(0, 0);
            LogMessage("Processing RequirementsType: " + reqType.Name);

            List reqTypes = (List)custTypes.GetEntityCustomizationTypes(0);

            for (int i = 0; i < reqTypes.Count; i++)
            {
                CustomizationReqType reqType2 = (CustomizationReqType)custTypes.GetEntityCustomizationType(0, i);
                LogMessage("2 Field: " + reqType2.Name);
                //Get all fields of the current ReqType
                //Avoid adding already existing fields -> trouble!
                ArrayList alreadyExsitingFields = new ArrayList();
                foreach (CustomizationTypedField reqTypeField in reqType2.Fields)
                {
                    CustomizationField currentField = (CustomizationField)reqTypeField.Field;
                    alreadyExsitingFields.Add(currentField.ColumnName);
                }

                //Add all the required fields here
                foreach (string attributeName in attributesList)
                {
                    if (!alreadyExsitingFields.Contains(attributeName))
                    {
                        LogMessage("AttributeName: " + attributeName);
                        reqType2.AddField(attributeName);
                    }
                }
            }
            */
        }
    }

    public static class AttributeValueExtender
    {
        public static string GetAttributeValue(this AttributeValue attribute, Go2ALMBusinessLogic model)
        {
            if (attribute.Attribute.UITypeName.Equals(attribute.Attribute.UI_NAME_BOOLEAN))
            {
                return ((UDABooleanValue)attribute).Value.ToString();
            }
            else if (attribute.Attribute.UITypeName.Equals(attribute.Attribute.UI_NAME_LONG_INTEGER))
            {
                return ((UDAIntegerValue)attribute).Value.ToString();
            }
            else if (attribute.Attribute.UITypeName.Equals(attribute.Attribute.UI_NAME_DURATION))
            {
                return ((UDAIntegerValue)attribute).Value.ToString();
            }
            else if (attribute.Attribute.UITypeName.Equals(attribute.Attribute.UI_NAME_MLTF) ||
                attribute.Attribute.UITypeName.Equals(attribute.Attribute.UI_NAME_STL))
            {
                return ((UDATextValue)attribute).Value.ToString();
            }
            else if (attribute is UDAListValue)
            {
                return model.GetListValue(attribute, attribute.Attribute.UITypeName);
            }

            return null;
        }
    }
}

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
        private List<Label> AlmCustomFieldRequiredLabels;
        private List<Label> AlmCustomFieldLabels;
        private List<ComboBox> AlmCustomFields;
        private List<string> AlmCustomFieldSourceLists;

        private void LoadAlmCustomFields()
        {
            //TODO: Configure optional custom fields
            for (int i = 0; i < AlmCustomFields.Count; i++)
            {
                ComboBox AlmCustomField = AlmCustomFields[i];
                string currentCustomFieldLabelName = AlmCustomFieldLabels[i].Text;
                string currentCustomFieldListName = AlmCustomFieldSourceLists[i];
                try
                {
                    LoadAlmCustomFieldSetup(AlmCustomField, currentCustomFieldLabelName);
                    List<string> AlmCustomFieldsList = new List<string>();
                    if (!string.IsNullOrEmpty(currentCustomFieldListName))
                    {
                        AlmCustomFieldsList = Go2ALMBusinessLogic.GetAlmCustomFieldList(currentCustomFieldListName);
                    }
                    LoadAlmCustomFieldSuccessful(AlmCustomFieldsList, AlmCustomField, currentCustomFieldLabelName);
                }
                catch (Exception ex)
                {
                    LoadAlmCustomFieldFailed(ex.Message, AlmCustomField, currentCustomFieldLabelName);
                }
            }
        }

        private void LoadAlmCustomFieldSetup(ComboBox AlmCustomField, string CustomFieldLabel)
        {
            AlmProjectGroup.Invoke(new MethodInvoker(delegate
            {
                AlmCustomField.Enabled = false;
                LogMessage("Loading list of " + CustomFieldLabel + "s in the selected project (" + SelectedAlmProjectName + ")... ");
                AlmCustomField.Items.Clear();
            }));
        }

        private void LoadAlmCustomFieldSuccessful(List<string> AlmCustomFieldsList, ComboBox AlmCustomField, string CustomFieldLabel)
        {
            AlmProjectGroup.Invoke(new MethodInvoker(delegate
            {
                AlmCustomField.Items.Clear();
                if (AlmCustomFieldsList.Count > 0)
                {
                    AlmCustomField.Items.AddRange(AlmCustomFieldsList.ToArray());
                    AlmCustomField.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
                }
                LogMessage("Finished loading " + CustomFieldLabel + "s.");
                AlmCustomField.Enabled = true;
                AlmCustomField.Focus();
            }));
        }

        private void LoadAlmCustomFieldFailed(string ErrorMessage, ComboBox AlmCustomField, string CustomFieldLabel)
        {
            AlmLoginGroup.Invoke(new MethodInvoker(delegate
            {
                LogMessage("FAILED loading " + CustomFieldLabel + "s.");
                LogMessage(ErrorMessage);
                AlmCustomField.Items.Clear();
                AlmCustomField.Refresh();
            }));
        }

        private void AlmCustomFieldsClear()
        {
            for (int i = 0; i < AlmCustomFields.Count; i++)
            {
                AlmCustomFields[i].Items.Clear();
                AlmCustomFields[i].ResetText();
            }
        }

        private void AlmCustomFieldsEnabled(bool enabled)
        {
            for (int i = 0; i < AlmCustomFields.Count; i++)
            {
                AlmCustomFields[i].Enabled = enabled;
            }
        }

        private bool AlmRequiredCustomFieldsValidation()
        {
            bool validationSuccessful = true;
            for (int i = 0; i < AlmCustomFields.Count && validationSuccessful; i++)
            {
                validationSuccessful = validationSuccessful && (AlmCustomFields[i].Enabled) && (!string.IsNullOrEmpty(AlmCustomFields[i].Text));//(AlmCustomFields[i].SelectedIndex > -1);
            }
            return validationSuccessful;
        }

        private string[] GetAlmCustomFieldsValues()
        {
            string[] AlmCustomFieldsValues = new string[AlmCustomFields.Count];
            for (int i = 0; i < AlmCustomFields.Count; i++)
            {
                AlmCustomFieldsValues[i] = AlmCustomFields[i].Text;
            }
            return AlmCustomFieldsValues;
        }

        private void SetupCustomFields()
        {
            AlmCustomFieldRequiredLabels = new List<Label>();
            AlmCustomFieldLabels = new List<Label>();
            AlmCustomFields = new List<ComboBox>();
            AlmCustomFieldSourceLists = new List<string>();

            for(int i = 0; i < Go2ALMBusinessLogic.mapping.UiCustomFields.Count; i++)
            {
                string AlmCustomFieldLabelName = Go2ALMBusinessLogic.mapping.UiCustomFields[i].AlmField;
                string AlmCustomFieldSourceList = Go2ALMBusinessLogic.mapping.UiCustomFields[i].AlmList;

                Label AlmCustomFieldRequiredLabel = new Label();
                AlmCustomFieldRequiredLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
                AlmCustomFieldRequiredLabel.AutoSize = true;
                AlmCustomFieldRequiredLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                AlmCustomFieldRequiredLabel.ForeColor = System.Drawing.Color.Red;
                AlmCustomFieldRequiredLabel.Location = new System.Drawing.Point(3, 0 + (i * 28));
                AlmCustomFieldRequiredLabel.Name = "AlmCustomFieldRequiredLabel" + (i + 1);
                AlmCustomFieldRequiredLabel.Size = new System.Drawing.Size(14, 20);
                AlmCustomFieldRequiredLabel.TabIndex = 200 + i;
                AlmCustomFieldRequiredLabel.Text = "*";

                Label AlmCustomFieldLabel = new Label();
                AlmCustomFieldLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
                AlmCustomFieldLabel.AutoSize = true;
                AlmCustomFieldLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                AlmCustomFieldLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(150)))), ((int)(((byte)(214)))));
                AlmCustomFieldLabel.Location = new System.Drawing.Point(23, 7 + (i * 28));
                AlmCustomFieldLabel.Name = "AlmCustomFieldLabel" + (i + 1);
                AlmCustomFieldLabel.Size = new System.Drawing.Size(63, 13);
                AlmCustomFieldLabel.TabIndex = 250 + i;
                AlmCustomFieldLabel.Text = AlmCustomFieldLabelName + ":";

                ComboBox AlmCustomField = new ComboBox();
                AlmCustomField.FormattingEnabled = true;
                AlmCustomField.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown;
                AlmCustomField.TextChanged += new System.EventHandler(AlmCustomField_TextChanged);
                AlmCustomField.Anchor = System.Windows.Forms.AnchorStyles.Left;
                AlmCustomField.Enabled = false;
                AlmCustomField.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                AlmCustomField.Location = new System.Drawing.Point(123, 3 + (i * 28));
                AlmCustomField.Name = "AlmCustomField" + (i + 1);
                AlmCustomField.Size = new System.Drawing.Size(230, 21);
                AlmCustomField.TabIndex = 300 + i;

                this.AlmCustomFieldsPanel.RowCount = i + 1;
                this.AlmCustomFieldsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));

                AlmCustomFieldsPanel.Controls.Add(AlmCustomFieldRequiredLabel,  0, i);
                AlmCustomFieldsPanel.Controls.Add(AlmCustomFieldLabel,          1, i);
                AlmCustomFieldsPanel.Controls.Add(AlmCustomField,               2, i);

                AlmCustomFieldRequiredLabels.Add(AlmCustomFieldRequiredLabel);
                AlmCustomFieldLabels.Add(AlmCustomFieldLabel);
                AlmCustomFields.Add(AlmCustomField);
                AlmCustomFieldSourceLists.Add(AlmCustomFieldSourceList);
            }
            for (int i = this.AlmCustomFieldsPanel.RowCount; i < 5; i++)
            {
                this.AlmCustomFieldsPanel.RowCount = i + 1;
                this.AlmCustomFieldsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            }
        }

        /// <summary>
        /// When ComboBox selected index changes:
        ///    Enable/Disable the Migrate Requirements Button.
        /// </summary>
        private void AlmCustomField_TextChanged(object sender, EventArgs e)
        {
            EnableMigrateRequirementsButton();
        }

    }
}

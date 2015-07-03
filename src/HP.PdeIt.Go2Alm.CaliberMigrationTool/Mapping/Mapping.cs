// go2ALM: migrate from various tools to QC/ALM
// Copyright (C) 2012 Hewlett Packard Company
// Authors: 
//      Arturo Torres
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

namespace HP.PdeIt.Go2Alm.CaliberMigrationTool.Mapping
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Serialization;

    /// <summary>
    /// Represents a set of output tools.
    /// </summary>
    public class MappingOutput
    {
        /// <summary>
        /// Gets a emails from a text.
        /// </summary>
        /// <param name="text">The text that contains the emails.</param>
        /// <returns>
        /// A emails if the text contains it, if not return string.Empty.
        /// </returns>
        /// <exception cref="Hp.Pdeit.Go2Alm.CaliberMigrationTool.Mapping.MappingException">
        /// If more than one emails found.
        /// </exception>
        public static string OutputAsSimpleEmail(string text)
        {
            MatchCollection matches = Regex.Matches(text,
                @"\b[A-Za-z0-9._%-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,4}\b",
                RegexOptions.ExplicitCapture);

            if (1 == matches.Count)
            {
                return matches[0].Value.Replace('@', '_');
            }
            else if (1 < matches.Count)
            {
                throw new MappingException(string.Format(
                    "More than one Email found on the evaluation of value {0}.",
                    text));
            }
            else
            {
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// Represents a exception for the mapping objects.
    /// </summary>
    public class MappingException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the Hp.Pdeit.Go2Alm.CaliberMigrationTool.Mapping.TemplateException with a specified error message.
        /// </summary>
        /// <param name="message">The message that explains the reason for the exception.</param>
        public MappingException(string message)
            : base(message)
        {

        }
    }

    /// <summary>
    /// Represents the mapping between Borland® CaliberRM™ and ALM™.
    /// </summary>
    [XmlRoot("alm_mapping")]
    public class AlmMapping
    {
        /// <summary>
        /// Initializes a new instance of the Hp.Pdeit.Go2Alm.CaliberMigrationTool.Mapping.AlmMapping.
        /// </summary>
        public AlmMapping()
        {
            this.UiCustomFields = new List<MappingField>();
            this.SystemFields = new List<MappingField>();
            this.CustomFields = new List<MappingField>();
        }
        
        /// <summary>
        /// Gets or Sets a list of a custom mappings. 
        /// </summary>
        [XmlArray("ui_custom_fields"), XmlArrayItem("field", typeof(MappingField))]
        public List<MappingField> UiCustomFields { get; set; }

        /// <summary>
        /// Gets or Sets a list of a system mappings.
        /// </summary>
        [XmlArray("system_fields"), XmlArrayItem("field", typeof(MappingField))]
        public List<MappingField> SystemFields { get; set; }

        /// <summary>
        /// Gets or Sets a list of a custom mappings. 
        /// </summary>
        [XmlArray("custom_fields"), XmlArrayItem("field", typeof(MappingField))]
        public List<MappingField> CustomFields { get; set; }

        /// <summary>
        /// Finds the best mapping for the specified field.
        /// </summary>
        /// <param name="id">The id of the field mapping in the required mappings.</param>
        /// <param name="requirementType">The requirement filter</param>
        /// <returns></returns>
        /// <exception cref="Hp.Pdeit.Go2Alm.CaliberMigrationTool.Mapping.MappingException">
        /// Thrown when a field definition is duplicated or is not found.
        /// </exception>
        public MappingField FindSystemMapping(string id, string requirementType = null)
        {
            var template = this.SystemFields.Where(s => 0 == string.Compare(id, s.ID, true));

            var byRequirementType = template.Where(s =>
                !string.IsNullOrEmpty(s.RequirementType) && string.IsNullOrEmpty(s.NotRequirementType)
                && 0 == string.Compare(s.RequirementType, requirementType, true));

            var byNotRequirementType = template.Where(s =>
                string.IsNullOrEmpty(s.RequirementType) && !string.IsNullOrEmpty(s.NotRequirementType)
                && 0 != string.Compare(s.NotRequirementType, requirementType, true));

            if (0 == byRequirementType.Count() || 0 == byNotRequirementType.Count())
            {
                if (1 == byRequirementType.Count())
                {
                    return byRequirementType.Single();
                }
                else if (1 < byRequirementType.Count())
                {
                    throw new MappingException(string.Format(
                        "Mapping Warning: Duplicated definition of field element with id <{0}> and requirement-type <{1}> found in the system_fields element.",
                        id, requirementType));
                }

                if (1 == byNotRequirementType.Count())
                {
                    return byNotRequirementType.Single();
                }
                else if (1 < byNotRequirementType.Count())
                {
                    throw new MappingException(string.Format(
                        "Mapping Warning: Duplicated definition or mutual exclusion of field element with id <{0}> and not-requirement-type <{1}> found in the system_fields element.",
                        id, requirementType));
                }

                var byDefault = template.Where(s =>
                string.IsNullOrEmpty(s.RequirementType) && string.IsNullOrEmpty(s.NotRequirementType));

                if (1 == byDefault.Count())
                {
                    return byDefault.Single();
                }
                else if (1 < byDefault.Count())
                {
                    throw new MappingException(string.Format(
                        "Mapping Warning: Duplicated definition of field element with id <{0}> found in the system_fields element.",
                        id));
                }
            }
            else
            {
                throw new MappingException(string.Format(
                        "Mapping Warning: Mutual exclusion found in field elements with id <{0}> where requirement-type and not-requirement-type can not be used simultaneously.",
                        id));
            }

            if (string.IsNullOrEmpty(requirementType))
            {
                throw new MappingException(string.Format(
                        "Mapping Warning: Definition of field element with id <{0}> not found in the system_fields element.",
                         id));
            }
            else
            {
                throw new MappingException(string.Format(
                        "Mapping Warning: Definition of field element with id <{0}> with requirement-type or not-requirement-type {1} not found in the system_fields element.",
                         id, requirementType));
            }
        }

        public MappingField FindUiCustomMapping(string AlmField)
        {
            MappingField CustomField = null;
            foreach (MappingField CurrentCustomField in this.CustomFields)
            {
                if (CurrentCustomField.AlmField == AlmField)
                {
                    CustomField = CurrentCustomField;
                    break;
                }
            }
            return CustomField;
        }

        /// <summary>
        /// Finds the best Template for the specified caliber field.
        /// </summary>
        /// <param name="caliberField">The name of the target Caliber field.</param>
        /// <param name="requirementType">The requirement type applied to the mapping field.</param>
        /// <returns>Hp.Pdeit.Go2Alm.CaliberMigrationTool.Mapping.MappingField</returns>
        /// <exception cref="Hp.Pdeit.Go2Alm.CaliberMigrationTool.Mapping.MappingException">
        /// Thrown when a field definition is duplicated or is not found.
        /// </exception>
        public MappingField FindCustomMapping(string caliberField, string requirementType = null)
        {
            var template = this.CustomFields.Where(s => caliberField.Equals(s.CaliberField));

            var byRequirementType = template.Where(s =>
                !string.IsNullOrEmpty(s.RequirementType) && string.IsNullOrEmpty(s.NotRequirementType)
                && 0 == string.Compare(s.RequirementType, requirementType, true));

            var byNotRequirementType = template.Where(s =>
                string.IsNullOrEmpty(s.RequirementType) && !string.IsNullOrEmpty(s.NotRequirementType)
                && !string.IsNullOrEmpty(requirementType)
                && 0 != string.Compare(s.NotRequirementType, requirementType, true));

            if (0 == byRequirementType.Count() || 0 == byNotRequirementType.Count())
            {
                if (1 == byRequirementType.Count())
                {
                    return byRequirementType.Single();
                }
                else if (1 < byRequirementType.Count())
                {
                    throw new MappingException(string.Format(
                        "Mapping Warning: Duplicated definition of field element with Caliber Field <{0}> and requirement-type <{1}> found in the custom_fields element.",
                        caliberField, requirementType));
                }

                if (1 == byNotRequirementType.Count())
                {
                    return byNotRequirementType.Single();
                }
                else if (1 < byNotRequirementType.Count())
                {
                    throw new MappingException(string.Format(
                        "Mapping Warning: Duplicated definition or mutual exclusion of field element with Caliber Field <{0}> and not-requirement-type <{1}> found in the custom_fields element.",
                        caliberField, requirementType));
                }

                var byDefault = template.Where(s =>
                string.IsNullOrEmpty(s.RequirementType) && string.IsNullOrEmpty(s.NotRequirementType));

                if (1 == byDefault.Count())
                {
                    return byDefault.Single();
                }
                else if (1 < byDefault.Count())
                {
                    throw new MappingException(string.Format(
                        "Mapping Warning: Duplicated definition of field element with Caliber Field <{0}> found in the custom_fields element.",
                        caliberField));
                }
            }
            else
            {
                throw new MappingException(string.Format(
                        "Mapping Warning: Mutual exclusion found in field elements with Caliber Field <{0}> where requirement-type and not-requirement-type can not be used simultaneously.",
                         caliberField));
            }

            if (string.IsNullOrEmpty(requirementType))
            {
                throw new MappingException(string.Format(
                        "Mapping Warning: Definition of field element with Caliber Field <{0}> not found in the custom_fields element. The value will not be migrated.",
                         caliberField));
            }
            else
            {
                throw new MappingException(string.Format(
                        "Mapping Warning: Mapping for Caliber Field <{0}> and requirement type <{1}> was not found in the custom_fields element. The value will not be migrated.",
                         caliberField, requirementType));
            }
        }

        /// <summary>
        /// Loads the Template from the specified URL.
        /// </summary>
        /// <param name="filename">URL for the file containing the XML document to load. The URL can be either a local file or an HTTP URL (a Web Address).</param>
        /// <returns>Hp.Pdeit.Go2Alm.CaliberMigrationTool.Mapping.AlmMapping</returns>
        /// <exception cref="System.IO.IOException">Thrown when <paramref name="filename"/><c> cannot be loaded.</c>.</exception>
        public static AlmMapping Load(string filename)
        {
            XmlSerializer s = new XmlSerializer(typeof(AlmMapping));

            AlmMapping template;
            using (StreamReader sr = new StreamReader(filename))
            {
                template = s.Deserialize(sr) as AlmMapping;
            }

            return template;
        }

        /// <summary>
        /// Saves the Template to the specified URL.
        /// </summary>
        /// <param name="filename">The location of the file where you want to save the document.</param>
        /// <param name="mapping">The mapping to be storaged.</param>
        /// <exception cref="System.IO.IOException">Thrown when <paramref name="filename"/><c> cannot be written.</c>.</exception>
        public static void Save(string filename, AlmMapping template)
        {
            XmlSerializer s = new XmlSerializer(typeof(AlmMapping));

            using (StreamWriter sw = new StreamWriter(filename))
            {
                s.Serialize(sw, template);
            }
        }
    }

    /// <summary>
    /// Represents a mapping for a field.
    /// </summary>
    public class MappingField
    {
        /// <summary>
        /// Gets or Sets the field identificator.
        /// </summary>
        [XmlAttribute("id")]
        public string ID { get; set; }

        /// <summary>
        /// Gets or Sets the skip attribute for the field.
        /// </summary>
        [XmlAttribute("skip")]
        public string Skip { get; set; }

        /// <summary>
        /// Gets or Sets the requirement type for the field.
        /// </summary>
        [XmlAttribute("requirement-type")]
        public string RequirementType { get; set; }

        [XmlAttribute("requirement-type-is-not")]
        public string NotRequirementType { get; set; }

        /// <summary>
        /// Gets or Sets the output type for the field.
        /// </summary>
        [XmlAttribute("output-type")]
        public string OutputType { get; set; }

        /// <summary>
        /// Gets or Sets the default value for the field.
        /// </summary>
        [XmlAttribute("default-value")]
        public string DefaultValue { get; set; }

        /// <summary>
        /// Gets or Sets the Caliber target field name.
        /// </summary>
        [XmlElement("caliber_field")]
        public string CaliberField { get; set; }

        /// <summary>
        /// Gets or Sets the ALM target field name.
        /// </summary>
        [XmlElement("alm_field")]
        public string AlmField { get; set; }

        /// <summary>
        /// Gets or Sets the ALM list name.
        /// </summary>
        [XmlElement("alm_list")]
        public string AlmList { get; set; }

        /// <summary>
        /// Gets or Sets the list of replace conditions.
        /// </summary>
        [XmlArray("replace_value")]
        [XmlArrayItem("when", typeof(MappingWhenCondition)), XmlArrayItem("otherwise", typeof(MappingOtherwiseCondition))]
        public List<MappingReplaceCondition> ReplaceValues { get; set; }

        /// <summary>
        /// Gets a value applying the transformation rules specified in the field. 
        /// The transformation rules are: output-type, default-value and replace-values.
        /// </summary>
        /// <param name="value">The value to be transformed.</param>
        /// <returns>The value after apply the transformations.</returns>
        public string ApplyTransformToValue(string value)
        {
            // Apply Default value
            if (string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(this.DefaultValue))
            {
                value = this.DefaultValue.Trim();
            }
            else if (null != this.ReplaceValues && 0 < this.ReplaceValues.Count)
            {
                // Apply value Transforms
                bool replaceApplied = false;
                var when = this.ReplaceValues.OfType<MappingWhenCondition>();

                if (0 < when.Count())
                {
                    foreach (var w in when)
                    {
                        if (w.EqualTo.Equals(value))
                        {
                            value = w.Value.Trim();
                            replaceApplied = true;
                            break;
                        }
                    }
                }

                if (!replaceApplied)
                {
                    var otherwise = this.ReplaceValues.OfType<MappingOtherwiseCondition>();

                    if (1 == otherwise.Count())
                    {
                        value = otherwise.Single().Value.Trim();
                    }
                    else if (1 < otherwise.Count())
                    {
                        throw new MappingException(string.Format(
                            "Template Warning: Duplicated <otherwise> statement on element {0}.",
                            this.GetType().Name));
                    }
                }
            }
            else if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(this.OutputType))
            {
                // Apply Filters
                switch (this.OutputType.ToUpperInvariant())
                {
                    case "EMAIL":
                        value = MappingOutput.OutputAsSimpleEmail(value).Trim();
                        break;
                }
            }

            return value;
        }
    }

    /// <summary>
    /// Represents a base replace condition.
    /// </summary>
    public abstract class MappingReplaceCondition
    {
        /// <summary>
        /// 
        /// </summary>
        [XmlText()]
        public string Value { get; set; }
    }

    /// <summary>
    /// Represents the replace mapping condition "when".
    /// </summary>
    public class MappingWhenCondition : MappingReplaceCondition
    {
        /// <summary>
        /// Gets or Sets the comparator value in the condition.
        /// </summary>
        [XmlAttribute("equal")]
        public string EqualTo { get; set; }
    }

    /// <summary>
    /// Represents the replace mapping condition "otherwise".
    /// Only one "otherwise" condition is allowed by mapping field.
    /// </summary>
    public class MappingOtherwiseCondition : MappingReplaceCondition { }
}
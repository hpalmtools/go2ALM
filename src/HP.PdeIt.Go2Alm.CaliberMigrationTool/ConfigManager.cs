using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace HP.PdeIt.Go2Alm.CaliberMigrationTool
{
    public class ConfigManager : ConfigurationSection
    {
        [ConfigurationProperty("caliberServerNames")]
        public ConfigCollection CaliberServerNames
        {
            get { return ((ConfigCollection)(base["caliberServerNames"])); }
        }

        [ConfigurationProperty("almServerNames")]
        public ConfigCollection ALMServerNames
        {
            get { return ((ConfigCollection)(base["almServerNames"])); }
        }
    }

    /// <summary>
    /// The collection class that will store the list of each element/item that
    /// is returned back from the configuration manager.
    /// </summary>
    [ConfigurationCollection(typeof(ConfigElement))]
    public class ConfigCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new ConfigElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ConfigElement)(element)).Name;
        }

        public ConfigElement this[int idx]
        {
            get
            {
                return (ConfigElement)BaseGet(idx);
            }
        }
    }

    /// <summary>
    /// The class that holds onto each element returned by the configuration manager.
    /// </summary>
    public class ConfigElement : ConfigurationElement
    {
        [ConfigurationProperty("name", DefaultValue = "", IsKey = true, IsRequired = true)]
        public string Name
        {
            get
            {
                return ((string)(base["name"]));
            }
            set
            {
                base["name"] = value;
            }
        }
    }
}

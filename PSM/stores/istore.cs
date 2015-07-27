using System;
using System.Configuration;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PSMonitor.Stores
{

    public class Setup : PSMonitor.Setup
    {

        public static TResult Get<T, TResult>(string name)
        {
            
            SettingsCollection settings = PSMonitor.Setup.Get<Setup>("stores").Settings;

            foreach (SettingElement element in settings)
            {
                if ((System.Type.GetType(element.For, false, true) ?? typeof(object)).Equals(typeof(T)) && element.Name == name && !String.IsNullOrEmpty(element.Value))
                    return (TResult)Convert.ChangeType(element.Value, typeof(TResult));
            }

            throw new ConfigurationErrorsException(String.Format("Can not find configuration key with the name: {0}", name));
        }

        [ConfigurationCollection(typeof(SettingElement), CollectionType = ConfigurationElementCollectionType.AddRemoveClearMap)]
        public class SettingsCollection : ConfigurationElementCollection
        {
            protected override ConfigurationElement CreateNewElement()
            {
                return new SettingElement();
            }

            protected override object GetElementKey(ConfigurationElement element)
            {
                return ((SettingElement)element).ElementInformation.LineNumber - base.ElementInformation.LineNumber - 1;
            }

            [ConfigurationProperty("setting", IsRequired = false)]
            public SettingElement Setting { get { return (SettingElement)base["setting"]; } }

        }

        public class SettingElement : ConfigurationElement
        {

            [ConfigurationProperty("for", IsRequired = true)]
            public string For { get { return (string)base["for"]; } }

            [ConfigurationProperty("name", IsRequired = true)]
            public string Name { get { return (string)base["name"]; } }

            [ConfigurationProperty("value", IsRequired = true)]
            public string Value { get { return (string)base["value"]; } }

        }

        [ConfigurationProperty("type", DefaultValue = "", IsRequired = false)]
        public string Type { get { return (string)base["type"]; } }

        [ConfigurationProperty("settings", IsRequired = false)]
        public SettingsCollection Settings { get { return (SettingsCollection)base["settings"]; } }

    }

    public interface IStore : IDisposable
    {
        event DataReceivedHandler DataReceived;

        Entry Get(string path);

        IEnumerable<Entry> Get(string path, DateTime start, DateTime end);
        IEnumerable<Entry> Get(string path, long start, long end);

        void Put(Envelope envelope);

        long Delete(string path);
        long Delete(string path, DateTime start, DateTime end);

        Key[] GetKeys(string path);

    }

    [Serializable]
    public class Key : ISerializable
    {
        public string Name { get; private set; }
        public Type Type { get; private set; }

        public Key(string Name, Type Type)
        {
            this.Name = Name;
            this.Type = Type;
        }

        public Key(SerializationInfo info, StreamingContext context)
        {
            Name = info.GetString("name");
            Type = Type.GetType(info.GetString("type"), false);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("name", Name);
            info.AddValue("type", Type != null ? Type.FullName : "");
        }

        public override string ToString()
        {
            return Name;
        }
    }

}

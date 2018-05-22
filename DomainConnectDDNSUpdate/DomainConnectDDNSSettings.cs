using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace DomainConnectDDNSUpdate
{
    public class DomainConnectDDNSSettings
    {
        public Dictionary<string, object> settings;

        public DomainConnectDDNSSettings()
        {
            this.settings = new Dictionary<string, object>();
        }

        public void Save(string fileName)
        {
            File.WriteAllText(fileName, new JavaScriptSerializer().Serialize(this.settings));
        }

        public void Load(string fileName)
        {
            string x = Directory.GetCurrentDirectory();
            string y = System.Reflection.Assembly.GetExecutingAssembly().Location;
            try
            {
                this.settings = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(fileName));
            }
            catch (FileNotFoundException)
            {                
                this.settings = new Dictionary<string, object>();
            }
            catch (Exception)
            {
                throw;
            }            
        }

        public void Clear()
        {
            this.settings.Clear();
        }

        public object ReadValue(string key, object defaultValue)
        {
            if (this.settings.ContainsKey(key))
                return this.settings[key];
            else
                return null;
        }

        public void WriteValue(string key, object value)
        {
            this.settings[key] = value;
        }

        
    }
}

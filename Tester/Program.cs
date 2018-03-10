using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GoDaddyRestAPI;

namespace Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            string domainName;           
            string apiKey;
            string aName;
            string aRecordTemplate;

            // Read values in from the registry
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\GoDaddyDNSUpdate\Config"))
            {
                domainName = (string)key.GetValue("Domain");
                apiKey = (string)key.GetValue("ApiKey");
                aName = (string)key.GetValue("AName", "@");
                aRecordTemplate = (string)key.GetValue("ATemplate", "[{\"data\" : \"%ip%\", \"ttl\" : 1800}]");
            }

            //string r = GoDaddyDNSUpdate.GoDaddyDynDNS.GetDomains(apiKey);

            //string s = GoDaddyDNSUpdate.GoDaddyDynDNS.GetDomainRecords(apiKey, domainName);
            string s;
            int status = 0;

            string ip = GoDaddyRestAPI.GoDaddyRestAPI.GetGoDaddyIP(domainName, apiKey, "A", "@", out status);

            bool result = GoDaddyRestAPI.GoDaddyRestAPI.UpdateGoDaddyIP(domainName, apiKey, "A", aName, "10.0.0.2", aRecordTemplate, out status);

            s = GoDaddyRestAPI.GoDaddyRestAPI.GetDomainRecords(apiKey, domainName, out status);

            ip = GoDaddyRestAPI.GoDaddyRestAPI.GetGoDaddyIP(domainName, "BadKey", "A", "@", out status);
        }
    }
}

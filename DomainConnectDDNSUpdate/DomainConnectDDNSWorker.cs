using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainConnectDDNSUpdate
{
    public class DomainConnectDDNSWorker
    {
        // The IP Address that DNS has for the A Record
        string currentDNSIP = null;

        // Service is initialized
        public bool initialized = false;
        private int numInitializeFails = 0;

        // Service is good for monitoring DNS changes
        public bool monitoring = false;
        private int numMonitorFails = 0;

        // Values from the registry        
        private string domain;                  // Name of the domain
        private string host;                    // Host (sub-domain)
        private string access_token;            // Access token for oauth
        private string refresh_token;           // Refresh token for oauth
        private string urlAPI;
        private string dns_provider;

        const string providerId = "exampleservice.domainconnect.org";

        EventLog eventLog1;

        public DomainConnectDDNSWorker(EventLog eventLog)
        {
            this.eventLog1 = eventLog;
        }

        private void WriteEvent(string message, EventLogEntryType elt = EventLogEntryType.Information)
        {
            if (this.eventLog1 != null)
            {
                this.WriteEvent(message, elt);
            }
        }

        //-------------------------------------------------------
        // UpdateIP
        //
        // Updates the IP with with the DNS Provider
        //
        public bool UpdateIP(string newIP)
        {
            int status = 0;

            // Apply template and store the response.
            string response = OAuthHelper.OAuthHelper.ApplyTemplate(newIP, out status);

            if (response == null || status < 200 || status >= 300)
            {
                this.WriteEvent("Failure to update IP", EventLogEntryType.Error);

                // Don't penalize if trys to update if the internet is down, or the server is down
                if (status != 0 || status < 500)
                    this.numInitializeFails++;

                // After 10 failures, we stop retrying
                if (this.numInitializeFails > 10)
                {
                    this.WriteEvent("Failure to update IP threshold reached. Service must be restarted to try again.", EventLogEntryType.Error);

                    this.monitoring = false;
                }

                return false;
            }
            else
            {
                this.numInitializeFails = 0;

                this.WriteEvent("IP Updated to " + newIP);

                return true;
            }
        }


        //-------------------------------------------------------
        // InitService
        //
        // Initializes the service
        //
        private void InitService()
        {
            int status = 0;

            RegistryKey lkey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\services\DomainConnectDDNSUpdate\Config");

            if (lkey == null)
            {
                this.WriteEvent("Unable to get configuration from registry", EventLogEntryType.Error);

                // Don't mark as initialized....we'll keep trying in case data is written to registry later

                return;
            }

            this.domain = (string)lkey.GetValue("domain_name");
            this.host = (string)lkey.GetValue("host");
            this.access_token = (string)lkey.GetValue("access_token");
            this.refresh_token = (string)lkey.GetValue("refresh_token");
            this.dns_provider = (string)lkey.GetValue("dns_provider");
            this.urlAPI = (string)lkey.GetValue("urlAPI");

            if (this.domain == null || this.domain == "" ||
                this.access_token == null || this.access_token == "" ||
                this.refresh_token == null || this.refresh_token == "" ||
                this.dns_provider == null || this.dns_provider == "" ||
                this.urlAPI == null || this.dns_provider == null)
            {
                this.WriteEvent("Initiaize failure: missing data. Run installer.", EventLogEntryType.Error);

                return;
            }

            // Query the initial (current) IP from DNS. Null is an error, "" means no current value
            string fqdn = this.domain;
            if (this.host != null && host != "")
                fqdn = this.host + "." + fqdn;
            this.currentDNSIP = RestAPIHelper.RestAPIHelper.GetDNSIP(fqdn);

            if (this.currentDNSIP == null)
            {
                this.WriteEvent("Failed to read current IP for domain.", EventLogEntryType.Error);

                // We won't penalize if internet or service is down
                if (status != 0 && status < 500)
                    this.numInitializeFails++;

                // After 10 tries to init that fail, we give up
                if (this.numInitializeFails > 10)
                {
                    this.WriteEvent("Threshold for attempted initialize reached. Restart service to try again.", EventLogEntryType.Error);
                    this.initialized = true;
                }

                return;
            }

            // Service successfully initalized
            this.initialized = true;
            this.monitoring = true;

            this.WriteEvent("Initialized and running.");

        }

        public void DoWork()
        {
            // If we haven't initialized, try to initialize now
            if (!this.initialized)
            {
                this.InitService();
            }

            if (this.monitoring)
            {
                int status = 0;
                string newIP = null;
                try
                {
                    // See if our IP has changed
                    newIP = RestAPIHelper.RestAPIHelper.GET("http://api.ipify.org", out status);
                }
                catch
                {
                    status = 0;
                }

                // Update the IP if it has changed
                if (newIP != null &&
                    (newIP != this.currentDNSIP && status >= 200 && status < 300)) // We need to add a || we haven't updated in a long time
                {
                    UpdateIP(newIP);
                }
            }
        }

    }
}

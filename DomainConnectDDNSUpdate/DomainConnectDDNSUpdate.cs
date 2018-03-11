using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Timers;
using System.Web.Script.Serialization;
using RestAPIHelper;


namespace GoDaddyDNSUpdate
{
    public partial class DomainConnectDDNSUpdate : ServiceBase
    {
        // The IP Address that GoDaddy has for the A Record
        string currentDNSIP = null;

        // Timer for the updates
        private Timer timer;
        private int interval = 600000;              // Every 10 minutes 
        private int shortinterval = 60000;          // Every 1 minute 

        // Service is initialized
        private bool initialized = false;
        private int numInitializeFails = 0;

        // Service is good for monitoring DNS changes
        private bool monitoring = false;
        private int numMonitorFails = 0;

        // Values from the registry        
        private string domain;                  // Name of the domain
        private string host;                    // Host (sub-domain)
        private string response_code;           // OAuth Response Code for getting access token
        private string access_token;            // Access token for oauth
        private string refresh_token;           // Refresh token for oauth
        private string urlAPI;
        private string dns_provider;    
        
        const string providerId = "exampleservice.domainconnect.org";

        //-------------------------------------------------------
        // UpdateIP
        //
        // Updates the IP with GoDaddy according to the template
        //
        public bool UpdateIP(string newIP)
        {
            bool result = false;
            int status = 0;

            string redirect_url = "http://exampleservice.domainconnect.org/async_oauth_response?domain=" + this.domain + "&hosts=" + this.host + "&dns_provider=" + this.dns_provider;

            string url = this.urlAPI + "/v2/oauth/access_token?code=" + this.access_token + "&grant_type=authorization_code&client_id=" + this.cli + "&client_secret=DomainConnectGeheimnisSecretString&redirect_uri=" + redirect_url;

            result = GoDaddyRestAPI.RestAPIHelper.UpdateGoDaddyIP(this.domainName, this.apiKey, "A", this.aName, newIP, this.aRecordTemplate, out status);            
            
            if (!result)
            {
                eventLog1.WriteEntry("Failure to update IP", EventLogEntryType.Error);

                // Status of 0 is failed internet.  We also won't penalize if the server is down.
                if (status != 0 || status < 500)
                    this.numMonitorFails++;

                // After 10 failures, we stop retrying
                if (this.numMonitorFails > 10)
                {
                    eventLog1.WriteEntry("Failure to update IP threshold reached. Service must be restarted to try again.", EventLogEntryType.Error);

                    this.monitoring = false;
                }
                    
                return false;
            }
            else
            { 
                this.goDaddyIP = newIP;

                this.numMonitorFails = 0;

                eventLog1.WriteEntry("IP Updated to " + newIP);

                return true;
            }
        }

        private void GetTokens()
        {
            if (access_token == null)
            {
                string redirect_url = "http://exampleservice.domainconnect.org/async_oauth_response?domain=" + this.domain + "&hosts=" + this.host + "&dns_provider=" + this.dns_provider;

                string url = this.urlAPI + "/v2/oauth/access_token?code=" + this.access_token + "&grant_type=authorization_code&client_id=" + this.cli + "&client_secret=DomainConnectGeheimnisSecretString&redirect_uri=" + redirect_url;

                string json = RestAPIHelper.RestAPIHelper.GET(url, out status);
                if (status >= 300)
                {
                    eventLog1.WriteEntry("OAuth error.", EventLogEntryType.Error);
                }

                var jss = new JavaScriptSerializer();
                var table = jss.Deserialize<dynamic>(json);
                this.access_token = table["access_token"];
                this.refresh_token = table["refresh_token"];
                //this.expires_in = table["expires_in"];



                var jss = new JavaScriptSerializer();
                var dict = jss.Deserialize<Dictionary>
             }

        }
        
        //-------------------------------------------------------
        // InitService
        //
        // Initializes the service
        //
        private void InitService()
        {

            // Read values in from the registry
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\DomainConnectDDNSUpdate\Config"))
            {
                int status = 0;

                if (key == null)
                {
                    eventLog1.WriteEntry("Unable to get configuration from registry", EventLogEntryType.Error);

                    // Don't mark as initialized....we'll keep trying in case data is written to registry later

                    return;
                }
                
                this.domain = (string)key.GetValue("domain", null);
                this.host = (string)key.GetValue("host", null);
                this.access_token = (string)key.GetValue("access_token", null);
                this.refresh_token = (string)key.GetValue("refresh_token", null);
                this.dns_provider = (string)key.GetValue("dns_provider", null);
                this.urlAPI = (string)key.GetValue("urlAPI", null);
                

                if (this.domain == null || this.domain == "")
                {
                    eventLog1.WriteEntry("Initiaize failure. Missing .", EventLogEntryType.Error);
                  
                    return;
                }

               

                // Query the initial (current) IP from GoDaddy
                string fqdn = this.domain;
                if (this.host != null && host != "")
                    fqdn = this.host + "." + fqdn;

                this.currentDNSIP = RestAPIHelper.RestAPIHelper.GetDNSIP(fqdn);
                  
                if (this.currentDNSIP == null)
                {
                    eventLog1.WriteEntry("Failed to read IP for domain.", EventLogEntryType.Error);

                    // We won't penalize if internet or service is down
                    if (status != 0 && status < 500)
                        this.numInitializeFails++;

                    if (this.numInitializeFails > 10)
                    {
                        eventLog1.WriteEntry("Failed to start threshold reached.  Restart service to try again.", EventLogEntryType.Error);
                        this.initialized = true;
                    }

                    return;
                }
                // Service successfully initalized
                this.initialized = true;
                this.monitoring = true;

                eventLog1.WriteEntry("Initialized and running.");

                // Get the IP Address as reported by a ping
                string newIP = RestAPIHelper.RestAPIHelper.GET("http://api.ipify.org", out status);

                if (newIP != null && newIP != this.currentDNSIP && status >= 200 && status < 300)
                {
                    this.UpdateIP(newIP);
                }                
            }

        }


        //----------------------------------------------------
        // timer_Elaspsed
        //
        // Call back function from the timer
        //
        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Stop the timer
            timer.Stop();

            // If we haven't initialized, try to initialize now
            if (!this.initialized)
            {
                this.InitService();                
            }
            else if (this.monitoring)
            {
                // We are initialized. See if our IP has changed
                int status = 0;
                string newIP = RestAPIHelper.RestAPIHelper.GET("http://api.ipify.org", out status);            

                // Update the IP if it has changed
                if (newIP != null && newIP != this.currentDNSIP && status >= 200 && status < 300)
                {
                    UpdateIP(newIP);
                }
            }

            
            // Re start the timer with short or standard interval
            if (!this.initialized)
            {
                timer.Interval = shortinterval;
                timer.Start();
            }
            else if (this.monitoring)
            {
                timer.Interval = interval;
                timer.Start();
            }

            
        }

        //--------------------------------------------------------------
        // OnStart
        //
        // Starts the service
        //
        protected override void OnStart(string[] args)
        {
            eventLog1.WriteEntry("Started");

            timer = new Timer();
            timer.Interval = shortinterval;
            timer.Elapsed += timer_Elapsed;
            timer.Start();
        }

        //-----------------------------------------------------------
        protected override void OnStop()
        {
            timer.Stop();
        }

        //-----------------------------------------------------------
        // DomainConnectDDNSUpdate
        //
        // Service initialization
        //
        public DomainConnectDDNSUpdate()
        {
            InitializeComponent();

            eventLog1 = new System.Diagnostics.EventLog();
            if (!System.Diagnostics.EventLog.SourceExists("GoDaddyDNSUpdate"))
            {
                System.Diagnostics.EventLog.CreateEventSource("GoDaddyDNSUpdate", "");
            }
            eventLog1.Source = "GoDaddyDNSUpdate";
            eventLog1.Log = "";
        }
    }
}

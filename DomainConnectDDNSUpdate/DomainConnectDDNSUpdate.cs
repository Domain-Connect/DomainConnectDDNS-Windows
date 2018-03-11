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
using System.Configuration;

namespace DomainConnectDDNSUpdate
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
        //private int numMonitorFails = 0;

        // Values from the registry        
        private string domain;                  // Name of the domain
        private string host;                    // Host (sub-domain)
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
            int status = 0;

            // Apply template and store the response.
            string response = OAuthHelper.OAuthHelper.ApplyTemplate(newIP, out status);

            if (response == null || status < 200 || status >= 300)
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
                this.numMonitorFails = 0;

                eventLog1.WriteEntry("IP Updated to " + newIP);

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

            this.domain = DomainConnectDDNS.DomainConnectDDNS.Default.domain_name;
            this.host = DomainConnectDDNS.DomainConnectDDNS.Default.host;
            this.access_token = DomainConnectDDNS.DomainConnectDDNS.Default.access_token;
            this.refresh_token = DomainConnectDDNS.DomainConnectDDNS.Default.refresh_token;
            this.dns_provider = DomainConnectDDNS.DomainConnectDDNS.Default.provider_name;
            this.urlAPI = DomainConnectDDNS.DomainConnectDDNS.Default.urlAPI;

            if (this.domain == null || this.domain == "")
            {
                eventLog1.WriteEntry("Initiaize failure. Missing .", EventLogEntryType.Error);
                  
                return;
            }

            // Query the initial (current) IP from DNS
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

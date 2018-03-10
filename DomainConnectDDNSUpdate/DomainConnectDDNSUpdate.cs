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

using GoDaddyRestAPI;


namespace GoDaddyDNSUpdate
{
    public partial class DomainConnectDDNSUpdate : ServiceBase
    {
        // The IP Address that GoDaddy has for the A Record
        string goDaddyIP = null;

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
        private string domainName;              // Name of the domain
        private string apiKey;                  // APIKey:Secret    

        private string aName;
        private string aRecordTemplate;

        //-------------------------------------------------------
        // UpdateIP
        //
        // Updates the IP with GoDaddy according to the template
        //
        public bool UpdateIP(string newIP)
        {
            bool result = false;
            int status = 0;

            result = GoDaddyRestAPI.GoDaddyRestAPI.UpdateGoDaddyIP(this.domainName, this.apiKey, "A", this.aName, newIP, this.aRecordTemplate, out status);            
            
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

        
        //-------------------------------------------------------
        // InitService
        //
        // Initializes the service
        //
        private void InitService()
        {

            // Read values in from the registry
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\GoDaddyDNSUpdate\Config"))
            {
                if (key == null)
                {
                    eventLog1.WriteEntry("Unable to get configuration from registry", EventLogEntryType.Error);

                    // Don't mark as initialized....we'll keep trying in case data is written to registry later

                    return;
                }
                
                this.domainName = (string)key.GetValue("Domain", null);
                this.apiKey = (string)key.GetValue("ApiKey", null);
                this.aName = (string)key.GetValue("AName", "@");
                this.aRecordTemplate = (string)key.GetValue("ATemplate", "[{\"data\" : \"%ip%\", \"ttl\" : 1800}]");

                if (this.apiKey == null || this.apiKey == "" || this.domainName == null || this.domainName == "")
                {
                    eventLog1.WriteEntry("Initiaize failure. Missing Key or domain name.", EventLogEntryType.Error);
                  
                    return;
                }
                
                // Query the initial (current) IP from GoDaddy
                int status = 0;
                this.goDaddyIP = GoDaddyRestAPI.GoDaddyRestAPI.GetGoDaddyIP(this.domainName, this.apiKey, "A", this.aName, out status);
                
                // If we have an IP to update
                if (this.goDaddyIP == null)
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
                string newIP = GoDaddyRestAPI.GoDaddyRestAPI.GET("http://api.ipify.org", out status);

                if (newIP != null && newIP != this.goDaddyIP && status >= 200 && status < 300)
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
                string newIP = GoDaddyRestAPI.GoDaddyRestAPI.GET("http://api.ipify.org", out status);            

                // Update the IP if it has changed
                if (newIP != null && newIP != this.goDaddyIP && status >= 200 && status < 300)
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
        // GoDaddyDNSUpdate
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

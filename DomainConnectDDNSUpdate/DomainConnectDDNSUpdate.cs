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
        // Timer for the updates
        private Timer timer;
        private int longInterval = 600000;          // Every 10 minutes 
        private int shortinterval = 60000;          // Every 1 minute 

        // Worker object
        DomainConnectDDNSWorker worker;

        //----------------------------------------------------
        // timer_Elaspsed
        //
        // Call back function from the timer
        //
        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Stop the timer
            timer.Stop();

            // Do the work
            this.worker.DoWork();
            
            // Re start the timer with short or standard interval depending on if we have initialized
            if (!this.worker.initialized)
            {
                timer.Interval = shortinterval;
                timer.Start();
            }
            else if (this.worker.monitoring)
            {
                timer.Interval = longInterval;
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

            this.worker = new DomainConnectDDNSWorker(eventLog1);

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
            if (!System.Diagnostics.EventLog.SourceExists("DomainConnectDDNSUpdate"))
            {
                System.Diagnostics.EventLog.CreateEventSource("DomainConnectDDNSUpdate", "");
            }
            eventLog1.Source = "DomainConnectDDNSUpdate";
            eventLog1.Log = "";
        }
    }
}

using System.ServiceProcess;
using System.Timers;

namespace DomainConnectDDNSUpdate
{
    public partial class DomainConnectDDNSUpdate : ServiceBase
    {
        // Timer for the updates
        private Timer timer;
        private int longInterval = 900001;          // Approximately 15 minutes, but a prime number because I'm a nerd.
        private int shortInterval = 60041;          // Approximately 1 minute, but a prime number because I'm still a nerd.
        private int tinyInterval = 1007;            // Approximately 10 seconds. Yup still prime.

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
                timer.Interval = shortInterval;
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
            eventLog1.WriteEntry("DomainConnectDDNSUpdate: Service Started");

            System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);

            this.worker = new DomainConnectDDNSWorker(eventLog1);

            timer = new Timer();
            timer.Interval = tinyInterval;
            timer.Elapsed += timer_Elapsed;
            timer.Start();
        }

        //-----------------------------------------------------------
        protected override void OnStop()
        {
            
            timer.Stop();
            eventLog1.WriteEntry("DomainConnectDDNSUpdate: Service Stopped");
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

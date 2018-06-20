using System.Diagnostics;
using System.ServiceProcess;
using System.Timers;

namespace DomainConnectDDNSUpdate
{
	public partial class DomainConnectDDNSUpdate : ServiceBase
    {
    	DomainConnectRegularUpdater _updater;

        //--------------------------------------------------------------
        // OnStart
        //
        // Starts the service
        //
        protected override void OnStart(string[] args)
        {
            eventLog1.WriteEntry("DomainConnectDDNSUpdate: Service Started");

            System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
			
            _updater = new DomainConnectRegularUpdater(eventLog1);
            _updater.Start();
        }

        //-----------------------------------------------------------
        protected override void OnStop()
        {
        	_updater.Stop();
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

            eventLog1 = new EventLog();
            if (!EventLog.SourceExists("DomainConnectDDNSUpdate"))
            {
                EventLog.CreateEventSource("DomainConnectDDNSUpdate", "");
            }
            eventLog1.Source = "DomainConnectDDNSUpdate";
            eventLog1.Log = "";
        }
    }
}

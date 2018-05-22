
namespace Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Diagnostics.EventLog eventLog1;

            eventLog1 = new System.Diagnostics.EventLog();
            if (!System.Diagnostics.EventLog.SourceExists("DomainConnectDDNSUpdate"))
            {
                System.Diagnostics.EventLog.CreateEventSource("DomainConnectDDNSUpdate", "");
            }
            eventLog1.Source = "DomainConnectDDNSUpdate";
            eventLog1.Log = "";

            DomainConnectDDNSUpdate.DomainConnectDDNSWorker worker = new DomainConnectDDNSUpdate.DomainConnectDDNSWorker(eventLog1);

            worker.InitService();

            if (worker.initialized)
            {
                if (worker.monitoring)
                {
                    worker.CheckToken();
                }
                if (worker.monitoring)
                {
                    worker.CheckIP();
                }
            }            
        }
    }
}

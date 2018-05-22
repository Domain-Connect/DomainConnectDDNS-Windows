using System.ServiceProcess;

namespace DomainConnectDDNSUpdate
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
            { 
                new DomainConnectDDNSUpdate() 
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}

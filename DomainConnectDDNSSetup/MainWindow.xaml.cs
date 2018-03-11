using System;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.IO;
using OAuthHelper;


namespace gddnsserviceuserinfo
{


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Make sure that the dialog is in front
            Application.Current.MainWindow.Activate();
        }



        private void Button_Click_Cancel(object sender, RoutedEventArgs e)
        {                        
            //Non zero should abort installation 
            this.shuttingDown = true;
            Application.Current.Shutdown(1);
        }

        private static bool IsValidDomainName(string name)
        {
            if (!name.Contains('.'))
                return false;

            return Uri.CheckHostName(name) != UriHostNameType.Unknown;
        }

        private static bool IsValidSubDomainName(string domainName, string subDomainName)
        {
            if (subDomainName.Contains('.'))
                return false;

            return IsValidDomainName(subDomainName + "." + domainName);
        }
       
        private void Button_Click_OK(object sender, RoutedEventArgs e)
        {
          

            string domainnameText = this.domainname.Text.Trim();
            string subdomainnameText = this.subdomainname.Text.Trim();

            if (domainnameText == null || domainnameText == "" || !IsValidDomainName(domainnameText))
            {
                this.Error.Content = "Valid domain name is required.";

                return;
            }

            if (subdomainnameText != null && subdomainnameText != "" && !IsValidSubDomainName(domainnameText, subdomainnameText))
            {
                this.Error.Content = "Sub domain entered is not valid.";

                return;
            }

            string providerName, urlAPI, urlAsyncUX;
            if (!OAuthHelper.OAuthHelper.GetConfig(domainnameText, out providerName, out urlAPI, out urlAsyncUX))
            {
                this.Error.Content = "Domain doesn't support Domain Connect";

                return;
            }

            DomainConnectDDNS.DomainConnectDDNS.Default.domain_name = domainnameText;
            DomainConnectDDNS.DomainConnectDDNS.Default.host = subdomainnameText;
            DomainConnectDDNS.DomainConnectDDNS.Default.provider_name = providerName;
            DomainConnectDDNS.DomainConnectDDNS.Default.urlAPI = urlAPI;
            DomainConnectDDNS.DomainConnectDDNS.Default.Save();


            // Get the value of the _domainconnect record
            string url = urlAsyncUX + "/v2/domainTemplates/providers/exampleservice.domainconnect.org/services/template1?";
            url += ("domain=" + domainnameText + "&client_id=exampleservice.domainconnect.org&scope=template1");

            string redirect_uri = "http://exampleservice.domainconnect.org/async_oauth_response?domain=" + domainnameText + "&hosts=" + subdomainnameText + "&dns_provider=" + providerName + "&code_only=1";

            url += "&redirect_uri=" + WebUtility.UrlEncode(redirect_uri);

            System.Diagnostics.Process.Start(url);
            
        }

        private bool shuttingDown = false;

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Non zero should abort installation 
            if (!this.shuttingDown)
                Application.Current.Shutdown(1);
        }

        private void finishbutton_Click(object sender, RoutedEventArgs e)
        {
            string accesscode = this.accesscode.Text;

            if (accesscode == null || accesscode == "")
            { 
                this.Error.Content = "Please get an access code and paste it in.";
                return;
            }

            string domain_name = DomainConnectDDNS.DomainConnectDDNS.Default.domain_name;
            string host = DomainConnectDDNS.DomainConnectDDNS.Default.host;
            string provider_name = DomainConnectDDNS.DomainConnectDDNS.Default.provider_name;
            string urlAPI = DomainConnectDDNS.DomainConnectDDNS.Default.urlAPI;

            string access_token;
            string refresh_token;
            int iat;
            int expires_in;
            if (!OAuthHelper.OAuthHelper.GetTokens(accesscode, domain_name, host, provider_name, urlAPI, out access_token, out refresh_token, out expires_in, out iat))
            {
                this.Error.Content = "Error using access code. Please try again.";
                return;
            }

            DomainConnectDDNS.DomainConnectDDNS.Default.access_token = access_token;
            DomainConnectDDNS.DomainConnectDDNS.Default.refresh_token = refresh_token;
            DomainConnectDDNS.DomainConnectDDNS.Default.iat = iat;
            DomainConnectDDNS.DomainConnectDDNS.Default.expires_in = expires_in;


            // -i is passed from installer.  When run from command line, tell the user they need to restart
            string[] args = Environment.GetCommandLineArgs();            
            if (!args.Contains("-i"))
                MessageBox.Show("Settings applied. You must restart the GoDaddy DNS Update Service for changes to take affect.", "Restart Service");

            // Exit application
            this.shuttingDown = true;
            Application.Current.Shutdown(0);
        }
    }
}
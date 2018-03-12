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
            // Verify we can write to the registry
            RegistryKey lkey = Registry.LocalMachine;
            try
            {
                lkey = lkey.CreateSubKey(@"SYSTEM\CurrentControlSet\services\DomainConnectDDNSUpdate\Config");
            }
            catch (System.UnauthorizedAccessException)
            {
                this.Error.Content = "Unable to save values. Please run application with Administrator privilages.";

                return;
            }

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

            // Get Domain Connect Config Settings for provider
            string providerName, urlAPI, urlAsyncUX;
            if (!OAuthHelper.OAuthHelper.GetConfig(domainnameText, out providerName, out urlAPI, out urlAsyncUX))
            {
                this.Error.Content = "Domain doesn't support Domain Connect";

                return;
            }

            // Verify our template is supported
            string checkURL = urlAPI + "/v2/templates/providers/exampleservice.domainconnect.org/services/template1/";
            int status = 0;
            RestAPIHelper.RestAPIHelper.GET(checkURL, out status);
            if (status != 200)
            {
                this.Error.Content = "DNS Provider does not support Dynamic DNS Template";

                return;
            }

            // Write the values to the registry
            lkey.SetValue("domain_name", domainnameText);
            lkey.SetValue("host", subdomainnameText);
            lkey.SetValue("provider_name", providerName);
            lkey.SetValue("urlAPI", urlAPI);

            // Get the value of the _domainconnect record
            string url = urlAsyncUX + "/v2/domainTemplates/providers/exampleservice.domainconnect.org/services/template1?";
            url += ("domain=" + domainnameText + "&client_id=exampleservice.domainconnect.org&scope=template1");

            string redirect_uri = "http://exampleservice.domainconnect.org/ddns_oauth_code?domain=" + domainnameText + "&hosts=" + subdomainnameText + "&dns_provider=" + providerName + "&code_only=1";

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

            // Verify we can write to the registry
            RegistryKey lkey = Registry.LocalMachine;
            try
            {
                lkey = lkey.CreateSubKey(@"SYSTEM\CurrentControlSet\services\DomainConnectDDNSUpdate\Config");
            }
            catch (System.UnauthorizedAccessException)
            {
                this.Error.Content = "Unable to save values. Please run application with Administrator privilages.";

                return;
            }

            string domain_name = (string)lkey.GetValue("domain_name");
            string host = (string)lkey.GetValue("host");
            string provider_name = (string)lkey.GetValue("provider_name");
            string urlAPI = (string)lkey.GetValue("urlAPI");

            string access_token;
            string refresh_token;
            int iat;
            int expires_in;
            if (!OAuthHelper.OAuthHelper.GetTokens(accesscode, domain_name, host, provider_name, urlAPI, out access_token, out refresh_token, out expires_in, out iat))
            {
                this.Error.Content = "Error using access code. Please try again.";
                return;
            }

            lkey.SetValue("access_token", access_token);
            lkey.SetValue("refresh_token", refresh_token);
            lkey.SetValue("iat", iat, RegistryValueKind.DWord);
            lkey.SetValue("expires_in", expires_in, RegistryValueKind.DWord);

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
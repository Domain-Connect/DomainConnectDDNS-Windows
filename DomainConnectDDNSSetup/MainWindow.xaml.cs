using System;
using System.Linq;
using System.Windows;
using System.Net;
using DomainConnectDDNSUpdate;

namespace DomainConnectDDNSSetup
{


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DomainConnectDDNSSettings settings;

        public MainWindow()
        {
            InitializeComponent();

            // Make sure that the dialog is in front
            Application.Current.MainWindow.Activate();

            this.settings = new DomainConnectDDNSSettings();
            this.settings.Load("settings.txt");
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

            // Get Domain Connect Config Settings for provider
            string providerName, urlAPI, urlAsyncUX;
            if (!OAuthHelper.OAuthHelper.GetConfig(domainnameText, out providerName, out urlAPI, out urlAsyncUX))
            {
                this.Error.Content = "Domain doesn't support Domain Connect";

                return;
            }

            // Verify our template is supported
            string checkURL = urlAPI + "/v2/domainTemplates/providers/domainconnect.org/services/dynamicdns";
            int status = 0;
            RestAPIHelper.RestAPIHelper.GET(checkURL, out status);
            if (status != 200)
            {
                this.Error.Content = "DNS Provider does not support Dynamic DNS Template";

                return;
            }

            // Write the settings
            this.settings.Clear();
            this.settings.WriteValue("domain_name", domainnameText);
            this.settings.WriteValue("host", subdomainnameText);
            this.settings.WriteValue("provider_name", providerName);
            this.settings.WriteValue("urlAPI", urlAPI);
            this.settings.Save("settings.txt");

            // Form the URL for getting consent
            string url;
            if (providerName.ToLower() == "godaddy" || providerName.ToLower() == "secureserver")
            {
                url = urlAsyncUX + "/v2/domainTemplates/providers/domainconnect.org/services/dynamicdns?";
            }
            else
            {
                url = urlAsyncUX + "/v2/domainTemplates/providers/domainconnect.org?";
            }

            url += ("domain=" + domainnameText + "&host=" + subdomainnameText + "&client_id=domainconnect.org&scope=dynamicdns");

            string redirect_uri = "https://dynamicdns.domainconnect.org/ddnscode";

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

            string domain_name = (string)this.settings.ReadValue("domain_name", "");
            string host = (string)this.settings.ReadValue("host", "");
            string provider_name = (string)this.settings.ReadValue("provider_name", "");
            string urlAPI = (string)this.settings.ReadValue("urlAPI", "");
          
            string access_token;
            string refresh_token;
            int iat;
            int expires_in;
            if (!OAuthHelper.OAuthHelper.GetTokens(accesscode, domain_name, host, provider_name, urlAPI, false, out access_token, out refresh_token, out expires_in, out iat))
            {
                this.Error.Content = "Error using access code. Please try again.";
                return;
            }

            this.settings.WriteValue("access_token", access_token);
            this.settings.WriteValue("refresh_token", refresh_token);
            this.settings.WriteValue("iat", iat);
            this.settings.WriteValue("expires_in", expires_in);
            this.settings.Save("settings.txt");
            
            // -i is passed from installer.  When run from command line, tell the user they need to restart
            string[] args = Environment.GetCommandLineArgs();            
            if (!args.Contains("-i"))
                MessageBox.Show("Settings applied. You must restart the Domain Connect DDNS Update Service for changes to take affect.", "Restart Service");

            // Exit application
            this.shuttingDown = true;
            Application.Current.Shutdown(0);
        }
    }
}
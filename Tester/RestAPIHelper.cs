using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace GoDaddyRestAPI
{
    public class GoDaddyRestAPI
    {
        static string serviceURL = "https://api.godaddy.com:443/api";

        /////////////////////////////////////////
        // GET
        //
        // Implements a very simple http GET, returning the response as a string. Failures return null.
        //
        public static string GET(string url, out int status)
        {
            HttpWebResponse response = null;
            status = 0;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                
                response = (HttpWebResponse)request.GetResponse();
                Stream stream = response.GetResponseStream();
                StreamReader reader = new StreamReader(stream);

                string data = reader.ReadToEnd();

                reader.Close();
                stream.Close();

                status = (int)response.StatusCode;

                return data;
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError)
                    status = (int)((HttpWebResponse)e.Response).StatusCode;                   

                return null;
            }
            catch
            {
                return null;
            }
        }

   

        /////////////////////////////////////////////////
        // GetDNSIP
        //
        // Will find the IP that DNS is reporting for the A Record for a domain
        //
        public static string GetDNSIP(string host)
        {
            IPHostEntry hostEntry = Dns.GetHostEntry(host);

            if (hostEntry.AddressList.Length == 1)
            {
                IPAddress ip = hostEntry.AddressList[0];

                return ip.ToString();
            }

            return null;
        }

        ////////////////////////////////////////////////////////////
        // Wrappers around the GoDaddyRest API
        ////////////////////////////////////////////////////////////

        /////////////////////////////////////////////////
        // GetGoDaddyIP
        //
        // Will get the A Record IP Address for the domain from GoDaddy's DNS
        //
        // null indicates a failure of some kind
        //
        public static string GetGoDaddyIP(string domainName, string apiKey, string recordType, string name, out int status)
        {
            string url = serviceURL + "/v1/domains/" + domainName + "/records/" + recordType + "/" + name;

            string data = GoDaddyRest(url, apiKey, "GET", null, out status);
         
            if (data == null)
                return null;

            try
            {
                JavaScriptSerializer jss = new JavaScriptSerializer();

                var jObject = jss.Deserialize<dynamic>(data);

                string result = (string)jObject[0]["data"];

                if (result == null) result = "";

                return result;
            }
            catch
            {
                return null;
            }
        }

        /////////////////////////////////////////////////
        // UpdateGoDaddyIP
        //
        // Will update an IP into GoDaddy's DNS
        //
        public static bool UpdateGoDaddyIP(string domainName, string apiKey, string recordType, string name, string ipAddress, string template, out int status)
        {            
            string data = template.Replace("%ip%", ipAddress);

            string url = serviceURL + "/v1/domains/" + domainName + "/records/" + recordType + "/" + name;

            string result_data = GoDaddyRest(url, apiKey, "PUT", data, out status);

            if (result_data == null)
                return false;
            else
                return true;            
        }
        
        public static string GetDomains(string apiKey, out int status)
        {
            string url = serviceURL + "/v1/domains/";

            string data = GoDaddyRest(url, apiKey, "GET", null, out status);

            return data;
        }

        public static string GetDomain(string apiKey, string domainName, out int status)
        {
            string url = serviceURL + "/v1/domains/" + domainName;

            string data = GoDaddyRest(url, apiKey, "GET", null, out status);

            return data;
        }

        public static string GetDomainRecords(string apiKey, string domainName, out int status)
        {
            string url = serviceURL + "/v1/domains/" + domainName + "/records/";

            string data = GoDaddyRest(url, apiKey, "GET", null, out status);

            return data;
        }
    }
}

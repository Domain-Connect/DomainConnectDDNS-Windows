using DnsClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace RestAPIHelper
{
    public class RestAPIHelper
    {

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
        // POST
        //
        // Implementation of http POST. Again returns responses as strings and failures as null.
        // Custom headers can be passed in as a dictionary of key/value string pairs, or as null for no header.
        //
        public static string POST(string url, Dictionary<string, string> headers, out int status)
        {
            HttpWebResponse response = null;
            status = 0;
            try
            { 
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                request.Method = "POST";
                if (headers != null)
                {
                    foreach (KeyValuePair<string, string> entry in headers)
                        request.Headers[entry.Key] = entry.Value;
                }

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
                    status = (int) ((HttpWebResponse) e.Response).StatusCode;                   

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
        public static string GetDNSIPOld(string host)
        {
            IPHostEntry hostEntry = Dns.GetHostEntry(host);

            if (hostEntry.AddressList.Length == 1)
            {
                IPAddress ip = hostEntry.AddressList[0];

                return ip.ToString();
            }

            return null;
        }


        static public string GetDNSIP(string host)
        {
            try
            {
                var client = new LookupClient();
                client.UseCache = true;

                var result = client.Query(host, QueryType.A);

                foreach (var answer in result.Answers)
                {
                    if (answer.RecordType == DnsClient.Protocol.ResourceRecordType.A)
                    {
                        DnsClient.Protocol.ARecord aRecord = (DnsClient.Protocol.ARecord)answer;

                        return aRecord.Address.ToString();

                    }
                }

                return "";
            }
            catch
            {
                return null;
            }

        }

    }
}

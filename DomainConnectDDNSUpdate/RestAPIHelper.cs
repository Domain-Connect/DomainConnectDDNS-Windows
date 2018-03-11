﻿using System;
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

        public static string POST(string url, out int status)
        {
            HttpWebResponse response = null;
            status = 0;
            try
            { 
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                request.Method = "POST";
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
       


    }
}

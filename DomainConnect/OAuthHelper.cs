using System;
using System.Web.Script.Serialization;
using System.Net;
using System.Collections.Generic;
using DnsClient;

namespace OAuthHelper
{
    public class OAuthHelper
    {        
        //
        // GetTokens
        //
        // Given an input of either a response code from an oauth authorization, or a refresh token,
        // will fetch the access_token and a refresh_token using oauth
        //
        static public bool GetTokens(string code, string domain, string host, string dns_provider, string urlAPI, bool use_refresh, out string access_token, out string refresh_token, out int expires_in, out int iat)
        {
            int status = 0;
            iat = 0;
            expires_in = 0;
            string grant = "authorization_code";
            string code_key = "code";
            if (use_refresh)
            {
                grant = "refresh_token";
                code_key = "refresh_token";
            }

            refresh_token = null;
            access_token = null;

            string redirect_url = "https://dynamicdns.domainconnect.org/ddnscode";

            string url = urlAPI + "/v2/oauth/access_token?" + code_key + "=" + code + "&grant_type=" + grant + "&client_id=domainconnect.org" + "&client_secret=inconceivable&redirect_uri=" + WebUtility.UrlEncode(redirect_url);

            string json = RestAPIHelper.RestAPIHelper.POST(url, null, out status);
            if (status < 200 || status >= 300)
            {
                return false;
            }
            
            var jss = new JavaScriptSerializer();
            var table = jss.Deserialize<dynamic>(json);


            access_token = table["access_token"];
            refresh_token = table["refresh_token"];
            expires_in = table["expires_in"];
            iat = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            return true;
        }

        static public bool UpdateIP(string domain_name, string host, string urlAPI, string access_token, string newIP)
        {
            int status = 0;

            string templateUrl = urlAPI + "/v2/domainTemplates/providers/domainconnect.org/services/dynamicdns/apply?domain=" + domain_name + "&host=" + host + "&force=1&IP=" + newIP;
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Authorization", "Bearer " + access_token);
            string response = RestAPIHelper.RestAPIHelper.POST(templateUrl, headers, out status);

            if (response != null && status >= 200 && status < 300)
            {
                return true;
            }

            return false;
        }

        static public string GetDomainConnectRecord(string host)
        {
            var client = new LookupClient();
            client.UseCache = true;

            var result = client.Query("_domainconnect." + host, QueryType.TXT);

            foreach (var answer in result.Answers)
            {
                if (answer.RecordType == DnsClient.Protocol.ResourceRecordType.TXT)
                {
                    DnsClient.Protocol.TxtRecord txtRecord = (DnsClient.Protocol.TxtRecord)answer;

                    return ((string[])txtRecord.Text)[0];
                }
            }

            return null;

        }

        static public bool GetConfig(string domain, out string providerName, out string urlAPI, out string urlAsyncUX)
        {
            providerName = null;
            urlAPI = null;
            urlAsyncUX = null;

            string dcr = GetDomainConnectRecord(domain);

            if (dcr != null)
            {
                string url = "https://" + dcr + "/v2/" + domain + "/settings";
                int status = 0;
                string json = RestAPIHelper.RestAPIHelper.GET(url, out status);
                if (json != null && status >= 200 && status < 300)
                {
                    var jss = new JavaScriptSerializer();
                    var table = jss.Deserialize<dynamic>(json);

                    providerName = table["providerName"];
                    urlAPI = table["urlAPI"];
                    urlAsyncUX = table["urlAsyncUX"];

                    return true;
                }
            }

            return false;
        }
    }
}

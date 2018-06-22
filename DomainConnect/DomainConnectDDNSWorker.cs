using System;
using System.Diagnostics;

namespace DomainConnectDDNSUpdate
{
    public class DomainConnectDDNSWorker
    {
        // The IP Address that DNS has for the A Record
        private string _currentIP = null;
		public string CurrentIP {
			get {
				return _currentIP;
			}
		}
        
        // Service is initialized
        private bool _initialized = false;
		public bool Initialized {
			get {
				return _initialized;
			}
		}
        
        private int numInitializeFails = 0;
		public int NumInitializeFails {
			get {
				return numInitializeFails;
			}
		}
        // Service is good for monitoring DNS changes or token refreshes
        private bool _monitoring = false;
		public bool Monitoring {
			get {
				return _monitoring;
			}
		}
        private int _numUpdateFails = 0;
		public int NumUpdateFails {
			get {
				return _numUpdateFails;
			}
		}

        private int _numRefreshFails = 0;
		public int NumRefreshFails {
			get {
				return _numRefreshFails;
			}
		}
        
        private DateTime _lastIPUpdate = DateTime.MinValue;
		public DateTime LastIPUpdate {
			get {
				return _lastIPUpdate;
			}
		}
        
        // Settings
        DomainConnectDDNSSettings settings;
        
        // Event log for logging errors and events
        EventLog eventLog1;

        public DomainConnectDDNSWorker(EventLog eventLog)
        {
            this.eventLog1 = eventLog;

            this.settings = new DomainConnectDDNSSettings();
            this.settings.Load("settings.txt");
        }

        private void WriteEvent(string message, EventLogEntryType elt = EventLogEntryType.Information)
        {
            if (this.eventLog1 != null)
            {
                this.eventLog1.WriteEntry(message, elt);
            }
            else
            	Debug.WriteLine(message, elt.ToString());
            if (OnStatusUpdate != null)
            	OnStatusUpdate(message, elt);
        }

        public delegate void dgStatusUpdate(string text, EventLogEntryType elt);
        public event dgStatusUpdate OnStatusUpdate;
        
        //---------------------------------------------------
        // RefreshToken
        //
        // Will refresh the access token using oAuth
        //
        public bool RefreshToken()
        {
            string new_access_token, new_refresh_token;
            int new_expires_in, new_iat;

            if (OAuthHelper.OAuthHelper.GetTokens((string)this.settings.ReadValue("refresh_token", ""),
                                    (string)this.settings.ReadValue("domain_name", ""),
                                    (string)this.settings.ReadValue("host", ""),
                                    (string)this.settings.ReadValue("provider_name", ""),
                                    (string)this.settings.ReadValue("urlAPI", ""),
                                    true,
                                    out new_access_token, out new_refresh_token, out new_expires_in, out new_iat))
            {
                this.settings.WriteValue("refresh_token", new_refresh_token);
                this.settings.WriteValue("access_token", new_access_token);
                this.settings.WriteValue("expires_in", new_expires_in);
                this.settings.WriteValue("iat", new_iat);
                this.settings.Save("settings.txt");

                return true;
            }

            return false;
        }

        //-------------------------------------------------------
        // UpdateIP
        //
        // Updates the IP with with the DNS Provider using oAuth
        //
        public bool UpdateIP(string newIP)
        {           
            // Apply template and store the response.
            string urlAPI = (string)this.settings.ReadValue("urlAPI", "");
            string domain_name = (string)this.settings.ReadValue("domain_name", "");
            string host = (string)this.settings.ReadValue("host", "");
            string access_token = (string)this.settings.ReadValue("access_token", "");

            if (OAuthHelper.OAuthHelper.UpdateIP(domain_name, host, urlAPI, access_token, newIP))
            { 
                this._currentIP = newIP;
                this._lastIPUpdate = DateTime.Now;
                return true;
            }

            return false;          
        }

        //-------------------------------------------------------
        // InitService
        //
        // Initializes the service
        //
        public void InitService()
        {
            int status = 0;

            this.settings = new DomainConnectDDNSSettings();
            this.settings.Load("settings.txt");

            string domain_name = (string)this.settings.ReadValue("domain_name", null);
            string access_token = (string)this.settings.ReadValue("access_token", null);
            string refresh_token = (string)this.settings.ReadValue("refresh_token", null);
            string provider_name = (string)this.settings.ReadValue("provider_name", null);
            string urlAPI = (string)this.settings.ReadValue("urlAPI", null);
            string host = (string)this.settings.ReadValue("host", null);

            if (String.IsNullOrEmpty(domain_name) ||
                String.IsNullOrEmpty(access_token) ||
                String.IsNullOrEmpty(refresh_token) ||
                String.IsNullOrEmpty(provider_name) ||
                String.IsNullOrEmpty(urlAPI))             
            {
                this.WriteEvent("Initiaize failure: missing data. Run installer and restart service.", EventLogEntryType.Error);

                this._initialized = true;

                return;
            }

            // Query the initial (current) IP from DNS. Null is an error, "" means no current value
            string fqdn = domain_name;
            if (!String.IsNullOrEmpty(host))
                fqdn = host + "." + fqdn;
            this._currentIP = RestAPIHelper.RestAPIHelper.GetDNSIP(fqdn);

            if (this._currentIP == null)
            {
                this.WriteEvent("Failed to read current IP for domain.", EventLogEntryType.Error);

                // We won't penalize if internet or service is down
                if (status != 0 && status < 500)
                    this.numInitializeFails++;

                // After 10 tries to init that fail, we give up
                if (this.numInitializeFails > 10)
                {
                    this.WriteEvent("Initialize failure: threshold for attempted initialize reached. Restart service.", EventLogEntryType.Error);
                    this._initialized = true;
                }

                return;
            }

            // Service successfully initalized. Mark it sas such and start monitoring
            this._initialized = true;
            this._monitoring = true;

            this.WriteEvent("Initialize success: Initialized and monitoring for changes.", EventLogEntryType.Information);
        }

        //------------------------------------------
        // CheckIP
        //
        // Checks for IP changes and does the update
        //
        public void CheckIP(bool force)
        {
            // See if our IP changed
            int status = 0;
            string newIP = null;
            try
            {
                newIP = RestAPIHelper.RestAPIHelper.GET("http://api.ipify.org", out status);
            }
            catch
            {
                status = 0;
            }

            // Update the IP if it has changed
            if (force || (newIP != null && newIP != this._currentIP && status >= 200 && status < 300))
            {
                this.WriteEvent("Updating IP to " + newIP);
                if (!UpdateIP(newIP))                
                {
                    this._numUpdateFails++;
                    this.WriteEvent("Updating IP failed.", EventLogEntryType.Error);

                    // After 10 failures, we stop updating
                    if (this._numUpdateFails > 10)
                    {
                        this.WriteEvent("Updating IP threshold reached. Must restart service.", EventLogEntryType.Error);

                        this._monitoring = false;
                    }
                }
                else
                {
                    this._numUpdateFails = 0;
                    this.WriteEvent("IP updated", EventLogEntryType.Information);
                }
            }
        }

        //----------------------------------
        // CheckToken
        //
        // Will check if the token is relatively "fresh" for doing updates
        //
        public void CheckToken(bool force)
        {
            // See if our token is in good shape. Refresh if we are getting close to expiry
            int expires_in = (int)this.settings.ReadValue("expires_in", 0);
            int iat = (int)this.settings.ReadValue("iat", 0);
            int now = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            if (force || expires_in * 7 / 8 + iat < now)
            {
                this.WriteEvent("Refreshing access token", EventLogEntryType.Information);
                if (!this.RefreshToken())
                {
                    this.WriteEvent("Refreshing access failed", EventLogEntryType.Error);
                    this._numRefreshFails++;
                    if (this._numRefreshFails > 10)
                    {
                        this.WriteEvent("Refreshing access token failed threshold reached. Must restart service.");

                        this._monitoring = false;
                    }
                }
                else
                {
                    this._numRefreshFails = 0;
                }
            }
        }

        //---------------------------------------
        // DoWork
        //
        // This is the main function that does the work of the service on the timer
        public void DoWork()
        {
            // If we haven't initialized, try to initialize now
            if (!this._initialized)
            {
                this.InitService();
                // makes sense to update IP at the start, so that we are sure we can use the token
                this.CheckIP(true);
            }

            // If we are monitoring (after successful initialize), check for token refresh
            if (this._monitoring)
            {
                this.CheckToken(false);
            }

            // If we are monitoring (after initialize and token refresh), check for IP changes
            if (this._monitoring)
            {
                this.CheckIP(false);
            }
        }

    }
}

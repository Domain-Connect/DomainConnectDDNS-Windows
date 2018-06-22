/*
 * Created by SharpDevelop.
 * User: pkowalik
 * Date: 20/06/2018
 * Time: 12:28
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Diagnostics;
using System.Drawing;
using System.Security;
using System.Threading;
using System.Windows.Forms;
using DomainConnectDDNSUpdate;

namespace DomainConnectDDNSUpdateTray
{
	public sealed class NotificationIcon
	{
		DomainConnectRegularUpdater	_updater;
		EventLog _log;
		
		private NotifyIcon notifyIcon;
		private ContextMenu notificationMenu;

		#region Initialize icon and menu
		public NotificationIcon()
		{
			notifyIcon = new NotifyIcon();
			notificationMenu = new ContextMenu(InitializeMenu());
			
			notifyIcon.DoubleClick += IconDoubleClick;
			notifyIcon.Disposed += notifyIcon_Disposed;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NotificationIcon));
			notifyIcon.Icon = (Icon)resources.GetObject("$this.Icon");
			notifyIcon.ContextMenu = notificationMenu;
					
			startDomainConnect();
		}
		
		private MenuItem[] InitializeMenu()
		{
			MenuItem[] menu = new MenuItem[] {
				new MenuItem("About", menuAboutClick),
				new MenuItem("Exit", menuExitClick)
			};
			return menu;
		}
		#endregion

		void _updater_OnStatusUpdate(string text, EventLogEntryType elt)
		{
			notifyIcon.ShowBalloonTip(0, "Dynamic IP", text, elt == EventLogEntryType.Error ? ToolTipIcon.Error : ToolTipIcon.Info );
		}

		
		#region Main - Program entry point
		/// <summary>Program entry point.</summary>
		/// <param name="args">Command Line Arguments</param>
		[STAThread]
		public static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			
			bool isFirstInstance;
			// Please use a unique name for the mutex to prevent conflicts with other programs
			using (Mutex mtx = new Mutex(true, "DomainConnectDDNSUpdateTray", out isFirstInstance)) {
				if (isFirstInstance) {
					NotificationIcon notificationIcon = new NotificationIcon();
					notificationIcon.notifyIcon.Visible = true;					
					Application.Run();
					notificationIcon.notifyIcon.Dispose();
				} else {
					MessageBox.Show("Application already running");
				}
			} // releases the Mutex
		}
		#endregion
		
		#region Event Handlers
		private void menuAboutClick(object sender, EventArgs e)
		{
			MessageBox.Show("Dynamic IP Updater over Domain Connect");
		}
		
		private void menuExitClick(object sender, EventArgs e)
		{
			Application.Exit();
		}
		
		private void IconDoubleClick(object sender, EventArgs e)
		{
			notifyIcon.ShowBalloonTip(0, "Dynamic IP Status", String.Format(
@"Current IP: {0}
Last run: {1}
Next run: {2}
{3}",
				_updater.Worker.CurrentIP,				
				_updater.LastRun,
				_updater.NextRun,				
				!_updater.Worker.Initialized 
				? String.Format(
@"
Initialized: False
Number of Initialize fails: {0}", _updater.Worker.NumInitializeFails) 
				: String.Format(
@"
Monitoring: {0}
Number of Update fails: {1}
Number of Refresh fails: {2}",
				_updater.Worker.Monitoring,
				_updater.Worker.NumUpdateFails,
				_updater.Worker.NumRefreshFails)),
				ToolTipIcon.Info);
		}

		void notifyIcon_Disposed(object sender, EventArgs e)
		{
			stopDomainConnect();						
		}
		#endregion
		
		private void startDomainConnect()
		{
			_log = new EventLog();
			try {
	            if (!EventLog.SourceExists("DomainConnectDDNSUpdate"))
	            {
	                EventLog.CreateEventSource("DomainConnectDDNSUpdate", "");
	            }
	            _log.Source = "DomainConnectDDNSUpdate";
	            _log.Log = "";
	
				_log.WriteEntry("DomainConnectDDNSUpdate: Tray Service Started");
			}
			catch (SecurityException)
			{
				// in case no permission to read logs or create source we need to skip logging
				_log = null;
			}

            System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
			
            _updater = new DomainConnectRegularUpdater(_log);
			_updater.OnStatusUpdate += _updater_OnStatusUpdate;
			_updater.Start();
		}

		private void stopDomainConnect()
		{
        	_updater.Stop();
        	if (_log != null)
            	_log.WriteEntry("DomainConnectDDNSUpdate: Tray Service Stopped");			
		}

	}
}

using System.Diagnostics;
using System.Timers;
namespace DomainConnectDDNSUpdate
{
	public class DomainConnectRegularUpdater
	{
		// Timer for the updates
		private Timer _timer;

		private int longInterval = 900001;

		// Approximately 15 minutes, but a prime number because I'm a nerd.
		private int shortInterval = 60041;

		// Approximately 1 minute, but a prime number because I'm still a nerd.
		private int tinyInterval = 1007;

		// Approximately 10 seconds. Yup still prime.
		// Worker object
		DomainConnectDDNSWorker _worker;
		
		public delegate void dgStatusUpdate(string text, EventLogEntryType elt);
		public event dgStatusUpdate OnStatusUpdate;

		public DomainConnectRegularUpdater(EventLog log)
		{
			_worker = new DomainConnectDDNSWorker(log);
			_worker.OnStatusUpdate += _worker_OnStatusUpdate;
			_timer = new Timer();
			_timer.Interval = tinyInterval;
			_timer.Elapsed += timer_Elapsed;
		}
		
		public void Start()
		{
			_timer.Start();
		}

		void _worker_OnStatusUpdate(string text, EventLogEntryType elt)
		{
			if (OnStatusUpdate != null)
				OnStatusUpdate(text, elt);
		}
		//----------------------------------------------------
		// timer_Elaspsed
		//
		// Call back function from the timer
		//
		private void timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			// Stop the timer
			_timer.Stop();
			// Do the work
			this._worker.DoWork();
			// Re start the timer with short or standard interval depending on if we have initialized
			if (!this._worker.initialized) {
				_timer.Interval = shortInterval;
				_timer.Start();
			}
			else if (this._worker.monitoring) {
				_timer.Interval = longInterval;
				_timer.Start();
			}
		}

		public void Stop()
		{
			_timer.Stop();
		}
	}
}



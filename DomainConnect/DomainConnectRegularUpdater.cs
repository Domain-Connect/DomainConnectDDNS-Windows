using System;
using System.Diagnostics;
using System.Timers;
namespace DomainConnectDDNSUpdate
{
	public class DomainConnectRegularUpdater
	{
		// Timer for the updates
		private Timer _timer;

		// Approximately 15 minutes, but a prime number because I'm a nerd.
		private int longInterval = 900001;

		// Approximately 1 minute, but a prime number because I'm still a nerd.
		private int shortInterval = 60041;

		// Approximately 10 seconds. Yup still prime.
		private int tinyInterval = 1007;

		// Worker object
		private DomainConnectDDNSWorker _worker;
		public DomainConnectDDNSWorker Worker {
			get {
				return _worker;
			}
		}
		
		private DateTime _lastrun = DateTime.MinValue;
		public DateTime LastRun {
			get {
				return _lastrun;
			}
		}

		private DateTime _nextrun = DateTime.MaxValue;
		public DateTime NextRun {
			get {
				return _nextrun;
			}
		}
		
		public delegate void dgStatusUpdate(string text, EventLogEntryType elt);
		public event dgStatusUpdate OnStatusUpdate;

		public DomainConnectRegularUpdater(EventLog log)
		{
			_worker = new DomainConnectDDNSWorker(log);
			_worker.OnStatusUpdate += _worker_OnStatusUpdate;
			_timer = new Timer();
			_timer.Interval = tinyInterval;
			_nextrun = DateTime.Now.AddMilliseconds(tinyInterval);
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
			_lastrun = DateTime.Now;
			// Stop the timer
			_timer.Stop();
			// Do the work
			this._worker.DoWork();
			// Re start the timer with short or standard interval depending on if we have initialized
			if (!this._worker.Initialized) {
				_timer.Interval = shortInterval;
				_nextrun = _lastrun.AddMilliseconds(shortInterval);
				_timer.Start();
			}
			else if (this._worker.Monitoring) {
				_timer.Interval = longInterval;
				_nextrun = _lastrun.AddMilliseconds(longInterval);
				_timer.Start();
			}
		}

		public void Stop()
		{
			_timer.Stop();
		}
	}
}



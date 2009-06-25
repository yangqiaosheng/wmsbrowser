// $File: //depot/WMS/WMS Overview/Wms.Client/Retriever.cs $ $Revision: #2 $ $Change: 21 $ $DateTime: 2004/06/02 17:03:25 $

namespace Wms.Client
{
	/// <summary>
	/// Provides asynchronous retrieval of a web resource, copying its content to a file.
	/// See the example programs for examples of usage.
	/// 
	/// This code is a bit tricky because some of it is executed in a worker thread and
	/// some of it is executed in a client thread, typically the UI thread of the invoking
	/// application. The code is careful to avoid modifying data structures in the client
	/// thread while the worker thread is running.
	/// </summary>
	public class Retriever : System.IDisposable
	{
		public enum RetrieverState {NotReady, Ready, Retrieving, Done};

		private System.Windows.Forms.Control		owner; // owning control
		private RetrieverState						state;
		private System.Object						tag;
		private System.TimeSpan						timeoutInterval;
		private System.TimeSpan						progressInterval;
		private bool								canceled;
		private string								destination;
		private System.DateTime						startTime;
		private System.DateTime						previousProgressTime;
		private event RetrieverDoneEventHandler		done;
		private event RetrieverProgressEventHandler	progress;
		private System.Net.WebRequest				webRequest;
		private System.Windows.Forms.Timer			monitor;
		private RequestBuilder						request;

		public Retriever(System.Windows.Forms.Control owner)
		{
			if (owner == null)
				throw new System.ArgumentNullException("System.Windows.Forms.Control owner");

			this.owner = owner;
			this.timeoutInterval = System.TimeSpan.FromSeconds(60);
			this.progressInterval = System.TimeSpan.FromSeconds(0.5);
			this.state = RetrieverState.NotReady;
			this.canceled = false;
			this.monitor = new System.Windows.Forms.Timer();
			this.monitor.Tick += new System.EventHandler(this.monitorTick);
		}

		private bool disposed;

		public void Dispose()
		{
			if(!this.disposed)
			{
				this.monitor.Dispose();

				if (this.state == RetrieverState.Retrieving)
					this.Cancel();

				System.GC.SuppressFinalize(this);
				this.disposed = true;
			}
		}

		public RetrieverState State
		{
			get {return this.state;}
		}

		public bool IsRetrieving
		{
			get {return this.state == RetrieverState.Retrieving;}
		}

		public bool Canceled
		{
			get {return this.canceled;}
		}

		public System.Object Tag
		{
			set {this.tag = value;}
			get {return this.tag;}
		}

		public RequestBuilder Request
		{
			set
			{
				// Since the request contains the URI of the server, it
				// must be set before the retriever can execute. Therefore
				// the retriever state is set to NotReady by default until
				// the request is specified.
				this.detectPropertyChangeError("Request");
				this.request = value;
				if (this.request != null && this.request.Uri != null)
				{
					this.state = RetrieverState.Ready;
				}
				else
				{
					this.state = RetrieverState.NotReady;
				}
			}
			get {return this.request;}
		}

		public string Destination
		{
			set
			{
				// The destination is the file path and name in which to
				// place the retrieved content.
				this.detectPropertyChangeError("Destination");
				this.destination = value;
			}

			get {return this.destination;}
		}

		// The timeout interval is the time to wait for server
		// content to be retrieved.
		public System.TimeSpan TimeoutInterval
		{
			set
			{
				this.detectPropertyChangeError("Timeout Interval");
				this.timeoutInterval = value;
			}

			get {return this.timeoutInterval;}
		}

		public System.DateTime StartTime
		{
			get {return this.startTime;}
		}

		public System.TimeSpan ProgressInterval
		{
			// Indicates how often to invoke the progress event handler.
			set
			{
				this.detectPropertyChangeError("Progress Interval");
				this.progressInterval = value;
			}

			get {return this.timeoutInterval;}
		}

		public event RetrieverDoneEventHandler Done
		{
			// The event handlers to be invoked when the retriever is
			// done, either because the content was retrieved, the
			// request timed out, or an error occurred.
			add
			{
				this.detectPropertyChangeError("Done event handler");
				this.done += value;
			}

			remove {this.done -= value;}
		}

		public event RetrieverProgressEventHandler Progress
		{
			add
			{
				this.detectPropertyChangeError("Progress event handler");
				this.progress += value;
			}

			remove {this.progress -= value;}
		}

		private void detectPropertyChangeError(string propertyName)
		{
			if (this.state == RetrieverState.Retrieving)
			{
				string msg = "Cannot set " + propertyName
					+ " after starting retrieval.";
				throw new RetrieverStateException(msg);
			}
		}

		// This function must be called by the client application to initiate retrieval.
		public void Start()
		{
			if (this.state == RetrieverState.NotReady)
			{
				throw new RetrieverStateException(
					"Retriever requires the URI to be set before calling Start().");
			}

			if (this.state == RetrieverState.Retrieving)
			{
				throw new RetrieverStateException("Retrieval already in progress.");
			}

			try
			{
				this.startTime = System.DateTime.Now;
				this.previousProgressTime = this.startTime;

				this.webRequest = System.Net.WebRequest.Create(this.request.Uri);

				// Handle any proxy the system may have defined.
				System.Net.WebProxy proxy = System.Net.WebProxy.GetDefaultProxy();
				if (proxy.Address != null)
				{
					this.webRequest.Proxy = proxy;
				}

				this.webRequest.BeginGetResponse(
					new System.AsyncCallback(this.onRetrieval), this);

				// Compute an interval to check on progress, but make sure it's
				// at least some reasonable minimum: say, 10 milliseconds.
				ulong monitorInterval = (ulong)System.Math.Min(
					this.timeoutInterval.Milliseconds,
					this.progressInterval.Milliseconds);
				this.monitor.Interval = (int)System.Math.Max(monitorInterval, 10);
				this.monitor.Start();

				this.canceled = false;
				this.state = RetrieverState.Retrieving;
			}
			catch (System.Exception e)
			{
				throw e;
			}
		}

		public void Cancel()
		{
			this.canceled = true;
			// Will actually cancel at next monitor pulse.
		}

		private void endRequest()
		{
			if (this.monitor.Enabled)
				this.monitor.Stop();

			try
			{
				if (this.state == RetrieverState.Retrieving)
					this.webRequest.Abort();
			}
			finally
			{
				this.state = RetrieverState.Done;
			}
		}

		private void monitorTick(System.Object sender, System.EventArgs ea)
		{
			if (this.state != RetrieverState.Retrieving)
				return;

			if (this.canceled)
			{
				this.endRequest();
				return;
			}

			System.TimeSpan span = System.DateTime.Now - this.startTime;
			if (span > this.timeoutInterval)
			{
				this.endRequest();
				if (this.done != null)
				{
					RetrieverDoneArgs pda = new RetrieverDoneArgs(this);
					pda.reason = RetrieverDoneArgs.CompletionReason.TimedOut;
					this.done(this, pda);
				}
				return;
			}

			span = System.DateTime.Now - this.previousProgressTime;
			if (span > this.progressInterval)
			{
				if (this.progress != null)
				{
					RetrieverProgressArgs pea = new RetrieverProgressArgs(this);
					this.progress(this, pea);
				}
				this.previousProgressTime = System.DateTime.Now;
			}
		}

		private void onRetrieval(System.IAsyncResult ar)
		{
			// This function is called by the System.Net.WebRequest class
			// on the *worker* thread, not the UI thread.

			System.Diagnostics.Debug.Assert(ar.IsCompleted);

			System.Net.WebResponse webResponse = null;
			try
			{
				webResponse = this.webRequest.EndGetResponse(ar);
			}
			catch (System.Net.WebException e)
			{
				if (e.Status == System.Net.WebExceptionStatus.RequestCanceled)
				{
					return; // the request was previously cancelled, a normal condition
				}
				else if (e.Status == System.Net.WebExceptionStatus.Success)
				{
					// ' Don't really understand this one. Why would an exception be thrown
					// if the request worked? Anyway, allow for it.
					System.Diagnostics.Debug.Assert(webResponse != null);
				}
				else
				{
					// Send the UI thread a message saying an error occurred.
					RetrieverDoneArgs erda = new RetrieverDoneArgs(this);
					erda.reason = RetrieverDoneArgs.CompletionReason.Error;
					erda.Message = e.Message;
					this.sendMessageToUiThread(erda);
					return;
				}
			}

			string suffixedDestination =
				Wms.Client.ExtensionMap.AddSuffixToPath(this.destination, webResponse.ContentType);
			try
			{
				System.IO.Stream rs = webResponse.GetResponseStream();
				copyStreamToFile(webResponse.GetResponseStream(), suffixedDestination);
			}
			catch (System.Exception e)
			{
				// Send the UI thread a message saying an error occurred.
				RetrieverDoneArgs erda = new RetrieverDoneArgs(this);
				erda.reason = RetrieverDoneArgs.CompletionReason.Error;
				erda.Message = e.Message;
				this.sendMessageToUiThread(erda);
				return;
			}
			finally
			{	
				webResponse.Close(); // closes the response stream
			}

			// Send the UI thread a message saying the file is ready.
			RetrieverDoneArgs rda = new RetrieverDoneArgs(this);
			rda.destinationFile = suffixedDestination;
			rda.contentLength = webResponse.ContentLength;
			rda.contentType = webResponse.ContentType;
			this.BeforeClientNotification(rda);
			this.sendMessageToUiThread(rda);
		}

		protected virtual void BeforeClientNotification(RetrieverDoneArgs rda)
		{
			return; // default implementation returns w/o doing anything.
		}

		private void onContentReady(System.Object sender, RetrieverDoneArgs ea)
		{
			// This function is called on the UI thread when a message is sent
			// from the worker thread. It merely invokes any listeners and then
			// terminates the retrieval request.
			if (this.monitor.Enabled)
				this.monitor.Stop();

			if (this.done != null)
			{
				this.done(this, ea);
			}
			this.endRequest();
		}

		private void sendMessageToUiThread(RetrieverDoneArgs rda)
		{
			this.owner.Invoke(
				new RetrieverDoneEventHandler(this.onContentReady),
				new object[] {System.Threading.Thread.CurrentThread, rda});
		}

		private static void copyStreamToFile(System.IO.Stream stream, string destination)
		{
			System.Diagnostics.Debug.Assert(stream != null);

			if (destination == null || destination == string.Empty)
				return;

			using (System.IO.BufferedStream bs = new System.IO.BufferedStream(stream))
			{
				using (System.IO.FileStream os = System.IO.File.OpenWrite(destination))
				{
					byte[] buffer = new byte[2 * 4096];
					int nBytes;
					while ((nBytes = bs.Read(buffer, 0, buffer.Length)) > 0)
					{
						os.Write(buffer, 0, nBytes);
					}
				}
			}
		}
	}

	public class CapabilitiesRetriever : Retriever
	{
		public CapabilitiesRetriever(System.Windows.Forms.Control owner) : base(owner)
		{
		}

		protected override void BeforeClientNotification(RetrieverDoneArgs rda)
		{
			// A WMS server object is created here because doing so causes the
			// returned WMS capabilities description to be parsed. During that
			// parsing, schema or DTD URIs might be invoked by the parser, which
			// will cause additional http requests to be sent to the net. We
			// want all that to go on in the worker thread, so that the client
			// thread won't have to block waiting for them to occur in the UI
			// thread.
			rda.destinationObject = new Server(rda.DestinationFile);
		}
	}

	public class MapRetriever : Retriever
	{
		public MapRetriever(System.Windows.Forms.Control owner) : base(owner)
		{
		}
	}

	public abstract class RetrieverArgs : System.EventArgs
	{
		protected Retriever retriever;
		protected string message;

		private RetrieverArgs() {;} // prevent use of default constructor

		public RetrieverArgs(Retriever retriever)
		{
			this.retriever = retriever;
			this.message = string.Empty;
		}
			
		public Retriever Retriever
		{
			get {return this.retriever;}
		}

		public string Message
		{
			set {this.message = value;}
			get {return this.message;}
		}
	}

	public class RetrieverUserArgs : RetrieverArgs
	{
		protected internal string contentType;
		protected internal long contentLength;

		public RetrieverUserArgs(Retriever retriever) : base(retriever)
		{
			this.contentLength = 0;
			this.contentType = string.Empty;
		}

		public string ContentType
		{
			get {return this.contentType;}
		}

		public long ContentLength
		{
			get {return this.ContentLength;}
		}
	}

	public class RetrieverProgressArgs : RetrieverUserArgs
	{
		private double progressFraction;

		public RetrieverProgressArgs(Retriever retriever) : base(retriever)
		{
			this.progressFraction = 0;
		}

		public double ProgressFraction
		{
			get {return this.progressFraction;}
		}
	}

	public class RetrieverDoneArgs : RetrieverUserArgs
	{
		public enum CompletionReason {Completed, TimedOut, Error};
		internal CompletionReason reason;
		internal string destinationFile;
		internal System.Object destinationObject;

		public RetrieverDoneArgs(Retriever retriever) : base(retriever)
		{
			this.reason = CompletionReason.Completed;
		}

		public CompletionReason Reason
		{
			get {return this.reason;}
		}

		public string DestinationFile
		{
			get {return this.destinationFile;}
		}

		public System.Object DestinationObject
		{
			get {return this.destinationObject;}
		}
	}

	public delegate void RetrieverProgressEventHandler(object sender, RetrieverProgressArgs ea);
	public delegate void RetrieverDoneEventHandler(object sender, RetrieverDoneArgs ea);

	public class RetrieverStateException : System.Exception
	{
		public RetrieverStateException(string msg) : base(msg)
		{
		}
	}
}
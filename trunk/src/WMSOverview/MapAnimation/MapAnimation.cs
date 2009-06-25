// $File: //depot/WMS/WMS Overview/MapAnimation/MapAnimation.cs $ $Revision: #1 $ $Change: 20 $ $DateTime: 2004/05/23 23:42:06 $

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;

namespace Wms.Client
{
	/// <summary>
	/// Animate a collection of maps retrieved from a WMS server.
	/// </summary>
	public class MapAnimationExample : System.Windows.Forms.Form
	{
		private System.Windows.Forms.PictureBox pictureBox;
		private System.Windows.Forms.StatusBar statusBar;
		private System.ComponentModel.IContainer components;
		private System.Windows.Forms.Timer timer;
		private System.Windows.Forms.ProgressBar progressBar;

		private Wms.Client.MapRequestBuilder	[] mapRequests;
		private System.Drawing.Image			[] mapImages;

		public MapAnimationExample()
		{
			InitializeComponent();

			// Create arrays to hold the map requests and the retrieved images.
			this.mapRequests = new Wms.Client.MapRequestBuilder[10];
			this.mapImages = new System.Drawing.Image[10];

			// Initialize the progress bar.
			this.progressBar.Minimum = 0;
			this.progressBar.Maximum = this.mapImages.Length;
		}

		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.statusBar = new System.Windows.Forms.StatusBar();
            this.timer = new System.Windows.Forms.Timer(this.components);
            this.progressBar = new System.Windows.Forms.ProgressBar();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox
            // 
            this.pictureBox.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pictureBox.Location = new System.Drawing.Point(10, 9);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(594, 323);
            this.pictureBox.TabIndex = 0;
            this.pictureBox.TabStop = false;
            // 
            // statusBar
            // 
            this.statusBar.Location = new System.Drawing.Point(0, 318);
            this.statusBar.Name = "statusBar";
            this.statusBar.Size = new System.Drawing.Size(616, 23);
            this.statusBar.TabIndex = 1;
            // 
            // timer
            // 
            this.timer.Tick += new System.EventHandler(this.timer_Tick);
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(144, 353);
            this.progressBar.Maximum = 10;
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(566, 9);
            this.progressBar.Step = 1;
            this.progressBar.TabIndex = 2;
            this.progressBar.Visible = false;
            // 
            // MapAnimationExample
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(6, 14);
            this.ClientSize = new System.Drawing.Size(616, 341);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.statusBar);
            this.Controls.Add(this.pictureBox);
            this.Name = "MapAnimationExample";
            this.Text = "Map Animation Example";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.ResumeLayout(false);

		}
		#endregion

		static void Main() 
		{
			MapAnimationExample form = new MapAnimationExample();
			form.statusBar.Text = "Retrieving Capabilities ";

			// Create and initialize a retriever to retrieve WMS capabilities.
			Wms.Client.CapabilitiesRetriever capsRetriever = new Wms.Client.CapabilitiesRetriever(form);
			capsRetriever.ProgressInterval = new System.TimeSpan(0,0,0,0,500);
			capsRetriever.Done += new Wms.Client.RetrieverDoneEventHandler(form.capsRetrieveDone);
			capsRetriever.Progress += new Wms.Client.RetrieverProgressEventHandler(form.showCapsProgress);
			capsRetriever.Request = new Wms.Client.CapabilitiesRequestBuilder(
				new System.Uri(@"http://viz.globe.gov/viz-bin/wmt.cgi"));
			capsRetriever.Destination = System.IO.Path.GetTempFileName();

			// Once the retriever is initialized, tell it to start the retrieval.
			capsRetriever.Start();

			Application.Run(form);
		}

		private void showCapsProgress(System.Object sender, Wms.Client.RetrieverProgressArgs ea)
		{
			// Increment the capabilities progress label.
			this.statusBar.Text += "+";
		}

		private void capsRetrieveDone(object sender, Wms.Client.RetrieverDoneArgs ea)
		{
			// This event handler is called when the capabilities description has
			// been retrieved from the WMS server and parsed on the client side.
			if (ea.Reason == Wms.Client.RetrieverDoneArgs.CompletionReason.Completed)
			{
				if (ea.ContentType.Equals("application/vnd.ogc.wms_xml")
					|| ea.ContentType.Equals("text/xml"))
				{
					// Capabilities successfully retrieved from server.
					initiateMapRequest(ea.DestinationObject as Wms.Client.Server);
				}
			}

		}

		private void initiateMapRequest(Wms.Client.Server server)
		{
			this.statusBar.Text = "Retrieving Maps ";

			for (int i = 0; i < this.mapRequests.Length; i++)
			{
				// Create a GetMap request for the layers COASTLINES and Cloud Cover.
				this.mapRequests[i] = new Wms.Client.MapRequestBuilder(
					new System.Uri(server.Capabilities.GetCapabilitiesRequestUri));
				this.mapRequests[i].Layers = "COASTLINES,RCOXXR";
				this.mapRequests[i].Styles = ","; // use default style for each layer
				this.mapRequests[i].Format = "image/gif";
				this.mapRequests[i].Srs = "EPSG:4326";
				this.mapRequests[i].BoundingBox = "-180.0,-90.0,180.0,90.0";
				this.mapRequests[i].Height = 300;
				this.mapRequests[i].Width = 600;
				this.mapRequests[i].Transparent = false;
				this.mapRequests[i].Time = System.String.Format("2004-05-{0:D2}", i+1);

				// Create a retriever to execute the request.
				Wms.Client.MapRetriever mapRetriever = new Wms.Client.MapRetriever(this);
				mapRetriever.ProgressInterval = new System.TimeSpan(0,0,0,0,500);
				mapRetriever.Done += new Wms.Client.RetrieverDoneEventHandler(this.mapRetrieveDone);
				mapRetriever.Request = this.mapRequests[i];
				mapRetriever.Destination = System.IO.Path.GetTempFileName();

				// Slow connections may require more than the default 60 second
				// timeout period for map requests, so increase it to 180 seconds.
				mapRetriever.TimeoutInterval = System.TimeSpan.FromSeconds(180);

				// Start the retrieval.
				mapRetriever.Start();

				// Initialize the progress bar and start the progress timer.
				this.statusBar.Text = "Retrieving Maps";
				this.progressBar.Visible = true;
				this.timer.Start();
			}
		}

		private void mapRetrieveDone(object sender, Wms.Client.RetrieverDoneArgs ea)
		{
			// This event handler is called when the map has been retrieved, or when an error
			// occurs in the retrieval.
			Wms.Client.MapRequestBuilder mapRequest = ea.Retriever.Request as Wms.Client.MapRequestBuilder;

			if (ea.Reason == Wms.Client.RetrieverDoneArgs.CompletionReason.Completed)
			{

				System.Drawing.Image image = System.Drawing.Image.FromFile(ea.DestinationFile);
				for (int i = 0; i < this.mapRequests.Length; i++)
				{
					if (this.mapRequests[i] == mapRequest)
					{
						this.mapImages[i] = image;
					}
				}
			}

		}

		private void timer_Tick(object sender, System.EventArgs e)
		{
			// Determine whether all the maps have arrived yet.
			int count = 0;
			foreach (System.Drawing.Image image in this.mapImages)
			{
				if (image != null)
					++count;
			}

			// If they have not, then update the progress bar.
			if (count < this.mapImages.Length)
			{
				this.progressBar.Value = count;
			}

			// If all maps have arrived, start the animation.
			if (count == this.mapImages.Length)
			{
				this.statusBar.Text = "Displaying sequence";

				if (this.pictureBox.Image == null) // first time image is displayed.
				{
					this.pictureBox.Image = this.mapImages[0];
				}
				else
				{
					// Repeatedly cycle through the images, displaying each in turn,
					// and returning to the first image after the last is shown.
					for (int i = 0; i < this.mapImages.Length; i++)
					{
						if (this.pictureBox.Image == this.mapImages[i])
						{
							this.pictureBox.Image = this.mapImages[(i+1) % this.mapImages.Length];
							this.pictureBox.Update();

							// Use the progress bar to indicate the image sequence begin and end.
							this.progressBar.Value = i+1;
							break;
						}
					}
				}
			}
		}
	}
}
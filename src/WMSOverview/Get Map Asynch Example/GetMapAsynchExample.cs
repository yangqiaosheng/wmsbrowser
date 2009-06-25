// $File: //depot/WMS/WMS Overview/Get Map Asynch Example/GetMapAsynchExample.cs $ $Revision: #1 $ $Change: 20 $ $DateTime: 2004/05/23 23:42:06 $

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;

namespace Wms.Client
{
	/// <summary>
	/// Retrieve WMS capabilities and maps asynchronously.
	/// </summary>
	public class GetMapAsynchExample : System.Windows.Forms.Form
	{
		private System.Windows.Forms.PictureBox pictureBox;
		private System.Windows.Forms.StatusBar statusBar;
		private System.ComponentModel.Container components = null;

		public GetMapAsynchExample()
		{
			InitializeComponent();
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
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.statusBar = new System.Windows.Forms.StatusBar();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox
            // 
            this.pictureBox.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pictureBox.Location = new System.Drawing.Point(10, 9);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(720, 323);
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
            // GetMapAsynchExample
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(6, 14);
            this.ClientSize = new System.Drawing.Size(616, 341);
            this.Controls.Add(this.statusBar);
            this.Controls.Add(this.pictureBox);
            this.Name = "GetMapAsynchExample";
            this.Text = "Get Map Asynch Example";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.ResumeLayout(false);

		}
		#endregion

		static void Main() 
		{
			GetMapAsynchExample form = new GetMapAsynchExample();
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
			// Update the progress bar.
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
				else if (ea.ContentType.Equals("application/vnd.ogc.se_xml")
					|| ea.ContentType.Equals("application/vnd.ogc.se+xml"))
				{
					// WMS servers indicate WMS exceptions using the above content types.
					string msg = "The WMS server returned an exception."
						+ System.Environment.NewLine;
					System.Windows.Forms.MessageBox.Show(msg, "WMS Server Exception",
						System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
				}
				else
				{
					// If the content type is something unexpected, then it's likely that we
					// reached an HTML page, which can be displayed in Internet Explorer.
					string msg = "The WMS server returned an incorrect format of "
						+ ea.ContentType + "." + System.Environment.NewLine
						+ "This is not a valid format." + System.Environment.NewLine
						+ "Would you like to see if Internet Explorer can show you what was returned?";
					System.Windows.Forms.DialogResult yesNo = System.Windows.Forms.MessageBox.Show(msg, "Invalid WMS Format",
						System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Error);
					if (yesNo == System.Windows.Forms.DialogResult.Yes)
					{
						invokeIe(ea.Retriever.Request.ToString());
					}
				}
			}
			else if (ea.Reason == Wms.Client.RetrieverDoneArgs.CompletionReason.TimedOut)
			{
				string msg = "Contacting WMS server timed out."
					+ System.Environment.NewLine;
				System.Windows.Forms.MessageBox.Show(msg, "WMS Server Contact Timed Out",
					System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
			}
			else // there was an error that we could not predict, most likely an http error.
			{
				string msg = "Error contacting WMS server: " + ea.Message
					+ System.Environment.NewLine;
				System.Windows.Forms.MessageBox.Show(msg, "Unable to contact WMS server",
					System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
			}
		}

		private void initiateMapRequest(Wms.Client.Server server)
		{
			this.statusBar.Text = "Retrieving Map ";

			// Create a GetMap request for the layers COASTLINES and RATMIN (min temperatures).
			Wms.Client.MapRequestBuilder mapRequest = new Wms.Client.MapRequestBuilder(
				new System.Uri(server.Capabilities.GetCapabilitiesRequestUri));
			mapRequest.Layers = "COASTLINES,RATMIN";
			mapRequest.Styles = ","; // use default style for each layer
			mapRequest.Format = "image/gif";
			mapRequest.Srs = "EPSG:4326";
			mapRequest.BoundingBox = "-180.0,-90.0,180.0,90.0";
			mapRequest.Height = 300;
			mapRequest.Width = 600;
			mapRequest.Transparent = false;

			// Create a retriever to execute the request.
			Wms.Client.MapRetriever mapRetriever = new Wms.Client.MapRetriever(this);
			mapRetriever.ProgressInterval = new System.TimeSpan(0,0,0,0,500);
			mapRetriever.Done += new Wms.Client.RetrieverDoneEventHandler(this.mapRetrieveDone);
			mapRetriever.Progress += new Wms.Client.RetrieverProgressEventHandler(this.showMapProgress);
			mapRetriever.Request = mapRequest;
			mapRetriever.Destination = System.IO.Path.GetTempFileName();

			// Start the retrieval.
			mapRetriever.Start();
		}

		private void showMapProgress(System.Object sender, Wms.Client.RetrieverProgressArgs ea)
		{
			// Update the progress bar.
			this.statusBar.Text += "+";
		}

		private void mapRetrieveDone(object sender, Wms.Client.RetrieverDoneArgs ea)
		{
			// This event handler is called when the map has been retrieved, or when an error
			// occurs in the retrieval.
			Wms.Client.MapRequestBuilder mapRequest = ea.Retriever.Request as Wms.Client.MapRequestBuilder;

			if (ea.Reason == Wms.Client.RetrieverDoneArgs.CompletionReason.Completed)
			{
				if (ea.ContentType.Equals("application/vnd.ogc.se_xml")
					|| ea.ContentType.Equals("application/vnd.ogc.se+xml")
					|| ea.ContentType.Equals("text/xml"))
				{
					string msg = "Retrieval of map returned an error:" + System.Environment.NewLine;
					System.Windows.Forms.MessageBox.Show(msg, "WMS Server Exception",
						System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
				}
				else
				{
					System.Drawing.Image image = System.Drawing.Image.FromFile(ea.DestinationFile);
					this.pictureBox.Image = image;
					this.statusBar.Text = System.String.Empty;
				}
			}
			else if (ea.Reason == Wms.Client.RetrieverDoneArgs.CompletionReason.Error)
			{
				System.Windows.Forms.MessageBox.Show("Error retrieving map: " + ea.Message,
					"Retrieval error",
					System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
			}
			else if (ea.Reason == RetrieverDoneArgs.CompletionReason.TimedOut)
			{
				System.Windows.Forms.MessageBox.Show("Retrieval of map timed out.",
					"Retrieval error",
					System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
			}
		}

		static private void invokeIe(string uri)
		{
			System.Diagnostics.Process ie = new System.Diagnostics.Process();
			ie.StartInfo.FileName = "iexplore.exe";
			ie.StartInfo.Arguments = uri;
			ie.Start();
		}
	}
}
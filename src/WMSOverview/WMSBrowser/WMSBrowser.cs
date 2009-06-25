// $File: //depot/WMS/WMS Overview/WMSBrowser/WMSBrowser.cs $ $Revision: #1 $ $Change: 20 $ $DateTime: 2004/05/23 23:42:06 $

namespace WMSBrowser
{
	/// <summary>
	/// Application showing use of the Wms.Client.WmsDialog class.
	/// </summary>
	public class WMSBrowser : Wms.Client.WmsDialog
	{
		private System.ComponentModel.Container components = null;

		public WMSBrowser()
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
            this.SuspendLayout();
            // 
            // WMSBrowser
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(6, 14);
            this.ClientSize = new System.Drawing.Size(784, 533);
            this.Name = "WMSBrowser";
            this.Text = "WMS Browser";
            this.ResumeLayout(false);

		}
		#endregion

		[System.STAThread]
		static void Main() 
		{
			System.Windows.Forms.Application.EnableVisualStyles();
			System.Windows.Forms.Application.DoEvents();
			WMSBrowser me = new WMSBrowser();
			me.MapSize = new System.Drawing.Size(1024, 512);
			me.MapFormatPreferences = new string[] {@"image/png", @"image/gif"};
			System.Windows.Forms.Application.Run(me);
            me.initializeServerDescriptors();
		}
	
		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			base.OnClosing(e);
			e.Cancel = false;
		}
	
		protected override void OnCloseButtonClick(object sender, System.EventArgs e)
		{
			base.OnCloseButtonClick(sender, e);
			this.Close();
		}
	
		protected override void OnDrawMap(object sender, Wms.Client.DrawMapEventArgs ea)
		{
			System.Drawing.Image image = System.Drawing.Image.FromFile(ea.MapFilePath);

			MapForm mf = new MapForm();
			mf.Size = new System.Drawing.Size(image.Width + 20, image.Height + 20);
			mf.mapBox.Image = image;
			mf.Text = ea.Layer.Title;
			mf.Show();

			base.OnDrawMap (sender, ea);
		}

		protected override void OnPreviewMap(object sender, Wms.Client.PreviewMapEventArgs ea)
		{
			base.OnPreviewMap (sender, ea);
		}

	}
}
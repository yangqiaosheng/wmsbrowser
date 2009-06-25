// $File: //depot/WMS/WMS Overview/WMSBrowser/MapForm.cs $ $Revision: #1 $ $Change: 20 $ $DateTime: 2004/05/23 23:42:06 $

namespace WMSBrowser
{
	/// <summary>
	/// Implements a simple dialog for displaying a WMS map.
	/// </summary>
	public class MapForm : System.Windows.Forms.Form
	{
		internal System.Windows.Forms.PictureBox mapBox;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public MapForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
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
            this.mapBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.mapBox)).BeginInit();
            this.SuspendLayout();
            // 
            // mapBox
            // 
            this.mapBox.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.mapBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapBox.Location = new System.Drawing.Point(0, 0);
            this.mapBox.Name = "mapBox";
            this.mapBox.Size = new System.Drawing.Size(736, 509);
            this.mapBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.mapBox.TabIndex = 3;
            this.mapBox.TabStop = false;
            // 
            // MapForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(6, 14);
            this.ClientSize = new System.Drawing.Size(736, 509);
            this.Controls.Add(this.mapBox);
            this.Name = "MapForm";
            this.Text = "MapForm";
            this.Closed += new System.EventHandler(this.MapForm_Closed);
            ((System.ComponentModel.ISupportInitialize)(this.mapBox)).EndInit();
            this.ResumeLayout(false);

		}
		#endregion

		private void MapForm_Closed(object sender, System.EventArgs e)
		{
			if (this.mapBox.Image != null)
			{
				this.mapBox.Image.Dispose();
			}
		}
	}
}
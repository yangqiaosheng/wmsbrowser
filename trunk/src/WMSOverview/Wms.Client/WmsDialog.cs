// $File: //depot/WMS/WMS Overview/Wms.Client/WmsDialog.cs $ $Revision: #1 $ $Change: 20 $ $DateTime: 2004/05/23 23:42:06 $

namespace Wms.Client
{
	/// <summary>
	/// Implements a dialog box for viewing WMS servers and the layers/maps they contain.
	/// Can be invoked via the Show() or ShowDialog() methods, or derived from to create
	/// a main form. See WMSBrowser in this solution for an example of the latter.
	/// </summary>
	public class WmsDialog : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.Button closeButton;
		private System.Windows.Forms.Splitter splitter1;
		private System.ComponentModel.IContainer components;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button drawButton;
		private System.Windows.Forms.Label downloadIndicator;
		private System.Windows.Forms.Button	previewButton;
		private System.Windows.Forms.Button cancelDrawButton;
		private System.Windows.Forms.Button cancelPreviewButton;
		private System.Windows.Forms.ImageList layerIcons;
		private System.Windows.Forms.TreeView layerTree;
		private System.Windows.Forms.MainMenu mainMenu1;
		private System.Windows.Forms.MenuItem menuItem1;
		private System.Windows.Forms.MenuItem addServerMenuItem;
		private System.Windows.Forms.MenuItem removeServerMenuItem;
		private System.Windows.Forms.TabPage infoPage;
		private System.Windows.Forms.TreeView infoTree;
		private System.Windows.Forms.Splitter splitter2;
		private System.Windows.Forms.RichTextBox abstractBox;
		private System.Windows.Forms.TabControl detailTabs;

		private System.Collections.ArrayList	serverDescriptors;
		private Wms.Client.Retriever			mapRetriever;
		private Wms.Client.DownloadCache		downloadCache;
		private PreviewDialog					previewDialog;
		private System.Drawing.Size				mapSize;
		private System.Drawing.Size				previewSize;
		private string[]						mapFormatPreferences;

		public event DrawMapEventHandler DrawMap;
		public event DrawMapEventHandler PreviewMap;

		public WmsDialog()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// Add any constructor code after InitializeComponent call
			//
			this.downloadCache = new DownloadCache();
			this.downloadCache.LoadPersisted();
			this.mapRetriever = new Retriever(this);
			this.mapRetriever.ProgressInterval = new System.TimeSpan(0,0,0,0,500);
			this.mapRetriever.Done += new RetrieverDoneEventHandler(this.mapRetrieveDone);
			this.mapRetriever.Progress += new RetrieverProgressEventHandler(this.showMapProgress);
			this.mapSize.Width = 512;
			this.mapSize.Height = 256;
			this.previewSize.Width = 300;
			this.previewSize.Height = 150;
			this.mapFormatPreferences = new string[] {"image/gif", "image/tiff", "image/png", "image/bmp", "image/jpg"};

			this.initializeServerDescriptors();

			
		}

		public void initializeServerDescriptors()
		{
			WmsServerDescriptors descriptorDataSet = new WmsServerDescriptors();

			// Read the server descriptors. These are stored as XML in a resource file associated
			// with this class. See WmsServerDescriptors.xsd for the XML schema used, and see
			// DefaultServerDescriptors.xml for the list of default servers.
			System.IO.Stream stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(
				"Wms.Client.DefaultServerDescriptors.xml");
			if (stream == null)
				return;
			try
			{
				descriptorDataSet.ReadXml(stream);
			}
			catch (System.Exception)
			{
				return;
			}
			finally
			{
				stream.Close();
			}

			WmsServerDescriptors.ServerDataTable serverDescriptorTable = descriptorDataSet.Server;
			this.serverDescriptors = new System.Collections.ArrayList();

			foreach (Wms.Client.WmsServerDescriptors.ServerRow row in descriptorDataSet.Server)
			{
				this.addServer(row.Uri.ToString(), row.Name);
			}

            // Start retrieval of the capabilities of all the servers we're to start up with.

            foreach (ServerDescriptor sd in this.serverDescriptors)
            {
                sd.retriever.Start();
            }
		}

		// Adds a server to the dialog.
		private ServerDescriptor addServer(string uriString, string serverName)
		{
			try
			{
				ServerDescriptor sd = new ServerDescriptor(uriString, serverName);
				sd.layerTreeNode = new System.Windows.Forms.TreeNode();
				sd.layerTreeNode.Text = sd.friendlyName;
				sd.layerTreeNode.ImageIndex = 3;
				sd.layerTreeNode.SelectedImageIndex = 3;

				sd.retriever = new Retriever(this);
				sd.retriever.Request = new CapabilitiesRequestBuilder(new System.Uri(sd.uri));
				sd.retriever.Request.ClientInfo["ServerDescriptor"] = sd;
				sd.retriever.Destination = this.downloadCache.CreateFilePath();
				sd.retriever.TimeoutInterval = new System.TimeSpan(0,0,1,1,0);
				sd.retriever.ProgressInterval = new System.TimeSpan(0,0,0,0,400);
				sd.retriever.Progress += new RetrieverProgressEventHandler(this.showServerProgress);
				sd.retriever.Done += new RetrieverDoneEventHandler(this.serverRetrieveDone);
				this.serverDescriptors.Add(sd);
				return sd;
			}
			catch (System.Exception e)
			{
				System.Windows.Forms.MessageBox.Show(e.Message, "Error Adding Server",
					System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
				return null;
			}
		}

		private void removeServer(string serverName)
		{
			foreach (ServerDescriptor sd in this.serverDescriptors)
			{
				if (sd.friendlyName.ToString().Equals(serverName))
				{
					this.serverDescriptors.Remove(sd);
					break;
				}
			}
		}

		// Builds a GetCapabilities request for a server.
		private void initiateServerCapabilitiesRequest(ServerDescriptor sd)
		{
			sd.retriever = new Retriever(this);
			sd.retriever.Request = new CapabilitiesRequestBuilder(new System.Uri(sd.uri));
			sd.retriever.Request.ClientInfo["ServerDescriptor"] = sd;
			sd.retriever.Destination = this.downloadCache.CreateFilePath();
			sd.retriever.TimeoutInterval = new System.TimeSpan(0,0,1,1,0);
			sd.retriever.ProgressInterval = new System.TimeSpan(0,0,0,0,400);
			sd.retriever.Progress += new RetrieverProgressEventHandler(this.showServerProgress);
			sd.retriever.Done += new RetrieverDoneEventHandler(this.serverRetrieveDone);
			sd.retriever.Start();
		}

		/// Clean up any resources being used.
		protected override void Dispose( bool disposing )
		{
			if (this.mapRetriever.IsRetrieving)
			{
				this.cancelMapRequest();
			}

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}

				if (this.previewDialog != null)
				{
					this.previewDialog.Close();
					this.previewDialog.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		public System.Drawing.Size MapSize
		{
			set {this.mapSize = value;}
			get {return this.mapSize;}
		}

		public System.Drawing.Size PreviewSize
		{
			set {this.previewSize = value;}
			get {return this.previewSize;}
		}

		// Identifies the client-preferred list of map format types
		// to request from the WMS server. See WMSBrowser for a usage
		// example.
		public string[] MapFormatPreferences
		{
			set {this.mapFormatPreferences = value;}
			get {return this.mapFormatPreferences;}
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WmsDialog));
            this.panel1 = new System.Windows.Forms.Panel();
            this.cancelPreviewButton = new System.Windows.Forms.Button();
            this.cancelDrawButton = new System.Windows.Forms.Button();
            this.previewButton = new System.Windows.Forms.Button();
            this.downloadIndicator = new System.Windows.Forms.Label();
            this.drawButton = new System.Windows.Forms.Button();
            this.closeButton = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.layerTree = new System.Windows.Forms.TreeView();
            this.layerIcons = new System.Windows.Forms.ImageList(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.detailTabs = new System.Windows.Forms.TabControl();
            this.infoPage = new System.Windows.Forms.TabPage();
            this.infoTree = new System.Windows.Forms.TreeView();
            this.splitter2 = new System.Windows.Forms.Splitter();
            this.abstractBox = new System.Windows.Forms.RichTextBox();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.mainMenu1 = new System.Windows.Forms.MainMenu(this.components);
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.addServerMenuItem = new System.Windows.Forms.MenuItem();
            this.removeServerMenuItem = new System.Windows.Forms.MenuItem();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.detailTabs.SuspendLayout();
            this.infoPage.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.cancelPreviewButton);
            this.panel1.Controls.Add(this.cancelDrawButton);
            this.panel1.Controls.Add(this.previewButton);
            this.panel1.Controls.Add(this.downloadIndicator);
            this.panel1.Controls.Add(this.drawButton);
            this.panel1.Controls.Add(this.closeButton);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 490);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(784, 43);
            this.panel1.TabIndex = 0;
            // 
            // cancelPreviewButton
            // 
            this.cancelPreviewButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelPreviewButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.cancelPreviewButton.Location = new System.Drawing.Point(419, 9);
            this.cancelPreviewButton.Name = "cancelPreviewButton";
            this.cancelPreviewButton.Size = new System.Drawing.Size(106, 25);
            this.cancelPreviewButton.TabIndex = 3;
            this.cancelPreviewButton.Text = "取消预览";
            this.cancelPreviewButton.Visible = false;
            this.cancelPreviewButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // cancelDrawButton
            // 
            this.cancelDrawButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelDrawButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.cancelDrawButton.Location = new System.Drawing.Point(544, 9);
            this.cancelDrawButton.Name = "cancelDrawButton";
            this.cancelDrawButton.Size = new System.Drawing.Size(105, 25);
            this.cancelDrawButton.TabIndex = 5;
            this.cancelDrawButton.Text = "取消查看";
            this.cancelDrawButton.Visible = false;
            this.cancelDrawButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // previewButton
            // 
            this.previewButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.previewButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.previewButton.Location = new System.Drawing.Point(419, 9);
            this.previewButton.Name = "previewButton";
            this.previewButton.Size = new System.Drawing.Size(106, 25);
            this.previewButton.TabIndex = 2;
            this.previewButton.Text = "Preview";
            this.previewButton.Click += new System.EventHandler(this.previewButton_Click);
            // 
            // downloadIndicator
            // 
            this.downloadIndicator.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.downloadIndicator.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.downloadIndicator.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.downloadIndicator.Location = new System.Drawing.Point(19, 9);
            this.downloadIndicator.Name = "downloadIndicator";
            this.downloadIndicator.Size = new System.Drawing.Size(461, 25);
            this.downloadIndicator.TabIndex = 2;
            this.downloadIndicator.Text = "正在下载...";
            this.downloadIndicator.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.downloadIndicator.Visible = false;
            // 
            // drawButton
            // 
            this.drawButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.drawButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.drawButton.Location = new System.Drawing.Point(544, 9);
            this.drawButton.Name = "drawButton";
            this.drawButton.Size = new System.Drawing.Size(105, 25);
            this.drawButton.TabIndex = 4;
            this.drawButton.Text = "Draw";
            this.drawButton.Click += new System.EventHandler(this.drawButton_Click);
            // 
            // closeButton
            // 
            this.closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.closeButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.closeButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.closeButton.Location = new System.Drawing.Point(669, 9);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(105, 25);
            this.closeButton.TabIndex = 6;
            this.closeButton.Text = "关闭";
            this.closeButton.Click += new System.EventHandler(this.OnCloseButtonClick);
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.SystemColors.Control;
            this.panel2.Controls.Add(this.layerTree);
            this.panel2.Controls.Add(this.label1);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Padding = new System.Windows.Forms.Padding(8, 8, 0, 8);
            this.panel2.Size = new System.Drawing.Size(470, 490);
            this.panel2.TabIndex = 1;
            // 
            // layerTree
            // 
            this.layerTree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layerTree.HideSelection = false;
            this.layerTree.ImageIndex = 0;
            this.layerTree.ImageList = this.layerIcons;
            this.layerTree.ItemHeight = 18;
            this.layerTree.Location = new System.Drawing.Point(8, 33);
            this.layerTree.Name = "layerTree";
            this.layerTree.SelectedImageIndex = 0;
            this.layerTree.ShowLines = false;
            this.layerTree.Size = new System.Drawing.Size(462, 449);
            this.layerTree.TabIndex = 1;
            this.layerTree.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.layerTree_BeforeExpand);
            this.layerTree.DoubleClick += new System.EventHandler(this.layerTree_DoubleClick);
            this.layerTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.layerTree_AfterSelect);
            // 
            // layerIcons
            // 
            this.layerIcons.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("layerIcons.ImageStream")));
            this.layerIcons.TransparentColor = System.Drawing.Color.Transparent;
            this.layerIcons.Images.SetKeyName(0, "");
            this.layerIcons.Images.SetKeyName(1, "");
            this.layerIcons.Images.SetKeyName(2, "");
            this.layerIcons.Images.SetKeyName(3, "");
            this.layerIcons.Images.SetKeyName(4, "");
            // 
            // label1
            // 
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(8, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(462, 25);
            this.label1.TabIndex = 2;
            this.label1.Text = "可获得的地图";
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.SystemColors.Control;
            this.panel3.Controls.Add(this.detailTabs);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel3.Location = new System.Drawing.Point(480, 0);
            this.panel3.Name = "panel3";
            this.panel3.Padding = new System.Windows.Forms.Padding(0, 8, 8, 8);
            this.panel3.Size = new System.Drawing.Size(304, 490);
            this.panel3.TabIndex = 3;
            // 
            // detailTabs
            // 
            this.detailTabs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.detailTabs.Controls.Add(this.infoPage);
            this.detailTabs.ItemSize = new System.Drawing.Size(42, 18);
            this.detailTabs.Location = new System.Drawing.Point(0, 9);
            this.detailTabs.Name = "detailTabs";
            this.detailTabs.SelectedIndex = 0;
            this.detailTabs.Size = new System.Drawing.Size(294, 476);
            this.detailTabs.TabIndex = 0;
            // 
            // infoPage
            // 
            this.infoPage.Controls.Add(this.infoTree);
            this.infoPage.Controls.Add(this.splitter2);
            this.infoPage.Controls.Add(this.abstractBox);
            this.infoPage.Location = new System.Drawing.Point(4, 22);
            this.infoPage.Name = "infoPage";
            this.infoPage.Size = new System.Drawing.Size(286, 450);
            this.infoPage.TabIndex = 0;
            this.infoPage.Text = "信息";
            this.infoPage.UseVisualStyleBackColor = true;
            // 
            // infoTree
            // 
            this.infoTree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.infoTree.Location = new System.Drawing.Point(0, 95);
            this.infoTree.Name = "infoTree";
            this.infoTree.ShowLines = false;
            this.infoTree.Size = new System.Drawing.Size(286, 355);
            this.infoTree.Sorted = true;
            this.infoTree.TabIndex = 2;
            // 
            // splitter2
            // 
            this.splitter2.Dock = System.Windows.Forms.DockStyle.Top;
            this.splitter2.Location = new System.Drawing.Point(0, 86);
            this.splitter2.Name = "splitter2";
            this.splitter2.Size = new System.Drawing.Size(286, 9);
            this.splitter2.TabIndex = 1;
            this.splitter2.TabStop = false;
            // 
            // abstractBox
            // 
            this.abstractBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.abstractBox.Location = new System.Drawing.Point(0, 0);
            this.abstractBox.Name = "abstractBox";
            this.abstractBox.Size = new System.Drawing.Size(286, 86);
            this.abstractBox.TabIndex = 0;
            this.abstractBox.Text = "";
            // 
            // splitter1
            // 
            this.splitter1.Location = new System.Drawing.Point(470, 0);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(10, 490);
            this.splitter1.TabIndex = 5;
            this.splitter1.TabStop = false;
            // 
            // mainMenu1
            // 
            this.mainMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem1});
            // 
            // menuItem1
            // 
            this.menuItem1.Index = 0;
            this.menuItem1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.addServerMenuItem,
            this.removeServerMenuItem});
            this.menuItem1.Text = "服务器( &S)";
            // 
            // addServerMenuItem
            // 
            this.addServerMenuItem.Index = 0;
            this.addServerMenuItem.Text = "增加...";
            this.addServerMenuItem.Click += new System.EventHandler(this.addServerMenuItem_Click);
            // 
            // removeServerMenuItem
            // 
            this.removeServerMenuItem.Index = 1;
            this.removeServerMenuItem.Text = "移除";
            this.removeServerMenuItem.Click += new System.EventHandler(this.removeServerMenuItem_Click);
            // 
            // WmsDialog
            // 
            this.AcceptButton = this.drawButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(6, 14);
            this.CancelButton = this.closeButton;
            this.ClientSize = new System.Drawing.Size(784, 533);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.splitter1);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Menu = this.mainMenu1;
            this.Name = "WmsDialog";
            this.Text = "WMS地图器浏览";
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.detailTabs.ResumeLayout(false);
            this.infoPage.ResumeLayout(false);
            this.ResumeLayout(false);

		}
		#endregion

		// Refreshes the dialog UI after changes have been made to the underlying
		// information.
		private void updateServerDisplay()
		{
			this.layerTree.BeginUpdate();
			this.layerTree.Nodes.Clear();
			foreach (ServerDescriptor sd in this.serverDescriptors)
			{
				if (sd.layerTreeNode == null)
				{
					sd.layerTreeNode = new System.Windows.Forms.TreeNode();
					sd.layerTreeNode.Text = sd.friendlyName;
					sd.layerTreeNode.ImageIndex = 3;
					sd.layerTreeNode.SelectedImageIndex = 3;
				}

				if (sd.server != null)
				{
					Wms.Client.Layer rootLayer = sd.server.Capabilities.Layers[0];
					sd.layerTreeNode.Text = rootLayer.Title;
					sd.layerTreeNode.Tag = rootLayer;
					this.addLayersToParentNode(sd.layerTreeNode);
				}

				this.layerTree.Nodes.Add(sd.layerTreeNode);
			}

			// If there's only one server in the list, expand it to show the
			// first-level layers. This is merely a user convenience.
			if (this.layerTree.Nodes.Count == 1)
			{
				this.layerTree.Nodes[0].Expand();
			}
			this.layerTree.EndUpdate();
		}

		private void addLayersToParentNode(System.Windows.Forms.TreeNode parentNode)
		{
			// This function is called to add children to a tree-view node when that
			// node is to be displayed. This makes it show up with an expand button
			// (a plus sign) next to it if the layer does indeed have child layers.

			if (parentNode.Nodes.Count > 0)
				return;

			Layer parentLayer = (Layer)parentNode.Tag;
			foreach (Layer layer in parentLayer.Layers)
			{
				System.Windows.Forms.TreeNode node = new System.Windows.Forms.TreeNode();
				node.Text = layer.Title;
				node.Tag = layer;
				if (layer.Name.Equals(string.Empty))
				{
					node.ImageIndex = 1;
					node.SelectedImageIndex = 2;
				}
				else
				{
					node.ImageIndex = 0;
					node.SelectedImageIndex = 0;
				}
				parentNode.Nodes.Add(node);
			}
		}

		private void layerTree_BeforeExpand(object sender, System.Windows.Forms.TreeViewCancelEventArgs e)
		{
			foreach(System.Windows.Forms.TreeNode node in e.Node.Nodes)
			{
				this.addLayersToParentNode(node);
			}
		}

		private void layerTree_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{
			this.updateInfoBox(e.Node.Tag as Layer);
		}

		private bool validateMapSelection(System.Windows.Forms.TreeNode selectedNode)
		{
			if (this.mapRetriever.IsRetrieving)
			{
				System.Windows.Forms.MessageBox.Show("A download is already in progress.", "Download in Progress",
					System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
				return false;
			}

			if (selectedNode == null)
			{
				System.Windows.Forms.MessageBox.Show("You must select a map first.", "No Map Selected",
					System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
				return false;
			}

			Layer layer = selectedNode.Tag as Layer;

			if (layer == null)
			{
				System.Windows.Forms.MessageBox.Show("An unavailable server is selected.", "Unavailable Server Selected",
					System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
				return false;
			}

			if (layer.Name == null || layer.Name == string.Empty)
			{
				System.Windows.Forms.MessageBox.Show("A folder is selected. You must select a map.", "Folder Selected",
					System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
				return false;
			}

			return true;
		}

		private void drawButton_Click(object sender, System.EventArgs e)
		{
			System.Windows.Forms.TreeNode selectedNode = this.layerTree.SelectedNode;
			if (!this.validateMapSelection(selectedNode))
				return;

			Layer layer = selectedNode.Tag as Layer;
			System.Diagnostics.Debug.Assert(layer != null);

			MapRequestBuilder mapRequest = layer.Server.CreateMapRequest();
			mapRequest.Format = this.determineMapRequestFormat(layer);
			mapRequest.Layers = layer.Name;
			mapRequest.Styles = string.Empty;
			mapRequest.IncludeDefaultExtents(layer);
			mapRequest.Width = this.mapSize.Width;
			mapRequest.Height = this.mapSize.Height;
			mapRequest.ClientInfo["Purpose"] = "draw";
			mapRequest.ClientInfo["Layer"] = layer;

			this.initiateMapRetrieval(mapRequest, layer);
		}

		private void layerTree_DoubleClick(object sender, System.EventArgs e)
		{
			System.Windows.Forms.TreeNode selectedNode = this.layerTree.SelectedNode;
			if (selectedNode == null)
				return;

			Layer layer = selectedNode.Tag as Layer;

			// If the layer handle is null, then the server hasn't connected yet,
			// and the selected node must be a server.
			if (layer == null)
			{
				foreach (ServerDescriptor sd in this.serverDescriptors)
				{
					if (sd.layerTreeNode == selectedNode)
					{
						this.initiateServerCapabilitiesRequest(sd);
						break;
					}
				}
				return;
			}

			// If no layer name, then the selection is not a map.
			// If layer name is empty, then there's no map to get either, but it's
			// a case that should only come up as a bug in the server.
			if (layer.Name == null || layer.Name.Equals(string.Empty))
				return;

			if (!this.validateMapSelection(selectedNode))
				return;

			MapRequestBuilder mapRequest = layer.Server.CreateMapRequest();
			mapRequest.Format = this.determineMapRequestFormat(layer);
			mapRequest.Layers = layer.Name;
			mapRequest.Styles = string.Empty;
			mapRequest.IncludeDefaultExtents(layer);
			mapRequest.Width = this.mapSize.Width;
			mapRequest.Height = this.mapSize.Height;
			mapRequest.ClientInfo["Purpose"] = "draw";
			mapRequest.ClientInfo["Layer"] = layer;

			this.initiateMapRetrieval(mapRequest, layer);
		}

		private void previewButton_Click(object sender, System.EventArgs e)
		{
			System.Windows.Forms.TreeNode selectedNode = this.layerTree.SelectedNode;
			if (!this.validateMapSelection(selectedNode))
				return;

			Layer layer = selectedNode.Tag as Layer;
			System.Diagnostics.Debug.Assert(layer != null);

			MapRequestBuilder mapRequest = layer.Server.CreateMapRequest();
			mapRequest.Format = this.determineMapRequestFormat(layer);
			mapRequest.Layers = layer.Name;
			mapRequest.Styles = string.Empty;
			mapRequest.IncludeDefaultExtents(layer);
			mapRequest.Width = this.previewSize.Width;
			mapRequest.Height = this.previewSize.Height;
			mapRequest.ClientInfo["Purpose"] = "preview";
			mapRequest.ClientInfo["Layer"] = layer;

			this.initiateMapRetrieval(mapRequest, layer);
		}

		private string determineMapRequestFormat(Layer layer)
		{
			string[] available = layer.Server.Capabilities.GetMapRequestFormats;
			if (available != null || available.Length > 0)
			{
				foreach (string desired in this.mapFormatPreferences)
				{
					foreach (string a in available)
					{
						if (desired.Equals(a))
						{
							return desired;
						}
					}
				}
			}
			return this.mapFormatPreferences[0];
		}

		private void cancelButton_Click(object sender, System.EventArgs e)
		{
			this.cancelMapRequest();
		}

		private void initiateMapRetrieval(MapRequestBuilder mapRequest, Layer layer)
		{
			if (this.downloadCache.Contains(mapRequest.Uri.ToString()))
			{
				string filePath = this.downloadCache.GetFileName(mapRequest.ToString());
				string purpose = mapRequest.ClientInfo["Purpose"] as string;
				if (purpose.Equals("draw"))
				{
					DrawMapEventArgs dmea = new DrawMapEventArgs(filePath, mapRequest.ToString(), layer);
					this.OnDrawMap(this, dmea);
				}
				else if (purpose.Equals("preview"))
				{
					PreviewMapEventArgs pmea = new PreviewMapEventArgs(filePath, mapRequest.ToString(), layer);
					this.OnPreviewMap(this, pmea);
				}
			}
			else
			{
				this.mapRetriever.Request = mapRequest;
				this.mapRetriever.Destination = this.downloadCache.CreateFilePath();
				this.mapRetriever.Start();
				this.turnOnMapCancelUi(layer.Title, mapRequest.ClientInfo["Purpose"] as string);
			}
		}

		private void cancelMapRequest()
		{
			if (this.mapRetriever.IsRetrieving)
			{
				this.turnOffMapCancelUi();
				this.mapRetriever.Cancel();
			}
		}

		private void mapRetrieveDone(object sender, RetrieverDoneArgs ea)
		{
			MapRequestBuilder mapRequest = ea.Retriever.Request as MapRequestBuilder;
			System.Diagnostics.Debug.Assert(mapRequest.ClientInfo["Purpose"] != null, "Purpose is null.");
			this.turnOffMapCancelUi();

			Layer layer = mapRequest.ClientInfo["Layer"] as Layer;

			// See if there were errors.
			if (ea.Reason == RetrieverDoneArgs.CompletionReason.Error)
			{
				System.Windows.Forms.MessageBox.Show("Error retrieving " + layer.Title + ": " + ea.Message,
					"Retrieval error",
					System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
			}
			else if (ea.Reason == RetrieverDoneArgs.CompletionReason.TimedOut)
			{
				System.Windows.Forms.MessageBox.Show("Retrieval of " + layer.Title + " timed out.",
					"Retrieval error",
					System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
			}
			else if (ea.ContentType.Equals("application/vnd.ogc.se_xml")
				|| ea.ContentType.Equals("application/vnd.ogc.se+xml")
				|| ea.ContentType.Equals("text/xml"))
			{
				string msg = "Retrieval of " + layer.Title + " returned an error:" + System.Environment.NewLine;
				WmsException wmse = new WmsException(ea.DestinationFile);
				System.Windows.Forms.MessageBox.Show(msg + wmse.Message, "WMS Server Exception",
					System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
				this.deleteDestinationFile(ea.DestinationFile);
			}
			else if (!ea.ContentType.Equals(mapRequest.Format))
			{
				string msg = "The WMS Server returned " + layer.Title + " in the wrong format.";
				System.Windows.Forms.MessageBox.Show(msg, "WMS Map Format Error",
					System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
				this.deleteDestinationFile(ea.DestinationFile);
			}
			else
			{
				System.Diagnostics.Debug.Assert(ea.Reason == RetrieverDoneArgs.CompletionReason.Completed);

				// Add the result to the download cache.
				this.downloadCache.Add(mapRequest.Uri.ToString(), ea.DestinationFile, true);

				// Notify any listeners that the map is here and ready for use.
				string purpose = mapRequest.ClientInfo["Purpose"] as string;
				if (purpose.Equals("draw"))
				{
					DrawMapEventArgs dmea = new DrawMapEventArgs(ea.DestinationFile,
						mapRequest.ToString(), layer);
					this.OnDrawMap(this, dmea);
				}
				else if (purpose.Equals("preview"))
				{
					PreviewMapEventArgs pmea = new PreviewMapEventArgs(ea.DestinationFile,
						mapRequest.ToString(), layer);
					this.OnPreviewMap(this, pmea);
				}
			}
		}

		protected virtual void OnDrawMap(object sender, DrawMapEventArgs ea)
		{
			if (this.DrawMap != null)
			{
				this.DrawMap(this, ea);
			}
		}

		protected virtual void OnPreviewMap(object sender, PreviewMapEventArgs ea)
		{
			if (this.PreviewMap != null)
			{
				this.PreviewMap(this, ea);
			}

			// Do the preview if nobody else did it.
			if (!ea.PreviewHandled)
			{
				this.showMapPreview(sender, ea);
			}
		}

		private void turnOnMapCancelUi(string mapName, string purpose)
		{
			this.downloadIndicator.Text = "Downloading " + mapName;
			this.downloadIndicator.Visible = true;
			if (purpose.Equals("draw"))
			{
				this.drawButton.Visible = false;
				this.cancelDrawButton.Visible = true;
			}
			else if (purpose.Equals("preview"))
			{
				this.previewButton.Visible = false;
				this.cancelPreviewButton.Visible = true;
			}
		}

		private void turnOffMapCancelUi()
		{
			this.downloadIndicator.Visible = false;
			this.drawButton.Visible = true;
			this.cancelDrawButton.Visible = false;
			this.previewButton.Visible = true;
			this.cancelPreviewButton.Visible = false;
		}

		private void showMapProgress(System.Object sender, RetrieverProgressArgs ea)
		{
			this.downloadIndicator.Visible = !this.downloadIndicator.Visible;
		}

		private void showServerProgress(System.Object sender, RetrieverProgressArgs ea)
		{
			Retriever r = sender as Retriever;
			ServerDescriptor descriptor = r.Request.ClientInfo["ServerDescriptor"] as ServerDescriptor;
			descriptor.layerTreeNode.Text += ".";
			this.updateServerDisplay();
		}

		private void deleteDestinationFile(string filePath)
		{
			try
			{
				System.IO.File.Delete(filePath);
			}
			catch (System.Exception)
			{
				// Just ignore the error.
			}
		}

		private void serverRetrieveDone(object sender, RetrieverDoneArgs ea)
		{
			Retriever r = sender as Retriever;
			ServerDescriptor descriptor = r.Request.ClientInfo["ServerDescriptor"] as ServerDescriptor;
			descriptor.layerTreeNode.Text = descriptor.friendlyName; // in case progress indicator has it off
			descriptor.layerTreeNode.ImageIndex = 4;
			descriptor.layerTreeNode.SelectedImageIndex = 4;

			if (ea.Reason == RetrieverDoneArgs.CompletionReason.Completed)
			{
				// Detect exceptions from the WMS server.
				if (ea.ContentType.Equals("application/vnd.ogc.wms_xml")
					|| ea.ContentType.Equals("text/xml"))
				{
					try
					{
						descriptor.server = new Server(ea.DestinationFile);
						descriptor.uri = descriptor.server.Uri.ToString();
						descriptor.friendlyName = descriptor.server.Capabilities.Layers[0].Title;
						descriptor.layerTreeNode.ImageIndex = 3;
						descriptor.layerTreeNode.SelectedImageIndex = 3;
					}
					catch (System.Exception)
					{
						string msg = "Information returned for " + descriptor.friendlyName + " is not valid.";
						System.Windows.Forms.MessageBox.Show(msg, "Invalid WMS Format",
							System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
					}
				}
				else if (ea.ContentType.Equals("application/vnd.ogc.se_xml")
					|| ea.ContentType.Equals("application/vnd.ogc.se+xml"))
				{
					string msg = "The WMS server named " + descriptor.friendlyName + " returned an error."
						+ System.Environment.NewLine;
					WmsException wmse = new WmsException(ea.DestinationFile);
					System.Windows.Forms.MessageBox.Show(msg + wmse.Message, "WMS Server Exception",
						System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
					this.deleteDestinationFile(ea.DestinationFile);
				}
				else
				{
					string msg = "The WMS server named " + descriptor.friendlyName + " returned an incorrect format of "
						+ ea.ContentType + "." + System.Environment.NewLine
						+ "This is not a valid format." + System.Environment.NewLine
						+ "Would you like to see if Internet Explorer can show you what was returned?";
					System.Windows.Forms.DialogResult yesNo = System.Windows.Forms.MessageBox.Show(msg, "Invalid WMS Format",
						System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Error);
					if (yesNo == System.Windows.Forms.DialogResult.Yes)
					{
						this.invokeIe(ea.Retriever.Request.ToString());
					}
				}
				descriptor.retriever = null;
				this.deleteDestinationFile(ea.DestinationFile);
			}
			else if (ea.Reason == RetrieverDoneArgs.CompletionReason.TimedOut)
			{
				string msg = "Contacting WMS server " + descriptor.friendlyName + " timed out."
					+ System.Environment.NewLine
					+ "Double click on the server name if you want to try contacting the server again.";
				System.Windows.Forms.MessageBox.Show(msg, "WMS Server Contact Timed Out",
					System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
			}
			else // there was an error
			{
				string msg = "Error contacting WMS server " + descriptor.friendlyName + ": " + ea.Message
					+ System.Environment.NewLine
					+ "Double click on the server name when you want to try contacting the server again.";
				System.Windows.Forms.MessageBox.Show(msg, "Unable to contact WMS server",
					System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
			}

			r.Dispose();
			this.updateServerDisplay();
		}

		private void showMapPreview(System.Object sender, PreviewMapEventArgs args)
		{
			if (this.previewDialog == null)
			{
				this.previewDialog = new PreviewDialog(null, null);
				this.previewDialog.Owner = this;
				this.previewDialog.Location = new System.Drawing.Point(
					this.Location.X + this.Width/2,
					this.Location.Y + this.Height/2);
				this.previewDialog.Visible = false;
			}
			this.previewDialog.Text = args.Layer.Title;
			this.previewDialog.Map = System.Drawing.Image.FromFile(args.MapFilePath);
			this.previewDialog.ShowDialog();
		}

		private void invokeIe(string uri)
		{
			// This function is here for help when debugging. It invokes Internet Explorer to
			// retrieve and display the uri.
			System.Diagnostics.Process ie = new System.Diagnostics.Process();
			ie.StartInfo.FileName = "iexplore.exe";
			ie.StartInfo.Arguments = uri;
			ie.Start();
		}
	
		protected virtual void OnCloseButtonClick(object sender, System.EventArgs e)
		{
			this.cancelMapRequest();
			this.Hide();
		}
	
		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			this.cancelMapRequest();
			base.OnClosing(e);

			// Prevent the dialog from being disposed when the user hits the Close button
			// on the form banner; that way the application can re-use it. The dialog will
			// be disposed when the application exits or closes the dialog explicitly.
			this.Hide();
			e.Cancel = true;
		}

		private void clearInfoPage()
		{
			this.abstractBox.Clear();
			this.infoTree.Nodes.Clear();
		}

		private void updateInfoBox(Layer layer)
		{
			this.clearInfoPage();

			if (layer == null) // Represents a server that isn't downloaded yet.
			{
				this.abstractBox.Text = "No information available."
					+ System.Environment.NewLine
					+ "To try contacting the server again, double click on its name.";
				return;
			}

			if (!layer.HasParentLayer)
			{
				// This is a server's top layer. Need to include the server info in
				// the abstract box.
				Server server = layer.Server;
				this.abstractBox.Text = server.Capabilities.ServiceAbstract.Equals(string.Empty)
					? "No description available." : server.Capabilities.ServiceAbstract;
			}
			else
			{
				this.abstractBox.Text = layer.Abstract.Equals(string.Empty)
					? "No description available." : layer.Abstract;
			}

			// Determine which info-box nodes are expanded so that they are left that
			// way when this function ends. See the companion code at function end below.
			System.Collections.ArrayList expanded = new System.Collections.ArrayList();
			foreach (System.Windows.Forms.TreeNode currentNode in this.infoTree.Nodes)
			{
				if (currentNode.IsExpanded)
				{
					expanded.Add(currentNode.Text);
				}
			}

			this.infoTree.BeginUpdate();
//			this.infoTree.Nodes.Clear();

			System.Windows.Forms.TreeNode node;

			if (!layer.HasParentLayer)
			{
				// This is a server's top layer, so need to include the server info in
				// the info box.
				Capabilities caps = layer.Server.Capabilities;
				this.infoTree.Nodes.Add(caps.ServiceName.Equals(string.Empty)
					? "Server name: none" : "Server name: " + caps.ServiceName);
				this.infoTree.Nodes.Add(caps.ServiceTitle.Equals(string.Empty)
					? "Server title: none" : "Server title: " + caps.ServiceTitle);
				this.infoTree.Nodes.Add(caps.ServiceOnlineResource.Equals(string.Empty)
					? "Server URI: none specified" : "Server URI: " + caps.ServiceOnlineResource);
				this.infoTree.Nodes.Add(caps.Version.Equals(string.Empty)
					? "WMS version: none specified" : "WMS version: " + caps.Version);
				this.infoTree.Nodes.Add(caps.UpdateSequence.Equals(string.Empty)
					? "Update sequence: none specified" : "Update sequence: " + caps.UpdateSequence);

				this.infoTree.Nodes.Add("Get Capabilities Uri: " + caps.GetCapabilitiesRequestUri);
				this.infoTree.Nodes.Add("Get Map Uri: " + caps.GetMapRequestUri);

				if (caps.ServiceFees.Equals(string.Empty))
				{
					this.infoTree.Nodes.Add("Service fees: none specified");
				}
				else if (caps.ServiceFees.Length < 40)
				{
					this.infoTree.Nodes.Add("Service fees: " + caps.ServiceFees);
				}
				else
				{
					node = this.infoTree.Nodes.Add("Service fees");
					node.Nodes.Add(caps.ServiceFees);
				}

				if (caps.ServiceAccessConstraints.Equals(string.Empty))
				{
					this.infoTree.Nodes.Add("Service access constraints: none specified");
				}
				else if (caps.ServiceAccessConstraints.Length < 40)
				{
					this.infoTree.Nodes.Add("Service access constraints: " + caps.ServiceAccessConstraints);
				}
				else
				{
					node = this.infoTree.Nodes.Add("Service access constraints");
					node.Nodes.Add(caps.ServiceAccessConstraints);
				}

				if (caps.ServiceKeywordList.Length > 0)
				{
					node = new System.Windows.Forms.TreeNode();
					node.Text = "Service keywords";
					this.infoTree.Nodes.Add(node);
					string[] serviceKeywords = caps.ServiceKeywordList;
					if (serviceKeywords.Length < 1)
					{
						node.Text += ": none";
					}
					else
					{
						foreach (string kw in serviceKeywords)
						{
							node.Nodes.Add(kw);
						}
					}
				}

				node = this.infoTree.Nodes.Add("Contact information");
				if (caps.ServiceContactPerson.Length > 0)
					node.Nodes.Add("Contact person: " + caps.ServiceContactPerson
						+ (caps.ServiceContactPosition.Equals(string.Empty) ? ""
						: "(" + caps.ServiceContactPosition + ")"));
				if (caps.ServiceContactOrganization.Length > 0)
					node.Nodes.Add("Organization: " + caps.ServiceContactOrganization);
				if (caps.ServiceContactVoiceTelephone.Length > 0)
					node.Nodes.Add("Voice telephone: " + caps.ServiceContactVoiceTelephone);
				if (caps.ServiceContactFacsimileTelephone.Length > 0)
					node.Nodes.Add("Fax: " + caps.ServiceContactFacsimileTelephone);
				if (caps.ServiceContactElectronicMailAddress.Length > 0)
					node.Nodes.Add("E-mail address: " + caps.ServiceContactElectronicMailAddress);
				if (caps.ServiceContactAddressType.Length > 0)
					node.Nodes.Add("Address type: " + caps.ServiceContactAddressType);
				if (caps.ServiceContactAddress.Length > 0)
					node.Nodes.Add("Address: " + caps.ServiceContactAddress);
				if (caps.ServiceContactAddressCity.Length > 0)
					node.Nodes.Add("City: " + caps.ServiceContactAddressCity);
				if (caps.ServiceContactAddressStateOrProvince.Length > 0)
					node.Nodes.Add("State or Province: " + caps.ServiceContactAddressStateOrProvince);
				if (caps.ServiceContactAddressPostCode.Length > 0)
					node.Nodes.Add("Postal code: " + caps.ServiceContactAddressPostCode);
				if (caps.ServiceContactAddressCountry.Length > 0)
					node.Nodes.Add("Country : " + caps.ServiceContactAddressCountry);
				// If no contact info was specified, just say "none" at the top.
				if (node.Nodes.Count == 0)
					node.Text = "Contact information: none";
			}

			this.infoTree.Nodes.Add(layer.Name.Equals(string.Empty) ? "Layer name: none" : "Layer name: " + layer.Name);
			this.infoTree.Nodes.Add(@"Lat/Lon Bounding Box: " + layer.LatLonBoundingBox.ToString());

			node = new System.Windows.Forms.TreeNode("Attribution");
			this.infoTree.Nodes.Add(node);
			if (layer.Attribution.IsEmpty)
			{
				node.Text += ": none";
			}
			else
			{
				node.Nodes.Add(layer.Attribution.Title);
				node.Nodes.Add("URI: " + layer.Attribution.Uri);
				node.Nodes.Add("Logo URI: " + layer.Attribution.LogoUri.Uri.Uri);
			}

			node = new System.Windows.Forms.TreeNode("Scale hint");
			this.infoTree.Nodes.Add(node);
			if (layer.ScaleHint.IsEmpty)
			{
				node.Text += ": none";
			}
			else
			{
				node.Text += ": Min = " + layer.ScaleHint.Min.ToString()
					+ ", Max = " + layer.ScaleHint.Max.ToString();
			}

			node = new System.Windows.Forms.TreeNode();
			node.Text = "Keywords";
			this.infoTree.Nodes.Add(node);
			string[] keywords = layer.KeywordList;
			if (keywords.Length < 1)
			{
				node.Text += ": none";
			}
			else
			{
				foreach (string keyword in keywords)
				{
					node.Nodes.Add(keyword);
				}
			}

			node = new System.Windows.Forms.TreeNode("Dimensions");
			this.infoTree.Nodes.Add(node);
			Layer.DimensionType[] dims = layer.Dimensions;
			if (dims.Length < 1)
			{
				node.Text += ": none";
			}
			else
			{
				foreach (Layer.DimensionType dim in dims)
				{
					node.Nodes.Add(dim.ToString());
				}
			}

			node = new System.Windows.Forms.TreeNode("Extents");
			this.infoTree.Nodes.Add(node);
			Layer.ExtentType[] extents = layer.Extents;
			if (extents.Length < 1)
			{
				node.Text += ": none";
			}
			else
			{
				foreach (Layer.ExtentType extent in extents)
				{
					node.Nodes.Add(extent.ToString());
				}
			}

			node = new System.Windows.Forms.TreeNode("Bounding Boxes");
			this.infoTree.Nodes.Add(node);
			Layer.BoundingBoxType[] bboxes = layer.BoundingBoxes;
			if (bboxes.Length < 1)
			{
				node.Text += ": none";
			}
			else
			{
				foreach (Layer.BoundingBoxType bbox in bboxes)
				{
					node.Nodes.Add(bbox.ToString());
				}
			}

			node = new System.Windows.Forms.TreeNode("SRS");
			this.infoTree.Nodes.Add(node);
			string[] srses = layer.Srs;
			if (srses.Length < 1)
			{
				node.Text += ": none";
			}
			else
			{
				foreach (string srs in srses)
				{
					node.Nodes.Add(srs);
				}
			}

			node = new System.Windows.Forms.TreeNode("Authorities");
			this.infoTree.Nodes.Add(node);
			Layer.AuthorityUriType[] auls = layer.AuthorityUris;
			if (auls.Length < 1)
			{
				node.Text += ": none";
			}
			else
			{
				foreach (Wms.Client.Layer.AuthorityUriType aul in auls)
				{
					node.Nodes.Add("Name: " + aul.Name + ", URI: " + aul.Uri);
				}
			}

			node = new System.Windows.Forms.TreeNode("Identifiers");
			this.infoTree.Nodes.Add(node);
			Layer.IdentifierType[] ids = layer.Identifiers;
			if (ids.Length < 1)
			{
				node.Text += ": none";
			}
			else
			{
				foreach (Layer.IdentifierType id in ids)
				{
					node.Nodes.Add(id.Identifier + " , Authority: " + id.Authority);
				}
			}

			node = new System.Windows.Forms.TreeNode("Metadata");
			this.infoTree.Nodes.Add(node);
			Layer.MetadataUriType[] mdus = layer.MetadataUris;
			if (mdus.Length < 1)
			{
				node.Text += ": none";
			}
			else
			{
				foreach (Layer.MetadataUriType mdu in mdus)
				{
					node.Nodes.Add("Type: " + mdu.Type
						+ ", Format: " + mdu.MetadataUri.Format
						+ ", URL: " + mdu.MetadataUri.Uri);
				}
			}

			node = new System.Windows.Forms.TreeNode("Further information URI");
			this.infoTree.Nodes.Add(node);
			Layer.UriAndFormatType[] dus = layer.DataUris;
			if (dus.Length < 1)
			{
				node.Text += ": none";
			}
			else
			{
				foreach (Layer.UriAndFormatType du in dus)
				{
					node.Nodes.Add("Format: " + du.Format + ", URI: " + du.Uri);
				}
			}

			node = new System.Windows.Forms.TreeNode("Feature Lists");
			this.infoTree.Nodes.Add(node);
			Layer.UriAndFormatType[] fls = layer.FeatureListUris;
			if (fls.Length < 1)
			{
				node.Text += ": none";
			}
			else
			{
				foreach (Layer.UriAndFormatType fl in fls)
				{
					node.Nodes.Add("Format: " + fl.Format + ", URI: " + fl.Uri);
				}
			}

			node = new System.Windows.Forms.TreeNode("Layer attributes");
			this.infoTree.Nodes.Add(node);
			node.Nodes.Add("Queryable: " + (layer.Queryable ? "yes" : "no"));
			node.Nodes.Add("Opaque: " + (layer.Opaque ? "yes" : "no"));
			node.Nodes.Add("No subsets: " + (layer.NoSubsets ? "yes" : "no"));
			node.Nodes.Add("Cascaded: " + (layer.Cascaded > 0 ? layer.Cascaded.ToString() : "no"));
			node.Nodes.Add("Fixed width: " + (layer.FixedWidth > 0 ? layer.FixedWidth.ToString() : "no"));
			node.Nodes.Add("Fixed height: " + (layer.FixedHeight > 0 ? layer.FixedHeight.ToString() : "no"));

			node = new System.Windows.Forms.TreeNode("Styles");
			this.infoTree.Nodes.Add(node);
			Layer.StyleType[] styles = layer.Styles;
			if (styles.Length < 1)
			{
				node.Text += ": none";
			}
			else
			{
				foreach (Layer.StyleType style in styles)
				{
					string title = style.Title.Length > 0 ? style.Title : style.Name;
					System.Windows.Forms.TreeNode styleNode = node.Nodes.Add(title);
					if (style.Abstract.Length > 0)
					{
						styleNode.Nodes.Add("Description: " + style.Abstract);
					}
					else
					{
						styleNode.Nodes.Add("Description: none");
					}
					if (style.Name.Length > 0)
					{
						styleNode.Nodes.Add("Name: " + style.Name);
					}
					else
					{
						styleNode.Nodes.Add("Name: none");
					}
					if (!style.StyleUri.IsEmpty)
					{
						styleNode.Nodes.Add("Style URL: " + style.StyleUri.Uri);
						styleNode.Nodes.Add("Style URL format: " + style.StyleUri.Format);
					}
					else
					{
						styleNode.Nodes.Add("Style URL: none");
					}
					if (!style.LegendUri.IsEmpty)
					{
						styleNode.Nodes.Add("Legend URL: " + style.LegendUri.Uri.Uri);
						styleNode.Nodes.Add("Legend URL format: " + style.LegendUri.Uri.Format);
						styleNode.Nodes.Add("Legend URL width: " + style.LegendUri.Width.ToString());
						styleNode.Nodes.Add("Legend URL height: " + style.LegendUri.Height.ToString());
					}
					else
					{
						styleNode.Nodes.Add("Legend URL: none");
					}
					if (!style.StyleSheetUri.IsEmpty)
					{
						styleNode.Nodes.Add("Style sheet URL: " + style.StyleSheetUri.Uri);
						styleNode.Nodes.Add("Style sheet URL format: " + style.StyleSheetUri.Format);
					}
					else
					{
						styleNode.Nodes.Add("Style sheet URL: none");
					}
				}
			}

			// Re-expand those nodes that were expanded when this function
			// was called.
			foreach (System.Windows.Forms.TreeNode newNode in this.infoTree.Nodes)
			{
				if (expanded.Contains(newNode.Text))
				{
					newNode.Expand();
				}
			}

			this.infoTree.EndUpdate();
		}

		private void addServerMenuItem_Click(object sender, System.EventArgs e)
		{
			using (ServerAddDialog dialog = new ServerAddDialog())
			{
				System.Windows.Forms.DialogResult result = dialog.ShowDialog();
				if (result == System.Windows.Forms.DialogResult.OK)
				{
					if (dialog.ServerName == null || dialog.ServerName.Equals(string.Empty)
						|| dialog.ServerUri == null || dialog.ServerUri.Equals(string.Empty))
					{
						string msg = "Either the server name or the server Uri was not specified. "
							+ "Both must be specified to add a new server.";
						System.Windows.Forms.MessageBox.Show(msg, "Server Specification Error",
							System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
						return;
					}

					foreach (ServerDescriptor sd in this.serverDescriptors)
					{
						if (sd.uri.Equals(dialog.ServerUri))
						{
							string msg = "A server with the same Uri is already in the server list."
								+ "Its name in the list is " + sd.friendlyName + ". "
								+ "It will remain in the list with the old name, and the Add request will be ignored.";
							System.Windows.Forms.MessageBox.Show(msg, "Duplicate Server",
								System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
							return;
						}
					}

					ServerDescriptor newSd = this.addServer(dialog.ServerUri, dialog.ServerName);
					if (newSd != null)
					{
						newSd.retriever.Start();
					}
				}
			}
		}

		private void removeServerMenuItem_Click(object sender, System.EventArgs e)
		{
			System.Windows.Forms.TreeNode selectedNode = this.layerTree.SelectedNode;
			if (selectedNode == null)
			{
				string msg = "You must first select a server to remove.";
				System.Windows.Forms.MessageBox.Show(msg, "No Server Specified",
					System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
				return;
			}

			Layer layer = selectedNode.Tag as Layer;
			System.Diagnostics.Debug.Assert(layer != null);
			if (layer.HasParentLayer)
			{
				string msg = "You must first select a server to remove, not a directory or a map.";
				System.Windows.Forms.MessageBox.Show(msg, "No Server Specified",
					System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
				return;
			}

			if (selectedNode.Text != null)
			{
				this.removeServer(selectedNode.Text);
				this.clearInfoPage();
				this.updateServerDisplay();
			}
		}
	}

	public delegate void DrawMapEventHandler(object sender, DrawMapEventArgs ea);
	public delegate void PreviewMapEventHandler(object sender, PreviewMapEventArgs ea);

	public class DrawMapEventArgs : System.EventArgs
	{
		protected string mapFilePath;
		protected string mapRequestString;
		protected Layer layer;

		internal DrawMapEventArgs(string mapFilePath, string mapRequestString, Layer layer)
		{
			this.mapFilePath = mapFilePath;
			this.mapRequestString = mapRequestString;
			this.layer = layer;
		}

		public string MapFilePath {get {return this.mapFilePath;}}
		public string MapRequestString {get {return this.mapRequestString;}}
		public Layer Layer {get {return this.layer;}}
	}

	public class PreviewMapEventArgs : DrawMapEventArgs
	{
		private bool previewHandled;

		internal PreviewMapEventArgs(string mapFilePath, string mapRequestString, Layer layer)
			: base(mapFilePath, mapRequestString, layer)
		{
			this.previewHandled = false;
		}

		public bool PreviewHandled
		{
			set {this.previewHandled = value;}
			get {return this.previewHandled;}
		}
	}
}
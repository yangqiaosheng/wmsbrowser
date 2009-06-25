// $File: //depot/WMS/WMS Overview/Wms.Client/ServerDescriptor.cs $ $Revision: #1 $ $Change: 20 $ $DateTime: 2004/05/23 23:42:06 $

namespace Wms.Client
{
	/// <summary>
	/// Holds information describing a WMS server in a Wms.Client.WmsDialog object.
	/// </summary>
	internal class ServerDescriptor
	{
		internal string				uri;
		internal string				friendlyName;
		internal Retriever			retriever;
		internal Server				server;
		internal System.Windows.Forms.TreeNode layerTreeNode;

		internal ServerDescriptor(string uri, string friendlyName)
		{
			this.uri = uri;
			this.friendlyName = friendlyName;
		}
	}
}
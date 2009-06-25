// $File: //depot/WMS/WMS Overview/Wms.Client/Server.cs $ $Revision: #1 $ $Change: 20 $ $DateTime: 2004/05/23 23:42:06 $

namespace Wms.Client
{
	/// <summary>
	/// Represents a WMS server and holds its capabilities description.
    /// 表示一个WMS服务器
	/// </summary>
	public class Server
	{
		private Wms.Client.Capabilities		capabilities;

		public Server(string filePath)
		{
			this.capabilities = new Capabilities(filePath, this);
		}

		public Capabilities Capabilities
		{
			get {return this.capabilities;}
		}

		public System.Uri Uri
		{
			get {return new System.Uri(this.capabilities.GetCapabilitiesRequestUri);}
		}

		public MapRequestBuilder CreateMapRequest()
		{
			return new MapRequestBuilder(new System.Uri(this.capabilities.GetMapRequestUri));
		}
	}
}
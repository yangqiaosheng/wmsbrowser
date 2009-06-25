// $File: //depot/WMS/WMS Overview/Get Map Example/GetMapExample.cs $ $Revision: #2 $ $Change: 22 $ $DateTime: 2004/06/02 17:13:56 $

namespace Wms.Client
{
	/// <summary>
	/// Summary description for GetMapExample.
	/// </summary>
	class GetMapExample
	{
		[System.STAThread]
		static void Main(string[] args)
		{
			// Create a request to get the capabilities document from the server.
			 System.UriBuilder serverUri =
				 new System.UriBuilder(@"http://viz.globe.gov/viz-bin/wmt.cgi");
			Wms.Client.CapabilitiesRequestBuilder capsRequest = 
				new Wms.Client.CapabilitiesRequestBuilder(serverUri.Uri);

			// Retrieve the capabilities document and cache it locally.
			System.Net.WebRequest wr = System.Net.WebRequest.Create(capsRequest.Uri);

			// Handle any proxy the system may have defined.
			System.Net.WebProxy proxy = System.Net.WebProxy.GetDefaultProxy();
			if (proxy.Address != null)
			{
				wr.Proxy = proxy;
			}

			System.Net.WebResponse response = wr.GetResponse();
			string fileName = System.IO.Path.GetTempPath() + @"capabilities.xml";
			copyStreamToFile(response.GetResponseStream(), fileName);

			// Parse the capabilities document and create a capabilities object
			// for reading the parsed information. This is done by creating a Server
			// object to represent the server associated with the capabilities.
			Wms.Client.Server server = new Wms.Client.Server(fileName);
			Wms.Client.Capabilities caps = server.Capabilities;

			// Create a GetMap request using the MapRequestBuilder class. When
			// creating the request, use the URI given in the server's capabilities
			// document. This URI may be different than the one used to get the
			// capabilities document.
			Wms.Client.MapRequestBuilder mapRequest = 
				new Wms.Client.MapRequestBuilder(new System.Uri(caps.GetMapRequestUri));
			mapRequest.Layers = "COASTLINES,RATMIN";
			mapRequest.Styles = ","; // use default style for each layer
			mapRequest.Format = "image/gif";
			mapRequest.Srs = "EPSG:4326";
			mapRequest.BoundingBox = "-180.0,-90.0,180.0,90.0";
			mapRequest.Height = 300;
			mapRequest.Width = 600;
			mapRequest.Transparent = false;

			// Retrieve the map and cache it locally.
			System.Net.WebRequest mwr = System.Net.WebRequest.Create(mapRequest.Uri);
			if (proxy.Address != null)
			{
				mwr.Proxy = proxy;
			}
			System.Net.WebResponse mresponse = mwr.GetResponse();
			string mapFileName = System.IO.Path.GetTempPath() + @"wmsmap.gif";
			copyStreamToFile(mresponse.GetResponseStream(), mapFileName);

			// Use Internet Explorer to display the map.
			invokeIe(mapFileName);
		}

		private static void copyStreamToFile(System.IO.Stream stream, string destination)
		{
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

		static private void invokeIe(string uri)
		{
			System.Diagnostics.Process ie = new System.Diagnostics.Process();
			ie.StartInfo.FileName = "iexplore.exe";
			ie.StartInfo.Arguments = uri;
			ie.Start();
		}
	}
}
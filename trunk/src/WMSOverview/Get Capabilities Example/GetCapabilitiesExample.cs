// $File: //depot/WMS/WMS Overview/Get Capabilities Example/GetCapabilitiesExample.cs $ $Revision: #2 $ $Change: 22 $ $DateTime: 2004/06/02 17:13:56 $

namespace Wms.Client
{
	class GetCapabilitesExample
	{
		[System.STAThread]
		static void Main(string[] args)
		{
			System.UriBuilder uri = new System.UriBuilder(@"http://viz.globe.gov/viz-bin/wmt.cgi");
			uri.Query = "SERVICE=WMS&REQUEST=GetCapabilities";
			System.Net.WebRequest wr = System.Net.WebRequest.Create(uri.Uri);

			// Handle any proxy the system may have defined.
			System.Net.WebProxy proxy = System.Net.WebProxy.GetDefaultProxy();
			if (proxy.Address != null)
			{
				wr.Proxy = proxy;
			}

			System.Net.WebResponse response = wr.GetResponse();
			System.IO.StreamReader sr = new System.IO.StreamReader(response.GetResponseStream());
			string fileName = System.IO.Path.GetTempPath() + @"capabilities.xml";
			System.IO.StreamWriter sw = System.IO.File.CreateText(fileName);
			sw.Write(sr.ReadToEnd());
			sr.Close();
			sw.Close();
			invokeIe(fileName);
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
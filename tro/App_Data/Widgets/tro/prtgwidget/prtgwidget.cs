using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SystemsGarden.mc2.MC2SiteEnvironment;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.Common;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Net;
using System.IO;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.Common;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using MongoDB.Bson;
using SystemsGarden.mc2.Core.Runtime;

namespace SystemsGarden.mc2.widgets.prtgwidget
{
	public class prtgwidget : MC2Controller
	{
		#region Actions

		/// <summary>
		/// Generated code for MC2 widget "prtgwidget".
		/// </summary>
		[GrantAccess(5,7)]
		public ActionResult getprtggraph(string graphgroup, string graphidentifier)
		{
			string graphAddress = Runtime.Config["monitoring"]["graphs"][graphgroup][graphidentifier];

			if (string.IsNullOrEmpty(graphAddress))
				return new AjaxResult("");

			string graphRawData = GetGraphRawData(graphAddress);

			string svgBase64 = ExtractSvg(graphRawData);

			return new AjaxResult(svgBase64);

		}

		private string ExtractSvg(string graphRawData)
		{
			const string SvgStartText = "<svg ";
			const string SvgEndText = "</svg>";

			int startIndex = graphRawData.IndexOf(SvgStartText);
			int endIndex = graphRawData.IndexOf(SvgEndText) + SvgEndText.Length;

			string graphsDataText = graphRawData.Substring(startIndex, endIndex - startIndex);

			var graphDataBytes = System.Text.Encoding.UTF8.GetBytes(graphsDataText);
			return System.Convert.ToBase64String(graphDataBytes);
		}

		private string GetGraphRawData(string graphAddress)
		{
			WebRequest webRequest = WebRequest.Create(graphAddress);
			webRequest.Method = "GET";
			webRequest.AuthenticationLevel = System.Net.Security.AuthenticationLevel.None;

			string responseFromServer = string.Empty;

			try
			{
				using (WebResponse response = webRequest.GetResponse())
				{
					using (Stream dataStream = response.GetResponseStream())
					{
						// Open the stream using a StreamReader for easy access.
						using (StreamReader reader = new StreamReader(dataStream))
						{
							responseFromServer = reader.ReadToEnd();
						}
					}
				}
			}
			catch (Exception ex)
			{
				logger.LogError("Failed to get PRTG data from server: ", ex.ToString());
			}

			return responseFromServer;
		}

		#endregion
	}
}
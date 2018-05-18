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
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.Common;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using MongoDB.Bson;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.Common.Constants;

namespace SystemsGarden.mc2.widgets.refreshdocumentwidget
{
	public class refreshdocumentwidget : MC2Controller
	{
		#region Actions

		[GrantAccessToGroup("sysadmin")]
		public AjaxResult refreshdisplaynames(string collection, int page, int documentsperpage)
		{
			var refreshMessage = new RCMessage(MongoDBHandlerConstants.mdbrefreshdisplaynames);

			DataTree handler = refreshMessage.Handlers[MongoDBHandlerConstants.mongodbhandler];

			handler[MongoDBHandlerConstants.collection] = collection;
			handler[MongoDBHandlerConstants.skip] = page * documentsperpage;
			handler[MongoDBHandlerConstants.limit] = documentsperpage;

			RCResponse response = Runtime.RemoteConnection.ProcessMessageAsync(refreshMessage).Result;

			DataTree handledDocuments = response.Handlers[MongoDBHandlerConstants.mongodbhandler];
			handledDocuments[MongoDBHandlerConstants.documentsprocessed].JsonSerializationType = JsonSerializationType.ChildrenAsArrays;
			handledDocuments[MongoDBHandlerConstants.documentsfailed].JsonSerializationType = JsonSerializationType.ChildrenAsArrays;

			return new AjaxResult(handledDocuments);
		}

		//[GrantAccessToGroup("sysadmin")]
		//public AjaxResult resavedocuments(string collection, int page, int documentsperpage)
		//{
		//	var query = new DBQuery("refreshdocumentwidget", "documents");

		//	query.AddParameter("collectionname", collection);				

		//	DBResponse response =  query.FindAsync().Result;
		//	DataTree results = response.Results;

		//	UpdateDatabase(results);
		//}


		#endregion
	}
}
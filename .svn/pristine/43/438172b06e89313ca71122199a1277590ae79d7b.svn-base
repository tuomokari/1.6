using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using SystemsGarden.mc2.Common;
using SystemsGarden.mc2.RemoteConnector.Handlers;
using SystemsGarden.mc2.Common.Constants;
using SystemsGarden.mc2.RemoteConnector.Handlers.CoreServerHandlers.MongoDBHandler;
using MongoDB.Bson;
using MongoDB.Shared;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace SystemsGarden.mc2.RemoteConnector.Handlers.VismaIntegrationHandlerServer
{
    public class VismaIntegrationHandlerServer : BaseHandler
    {
        private TroToVismaExport troToVismaExport;

        private MongoDatabase database;

        private static string IntegrationTroToVismaFolder = "integration\\trotovisma";

        private int troToVismaPollingInterval;

        private Thread troToVismaPollingThread;

        public VismaIntegrationHandlerServer(
            IRemoteConnection remoteConnection, ILogger parentLogger, IHandlerContainer handlerContainer)
            : base(remoteConnection, parentLogger, VismaIntegrationHandlerServerInfo.HandlerName, handlerContainer)
        {
            _handlerInfo = new VismaIntegrationHandlerServerInfo();
        }

        public override void HandleMessageInternal()
        {
            ForwardMessage();
        }

        // Note that integration must be set AFTER the MongoDB handler module
        protected override void Initialize()
        {
            database = ((MongoDBHandlerServer)handlerContainer.GetHandler("mongodbhandler")).Database;

            troToVismaPollingInterval = (int)Message["trotovismapollinginterval"].GetValueOrDefault(12000);

            string integrationFolderTroToVisma = remoteConnection.GetDataPath() + Path.DirectorySeparatorChar + IntegrationTroToVismaFolder;

            troToVismaExport = new TroToVismaExport(logger, integrationFolderTroToVisma, database, (DataTree)Message.Clone());
            troToVismaPollingThread = new Thread(RuntTroToVismaExport);
            troToVismaPollingThread.Start();
        }

        private void RuntTroToVismaExport()
        {
            logger.LogDebug("Running TRO to Visma export thread.");

            while (!stopping.WaitOne(0))
            {
                try
                {
                    troToVismaExport.ExportDocuments();
                }
                catch(Exception ex)
                {
                    logger.LogError("Failed to export documents to visma", ex);
                }

                stopping.WaitOne(troToVismaPollingInterval);
            }
        }
    }
}

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

        public VismaIntegrationHandlerServer(
            IRemoteConnection remoteConnection, ILogger parentLogger, IHandlerContainer handlerContainer)
            : base(remoteConnection, parentLogger, VismaIntegrationHandlerServerInfo.HandlerName, handlerContainer)
        {
            _handlerInfo = new VismaIntegrationHandlerServerInfo();
        }

        public override void HandleMessageInternal()
        {
            switch ((string)Message[RCConstants.action])
            {
                case "monitorpoll":
                    break;

                case "vismaexportdata":
                    VismaExport();
                    break;

                case "vismacancelexport":
                    VismaCancelExport();
                    break;
            }

            ForwardMessage();
        }

        // Note that integration must be set AFTER the MongoDB handler module
        protected override void Initialize()
        {
            database = ((MongoDBHandlerServer)handlerContainer.GetHandler("mongodbhandler")).Database;
            string integrationFolderTroToVisma = remoteConnection.GetDataPath();

            integrationFolderTroToVisma = Path.Combine(integrationFolderTroToVisma, IntegrationTroToVismaFolder);

            troToVismaExport = new TroToVismaExport(logger, integrationFolderTroToVisma, database, Message);
        }

        private void VismaExport()
        {
            logger.LogDebug("Exporting data to Visma.");

            try
            {
                // Allow only one export at a time to run.
                lock (troToVismaExport)
                {
                    troToVismaExport.ExportDocuments(Message["user"], Message["project"], Message["payrollperiod"]);
                }
            }
            catch(Exception ex)
            {
                logger.LogError("Failed to export documents to visma", ex);
                remoteConnection.ReportError(originalMessage.Value, originalResponse.Value, ex.Message);
            }
        }

        private void VismaCancelExport()
        {
            logger.LogDebug("Restoring exported items to unexported state.");

            try
            {
                // Allow only one export at a time to run.
                lock (troToVismaExport)
                {
                    troToVismaExport.CancelDocumentExport(Message["user"], Message["project"], Message["payrollperiod"]);
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to export documents to visma", ex);
                remoteConnection.ReportError(originalMessage.Value, originalResponse.Value, ex.Message);
            }
        }
    }
}

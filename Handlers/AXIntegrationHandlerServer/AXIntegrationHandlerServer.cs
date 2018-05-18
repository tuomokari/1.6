using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using SystemsGarden.mc2.RemoteConnector.Handlers;
using SystemsGarden.mc2.Common;
using SystemsGarden.mc2.Common.Constants;
using SystemsGarden.mc2.RemoteConnector.Handlers.CoreServerHandlers.MongoDBHandler;
using System.IO;
using System.Globalization;
using MongoDB.Bson;
using MongoDB.Shared;
using MongoDB.Driver;
using MongoDB.Driver.Builders;


namespace SystemsGarden.mc2.RemoteConnector.Handlers.AXIntegrationHandlerServer
{
    public class AXIntegrationHandlerServer : BaseHandler
    {
        private AXToTroImport axToTroImport;
        private TroToAXExport troToAxExport;

        private static string IntegrationAxToTroFolder = "integration\\axtotro";
        private static string IntegrationTroToAxFolder = "integration\\trotoax";
        private static string IntegrationTroToAxFolderCopy = "integration\\trotoaxcopy";

        private int axToTroPollingInterval;
        private int troToAxPollingInterval;

        private Thread axToTroPollingThread;
        private Thread troToAXPollingThread;

        // Monitor data
        long initializedTimestamp = 0;

        int importFolderChecked = 0;
        int exportDataChecked = 0;

        public AXIntegrationHandlerServer(
            IRemoteConnection remoteConnection, ILogger parentLogger, IHandlerContainer handlerContainer)
            : base(remoteConnection, parentLogger, AXIntegrationHandlerServerInfo.HandlerName, handlerContainer)
        {
            _handlerInfo = new AXIntegrationHandlerServerInfo();
        }

        public override void HandleMessageInternal()
        {
            switch ((string)Message[RCConstants.action])
            {
                case "monitorpoll":

                    GetMonitorData();
                    break;

                case "getinvalidatedcacheitems":

                    GetInvalidatedCacheItems();
                    break;

				case "addproject":

					AddProject();
					break;
			}

			ForwardMessage();
        }

		/// <summary>
		/// Add project based on message from front. Can be used to add projects faster or outside
		/// the normal polling AX import polilng cycle.
		/// </summary>
		private void AddProject()
		{
			// Size of array is the index of last enum value (starting from 0) plus one.
			string[] projectLine = new string[(int)Project.ProjectNoResourcing + 1];

			DataTree projectData = Message["projectdata"];

			projectLine[(int)Project.ProjectId] = projectData["projectid"];
			projectLine[(int)Project.ProjectName] = projectData["projectname"];
			projectLine[(int)Project.CustomerId] = projectData["customerid"];
			projectLine[(int)Project.CustomerName] = projectData["customername"];
			projectLine[(int)Project.ProjectDeliveryAddressStreet] = projectData["projectdeliveryaddressstreet"];
			projectLine[(int)Project.ProjectDeliveryAddressZip] = projectData["projectdeliveryaddresszip"];
			projectLine[(int)Project.ProjectDeliveryAddressCity] = projectData["projectdeliveryaddresscity"];
			projectLine[(int)Project.ProjectDeliveryAddressCountry] = projectData["projectdeliveryaddresscountry"];
			projectLine[(int)Project.ProjectDescription] = projectData["projectdescription"];
			projectLine[(int)Project.ProjectProfitCenter] = projectData["projectprofitcenter"];
			projectLine[(int)Project.ProjectStatus] = projectData["projectstatus"];
			projectLine[(int)Project.ProjectContactPersonName] = projectData["projectcontactpersonname"];
			projectLine[(int)Project.ProjectContactPersonTelephone] = projectData["projectcontactpersontelephone"];
			projectLine[(int)Project.ProjectContactPersonEmail] = projectData["projectcontactpersonemail"];
			projectLine[(int)Project.ProjectConstructionSiteKey] = projectData["projectconstructionsitekey"];
			projectLine[(int)Project.ProjectManager] = projectData["projectmanager"];
			projectLine[(int)Project.ProjectStartDate] = projectData["projectstartdate"];
			projectLine[(int)Project.ProjectEndDate] = projectData["projectenddate"];
			projectLine[(int)Project.ProjectNoResourcing] = projectData["projectnoresourcing"];

			DateTime projectDateTime = DateTime.MinValue;
			projectDateTime = DateTime.ParseExact(projectData["timestamp"], "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
			
			axToTroImport.ImportProject(projectLine, projectDateTime);
		}

		private void GetInvalidatedCacheItems()
        {
            if (Response.Contains("invalidatedcacheitems"))
                Response["invalidatedcacheitems"].Merge(axToTroImport.GeAndClearInvalidatedCacheItems());
            else
                Response["invalidatedcacheitems"] = axToTroImport.GeAndClearInvalidatedCacheItems();
        }

        // Note that integration must be set AFTER the MongoDB handler module
        protected override void Initialize()
        {
            Interlocked.Exchange(ref initializedTimestamp, MC2DateTimeValue.Now().Ticks);
            
            axToTroPollingInterval = (int)Message["axtotropollinginterval"].GetValueOrDefault(10000);
            troToAxPollingInterval = (int)Message["trotoaxpollinginterval"].GetValueOrDefault(12000);

            string integrationFolderAxToTro = remoteConnection.GetDataPath() + Path.DirectorySeparatorChar + IntegrationAxToTroFolder;
            string integrationFolderTroToAx = remoteConnection.GetDataPath() + Path.DirectorySeparatorChar + IntegrationTroToAxFolder;
            string integrationFolderTroToAxCopy = remoteConnection.GetDataPath() + Path.DirectorySeparatorChar + IntegrationTroToAxFolderCopy;

			new DirectoryInfo(integrationFolderAxToTro).Create();
			new DirectoryInfo(integrationFolderTroToAx).Create();
			new DirectoryInfo(integrationFolderTroToAxCopy).Create();

			var mongoDBHandler = ((MongoDBHandlerServer)handlerContainer.GetHandler("mongodbhandler"));

            axToTroImport = new AXToTroImport(
                logger,
                integrationFolderAxToTro,
                mongoDBHandler);
            
            axToTroPollingThread = new Thread(RunAXToTroImport);
            axToTroPollingThread.Start();

            troToAxExport = new TroToAXExport(logger, integrationFolderTroToAx, integrationFolderTroToAxCopy, mongoDBHandler.Database, (DataTree)Message.Clone());
            troToAXPollingThread = new Thread(RunTroToAXExport);
            troToAXPollingThread.Start();
        }

        private void RunAXToTroImport()
        {
            logger.LogDebug("Running AX to Tro import thread.");

            while (!stopping.WaitOne(0))
            {
                Interlocked.Increment(ref importFolderChecked);

                try 
                {
                    axToTroImport.ProcessFiles();
                }
                catch(Exception ex)
                {
                    logger.LogError("Failed import entries from AX.", ex);
                }

                stopping.WaitOne(axToTroPollingInterval);
            }
        }

        private void RunTroToAXExport()
        {
            logger.LogDebug("Running TRO to AX export thread.");


            while (!stopping.WaitOne(0))
            {
                Interlocked.Increment(ref exportDataChecked);

                try
                {
                    troToAxExport.ExportDocuments();
                }
                catch (Exception ex)
                {
                    logger.LogError("Failed to export entries to AX.", ex);
                }

                stopping.WaitOne(troToAxPollingInterval);
            }
        }

        private void GetMonitorData()
        {
            Response["import"]["fileimportssucceeded"] = axToTroImport.FileImportsSucceeded;
            Response["import"]["fileimportsfailed"] = axToTroImport.FileImportsFailed;
            Response["import"]["initialized"] = new DateTime(initializedTimestamp);
            Response["import"]["importfolderchecked"] = importFolderChecked;
            Response["import"]["workersimportednew"] = axToTroImport.WorkersImportedNew;
            Response["import"]["workersimportedfail"] = axToTroImport.WorkersImportedFail;
            Response["import"]["contractorsimportednew"] = axToTroImport.ContractorsImportedNew;
            Response["import"]["contractorsimportedupdate"] = axToTroImport.ContractorsImportedUpdate;
            Response["import"]["contractorsimportedfail"] = axToTroImport.ContractorsImportedFail;
            Response["import"]["projectsimportednew"] = axToTroImport.ProjectsImportedNew;
            Response["import"]["projectsimportedupdate"] = axToTroImport.ProjectsImportedUpdate;
            Response["import"]["projectsimportedfail"] = axToTroImport.ProjectsImportedFail;
            Response["import"]["articlesimportednew"] = axToTroImport.ArticlesImportedNew;
            Response["import"]["articlesimportedupdate"] = axToTroImport.ArticlesImportedUpdate;
            Response["import"]["articlesimportedfail"] = axToTroImport.ArticlesImportedFail;
            Response["import"]["projectcategoriesimportednew"] = axToTroImport.ProjectCategoriesImportedNew;
            Response["import"]["projectcategoriesimportedupdate"] = axToTroImport.ProjectCategoriesImportedUpdate;
            Response["import"]["projectcategoriesimportedfail"] = axToTroImport.ProjectCategoriesImportedFail;
            Response["import"]["articlesimportednew"] = axToTroImport.ArticlesImportedNew;
            Response["import"]["articlesimportedupdate"] = axToTroImport.ArticlesImportedUpdate;
            Response["import"]["machineryimportednew"] = axToTroImport.MachineryImportedNew;
            Response["import"]["machineryimportedupdate"] = axToTroImport.MachineryImportedUpdate;
            Response["import"]["machineryimportedfail"] = axToTroImport.MachineryImportedFail;
            
            Response["export"]["exportfolderchecked"] = exportDataChecked;
            Response["export"]["WorkerTimesheetEntryFragmentsExported"] = troToAxExport.WorkerTimesheetEntryFragmentsExported;
            Response["export"]["WorkerTimesheetEntryFilesCreated"] = troToAxExport.WorkerTimesheetEntryFilesCreated;
            Response["export"]["WorkerTimesheetEntryExportsFailed"] = troToAxExport.WorkerTimesheetEntryExportsFailed;
            Response["export"]["AssetTimesheetEntriesExported"] = troToAxExport.AssetTimesheetEntriesExported;
            Response["export"]["AssetTimesheetEntryFilesCreated"] = troToAxExport.AssetTimesheetEntryFilesCreated;
            Response["export"]["AssetTimesheetEntryExportsFailed"] = troToAxExport.AssetTimesheetEntryExportsFailed;
            Response["export"]["ItemsExported"] = troToAxExport.ItemsExported;
            Response["export"]["ItemsFilesCreated"] = troToAxExport.ItemsFilesCreated;
            Response["export"]["ItemExportsFailed"] = troToAxExport.ItemExportsFailed;
            Response["export"]["ExpensesExported"] = troToAxExport.ExpensesExported;
            Response["export"]["ExpensesFilesCreated"] = troToAxExport.ExpensesFilesCreated;
            Response["export"]["ExpenseExportsFailed"] = troToAxExport.ExpenseExportsFailed;
        }
    }
}

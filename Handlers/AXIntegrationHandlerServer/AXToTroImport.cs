using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SystemsGarden.mc2.Common;
using System.Globalization;
using SystemsGarden.mc2.RemoteConnector.Handlers.CoreServerHandlers.MongoDBHandler;
using MongoDB.Bson;
using MongoDB.Shared;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using SystemsGarden.mc2.RemoteConnector.Handlers.TroIntegrationCommon;

namespace SystemsGarden.mc2.RemoteConnector.Handlers.AXIntegrationHandlerServer
{
    public enum Worker
    {
        PersonnelNumber = 0,
        FirstName = 1,
        MiddleName = 2,
        LastName = 3,
        OrganizationCode = 4,
        Pin = 5,
        LanguageCode = 6,
        EmployeeCountryCode = 7,
        EmploymentStartDate = 8,
        EmploymentEndDate = 9,
        ProbationPeriodEndDate = 10,
        DefaultDimension = 11, // Note that this is similar to Worker's ProfitCenter = 19
        DefaultDimensionDescription = 12,
        Office = 13,
        Title = 14,
        Email = 15,
        TaxRegistrationId = 16,
        SharepointLinkToPhotograph = 17,
        Supervisor = 18,
        WeeklyWorkingHours = 19,
        CLAContract = 20,
        PersonnelGroup = 21,
        SilmuGroup = 22
    }

    public enum Contractor
    {
        PersonnelNumber = 0,
        FirstName = 1,
        MiddleName = 2,
        LastName = 3,
        EmploymentContractor = 4,
        EmploymentContractorValidFrom = 5,
        EmploymentContractorValidTo = 6,
        Pin = 7,
        TaxRegistrationId = 8,
        OrganizationCode = 9,
        VendorId = 10,
        VendorName = 11,
        LanguageCode = 12,
        EmployeeCountryCode = 13,
        Office = 14,
        WeeklyWorkingHours = 15,
        SilmuGroup = 16,
        Email = 17,
        Supervisor = 18,
        ProfitCenter = 19, // Note that this is similar to Worker's DefaultDimenison = 11
        ProfitCenterDescription = 20 // Note that this is similar to Worker's DefaultDimenisonDescription = 12
    }

    public enum Project
    {
        ProjectId = 0,
        ProjectName = 1,
        CustomerId = 2,
        CustomerName = 3,
        ProjectDeliveryAddressStreet = 4,
        ProjectDeliveryAddressZip = 5,
        ProjectDeliveryAddressCity = 6,
        ProjectDeliveryAddressCountry = 7,
        ProjectDescription = 8,
        ProjectProfitCenter = 9,
        ProjectStatus = 10,
        ProjectContactPersonName = 11,
        ProjectContactPersonTelephone = 12,
        ProjectContactPersonEmail = 13,
        ProjectConstructionSiteKey = 14,
        ProjectManager = 15,
        ProjectStartDate = 16,
        ProjectEndDate = 17,
        ProjectNoResourcing = 18
    }

    public enum Item
    {
        ItemNumber = 0,
        ItemDescription = 1,
        ItemGroup = 2,
        ItemGroupDescription = 3,
        ItemUnitOfMeasure = 4
    }

    public enum ProjectCategory
    {
        CategoryType = 0,
        CategoryId = 1,
        CategoryDescription = 2,
        CategoryGroup = 3
    }

    public enum Machinery
    {
        MachineryRowType = 0,
        MachineId = 1,
        LicensePlate = 2,
        Description = 3,
        Supervisor = 4,
        Office = 5,
        SilmuGroup = 6,
        FixedAssetNumber = 7,
        ProjectCategory = 8,
        ProfitCenter = 9
    }

    public class AXToTroImport
    {
        private const string WorkerFileNameStart = "SWRK_";
        private const string ContractorFileNameStart = "SCTR_";
        private const string ProjectFileNameStart = "Projects";
        private const string ArticleFileNameStart = "Items";
        private const string ProjectCategoryNameStart = "ProjCategories";
        private const string MachineryNameStart = "SMACH_";

        private const string DefaultClaContract = "FI06";
        private const string ContractorClaContract = "EXT";

        private const string SubProjectSeparator = "-";

        private const string IncomingFileLocation = "Incoming";
        private const string DoneFileLocation = "Done";
        private const string FailedFileLocation = "Failed";

        private const char CsvSeparator = ';';

        private const string SilmuGroup_1 = "";
        private const string SilmuGroup_2 = "Laajennettu";
        private const string SilmuGroup_3 = "Tyonjohtaja";
        private const string SilmuGroup_4 = "Palkanlaskija";

        private const string ProjectNoResourcing_NoResourcing = "EI-RESURSS";

        private const string ProjectStatus_InPlanning = "In planning";
        private const string ProjectStatus_Created = "Created";
        private const string ProjectStatus_Delivered = "Delivered";
        private const string ProjectStatus_Finished = "Finished";

        private DataTree invalidatedCacheItems = new DataTree();

        private ILogger logger;
        private string filePath;
        private MongoDatabase database;
        private MongoDBHandlerServer mongoDBHandler;

        private bool currentProcessFailed = false;

        private object lockObject = new object();

        public AXToTroImport(
            ILogger logger,
            string filePath,
            MongoDBHandlerServer mongoDBHandler)
        {
            this.logger = logger.CreateChildLogger("AXToTroImport");
            this.filePath = filePath;
            this.database = mongoDBHandler.Database;
            this.mongoDBHandler = mongoDBHandler;

            if (!this.filePath.EndsWith(Convert.ToString(Path.DirectorySeparatorChar)))
                this.filePath += Path.DirectorySeparatorChar;
        }

        // Monitor data

        public int WorkersImportedNew = 0;
        public int WorkersImportedUpdate = 0;
        public int WorkersImportedFail = 0;
        public int ContractorsImportedNew = 0;
        public int ContractorsImportedUpdate = 0;
        public int ContractorsImportedFail = 0;
        public int ProjectsImportedNew = 0;
        public int ProjectsImportedUpdate = 0;
        public int ProjectsImportedFail = 0;
        public int ProjectCategoriesImportedNew = 0;
        public int ProjectCategoriesImportedUpdate = 0;
        public int ProjectCategoriesImportedFail = 0;
        public int ArticlesImportedNew = 0;
        public int ArticlesImportedUpdate = 0;
        public int ArticlesImportedFail = 0;
        public int MachineryImportedNew = 0;
        public int MachineryImportedUpdate = 0;
        public int MachineryImportedFail = 0;

        public int FileImportsSucceeded = 0;
        public int FileImportsFailed = 0;

        public void ProcessFiles()
        {
            if (!MongoDBHandlerServer.SchemaApplied)
            {
                logger.LogTrace("Schema not yet applied. Cannot import files from AX.");
                return;
            }

            lock (lockObject)
            {
                string incomingFolder = filePath + IncomingFileLocation;

                logger.LogTrace("Processing files in incoming folder.", incomingFolder);

                var di = new DirectoryInfo(incomingFolder);

                if (!di.Exists)
                    di.Create();

                FileInfo[] files = di.GetFiles();

                //User Enumerable.OrderBy to sort the files array and get a new array of sorted files
                FileInfo[] sortedFiles = files.OrderBy(r => r.Name).ToArray();

				foreach (FileInfo fi in sortedFiles)
				{
					// Process the file twice in case it has any internal relations
					ProcessFile(fi, false);

					if (!ProcessFile(new FileInfo(fi.FullName), false))
						continue;

					ProcessFile(new FileInfo(fi.FullName), true);
				}

				logger.LogTrace("File processing completed.");
            }
        }

        public DataTree GeAndClearInvalidatedCacheItems()
        {
            lock (lockObject)
            {
                var result = (DataTree)invalidatedCacheItems.Clone();
                invalidatedCacheItems.Clear();
                return result;
            }
        }

		private DateTime GetImportFileDateTime(FileInfo fi, string fileStart)
		{
			string timeStampString = fi.Name.Replace(fileStart, "").Replace(".csv", "");
			DateTime fileImportDateTime = DateTime.ParseExact(timeStampString, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
			return fileImportDateTime;
		}

		private bool ProcessFile(FileInfo fi, bool secondPass)
        {
			logger.LogTrace("Processing file", fi.FullName);


			currentProcessFailed = false;

            try
            {
                using (StreamReader sr = new StreamReader(fi.FullName, Encoding.GetEncoding("iso-8859-1")))
                {
                    if (fi.Name.StartsWith(WorkerFileNameStart))
                    {
                        ImportWorkersToTro(sr);
                    }
                    else if (fi.Name.StartsWith(ContractorFileNameStart))
                    {
                        ImportContractorsToTro(sr);
                    }
                    else if (fi.Name.StartsWith(ProjectFileNameStart))
                    {
                        DateTime fileDate = GetImportFileDateTime(fi, ProjectFileNameStart);
                        ImportProjectsToTro(sr, fileDate);
                    }
                    else if (fi.Name.StartsWith(ArticleFileNameStart))
                    {
                        ImportArticlesToTro(sr);
                    }
                    else if (fi.Name.StartsWith(ProjectCategoryNameStart))
                    {
                        ImportProjectCategoriesToTro(sr);
                    }
                    else if (fi.Name.StartsWith(MachineryNameStart))
                    {
                        ImportMachineryToTro(sr);
                    }
                    else
                    {
                        throw new InvalidDataException("Could not map file to any existing AX import handler.");
                    }
                }

                if (currentProcessFailed)
                {
                    throw new InvalidDataException("Errors encountered when processing file");
                }
                else
                {
                    if (secondPass)
                    {
                        logger.LogTrace("Successfully processed file second time. Moving to done folder.", fi.Name);
                        MoveFileToDoneLocation(fi);
                        FileImportsSucceeded++;
                    }
                    else
                    {
                        logger.LogTrace("Successfully processed file for the first time. Not moving before second pass.", fi.Name);
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError("Processing file failed.", fi.FullName, e);
                MoveFileToFailedLocation(fi);
                FileImportsFailed++;
				return false;
            }

			return true;
        }

        private void ImportWorkersToTro(StreamReader sr)
        {
            string[] csvLine = ReadCsvLine(sr);

            while (csvLine != null)
            {
                try
                {
                    string identifier = csvLine[(int)Worker.PersonnelNumber];

                    logger.LogTrace("Importing worker to tro", csvLine[(int)Worker.FirstName] + " " + csvLine[(int)Worker.LastName], identifier);

                    lock (database)
                    {
                        MongoCollection<BsonDocument> userCollection = database.GetCollection("user");

                        // Find out if worker exists
                        MongoCursor cursor = userCollection.Find(Query.EQ("identifier", identifier));

                        BsonDocument workerDocument = null;

                        bool isNew = false;
                        if (cursor.Count() == 0)
                        {
                            // New item
                            isNew = true;
                            workerDocument = new BsonDocument();
                            workerDocument.Set(DBQuery.Id, ObjectId.GenerateNewId());
                        }
                        else if (cursor.Count() == 1)
                        {
                            foreach (BsonDocument doc in cursor)
                                workerDocument = doc;

                            invalidatedCacheItems.Add(new DataTree(workerDocument[DBQuery.Id].ToString()));
                        }
                        else
                        {
                            logger.LogError("More than one existing worker found: ", identifier, cursor.Count());
                            currentProcessFailed = true;
                        }

                        string profitCenter = csvLine[(int)Worker.DefaultDimension];

                        if (string.IsNullOrEmpty(profitCenter))
                            throw new HandlerException("Worker is missing profit center: " + identifier);

                        SetWorkerData(csvLine, workerDocument);
                        SetSupervisor(workerDocument, csvLine[(int)Worker.Supervisor], userCollection);
                        SetWorkerClaContract(csvLine, workerDocument);

                        SetProfitCenter(profitCenter, csvLine[(int)Worker.DefaultDimensionDescription], workerDocument);

                        userCollection.Save(workerDocument, WriteConcern.Acknowledged);

                        mongoDBHandler.RefreshDocumentCachedInfo((ObjectId)workerDocument[DBQuery.Id], userCollection.Name);

                        if (isNew)
                            WorkersImportedNew++;
                        else
                            WorkersImportedUpdate++;
                    }
                }
                catch (Exception e)
                {
                    logger.LogError("Failed to add worker", e);
                    currentProcessFailed = true;
                    WorkersImportedFail++;
                }

                csvLine = ReadCsvLine(sr);
            }
        }

        private void SetWorkerData(string[] values, BsonDocument workerRecord)
        {
            logger.LogDebug("Setting worker data.");

            for (int i = 0; i < (int)Worker.SilmuGroup; i++)
                logger.LogTrace((Worker)i, values[i]);

            workerRecord.Set("email", values[(int)Worker.Email]);
            workerRecord.Set("language", GetLanguageFromAXLanguage(values[(int)Worker.LanguageCode]));
            workerRecord.Set("identifier", values[(int)Worker.PersonnelNumber]);
            workerRecord.Set("firstname", values[(int)Worker.FirstName]);
            workerRecord.Set("middlename", values[(int)Worker.MiddleName]);
            workerRecord.Set("lastname", values[(int)Worker.LastName]);
            workerRecord.Set("organizationcode", values[(int)Worker.OrganizationCode]);
            workerRecord.Set("pin", values[(int)Worker.Pin]);
            workerRecord.Set("languagecode", GetLanguageFromAXLanguage(values[(int)Worker.LanguageCode]));
            workerRecord.Set("countrycode", values[(int)Worker.EmployeeCountryCode]);
            workerRecord.Set("employmentstart", GetDateFromAXString(values[(int)Worker.EmploymentStartDate]));
            workerRecord.Set("employmentend", GetDateFromAXString(values[(int)Worker.EmploymentEndDate]));
            workerRecord.Set("probationend", GetDateFromAXString(values[(int)Worker.ProbationPeriodEndDate]));
            workerRecord.Set("office", values[(int)Worker.Office]);
            workerRecord.Set("title", values[(int)Worker.Title]);
            workerRecord.Set("taxregistrationid", values[(int)Worker.TaxRegistrationId]);
            workerRecord.Set("sharepointlinktophotograph", values[(int)Worker.SharepointLinkToPhotograph]);
            workerRecord.Set("weeklyworkinghours", GetDoubleFromAXString(values[(int)Worker.WeeklyWorkingHours]));
            workerRecord.Set("personnelgroup", values[(int)Worker.PersonnelGroup]);
            workerRecord.Set("internalworker", true);
            workerRecord.Set("exported_silmu", false);

			string silmuGroup = values[(int)Worker.SilmuGroup];

			logger.LogDebug("Updating worker user.");

			if (silmuGroup == SilmuGroup_2)
				workerRecord.Set("level", 2);
			else if (silmuGroup == SilmuGroup_3)
				workerRecord.Set("level", 3);
			else if (silmuGroup == SilmuGroup_4)
				workerRecord.Set("level", 4);
			else
				workerRecord.Set("level", 1);
		}

		private void SetSupervisor(BsonDocument record, string supervisorIdentifier, MongoCollection<BsonDocument> userCollection)
        {
            if (string.IsNullOrEmpty(supervisorIdentifier))
            {
                logger.LogDebug("No supervisor for worker/asset.", record["identifier"]);
                return;
            }

            BsonDocument supervisorRecord = userCollection.FindOne(Query.EQ("identifier", supervisorIdentifier));

            if (supervisorRecord == null)
            {
                logger.LogDebug("No supervisor for worker/asset.", record["identifier"]);
                return;
            }

            logger.LogDebug("Setting supervisor for worker/asset.", "Worker/asset: " + record["identifier"], "Supervisor: " + supervisorRecord["identifier"], supervisorRecord[DBQuery.Id]);

            var supervisorIdsArray = new BsonArray();
            supervisorIdsArray.Add(supervisorRecord[DBQuery.Id]);

            record.Set("supervisor", supervisorIdsArray);
        }

        private void SetWorkerClaContract(string[] values, BsonDocument workerDocument)
        {
            logger.LogDebug("Setting worker CLA contract.");

            MongoCollection<BsonDocument> claContractsCollection = database.GetCollection("clacontract");

            string claContractId = values[(int)Worker.CLAContract];

            if (string.IsNullOrEmpty(claContractId))
            {
                logger.LogWarning("Internal worker is missing a CLA contract. Substituting with default CLA contract.", workerDocument["identifier"], DefaultClaContract);
                claContractId = DefaultClaContract;
            }

            BsonDocument claContractDocument = claContractsCollection.FindOne(Query.EQ("identifier", claContractId));

            if (claContractDocument == null)
            {
                logger.LogDebug("Creating CLA contract.");
                claContractDocument = new BsonDocument();
                claContractDocument.Set(DBQuery.Id, ObjectId.GenerateNewId());
                claContractDocument.Set("identifier", claContractId);
                claContractDocument.Set("name", claContractId);
                claContractsCollection.Save(claContractDocument, WriteConcern.Acknowledged);
            }

            var claContractIdsArray = new BsonArray();
            claContractIdsArray.Add(claContractDocument[DBQuery.Id]);

            workerDocument.Set("clacontract", claContractIdsArray);
        }

        private void ImportContractorsToTro(StreamReader sr)
        {
            string[] csvLine = ReadCsvLine(sr);

            while (csvLine != null)
            {
                try
                {
                    string identifier = csvLine[(int)Contractor.PersonnelNumber];

                    logger.LogTrace("Importing contractor to tro", csvLine[(int)Contractor.FirstName] + " " + Contractor.LastName, identifier);

                    lock (database)
                    {
                        MongoCollection<BsonDocument> userCollection = database.GetCollection("user");

                        // Find out if worker exists
                        MongoCursor cursor = userCollection.Find(new QueryDocument("identifier", identifier));

                        BsonDocument contractorDocument = null;

                        bool isNew = false;

                        if (cursor.Count() == 0)
                        {
                            // New item
                            isNew = true;

                            contractorDocument = new BsonDocument();
                            contractorDocument.Set(DBQuery.Id, ObjectId.GenerateNewId());

                            SetContractorData(csvLine, contractorDocument);
                        }
                        else if (cursor.Count() == 1)
                        {
                            foreach (BsonDocument doc in cursor)
                                contractorDocument = doc;

                            SetContractorData(csvLine, contractorDocument);
                            SetSupervisor(contractorDocument, csvLine[(int)Contractor.Supervisor], userCollection);
                            SetContractorClaContract(csvLine, contractorDocument);

                            invalidatedCacheItems.Add(new DataTree(contractorDocument[DBQuery.Id].ToString()));
                        }
                        else
                        {
                            logger.LogError("More than one existing contractor found: ", identifier, cursor.Count());
                            currentProcessFailed = true;
                        }

                        string profitCenter = csvLine[(int)Contractor.ProfitCenter];

                        if (string.IsNullOrEmpty(profitCenter))
                            logger.LogInfo("Contractor is missing profit center: " + identifier);
                        else
                            SetProfitCenter(profitCenter, csvLine[(int)Contractor.ProfitCenterDescription], contractorDocument);

                        userCollection.Save(contractorDocument, WriteConcern.Acknowledged);

                        mongoDBHandler.RefreshDocumentCachedInfo((ObjectId)contractorDocument[DBQuery.Id], userCollection.Name);

                        if (isNew)
                            ContractorsImportedNew++;
                        else
                            ContractorsImportedUpdate++;
                    }
                }
                catch (Exception e)
                {
                    logger.LogError("Failed to add contractor", e);
                    currentProcessFailed = true;
                    ContractorsImportedFail++;
                }

                csvLine = ReadCsvLine(sr);
            }
        }

        private void SetContractorData(string[] values, BsonDocument contractorDocument)
        {
            logger.LogDebug("Setting contractor data.");

            for (int i = 0; i < (int)Contractor.WeeklyWorkingHours; i++)
                logger.LogTrace((Contractor)i, values[i]);

            contractorDocument.Set("email", values[(int)Contractor.Email]);
            contractorDocument.Set("language", GetLanguageFromAXLanguage(values[(int)Contractor.LanguageCode]));
            contractorDocument.Set("identifier", values[(int)Contractor.PersonnelNumber]);
            contractorDocument.Set("firstname", values[(int)Contractor.FirstName]);
            contractorDocument.Set("middlename", values[(int)Contractor.MiddleName]);
            contractorDocument.Set("lastname", values[(int)Contractor.LastName]);
            contractorDocument.Set("organizationcode", values[(int)Contractor.OrganizationCode]);
            contractorDocument.Set("pin", values[(int)Contractor.Pin]);
            contractorDocument.Set("languagecode", GetLanguageFromAXLanguage(values[(int)Contractor.LanguageCode]));
            contractorDocument.Set("countrycode", values[(int)Contractor.EmployeeCountryCode]);
            contractorDocument.Set("office", values[(int)Contractor.Office]);
            contractorDocument.Set("taxregistrationid", values[(int)Contractor.TaxRegistrationId]);
            contractorDocument.Set("weeklyworkinghours", GetDoubleFromAXString(values[(int)Contractor.WeeklyWorkingHours]));
            contractorDocument.Set("internalworker", false);
            contractorDocument.Set("exported_silmu", false);
			// Todo: get level from data.
			contractorDocument.Set("level", 1);
			contractorDocument.Set("email", values[(int)Contractor.Email]);
			contractorDocument.Set("language", values[(int)Contractor.LanguageCode]);
		}

		private void SetContractorClaContract(string[] values, BsonDocument contractorDocument)
        {
            logger.LogDebug("Setting contractor CLA contract.");

            MongoCollection<BsonDocument> claContractsCollection = database.GetCollection("clacontract");

            BsonDocument claContractDocument = claContractsCollection.FindOne(Query.EQ("identifier", ContractorClaContract));

            if (claContractDocument == null)
            {
                logger.LogDebug("Creating CLA contract.");
                claContractDocument = new BsonDocument();
                claContractDocument.Set(DBQuery.Id, ObjectId.GenerateNewId());
                claContractDocument.Set("identifier", ContractorClaContract);
                claContractDocument.Set("name", ContractorClaContract);
                claContractsCollection.Save(claContractDocument, WriteConcern.Acknowledged);
            }

            var claContractIdsArray = new BsonArray();
            claContractIdsArray.Add(claContractDocument[DBQuery.Id]);

            contractorDocument.Set("clacontract", claContractIdsArray);
        }

        private void ImportProjectsToTro(StreamReader sr, DateTime fileDate)
        {
            string[] csvLine = ReadCsvLine(sr);

            while (csvLine != null)
            {
                try
				{
					ImportProject(csvLine, fileDate);
				}
				catch (Exception e)
                {
                    logger.LogError("Failed to add project", e);
                    currentProcessFailed = true;
                    ProjectsImportedFail++;
                }

                csvLine = ReadCsvLine(sr);
            }
        }

		internal void ImportProject(string[] csvLine, DateTime importTimestamp)
		{
			lock (lockObject)
			{
				string projectId = csvLine[(int)Project.ProjectId];

				logger.LogTrace("Importing project to tro", csvLine[(int)Project.ProjectName], projectId);

				if (projectId.IndexOf(SubProjectSeparator) != projectId.LastIndexOf(SubProjectSeparator))
				{
					logger.LogInfo("Project is a subproject's subproject and is not imported to tro.", projectId);
					return;
				}

				MongoCollection<BsonDocument> projectsCollection = database.GetCollection("project");

				// Find out if project exists
				MongoCursor cursor = projectsCollection.Find(new QueryDocument("identifier", projectId));

				BsonDocument projectDocument = null;

				bool isNew = false;

				if (cursor.Count() == 0)
				{
					// New item
					isNew = true;
					projectDocument = new BsonDocument();
					projectDocument.Set(DBQuery.Id, ObjectId.GenerateNewId());
				}
				else if (cursor.Count() == 1)
				{
					foreach (BsonDocument doc in cursor)
						projectDocument = doc;

					if (DateTime.Compare((DateTime)projectDocument.GetValue("created", DateTime.MaxValue), importTimestamp) < 0)
					{
						logger.LogInfo("Project update has earlier timestamp than existing project update. Ignoring update", "Created: "+ (DateTime)projectDocument.GetValue("created", DateTime.MinValue), "Imported: " + importTimestamp);
						return;
					}

					invalidatedCacheItems.Add(new DataTree(projectDocument[DBQuery.Id].ToString()));
				}
				else
				{
					logger.LogError("More than one existing project found: ", projectId, cursor.Count());
					currentProcessFailed = true;
					return;
				}

				SetProjectData(csvLine, projectDocument);
				SetProjectManager(csvLine, projectDocument);
				SetProjectCustomer(csvLine, projectDocument);
				SetProfitCenter(csvLine[(int)Project.ProjectProfitCenter], null, projectDocument);
				SetParentProject(csvLine, projectDocument);

                logger.LogInfo("Project imported to TRO", projectId, projectDocument["name"]);

                projectsCollection.Save(projectDocument, WriteConcern.Acknowledged);

				mongoDBHandler.RefreshDocumentCachedInfo((ObjectId)projectDocument[DBQuery.Id], projectsCollection.Name);

				if (isNew)
					ProjectsImportedNew++;
				else
					ProjectsImportedUpdate++;
			}
		}

		private void SetProjectData(string[] values, BsonDocument projectRecord)
        {
            logger.LogDebug("Setting project data.");

            for (int i = 0; i < (int)Project.ProjectNoResourcing; i++)
                logger.LogTrace((Project)i, values[i]);

            // Project start and end are porvided as dates but we need to have time too. Use 7:30 to 15:30 as sepcified by Delete.
            DateTime projectStart = GetDateFromAXString2(values[(int)Project.ProjectStartDate]);
            DateTime projectEnd = GetDateFromAXString2(values[(int)Project.ProjectEndDate]);
            if (projectStart.CompareTo(DateTime.MinValue) != 0)
                projectStart = new DateTime(projectStart.Year, projectStart.Month, projectStart.Day, 7, 30, 0, DateTimeKind.Local);

            if (projectEnd.CompareTo(DateTime.MinValue) != 0)
                projectEnd = new DateTime(projectEnd.Year, projectEnd.Month, projectEnd.Day, 15, 30, 0, DateTimeKind.Local);

            projectRecord.Set("identifier", values[(int)Project.ProjectId]);
            projectRecord.Set("streetaddress", values[(int)Project.ProjectDeliveryAddressStreet]);
            projectRecord.Set("postalcode", values[(int)Project.ProjectDeliveryAddressZip]);
            projectRecord.Set("city", values[(int)Project.ProjectDeliveryAddressCity]);
            projectRecord.Set("country", values[(int)Project.ProjectDeliveryAddressCountry]);
            projectRecord.Set("name", values[(int)Project.ProjectName]);
            projectRecord.Set("status", GetTroProjectStatusFromAxStatus(values[(int)Project.ProjectStatus]));
            projectRecord.Set("note", values[(int)Project.ProjectDescription]);
            projectRecord.Set("projectstart", (projectStart.CompareTo(DateTime.MinValue) == 0) ? BsonNull.Value : (BsonValue)projectStart);
            projectRecord.Set("projectend", (projectEnd.CompareTo(DateTime.MinValue) == 0) ? BsonNull.Value : (BsonValue)projectEnd);
            projectRecord.Set("contactpersonname", values[(int)Project.ProjectContactPersonName]);
            projectRecord.Set("phonenumber", values[(int)Project.ProjectContactPersonTelephone]);
            projectRecord.Set("email", values[(int)Project.ProjectContactPersonEmail]);
            projectRecord.Set("noresourcing", values[(int)Project.ProjectNoResourcing] == ProjectNoResourcing_NoResourcing);
            projectRecord.Set("created", DateTime.UtcNow);
        }

        private BsonValue GetTroProjectStatusFromAxStatus(string axProjectStatus)
        {
            string troProjectStatus = IntegrationConstants.ProjectStatus_Done;

            if (axProjectStatus == ProjectStatus_Created)
                troProjectStatus = IntegrationConstants.ProjectStatus_InProgress;
            if (axProjectStatus == ProjectStatus_InPlanning)
                troProjectStatus = IntegrationConstants.ProjectStatus_Unallocated;

            return troProjectStatus;
        }

        private void SetParentProject(string[] values, BsonDocument projectDocument)
        {
            string identifier = values[(int)Project.ProjectId];

            if (identifier.IndexOf(SubProjectSeparator) == -1)
            {
                logger.LogTrace("Project is not a subproject.");
                return;
            }

            string parentProjectIdentifier = identifier.Substring(0, identifier.LastIndexOf(SubProjectSeparator));

            logger.LogTrace("Setting parent project", parentProjectIdentifier);

            MongoCollection<BsonDocument> projectsCollection = database.GetCollection("project");

            // Find out if customer exists
            BsonDocument parentProjectDocument = projectsCollection.FindOne(Query.EQ("identifier", parentProjectIdentifier));

            if (parentProjectDocument == null)
            {
                logger.LogError("Parent project not found for project.");
                return;
            }

            var parentProjectIds = new BsonArray();
            parentProjectIds.Add(parentProjectDocument[DBQuery.Id]);

            projectDocument.Set("parentproject", parentProjectIds);
        }

        private void SetProjectManager(string[] values, BsonDocument projectDocument)
        {
            logger.LogTrace("Setting manager for project", values[(int)Project.ProjectName], values[(int)Project.CustomerName]);
            MongoCollection<BsonDocument> usersCollection = database.GetCollection("user");

            if (string.IsNullOrEmpty(values[(int)Project.ProjectManager]))
            {
                logger.LogTrace("No project manager specified");
                return;
            }

            // Find out if project manager exists
            BsonDocument projectManagerDocument = usersCollection.FindOne(Query.EQ("identifier", values[(int)Project.ProjectManager]));

            if (projectManagerDocument == null)
            {
                logger.LogError("Project manager not found. Cannot set project manager for project.", "Project:" + values[(int)Project.ProjectName], "Project manager: " + values[(int)Project.ProjectManager]);
                return;
            }

            var porjectManagerIds = new BsonArray();
            porjectManagerIds.Add(projectManagerDocument[DBQuery.Id]);

            projectDocument.Set("projectmanager", porjectManagerIds);
        }

        private void SetProjectCustomer(string[] values, BsonDocument projectDocument)
        {
            logger.LogTrace("Setting customer for project", values[(int)Project.ProjectName], values[(int)Project.CustomerName]);
            MongoCollection<BsonDocument> customersCollection = database.GetCollection("customer");

            // Find out if customer exists
            BsonDocument customerDocument = customersCollection.FindOne(Query.EQ("identifier", values[(int)Project.CustomerId]));

            bool save = false;

            if (customerDocument == null)
            {
                logger.LogTrace("Customer doesn't exist. Creating it.");
                customerDocument = new BsonDocument();
                customerDocument.Set(DBQuery.Id, ObjectId.GenerateNewId());
                save = true;
            }
            else
            {
                invalidatedCacheItems.Add(new DataTree(customerDocument[DBQuery.Id].ToString()));
            }

            if ((string)customerDocument["identifier"] != values[(int)Project.CustomerId] ||
                (string)customerDocument["name"] != values[(int)Project.CustomerName])
            {
                logger.LogDebug("Customer name or identifier changed", "Old name: " + customerDocument["name"] + ", Old identifier: " + customerDocument["identifier"], "new name: " + values[(int)Project.CustomerName] + ", new identifier: " + values[(int)Project.CustomerId]);
                customerDocument.Set("identifier", values[(int)Project.CustomerId]);
                customerDocument.Set("name", values[(int)Project.CustomerName]);
                save = true;
            }


            if(save)
            {
                customersCollection.Save(customerDocument, WriteConcern.Acknowledged);
                mongoDBHandler.RefreshDocumentCachedInfo((ObjectId)customerDocument[DBQuery.Id], customersCollection.Name);
            }
            else
            {
                logger.LogDebug("Customer info not changed. Not saving.");
            }

            var customerIds = new BsonArray();
            customerIds.Add(customerDocument[DBQuery.Id]);

            projectDocument.Set("customer", customerIds);
        }

        private void SetMachineProfitCenter(string identifier, BsonDocument bsonDocument)
        {
            logger.LogTrace("Setting machine profit center.", identifier);

            MongoCollection<BsonDocument> profitCentersCollection = database.GetCollection("profitcenter");

            // Find out if profit center exists
            BsonDocument profitCenterDocument = profitCentersCollection.FindOne(Query.EQ("identifier", identifier));

            if (profitCenterDocument == null)
            {
                logger.LogTrace("Profit center doesn't exist. Creating it.");
                profitCenterDocument = new BsonDocument();
                profitCenterDocument.Set(DBQuery.Id, ObjectId.GenerateNewId());
            }
            else
            {
                invalidatedCacheItems.Add(new DataTree(profitCenterDocument[DBQuery.Id].ToString()));
            }

            profitCenterDocument.Set("identifier", identifier);

            // Machinery interface provides no name for profit center. If we get a profit center with no previous name
            // we don't with to change it. In case there is no name we use id as a placeholder.
            if (!profitCenterDocument.Contains("name"))
                profitCenterDocument.Set("name", identifier);

            profitCentersCollection.Save(profitCenterDocument, WriteConcern.Acknowledged);

            var profitCenterIds = new BsonArray();
            profitCenterIds.Add(profitCenterDocument[DBQuery.Id]);

            bsonDocument.Set("profitcenter", profitCenterIds);
        }

        private void SetProfitCenter(string identifier, string name, BsonDocument bsonDocument)
        {
            logger.LogTrace("Setting profit center.", identifier, name);

            MongoCollection<BsonDocument> profitCentersCollection = database.GetCollection("profitcenter");

            // Find out if profit center exists
            BsonDocument profitCenterDocument = profitCentersCollection.FindOne(Query.EQ("identifier", identifier));

            bool save = false;

            if (profitCenterDocument == null)
            {
                logger.LogDebug("Profit center doesn't exist. Creating it.");
                profitCenterDocument = new BsonDocument();
                profitCenterDocument.Set(DBQuery.Id, ObjectId.GenerateNewId());
                profitCenterDocument.Set("identifier", identifier);
                save = true;
            }
            else
            {
                invalidatedCacheItems.Add(new DataTree(profitCenterDocument[DBQuery.Id].ToString()));
            }


            if (name != null && (string)profitCenterDocument["name"] != name)
            {
                logger.LogDebug("Profit center has a new name", "Old: " + profitCenterDocument["name"], "New: " + name);
                profitCenterDocument.Set("name", name);
                save = true;

            }

            if (save)
            {
                logger.LogInfo("Saving profit center.", name, identifier);
                profitCentersCollection.Save(profitCenterDocument, WriteConcern.Acknowledged);
                mongoDBHandler.RefreshDocumentCachedInfo((ObjectId)profitCenterDocument[DBQuery.Id], profitCentersCollection.Name);
            }
            else
            {
                logger.LogDebug("Profit center remains the same. Not saving");
            }

            var profitCenterIds = new BsonArray();
            profitCenterIds.Add(profitCenterDocument[DBQuery.Id]);

            bsonDocument.Set("profitcenter", profitCenterIds);
        }

        private void ImportArticlesToTro(StreamReader sr)
        {
            string[] csvLine = ReadCsvLine(sr);

            while (csvLine != null)
            {
                try
                {
                    string itemId = csvLine[(int)Item.ItemNumber];

                    logger.LogTrace("Importing items to tro", csvLine[(int)Project.ProjectName], itemId);

                    lock (database)
                    {
                        MongoCollection<BsonDocument> articlesCollection = database.GetCollection("article");

                        // Find out if project exists
                        MongoCursor cursor = articlesCollection.Find(new QueryDocument("identifier", itemId));

                        BsonDocument articleDocument = null;

                        bool isNew = false;

                        if (cursor.Count() == 0)
                        {
                            // New item
                            isNew = true;

                            articleDocument = new BsonDocument();
                            articleDocument.Set(DBQuery.Id, ObjectId.GenerateNewId());

                            SetArticleData(csvLine, articleDocument);
                            SetArticleGroup(csvLine, articleDocument);
                        }
                        else if (cursor.Count() == 1)
                        {
                            foreach (BsonDocument doc in cursor)
                                articleDocument = doc;

                            SetArticleData(csvLine, articleDocument);
                            SetArticleGroup(csvLine, articleDocument);
                            invalidatedCacheItems.Add(new DataTree(articleDocument[DBQuery.Id].ToString()));
                        }
                        else
                        {
                            logger.LogError("More than one existing article found: ", itemId, cursor.Count());
                            currentProcessFailed = true;
                        }

                        articlesCollection.Save(articleDocument, WriteConcern.Acknowledged);

                        mongoDBHandler.RefreshDocumentCachedInfo((ObjectId)articleDocument[DBQuery.Id], articlesCollection.Name);

                        if (isNew)
                            ArticlesImportedNew++;
                        else
                            ArticlesImportedUpdate++;
                    }
                }
                catch (Exception e)
                {
                    logger.LogError("Failed to add article", e);
                    currentProcessFailed = true;
                    ArticlesImportedFail++;
                }

                csvLine = ReadCsvLine(sr);

            }
        }

        private void SetArticleData(string[] values, BsonDocument articleRecord)
        {
            logger.LogDebug("Setting article data.");

            for (int i = 0; i < (int)Item.ItemUnitOfMeasure; i++)
                logger.LogTrace((Project)i, values[i]);

            articleRecord.Set("identifier", values[(int)Item.ItemNumber]);
            articleRecord.Set("name", values[(int)Item.ItemDescription]);
            articleRecord.Set("unit", values[(int)Item.ItemUnitOfMeasure]);

        }

        private void SetArticleGroup(string[] values, BsonDocument itemRecord)
        {
            logger.LogDebug("Setting article group.");


            string groupId = values[(int)Item.ItemGroup];
            if (string.IsNullOrEmpty(groupId))
                return;

            MongoCollection<BsonDocument> articleGroupsCollection = database.GetCollection("articlegroup");

            BsonDocument articleGroup = articleGroupsCollection.FindOne(Query.EQ("identifier", groupId));

            ObjectId id;
            if (articleGroup == null)
            {
                logger.LogDebug("Article group not foud. Creating new group", groupId, values[(int)Item.ItemGroupDescription]);

                articleGroup = new BsonDocument();
                articleGroup["identifier"] = groupId;

                id = ObjectId.GenerateNewId();
                articleGroup.Add(DBQuery.Id, id);
            }
            else
            {
                id = (ObjectId)articleGroup[DBQuery.Id];
                invalidatedCacheItems.Add(new DataTree(articleGroup[DBQuery.Id].ToString()));
            }

            articleGroup["name"] = values[(int)Item.ItemGroupDescription];

            articleGroupsCollection.Save(articleGroup, WriteConcern.Acknowledged);

            mongoDBHandler.RefreshDocumentCachedInfo((ObjectId)articleGroup[DBQuery.Id], articleGroupsCollection.Name);

            var articleGroupIdsArray = new BsonArray();
            articleGroupIdsArray.Add(id);

            itemRecord.Set("articlegroup", articleGroupIdsArray);
        }

		private void ImportProjectCategoriesToTro(StreamReader sr)
        {
            string[] csvLine = ReadCsvLine(sr);

            while (csvLine != null)
            {
                try
                {
                    string itemId = csvLine[(int)ProjectCategory.CategoryId];

                    logger.LogTrace("Importing project category to TRO", csvLine[(int)ProjectCategory.CategoryDescription], itemId);

                    lock (database)
                    {
                        MongoCollection<BsonDocument> projectCategoriesCollection = database.GetCollection("projectcategory");
                        var projectCategoriesDocument = new BsonDocument();

                        MongoCursor<BsonDocument> cursor = projectCategoriesCollection.Find(Query.EQ("identifier", itemId));

                        bool isNew = false;
                        if (cursor.Count() == 0)
                        {
                            // New item
                            isNew = true;

                            projectCategoriesDocument = new BsonDocument();
                            projectCategoriesDocument.Set(DBQuery.Id, ObjectId.GenerateNewId());

                            SetProjectCategoryData(csvLine, projectCategoriesDocument);
                        }
                        else if (cursor.Count() == 1)
                        {
                            foreach (BsonDocument doc in cursor)
                                projectCategoriesDocument = doc;

                            SetProjectCategoryData(csvLine, projectCategoriesDocument);
                            invalidatedCacheItems.Add(new DataTree(projectCategoriesDocument[DBQuery.Id].ToString()));
                        }
                        else
                        {
                            throw new Exception("More than one existing project category found: " + itemId);
                        }

                        projectCategoriesCollection.Save(projectCategoriesDocument, WriteConcern.Acknowledged);

                        mongoDBHandler.RefreshDocumentCachedInfo((ObjectId)projectCategoriesDocument[DBQuery.Id], projectCategoriesCollection.Name);

                        if (isNew)
                            ProjectCategoriesImportedNew++;
                        else
                            ProjectCategoriesImportedUpdate++;
                    }
                }
                catch (Exception e)
                {
                    logger.LogError("Failed to add project category", e);
                    currentProcessFailed = true;
                    ProjectsImportedFail++;
                }

                csvLine = ReadCsvLine(sr);
            }
        }

        private void SetProjectCategoryData(string[] values, BsonDocument projectCategoryRecord)
        {
            logger.LogDebug("Setting project category data.");

            for (int i = 0; i < (int)ProjectCategory.CategoryGroup; i++)
                logger.LogTrace((Project)i, values[i]);

            projectCategoryRecord.Set("identifier", values[(int)ProjectCategory.CategoryId]);
            projectCategoryRecord.Set("name", values[(int)ProjectCategory.CategoryDescription]);
            projectCategoryRecord.Set("type", values[(int)ProjectCategory.CategoryType]);
            projectCategoryRecord.Set("projectcategorygroup", values[(int)ProjectCategory.CategoryGroup]);
        }

        private void ImportMachineryToTro(StreamReader sr)
        {
            string[] csvLine = ReadCsvLine(sr);

            while (csvLine != null)
            {
                // Generic machines are used for forecasts and are delivered using the same interfaces.
                bool isGenericMachine = false;

                if (csvLine[(int)Machinery.MachineryRowType] != "M")
                {
                    logger.LogDebug("Machine with rowtype other than M found. Skipping it.", csvLine[(int)Machinery.MachineryRowType]);
                    csvLine = ReadCsvLine(sr);
                    continue;
                }

                if (csvLine[(int)Machinery.SilmuGroup] == "ResourcingMachine")
                {
                    logger.LogDebug("Machine with silmu group found. Adding as a generic machine.", csvLine[(int)Machinery.SilmuGroup]);
                    isGenericMachine = true;
                }

                try
                {
                    string itemId = csvLine[(int)Machinery.MachineId];

                    logger.LogTrace("Importing machinery to TRO", csvLine[(int)Machinery.Description], itemId);

                    lock (database)
                    {
                        MongoCollection<BsonDocument> assetsCollection;

                        if (isGenericMachine)
                            assetsCollection = database.GetCollection("genericasset");
                        else
                            assetsCollection = database.GetCollection("asset");

                        var assetDocument = new BsonDocument();

                        MongoCursor<BsonDocument> cursor = assetsCollection.Find(Query.EQ("identifier", itemId));

                        bool isNew = false;
                        if (cursor.Count() == 0)
                        {
                            // New item
                            isNew = true;

                            assetDocument = new BsonDocument();
                            assetDocument.Set(DBQuery.Id, ObjectId.GenerateNewId());

                            SetMachineData(csvLine, assetDocument);
                        }
                        else if (cursor.Count() == 1)
                        {
                            foreach (BsonDocument doc in cursor)
                                assetDocument = doc;

                            SetMachineData(csvLine, assetDocument);
                            SetSupervisor(assetDocument, csvLine[(int)Machinery.Supervisor], database.GetCollection("user"));
                            SetMachineProjectCategory(csvLine[(int)Machinery.ProjectCategory], assetDocument);
                            SetMachineProfitCenter(csvLine[(int)Machinery.ProfitCenter], assetDocument);
                            invalidatedCacheItems.Add(new DataTree(assetDocument[DBQuery.Id].ToString()));
                        }
                        else
                        {
                            logger.LogError("More than one existing machine found: ", cursor.Count());
                            currentProcessFailed = true;
                        }

                        assetsCollection.Save(assetDocument, WriteConcern.Acknowledged);

                        mongoDBHandler.RefreshDocumentCachedInfo((ObjectId)assetDocument[DBQuery.Id], assetsCollection.Name);

                        if (isNew)
                            MachineryImportedNew++;
                        else
                            MachineryImportedUpdate++;

                    }
                }
                catch (Exception e)
                {
                    logger.LogError("Failed to add project category", e);
                    currentProcessFailed = true;
                    MachineryImportedFail++;
                }

                csvLine = ReadCsvLine(sr);
            }
        }

        private void SetMachineData(string[] values, BsonDocument assetRecord)
        {
            logger.LogDebug("Setting machine category data.");

            for (int i = 0; i < (int)Machinery.ProfitCenter; i++)
                logger.LogTrace((Machinery)i, values[i]);

            assetRecord.Set("identifier", values[(int)Machinery.MachineId]);
            assetRecord.Set("licenseplate", values[(int)Machinery.LicensePlate]);
            assetRecord.Set("name", values[(int)Machinery.Description]);
            assetRecord.Set("office", values[(int)Machinery.Office]);
            assetRecord.Set("fixedassetnumber", values[(int)Machinery.FixedAssetNumber]);
        }

        private void SetMachineProjectCategory(string projectCategoryIdentifier, BsonDocument assetRecord)
        {
            if (string.IsNullOrEmpty(projectCategoryIdentifier))
            {
                logger.LogDebug("No project category specified for machine.");
                return;
            }

            MongoCollection<BsonDocument> projectCategoryCollection = database.GetCollection("projectcategory");

            BsonDocument projectCategoryDocument = projectCategoryCollection.FindOne(Query.EQ("identifier", projectCategoryIdentifier));

            if (projectCategoryDocument == null)
            {
                logger.LogDebug("No project category found for forecast machine.");
                return;
            }

            var projectCategoryIdsArray = new BsonArray();
            projectCategoryIdsArray.Add(projectCategoryDocument[DBQuery.Id]);

            assetRecord.Set("projectcategory", projectCategoryIdsArray);
        }

        private void MoveFileToFailedLocation(FileInfo fi)
        {

            var fiTarget = new FileInfo(
                filePath + FailedFileLocation + Path.DirectorySeparatorChar + fi.Name);

            logger.LogInfo("Moving file to failed location", fi.FullName, fiTarget.FullName);

            if (!new DirectoryInfo(fiTarget.DirectoryName).Exists)
                new DirectoryInfo(fiTarget.DirectoryName).Create();

            if (fiTarget.Exists)
            {
                logger.LogTrace("Target file exists. Removing it.", fiTarget.Name);
                fiTarget.Delete();
            }

            fi.MoveTo(fiTarget.FullName);
        }

        private void MoveFileToDoneLocation(FileInfo fi)
        {
            var fiTarget = new FileInfo(
                filePath + DoneFileLocation + Path.DirectorySeparatorChar + fi.Name);

            logger.LogTrace("Moving file to done location", fi.FullName, fiTarget.FullName);

            if (fiTarget.Exists)
            {
                logger.LogTrace("Target file exists. Removing it.");
                fiTarget.Delete();
            }

            if (!new DirectoryInfo(fiTarget.DirectoryName).Exists)
                new DirectoryInfo(fiTarget.DirectoryName).Create();

            fi.MoveTo(fiTarget.FullName);
        }

        private string[] ReadCsvLine(StreamReader sr)
        {
            while (true)
            {
                string line = sr.ReadLine();

                if (line == string.Empty)
                    continue;

                if (line == null)
                    return null;
                else
                    return line.Split(CsvSeparator);
            }
        }

        private BsonValue GetDoubleFromAXString(string doubleStr)
        {
            if (string.IsNullOrEmpty(doubleStr))
                return null;

            return Convert.ToDouble(doubleStr, CultureInfo.GetCultureInfo("FI"));
        }

        private BsonValue GetDateFromAXString(string dateStr)
        {
            if (string.IsNullOrEmpty(dateStr))
                return BsonNull.Value;

            return (BsonValue)Convert.ToDateTime(dateStr, CultureInfo.GetCultureInfo("FI"));
        }

        // Different formats in different CSV files
        private DateTime GetDateFromAXString2(string dateStr)
        {
            if (string.IsNullOrEmpty(dateStr))
                return DateTime.MinValue;

            int year = Convert.ToInt32(dateStr.Substring(0, 4));
            int month = Convert.ToInt32(dateStr.Substring(4, 2));
            int day = Convert.ToInt32(dateStr.Substring(6, 2));

            return new DateTime(year, month, day);
        }

        private string GetLanguageFromAXLanguage(string languageString)
        {
            languageString = languageString.ToLower();
            if (languageString.StartsWith("en-"))
                return ("en-US");
            else if (languageString.ToLower().StartsWith("se"))
                return ("se");
            else
                return ("fi");
        }
    }
}

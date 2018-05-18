using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;
using System.Threading.Tasks;
using SystemsGarden.mc2.MC2SiteEnvironment;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.Core;
using SystemsGarden.mc2.Common.Constants;
using SystemsGarden.mc2.Common;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using MongoDB.Bson;
using mc2.Controllers;
using SystemsGarden.mc2.Tro.Logic;

namespace tro.App_Code.Controllers.tro
{
    public class tro : MC2Controller, IMonitorDataSource
    {
        #region Constants

        public const string ProjectStateDone = "Done";
        public const string ProjectStateInProgress = "In progress";
        public const string ProjectStateUnallocated = "Unallocated";
        public const string ProjectStateSubcontract = "Subcontract";

        private const int DaysToAcceptDefault = 45;

		#endregion

		#region Members

		ILogger entryLogger;

		static private DBDocument basePayReference = null;
		static private object basePayReferenceLockObject = new object();

        static private DBDocument travelTimeReference = null;
        static private object travelTimeReferenceLockObject = new object();

        TimesheetUtils timesheetUtils;

        private bool dailyTimesheetEntryMode = false;
        private bool timeTrackingMode = false;
		private bool allowFutureAbsences = false;
		private bool allowDifferentDurationForBilling = false;
        private int dateDefaultHour = 8;
        private int dateDefaultMinute = 0;

        private DataTree socialProject = null;

		// Monitor counters

		int homeScreenOpened = 0;
		int approveWorkViewOpened = 0;
		int approveWorkManagerViewOpened = 0;
		int hrViewOpened = 0;
		int settingsViewOpened = 0;
		int managementViewOpened = 0;
		int myPlacesViewOpened = 0;
		int favouriteusersViewOpened = 0;

		int timesheetEntriesAdded = 0;
		int absenceEntriesAdded = 0;
		int expenseEntriesAdded = 0;
		int articleEntriesAdded = 0;
		int assetEntriesAdded = 0;

		int workerApprovalRequests = 0;
		int managerApprovalRequests = 0;
		int managerFilterWork = 0;

		public string MonitorDataSourceName
		{
			get
			{
				return "tro";
			}
		}

		#endregion

		#region Init

		public override void Init()
        {
			entryLogger = logger.CreateChildLogger("EntryLogger");

			Runtime.CreatingDocumentBefore += CreatingDocumentBefore;
			Runtime.UpdatingDocumentBefore += UpdatingDocumentBefore;
			Runtime.RemovingDocumentBefore += RemovingDocumentBefore;

			Runtime.ValidationAfter += ValidationAfter;

			Runtime.CreatingDocumentAfter += CreatingDocumentAfter;
			Runtime.UpdatingDocumentAfter += UpdatingDocumentAfter;
			Runtime.RemovingDocumentAfter += RemovingDocumentAfter;


			timesheetUtils = new TimesheetUtils(Runtime);

            dailyTimesheetEntryMode = (bool)Runtime.Config["application"]["features"]["dailyentries"];
            timeTrackingMode = (bool)Runtime.Config["application"]["features"]["timetracking"];
            dateDefaultHour = (int)Runtime.Config["application"]["settings"]["datedefaulthour"].GetValueOrDefault(dateDefaultHour);
            dateDefaultMinute = (int)Runtime.Config["application"]["settings"]["datedefaultminute"].GetValueOrDefault(dateDefaultMinute);
            allowDifferentDurationForBilling = (bool)Runtime.Config["application"]["features"]["allowdifferentdurationforbilling"];
			allowFutureAbsences = (bool)Runtime.Config["application"]["features"]["allowfutureabsences"];

			RegisterTimedTasks();

			Runtime.Monitoring.RegisterMonitorDataSource(this);
        }

        private void RegisterTimedTasks()
        {
            var task = new TimedTask("autoapprovework", AutoApproveWork);
            task.Interval = new TimeSpan(0, 20, 0);
            Runtime.RegisterTimedTask(task);
        }

        #endregion

        #region Blocks

        public MC2Value version()
        {
            return typeof(tro).Assembly.GetName().Version.ToString();
        }

        #endregion

        #region Actions

        [Navigation(true)]
		[GrantAccessToGroup("authenticated")]
        public ActionResult home()
        {
			Interlocked.Increment(ref homeScreenOpened);

			MC2DateTimeValue currDateTime = new MC2DateTimeValue(MC2DateTimeValue.Now());
            
            // Read data from database and provide it to the view
            return View(Runtime.SessionManager.CurrentUser, currDateTime);
        }

        [Navigation(true)]
		[GrantAccessToGroup("canlogwork")]
		public ActionResult approvework(bool inactiveprojectsfound = false)
        {
			Interlocked.Increment(ref approveWorkViewOpened);

			DataTree currentUser = DBDocument.FindOne("user", Runtime.SessionManager.CurrentUser["_id"]);

            ViewBag["entrieslastapproved"] = currentUser["entrieslastapproved"];

            ViewBag["inactiveprojectsfound"] = inactiveprojectsfound;

            DateTime now = MC2DateTimeValue.Now();
            DateTime startOfWeek = now.StartOfWeekUtc();

			// We need to show entries during one week. One week equals 7 days.
            DateTime lastDayOfWeek = startOfWeek.AddDays(7);

            Runtime.HistoryManager.GetCurrentHistoryEntryState()["approveworkbody_state"].Clear();
            Runtime.HistoryManager.GetCurrentHistoryEntryState()["approveworkbody_state"]["daterangestart"] = startOfWeek;
            Runtime.HistoryManager.GetCurrentHistoryEntryState()["approveworkbody_state"]["daterangeend"] = lastDayOfWeek;

            return View();
        }

		[GrantAccessToGroup("canlogwork")]
		[HttpPost]
        public ActionResult approvework(DataTree formData)
        {
			DataTree currentUser = DBDocument.FindOne("user", Runtime.SessionManager.CurrentUser["_id"]);

			ViewBag["entrieslastapproved"] = currentUser["entrieslastapproved"];

            Runtime.HistoryManager.GetCurrentHistoryEntryState()["approveworkbody_state"].Clear();

            Runtime.HistoryManager.GetCurrentHistoryEntryState()["approveworkbody_state"].Merge(formData);

            return View();
        }

		[GrantAccessToGroup("canlogwork")]
		[History(false)]
        public ActionResult doapprovework()
        {
            string userId = Runtime.SessionManager.CurrentUser[DBQuery.Id];

            var entryAndQueries = new List<IMongoQuery>();
            entryAndQueries.Add(Query.EQ("creator", new ObjectId(userId)));
            entryAndQueries.Add(Query.NE("approvedbyworker", true));

            DBQuery articleEntriesQuery = new DBQuery();
            articleEntriesQuery["articleentry"][DBQuery.Condition] = Query.And(entryAndQueries).ToString();

            DBQuery dayEntriesQuery = new DBQuery();
            dayEntriesQuery["dayentry"][DBQuery.Condition] = Query.And(entryAndQueries).ToString();

            entryAndQueries.Add(Query.NotExists("parent"));

            DBQuery timesheetEntriesQuery = new DBQuery();
            timesheetEntriesQuery["timesheetentry"][DBQuery.Condition] = Query.And(entryAndQueries).ToString();

            DBQuery absenceEntriesQuery = new DBQuery();
            absenceEntriesQuery["absenceentry"][DBQuery.Condition] = Query.And(entryAndQueries).ToString();

            DBQuery assetEntriesQuery = new DBQuery();
            assetEntriesQuery["assetentry"][DBQuery.Condition] = Query.And(entryAndQueries).ToString();

            DBQuery userQuery = new DBQuery();
            userQuery["user"][DBQuery.Condition] = Query.EQ(DBQuery.Id, new ObjectId(Runtime.SessionManager.CurrentUser["_id"])).ToString();

			var timesheetEntriesTask = timesheetEntriesQuery.FindAsync();
			var articleEntriesTask = articleEntriesQuery.FindAsync();
			var dayEntriesTask = dayEntriesQuery.FindAsync();
			var absenceEntriesTask = absenceEntriesQuery.FindAsync();
			var assetEntriesTask = assetEntriesQuery.FindAsync();
			
			bool inactiveProjectsFound = false;

            foreach (DBDocument timesheetEntry in timesheetEntriesTask.Result.FirstCollection)
            {
				timesheetEntry.NoCachedInfoRefresh = true;

				if (IsProjectActiveForEntry(timesheetEntry))
                    timesheetEntry["approvedbyworker"] = true;
                else
                    inactiveProjectsFound = true;
            }

            foreach (DBDocument articleEntry in articleEntriesTask.Result.FirstCollection)
            {
				articleEntry.NoCachedInfoRefresh = true;

				if (IsProjectActiveForEntry(articleEntry))
                    articleEntry["approvedbyworker"] = true;
                else
                    inactiveProjectsFound = true;
            }

            foreach (DBDocument dayEntry in dayEntriesTask.Result.FirstCollection)
            {
				dayEntry.NoCachedInfoRefresh = true;

				if (IsProjectActiveForEntry(dayEntry))
                    dayEntry["approvedbyworker"] = true;
                else
                    inactiveProjectsFound = true;
            }

            foreach (DBDocument absenceEntry in absenceEntriesTask.Result.FirstCollection)
            {
				absenceEntry.NoCachedInfoRefresh = true;

				absenceEntry["approvedbyworker"] = true;
            }

            foreach (DBDocument assetEntry in assetEntriesTask.Result.FirstCollection)
            {
				assetEntry.NoCachedInfoRefresh = true;

				if (IsProjectActiveForEntry(assetEntry))
                    assetEntry["approvedbyworker"] = true;
                else
                    inactiveProjectsFound = true;
            }

			DBDocument currentUser = new DBDocument();
			currentUser["entrieslastapproved"] = MC2DateTimeValue.Now();
			currentUser[DBQuery.Id] = Runtime.SessionManager.CurrentUser[DBQuery.Id];
			currentUser.UpdateDatabaseAsync(new DBCallProperties() { SkipValidityChecks = true, SkipDefaultValues = true, DisableEventFiring = true });

			var tasks = new List<Task<DBUpdateResponse>>();

			tasks.Add(timesheetEntriesTask.Result.FirstCollection.UpdateDatabaseAsync(new DBCallProperties() { SkipValidityChecks = true, SkipDefaultValues = true, DisableEventFiring = true }));

			tasks.Add(articleEntriesTask.Result.FirstCollection.UpdateDatabaseAsync(new DBCallProperties() { SkipValidityChecks = true, SkipDefaultValues = true, DisableEventFiring = true }));

			tasks.Add(dayEntriesTask.Result.FirstCollection.UpdateDatabaseAsync(new DBCallProperties() { SkipValidityChecks = true, SkipDefaultValues = true, DisableEventFiring = true }));

			tasks.Add(absenceEntriesTask.Result.FirstCollection.UpdateDatabaseAsync(new DBCallProperties() { SkipValidityChecks = true, SkipDefaultValues = true, DisableEventFiring = true }));

			tasks.Add(assetEntriesTask.Result.FirstCollection.UpdateDatabaseAsync(new DBCallProperties() { SkipValidityChecks = true, SkipDefaultValues = true, DisableEventFiring = true }));

			Task.WaitAll(tasks.ToArray());

			// Go back to current page

			Interlocked.Increment(ref workerApprovalRequests);

            if (inactiveProjectsFound)
                return new RedirectResult(Runtime.HistoryManager.GetCurrentAddress() + "&inactiveprojectsfound=true");
            else
                return new RedirectResult(Runtime.HistoryManager.GetCurrentAddress());
        }

        [Navigation(true)]
        [GrantAccess(3, 4, 5, 6)]
        public ActionResult approveworkmanager()
        {
			Interlocked.Increment(ref approveWorkManagerViewOpened);

			DataTree state = Runtime.HistoryManager.GetCurrentHistoryEntry()["state"]["approveworkmanager_state"];

            // Defaults to true
            if (!state.Contains("showonlyentriesnotaccepted"))
                state["showonlyentriesnotaccepted"] = true;

			if ((bool)Runtime.Config["application"]["features"]["managerfilter_defaultprojectmanageriscurrentuser"] &&
				string.IsNullOrEmpty(state["userfilter"]) &&
				string.IsNullOrEmpty(state["assetfilter"]) &&
				string.IsNullOrEmpty(state["projectfilter"]) &&
				string.IsNullOrEmpty(state["profitcenterfilter"]) &&
				string.IsNullOrEmpty(state["managerprojectsfilter"]) &&
				string.IsNullOrEmpty(state["favouriteusersfilter"]))
			{
				DataTree currentUser = Runtime.SessionManager.CurrentUser;
				state["managerprojectsfilter"] = currentUser[DBQuery.Id];
				state["managerprojectsfiltername"] = currentUser["firstname"] + " " + currentUser["lastname"];
			}
			else
			{
				state["managerprojectsfiltername"].Remove();
			}

			return View();
        }

        [GrantAccess(3, 4, 5, 6)]
        [HttpPost]
        public ActionResult approveworkmanager(
            DataTree formData,
            string actiontype,
            bool showonlyentriesnotaccepted = false,
            bool showdaterange = false)
        {
            DataTree state = Runtime.HistoryManager.GetCurrentHistoryEntry()["state"]["approveworkmanager_state"];

			// Clear overridden project manager name from default.
			state["managerprojectsfiltername"].Remove();

            state.Merge(formData);

            // Required to fix checkboxes not posting any data when not checked.
            state["showonlyentriesnotaccepted"] = showonlyentriesnotaccepted;
            state["showdaterange"] = showdaterange;

			// Do not allow assistant HR personnell to accept worker bookings
            if (actiontype == "approve" && (int)Runtime.SessionManager.CurrentUser["level"] != 6)
            {
				Interlocked.Increment(ref managerApprovalRequests);

				var approveOrQueries = new List<IMongoQuery>();
				var approveOrQueriesTSE = new List<IMongoQuery>();

                foreach (DataTree item in formData)
                {
                    if (item.Name.StartsWith("__listitem_"))
					{
						approveOrQueries.Add(Query.EQ(DBQuery.Id, new ObjectId((string)item)));

						// For timesheetentries accept both items and their children (details)
						approveOrQueriesTSE.Add(Query.EQ(DBQuery.Id, new ObjectId((string)item)));
						approveOrQueriesTSE.Add(Query.EQ("parent", new ObjectId((string)item)));
					}
                }

                // Update timesheet entries and article entries with selected ids to be approved by manager. Note that we send
                // all items to both collections since we don't really know whether an item is timesheet entry or article entry.
                // Ids are unique and this shouldn't be a big problem.

                if (approveOrQueries.Count > 0)
                {
                    DBQuery timesheetEntriesQuery = new DBQuery();
                    timesheetEntriesQuery["timesheetentry"][DBQuery.Condition] = Query.Or(approveOrQueriesTSE).ToString();

                    DBQuery articleEntriesQuery = new DBQuery();
                    articleEntriesQuery["articleentry"][DBQuery.Condition] = Query.Or(approveOrQueries).ToString();

                    DBQuery dayEntriesQuery = new DBQuery();
                    dayEntriesQuery["dayentry"][DBQuery.Condition] = Query.Or(approveOrQueries).ToString();

                    DBQuery absenceEntriesQuery = new DBQuery();
                    absenceEntriesQuery["absenceentry"][DBQuery.Condition] = Query.Or(approveOrQueries).ToString();

                    DBQuery assetEntriesQuery = new DBQuery();
                    assetEntriesQuery["assetentry"][DBQuery.Condition] = Query.Or(approveOrQueries).ToString();

					var timesheetEntriesQueryTask = timesheetEntriesQuery.FindAsync();
					var articleEntriesQueryTask = articleEntriesQuery.FindAsync();
					var dayEntriesQueryTask = dayEntriesQuery.FindAsync();
					var absenceEntriesQueryTask = absenceEntriesQuery.FindAsync();
					var assetEntriesQueryTask = assetEntriesQuery.FindAsync();

                    foreach (DBDocument entry in timesheetEntriesQueryTask.Result.FirstCollection)
                    {
						entry.NoCachedInfoRefresh = true;

						if (IsProjectActiveForEntry(entry))
                        {
                            entry["approvedbymanager"] = true;
                        }
                    }

                    foreach (DBDocument entry in articleEntriesQueryTask.Result.FirstCollection)
                    {
						entry.NoCachedInfoRefresh = true;

						if (IsProjectActiveForEntry(entry))
                        {
                            entry["approvedbymanager"] = true;
                        }
                    }

                    foreach (DBDocument entry in dayEntriesQueryTask.Result.FirstCollection)
                    {
						entry.NoCachedInfoRefresh = true;

						if (IsProjectActiveForEntry(entry))
                        {
                            entry["approvedbymanager"] = true;
                        }
                    }

                    foreach (DBDocument entry in absenceEntriesQueryTask.Result.FirstCollection)
                    {
						entry.NoCachedInfoRefresh = true;

						entry["approvedbymanager"] = true;
                    }

                    foreach (DBDocument entry in assetEntriesQueryTask.Result.FirstCollection)
                    {
						entry.NoCachedInfoRefresh = true;

						if (IsProjectActiveForEntry(entry))
                        {
                            entry["approvedbymanager"] = true;
                        }
                    }

					var tasks = new List<Task<DBUpdateResponse>>();

					tasks.Add(timesheetEntriesQueryTask.Result.FirstCollection.UpdateDatabaseAsync(new DBCallProperties() { SkipDefaultValues = true, SkipValidityChecks = true }));

					tasks.Add(articleEntriesQueryTask.Result.FirstCollection.UpdateDatabaseAsync(new DBCallProperties() { SkipDefaultValues = true, SkipValidityChecks = true }));

					tasks.Add(dayEntriesQueryTask.Result.FirstCollection.UpdateDatabaseAsync(new DBCallProperties() { SkipDefaultValues = true, SkipValidityChecks = true }));

					tasks.Add(absenceEntriesQueryTask.Result.FirstCollection.UpdateDatabaseAsync(new DBCallProperties() { SkipDefaultValues = true, SkipValidityChecks = true }));

					tasks.Add(assetEntriesQueryTask.Result.FirstCollection.UpdateDatabaseAsync(new DBCallProperties() { SkipDefaultValues = true, SkipValidityChecks = true }));

					Task.WaitAll(tasks.ToArray());
				}
            }
			else
			{
				Interlocked.Increment(ref managerFilterWork);
			}


			// Show totals regrdless of whether this was an acceptance message or not
			var getTotalsMessage = new RCMessage("tro_gettimesheetentrytotals");

            DataTree handler = getTotalsMessage.Handlers["trohelpershandler"];

            handler["user"] = formData["userfilter"];
            handler["asset"] = formData["assetfilter"];
            handler["project"] = formData["projectfilter"];
            handler["profitcenter"] = formData["profitcenterfilter"];
			handler["projectmanager"] = formData["managerprojectsfilter"];
			handler["favouriteusers"] = formData["favouriteusers"];

			handler["approvedbyworker"] = true;

            if (showonlyentriesnotaccepted)
            {
                handler["approvedbymanager"] = false;
            }
            else 
            {
                handler["starttimestamp"] = (MC2DateTimeValue)(string)formData["daterangestart"];
                handler["endtimestamp"] = (MC2DateTimeValue)(string)formData["daterangeend"];
            }

            RCResponse response = Runtime.SendRemoteMessage(getTotalsMessage);

            DataTree totals = response.Handlers["trohelpershandler"];

            Runtime.HistoryManager.GetCurrentHistoryEntry()["state"]["approveworkmanagerbody_state"].Merge(formData);
            return View(totals);
        }

        [Navigation(true)]
		[GrantAccessToGroup("authenticated")]
		[FeatureRequired("settings")]
        public ActionResult settings()
        {
			Interlocked.Increment(ref settingsViewOpened);
			return View();
        }

		[GrantAccessToGroup("authenticated")]
		[FeatureRequired("myplaces")]
        public ActionResult myplaces()
        {
			Interlocked.Increment(ref myPlacesViewOpened);
			return View();
        }

		[GrantAccessToGroup("authenticated")]
		[FeatureRequired("favouriteusers")]
		public ActionResult favouriteuserslists()
		{
			Interlocked.Increment(ref favouriteusersViewOpened);
			return View();
		}

		[Navigation(true)]
        [GrantAccess(4, 5, 6)]
        public ActionResult hrview()
        {
			Interlocked.Increment(ref hrViewOpened);

			return View();
        }

        [GrantAccess(4, 5, 6)]
        [HttpPost]
        public ActionResult hrview(
            DataTree formData, 
            string actiontype,
            string userfilter,
            string projectfilter,
            string payrollperiodfilter,
            bool showonlyentriesnotacceptedbymanager = false,
            bool showonlyentriesnotacceptedbyworker = false,
            bool showonlyexportedentries = false)
        {
            // Save history information
            DataTree state = Runtime.HistoryManager.GetCurrentHistoryEntry()["state"]["hrview_state"];
            state.Merge(formData);

            state["showonlyentriesnotacceptedbymanager"] = showonlyentriesnotacceptedbymanager;
            state["showonlyentriesnotacceptedbyworker"] = showonlyentriesnotacceptedbyworker;
            state["showonlyexportedentries"] = showonlyexportedentries;

            state["exportdisabled"] = false;
            state["canceldisabled"] = false;
			state["exporttoexceldisabled"] = false;

			if (showonlyexportedentries)
                state["exportdisabled"] = true;
            else
                state["canceldisabled"] = true;

            if (showonlyentriesnotacceptedbymanager || showonlyentriesnotacceptedbyworker ||
                (string.IsNullOrEmpty(payrollperiodfilter) || !string.IsNullOrEmpty(userfilter)))
            {
                state["canceldisabled"] = true;
                state["exportdisabled"] = true;
            }

            if (actiontype == "approve")
            {
                logger.LogInfo("Approving HR data", userfilter, projectfilter, payrollperiodfilter);

                var approveHRData = new RCMessage("vismaexportdata");

                DataTree vismaHandler = approveHRData.Handlers["vismaintegrationhandler"];

                vismaHandler["user"] = userfilter;
                vismaHandler["project"] = projectfilter;
                vismaHandler["payrollperiod"] = payrollperiodfilter;

                // Exproting data may take time. Wait for 2 minutes.
                approveHRData.Handlers[TCPHandlerConstants.tcphandler][TCPHandlerConstants.sendtimeout] = 1800000;

                Runtime.SendRemoteMessage(approveHRData);

                return View();
            }
            else if (actiontype == "cancel")
            {
                logger.LogInfo("Canceling HR data", userfilter, projectfilter, payrollperiodfilter);

                var canceledHRData = new RCMessage("vismacancelexport");

                DataTree vismaHandler = canceledHRData.Handlers["vismaintegrationhandler"];

                vismaHandler["user"] = userfilter;
                vismaHandler["project"] = projectfilter;
                vismaHandler["payrollperiod"] = payrollperiodfilter;

                // Exproting data may take time. Wait for 2 minutes.
                canceledHRData.Handlers[TCPHandlerConstants.tcphandler][TCPHandlerConstants.sendtimeout] = 1800000;

                Runtime.SendRemoteMessage(canceledHRData);

                return View();

            }
            else
            {
                return View();
            }
        }

		[Navigation(true)]
		[GrantAccess(3, 5)]
		[FeatureRequired("resourcing")]
		public ActionResult resourcing()
		{
			ViewBag["userprofitcenter"] = Runtime.SessionManager.CurrentUser["profitcenter"].GetValueOrDefault("notfound");

			const int DefaultStartTimeHourDefault = 7;
			const int DefaultStartTimeMinuteDefault = 30;
			const int DefaultEndTimeHourDefault = 15;
			const int DefaultEndTimeMinuteDefault = 30;
			const decimal DefaultDayCompleteThreshold = 8;
			const decimal DefaultOvertimeThreshold = 8;

			ViewBag["allocationstarthour"] = (int)Runtime.Config["dailyresourcingwidget"]["defaultstarttimehour"].GetValueOrDefault(DefaultStartTimeHourDefault);
			ViewBag["allocationstartminute"] = (int)Runtime.Config["dailyresourcingwidget"]["defaultstarttimeminute"].GetValueOrDefault(DefaultStartTimeMinuteDefault);
			ViewBag["allocationendhour"] = (int)Runtime.Config["dailyresourcingwidget"]["defaultendtimehour"].GetValueOrDefault(DefaultEndTimeHourDefault);
			ViewBag["allocationendminute"] = (int)Runtime.Config["dailyresourcingwidget"]["defaultendtimeminute"].GetValueOrDefault(DefaultEndTimeMinuteDefault);

			ViewBag["daycompletethreshold"] = (decimal)Runtime.Config["dailyresourcingwidget"]["defaultdaycompletethreshold"].GetValueOrDefault(DefaultDayCompleteThreshold);
			ViewBag["overtimethreshold"] = (decimal)Runtime.Config["dailyresourcingwidget"]["defaultovertimethreshold"].GetValueOrDefault(DefaultOvertimeThreshold);

			return View();
		}

		[Navigation(false)]
        [GrantAccess(3, 5)]
        [FeatureRequired("resourcing")]
        public ActionResult resourceprojects()
        {
			// Read data from database and provide it to the view
			ViewBag["canceldisabled"] = true;
            ViewBag["exportdisabled"] = true;

            return View();
        }

		[Navigation(true)]
		[GrantAccess(5, 7)]
		[FeatureRequired("prtgmonitor")]
		public ActionResult monitoring()
		{
			return View();
		}

		[GrantAccess(3)]
        [HttpPost]
        public ActionResult resourceprojects(DataTree formData)
        {
            Runtime.HistoryManager.GetCurrentHistoryEntry()["state"]["resourceprojects_state"].Merge(formData);

            // Read data from database and provide it to the view
            return View();
        }

        [GrantAccess(3)]
        public ActionResult resourceallocation(string projectid)
        {
            // Read data from database and provide it to the view

            DBQuery projectQuery = new DBQuery();
            projectQuery["project"][DBQuery.Condition] = Query.EQ(DBQuery.Id, new ObjectId(projectid)).ToString();            
            ViewBag["currentproject"] = projectQuery.FindOne();
            return View(projectid);
        }

        [HttpPost]
		[GrantAccessToGroup("authenticated")]
		public ActionResult home(DataTree formData)
        {
            return Redirect("/main.aspx?controller=tro&action=home");
        }

        [GrantAccess(3, 4, 5, 6, 7)]
        [Navigation(true)]
        public ActionResult manage() 
        {
			Interlocked.Increment(ref managementViewOpened);

			return View();
        }

		[GrantAccessToGroup("authenticated")]
		public ActionResult timesheetentrydetaildata(string timesheetentryid)
        {
            DBQuery query = new DBQuery();

            query["timesheetentry"][DBQuery.Condition] = Query.EQ("parent", new ObjectId(timesheetentryid)).ToString();

            var itemCollectionTask = query.FindAsync();
            var parentItemTask = DBDocument.FindOneAsync("timesheetentry", timesheetentryid);

            DataTree result = new DataTree("timesheetentrydetails");

            result["item"] = (DataTree)itemCollectionTask.Result.FirstCollection;
            result["parentitem"] = parentItemTask.Result;

            return new AjaxResult(result.ToJson());
        }

        [GrantAccess(3, 5)]
        [Navigation(true)]
        [FeatureRequired("reports")]
        public ActionResult reports()
        {
            return View();
        }

		[GrantAccessToGroup("authenticated")]
		public ActionResult projectleadreport(string project)
		{
            var projectleadsMessage = new RCMessage("tro_getprojectleads");

            DataTree handler = projectleadsMessage.Handlers["trohelpershandler"];

            handler["project"] = project;

            RCResponse response = Runtime.SendRemoteMessage(projectleadsMessage);

            bool userScope = true;
            foreach (var projectLead in response["handlers"]["trohelpershandler"]["projectleads"])
            {
                if (projectLead.Name == Runtime.SessionManager.CurrentUser[DBDocument.Id])
                {
                    userScope = false;
                    break;
                }
            }


            ViewBag["userscope"] = userScope;
            ViewBag["project"] = DBDocument.FindOne("project", project);
			return View();
		}

		#endregion

		#region Events

		public void ValidationAfter(ValidatedDocument validatedDocument)
        {
            string collectionName = validatedDocument.Schema.Name;

            if (validatedDocument.DataValid)
            {
                if (collectionName == "timesheetentry" || collectionName == "absenceentry" || collectionName == "assetentry")
                {
                    // Only make consistency checks if data is valid in other ways and only for base entries
                    if (timeTrackingMode && 
						validatedDocument.Tree["starttimestamp"].HasValue && 
						validatedDocument.Tree["endtimestamp"].HasValue &&
						!validatedDocument.Tree["parent"][DBQuery.Id].HasValue)
                    {
                        // If start is after end
                        DateTime startTimestamp = (DateTime)validatedDocument.Tree["starttimestamp"];
                        DateTime endTimestamp = (DateTime)validatedDocument.Tree["endtimestamp"];

                        ValidateStartIsNotBeforeEnd(validatedDocument, startTimestamp, endTimestamp);
                        ValidateNoCollisionsAndPayPeriod(validatedDocument, collectionName);
                        ValidateStartNotTooEarly(validatedDocument, startTimestamp);
                        ValidateStartNotTooLate(validatedDocument, collectionName, startTimestamp);
                    }

                    if (dailyTimesheetEntryMode)
                    {
                        DateTime date = (DateTime)validatedDocument.Tree["date"];

                        ValidatePayPeriod(validatedDocument, collectionName);
                        ValidateDayEntryStartNotTooEarlyOrLate(validatedDocument, collectionName);
                    }
                }

                if (collectionName == "timesheetentry" || collectionName == "dayentry" || collectionName == "assetentry" ||
                        collectionName == "articleentry")
                {
                    ValidateProjectActive(validatedDocument);
                    ValidateNotAccepted(validatedDocument, collectionName);
					ValidateProjectCategory(validatedDocument);
					ValidateRoute(validatedDocument);
					ValidateTime(validatedDocument);
                }
            }

            if (collectionName == "dayentry")
            {
                ValidateDayEntryStartNotTooEarlyOrLate(validatedDocument, collectionName);
                ValidatePayPeriod(validatedDocument, collectionName);
            }
        }

        private void ValidateProjectActive(ValidatedDocument validatedDocument)
        {
            // Project active constraints not used for HR or admins
            if ((int)Runtime.SessionManager.CurrentUser["level"] == 5)
                return;

            string collectionName = validatedDocument.Schema.Name;

            DataTree project = DBDocument.FindOne("project", (string)validatedDocument.Tree["project"]);

            if (project != null)
            {
                if ((string)project["status"] == ProjectStateDone)
                    {
                        validatedDocument.GetProperty("project").AddValidationError("projectnotactive");
                }
            }
        }

		private void ValidateProjectCategory(ValidatedDocument validatedDocument)
		{
			if (Runtime.CurrentActionCall.Parameters["requireprojectcategory"] == "true")
			{
				if (string.IsNullOrEmpty(validatedDocument.Tree["projectcategory"]))
					validatedDocument.GetProperty("projectcategory").AddValidationError(ValidatedDocumentProperty.ValidationErrorRequired);
			}
		}

		private void ValidateRoute(ValidatedDocument validatedDocument)
		{
			if (Runtime.CurrentActionCall.Parameters["requireroute"] == "true")
			{
				if (string.IsNullOrEmpty(validatedDocument.Tree["route"]))
					validatedDocument.GetProperty("route").AddValidationError(ValidatedDocumentProperty.ValidationErrorRequired);
			}
		}

		private void ValidateTime(ValidatedDocument validatedDocument)
		{
			if (Runtime.CurrentActionCall.Parameters["requiretime"] == "true")
			{
				bool timeOk = true;

				if (string.IsNullOrEmpty(validatedDocument.Tree["starttime"]))
				{
					validatedDocument.GetProperty("starttime").AddValidationError(ValidatedDocumentProperty.ValidationErrorRequired);
					timeOk = false;
				}
				if (string.IsNullOrEmpty(validatedDocument.Tree["endtime"]))
				{
					validatedDocument.GetProperty("endtime").AddValidationError(ValidatedDocumentProperty.ValidationErrorRequired);
					timeOk = false;
				}

				if (timeOk)
				{
					if (((DateTime)validatedDocument.Tree["starttime"]).CompareTo((DateTime)validatedDocument.Tree["endtime"]) >= 0)
						validatedDocument.GetProperty("starttime").AddValidationError("startmustbebeforeend");
                }
			}
		}

		private void ValidateDayEntryStartNotTooEarlyOrLate(ValidatedDocument validatedDocument, string collectionName)
        {
            // If start is earlier than two weeks before current date
            if (validatedDocument.Tree["date"].HasValue && validatedDocument.DataValid && Runtime.Security.IsCurrentUserMemberOfAccessGroup("workers"))
            {
                int daysToAccept = (int)Runtime.Config["tro"]["hourreporting"]["maxreportdaysallowedpast"].GetValueOrDefault(DaysToAcceptDefault);

                DateTime startDate = (DateTime)validatedDocument.Tree["date"];
                DateTime earliestPossibleDate = MC2DateTimeValue.Now().AddDays(- daysToAccept);

                if ((startDate).CompareTo(earliestPossibleDate) < 0)
                    validatedDocument.GetProperty("date").AddValidationError("mustnotbetooearly");

                DateTime startOfTomorrow = MC2DateTimeValue.Now().AddDays(1).StartOfDayUtc();

                if ((startDate).CompareTo(startOfTomorrow) >= 0 &&
                        (collectionName != "absenceentry" || !allowFutureAbsences))
                    validatedDocument.GetProperty("date").AddValidationError("mustnotbelaterthantoday");
            }
        }

		private void ValidateStartNotTooLate(ValidatedDocument validatedDocument, string collectionName, DateTime startTimestamp)
		{
			// If end is tomorrow do not accept booking
			if (collectionName != "absenceentry" && validatedDocument.DataValid)
			{
				DateTime startOfTomorrow = MC2DateTimeValue.Now().AddDays(1).StartOfDayUtc();

				if (validatedDocument.Tree["starttimestamp"].HasValue)
				{
					// It may be ok to add absences for the future
					if ((startTimestamp).CompareTo(startOfTomorrow) > 0 &&
						(collectionName != "absenceentry" || !allowFutureAbsences))
						validatedDocument.GetProperty("starttimestamp").AddValidationError("mustnotbelaterthantoday");
				}
			}
		}

        /// <summary>
        /// Ensures that workers and managers can not change entries that they have accepted
        /// at their respective level.
        /// </summary>
        /// <param name="validatedDocument"></param>
        /// <param name="collectionName"></param>
        private void ValidateNotAccepted(ValidatedDocument validatedDocument, string collectionName)
        {
			// Copy operations suppress checks for approved statuses since any invalid approval states are cleared
			if (validatedDocument.Tree.Contains("__copyoperation"))
				return;

			bool approvedByWorker = false;
            bool approvedByManager = false;

            approvedByWorker = (bool)validatedDocument.Tree["approvedbyworker"];
            approvedByManager = (bool)validatedDocument.Tree["approvedbymanager"];
            
            if (Runtime.Security.IsCurrentUserMemberOfAccessGroup("workers") && approvedByWorker)
            {
                validatedDocument.AddValidationError("alreadyaccepted");
            }
            else if ((int)Runtime.SessionManager.CurrentUser["level"] == 3 && approvedByManager)
            {
                validatedDocument.AddValidationError("alreadyaccepted");
            }
        }

        private void ValidateStartNotTooEarly(ValidatedDocument validatedDocument, DateTime startTimestamp)
        {
            // If start is earlier than two weeks before current date. Not valid for managers or HR.
            if (validatedDocument.Tree["starttimestamp"].HasValue && validatedDocument.DataValid && Runtime.Security.IsCurrentUserMemberOfAccessGroup("workers"))
            {
                int daysToAccept = (int)Runtime.Config["tro"]["hourreporting"]["maxreportdaysallowedpast"].GetValueOrDefault(DaysToAcceptDefault);

                DateTime firstAcceptedDate = MC2DateTimeValue.Now().AddDays(- daysToAccept);

                if ((startTimestamp).CompareTo(firstAcceptedDate) < 0)
                    validatedDocument.GetProperty("starttimestamp").AddValidationError("mustnotbetooearly");
            }
        }


        // Todo: refactor into separate methods for collisions an pay periods and make better use of 
        // tasks.
        private void ValidateNoCollisionsAndPayPeriod(ValidatedDocument validatedDocument, string collectionName)
        {
            if (!timeTrackingMode)
                return;

			if (collectionName != "assetentry")
			{
				// Make sure there isn't already timesheet or absence on the same date
				var timesheetQuery = new DBQuery();
				var absenceQuery = new DBQuery();
				var payrollPeriodQuery = new DBQuery();

				var entryAndQueries = new List<IMongoQuery>();
				entryAndQueries.Add(Query.EQ("user", new ObjectId(validatedDocument.Tree["user"][DBQuery.Id])));
				entryAndQueries.Add(Query.LT("starttimestamp", (DateTime)validatedDocument.Tree["endtimestamp"]));
				entryAndQueries.Add(Query.GT("endtimestamp", (DateTime)validatedDocument.Tree["starttimestamp"]));
				entryAndQueries.Add(Query.NotExists("parent"));

				// When checkin for existin entries make sure current entry isn't included
				if (validatedDocument.Tree[DBQuery.Id].HasValue && !string.IsNullOrEmpty(validatedDocument.Tree[DBQuery.Id]))
					entryAndQueries.Add(Query.NE(DBQuery.Id, new ObjectId(validatedDocument.Tree[DBQuery.Id])));

				timesheetQuery["timesheetentry"][DBQuery.Condition] = Query.And(entryAndQueries).ToString();
				timesheetQuery["timesheetentry"][DBQuery.CountOnly] = true;
				timesheetQuery["timesheetentry"][DBQuery.IncludeTotals] = true;

				absenceQuery["absenceentry"][DBQuery.Condition] = Query.And(entryAndQueries).ToString();
				absenceQuery["absenceentry"][DBQuery.CountOnly] = true;
				absenceQuery["absenceentry"][DBQuery.IncludeTotals] = true;

				DateTime startDate = ((DateTime)validatedDocument.Tree["starttimestamp"]).ToLocalTime();
				DateTime endDate = ((DateTime)validatedDocument.Tree["endtimestamp"]).ToLocalTime();

				// Convert dates to what would be the date's timestamp in UTC timezone. This is to take in to account the fact 
				// that dates are not timestamps and to compare between date and a datetime we must assign a timezone to the date.
				startDate = new DateTime(startDate.Year, startDate.Month, startDate.Day, 0, 0, 0, DateTimeKind.Utc);
				endDate = new DateTime(endDate.Year, endDate.Month, endDate.Day, 0, 0, 0, DateTimeKind.Utc);

				payrollPeriodQuery["payrollperiod"][DBQuery.Condition] = Query.And(
					Query.LTE("startdate", startDate),
					Query.GT("enddate", endDate),
					Query.EQ("locked", true)
					).ToString();

				var payrollPeriodQueryTask = payrollPeriodQuery.FindAsync(new DBCallProperties() { RunWithPrivileges = 5 });
				var userTask = DBDocument.FindOneAsync("user", (string)validatedDocument.Tree["user"][DBQuery.Id]);

                // Collisions are only checked when timetracking is enabled
                if (timeTrackingMode)
				{
					var timesheetQueryTask = timesheetQuery.FindAsync(new DBCallProperties() { RunWithPrivileges = 5 });
					var absenceQueryTask = absenceQuery.FindAsync(new DBCallProperties() { RunWithPrivileges = 5} );

					if ((int)timesheetQueryTask.Result.QueryInfo["timesheetentry"]["totalrecords"] > 0)
					{
						validatedDocument.GetProperty("endtimestamp").AddValidationError("timesheetalreadyexists");
					}
					else if ((int)absenceQueryTask.Result.QueryInfo["absenceentry"]["totalrecords"] > 0)
					{
						validatedDocument.GetProperty("endtimestamp").AddValidationError("absencealreadyexists");
					}
				}

				// Todo: This check is doubled. See if it can be removed.

				// Only evaluate payroll period collisions for basic and extended workers and limited HR users. 
				int currentUserLevel = (int)Runtime.SessionManager.CurrentUser["level"];
				if (currentUserLevel < 3 || currentUserLevel == 6 || !(bool)Runtime.Config["application"]["features"]["allowmanagerstomodifylockedpayrollperiods"])
				{
					DataTree user = userTask.Result;
					foreach (var payrollPeriod in payrollPeriodQueryTask.Result.FirstCollection)
					{
						if ((string)payrollPeriod["clacontract"] == (string)user["clacontract"])
						{
							validatedDocument.GetProperty("starttimestamp").AddValidationError("payrollperiodlocked");
						}
					}
				}
			}
        }

        private void ValidatePayPeriod(ValidatedDocument validatedDocument, string collectionName)
        {
            var payrollPeriodQuery = new DBQuery();

            payrollPeriodQuery["payrollperiod"][DBQuery.Condition] = Query.And(
                Query.LTE("startdate", (DateTime)validatedDocument.Tree["date"]),
                Query.GT("enddate", (DateTime)validatedDocument.Tree["date"]),
                Query.EQ("locked", true)
                ).ToString();

			// Use privileges for manager to get any possible colliding items.

			var payrollPeriodQueryTask = payrollPeriodQuery.FindAsync(new DBCallProperties { RunWithPrivileges = 5 });

            DataTree user = DBDocument.FindOne("user", (string)validatedDocument.Tree["user"][DBQuery.Id]);

			// Only evaluate payroll period collisions for basic and extended workers and limited HR users. 
			int currentUserLevel = (int)Runtime.SessionManager.CurrentUser["level"];
			if (currentUserLevel < 3 || currentUserLevel == 6 || !(bool)Runtime.Config["application"]["features"]["allowmanagerstomodifylockedpayrollperiods"])
			{
				foreach (var payrollPeriod in payrollPeriodQueryTask.Result.FirstCollection)
				{
					if ((string)payrollPeriod["clacontract"] == (string)user["clacontract"])
					{
						validatedDocument.GetProperty("date").AddValidationError("payrollperiodlocked");
					}
				}
			}
		}

        private static void ValidateStartIsNotBeforeEnd(ValidatedDocument validatedDocument, DateTime startTimestamp, DateTime endTimestamp)
        {
            if ((startTimestamp).CompareTo(endTimestamp) >= 0)
            {
                validatedDocument.GetProperty("endtimestamp").AddValidationError("startmustbebeforeend");
            }
        }		

		public void UpdateMonitorValuesForCreate(DataTree document, Task<RCResponse> response)
		{
			if (document["__collectionname"] == "timesheetentry")
				Interlocked.Increment(ref timesheetEntriesAdded);
			else if (document["__collectionname"] == "absenceentry")
				Interlocked.Increment(ref absenceEntriesAdded);
			else if (document["__collectionname"] == "dayentry")
				Interlocked.Increment(ref expenseEntriesAdded);
			else if (document["__collectionname"] == "articleentry")
				Interlocked.Increment(ref articleEntriesAdded);
			else if (document["__collectionname"] == "assetentry")
				Interlocked.Increment(ref assetEntriesAdded);
		}

		/// <summary>
		/// If timesheet entry is copied then copy it's details as well.
		/// </summary>
		/// <param name="document"></param>
		/// <returns></returns>
		public void ApplyDetailsToCopy(DataTree document, Task<RCResponse> response)
        {
            if (document.Contains("__copyoperation") && document["__collectionname"] == "timesheetentry")
            {
                if (response.Result.Success)
                {
                    var query = new DBQuery( "tro", "timesheetanddetailsbyid");

                    query.AddParameter("id", document["__copyoperation"]);

                    DBCollection entryDetails = query.FindAsync().Result;

                    if (entryDetails.Count > 0)
                    {
                        // Get the created item id from result
                        string newItemId = response.Result.Handlers["mongodbhandler"]["data"]["timesheetentry"][0][DBQuery.Id];

                        // Create new ids for items to copy them
                        foreach (DBDocument detail in entryDetails)
                        {
                            string detailId = ObjectId.GenerateNewId().ToString();
                            detail[DBQuery.Id] = detailId;
                            detail.Name = detailId;

                            detail["timesheetentry"].Clear();
                            detail["timesheetentry"] = newItemId;
                        }

						entryDetails.UpdateDatabaseAsync();
                    }
                }
            }
        }

		public void CreatingDocumentBefore(DataTree document, ref bool cancel)
		{
			string collectionName = document["__collectionname"];

			entryLogger.LogDebug("Creating document", collectionName);

			ApplyBasePayType(document, collectionName);

			NormalizeTimesheetDataForDailyEntriesAndTimetracking(document, collectionName);

			CacheDataToTimesheetEntries(document, collectionName);

			CacheDataToAllocationEntries(document, collectionName);

			SetUserAsOwner(document);

			MakeModificationsForCopyOperation(document);

			AutoWorkerApproveEntriesForManagement(document);

			// Add timestamp to all documents 
			document["frontupdatetimestamp"] = MC2DateTimeValue.Now();
		}

		public void UpdatingDocumentBefore(DataTree document, ref bool cancel)
        {
            string collectionName = document["__collectionname"];

			ApplyBasePayType(document, collectionName);

			NormalizeTimesheetDataForDailyEntriesAndTimetracking(document, collectionName);

			CacheDataToTimesheetEntries(document, collectionName);

            CacheDataToAllocationEntries(document, collectionName);

			// Add timestamp to all documents 
			document["frontupdatetimestamp"] = MC2DateTimeValue.Now();
        }

		private void ApplyBasePayType(DataTree document, string collectionName)
		{
			try
			{
				if (collectionName == "timesheetentry" && (bool)Runtime.Features["timetracking"] && !document["timesheetentrydetailpaytype"][DBQuery.Id].Exists)
				{
					entryLogger.LogDebug("applying base pay type");
                    if (document["istraveltime"].Exists && (bool)document["istraveltime"].Value == true)
                        document["timesheetentrydetailpaytype"][DBQuery.Id] = GetTravelTimePayType()[DBQuery.Id];
                    else
                        document["timesheetentrydetailpaytype"][DBQuery.Id] = GetBasePay()[DBQuery.Id];
				}
			}
			catch(Exception e)
			{
				throw new AggregateException("Failed get base pay type", e);
			}
		}

		public void CreatingDocumentAfter(DataTree document, Task<RCResponse> response)
        {
			string collectionName = document["__collectionname"];

			ApplyDetailsToCopy(document, response);
			UpdateMonitorValuesForCreate(document, response);

			UpdateRelatedTimesheetEntries(document, collectionName);
		}

		public void UpdatingDocumentAfter(DataTree document, Task<RCResponse> response)
		{
			string collectionName = document["__collectionname"];

			UpdateRelatedTimesheetEntries(document, collectionName);
		}

        private void CacheDataToAllocationEntries(DataTree document, string collectionName)
        {
			if (collectionName == "allocationentry")
			{
				Task<DBDocument> userTask = null;
				DataTree user = null;

				if (document["user"].Exists && !string.IsNullOrEmpty(document["user"]))
					userTask = DBDocument.FindOneAsync("user", document["user"]);

				Task<DBDocument> projectTask = null;
				Task<DBDocument> customerTask = null;

				if (document["project"][DBQuery.Id].HasValue)
					projectTask = DBDocument.FindOneAsync("project", document["project"][DBQuery.Id]);

				if (document["customer"][DBQuery.Id].HasValue)
					customerTask = DBDocument.FindOneAsync("customer", document["project"][DBQuery.Id]);

				if (userTask != null)
					user = userTask.Result;

				if (user != null)
                {
					// Set initials
                    string firstName = (string)user["firstname"];
                    string lastName = (string)user["lastname"];

                    string initials = string.Empty;

                    if (!string.IsNullOrEmpty(firstName))
                        initials += firstName + " ";

                    if (!string.IsNullOrEmpty(lastName))
                        initials += lastName.Substring(0, 1) + ".";

                    document["initials"] = initials;

					// Set user's profit center for allocation
					document["userprofitcenter"] = user["profitcenter"];					
                }
				else if (document["asset"].Exists && !string.IsNullOrEmpty(document["asset"]))
                {
                    DataTree asset = DBDocument.FindOne("asset", document["asset"]);

                    string licensePlate = (string)asset["licenseplate"];

                    string returnValue = string.Empty;

                    if (!string.IsNullOrEmpty(licensePlate))
                        returnValue = licensePlate;

                    document["initials"] = returnValue;
                }

				if (projectTask != null && projectTask.Result != null)
				{
					// Set project's profit center for allocation
					document["projectprofitcenter"] = projectTask.Result["profitcenter"];
					document["projectcustomer"] = projectTask.Result["customer"];
					document["projectnote"] = projectTask.Result["note"];
				}

                if (projectTask != null && userTask != null)
                {
                    // Allocation's identifier is a combination of user's identifier and project's identifier.
                    document["identifier"] = string.Format("{0} {1}", projectTask.Result["identifier"], userTask.Result["identifier"]);
                }
            }
		}

        /// <summary>
        /// Timesheets can be reported with start and end timestamps or start date and duration.
        /// This method fills out the missing fields accordingly. If timestamps are used, duration 
        /// and date will be counted from these. If date is used, timestamp will be counted from 
        /// date.
        /// </summary>
        private void NormalizeTimesheetDataForDailyEntriesAndTimetracking(DataTree document, string collectionName)
        {
            if (collectionName == "timesheetentry" || collectionName == "absenceentry" || collectionName == "assetentry")
            {

				if (dailyTimesheetEntryMode)
				{
					entryLogger.LogDebug("Normalizing timesheet data (daily entry mode)");

					if (!document["date"].HasValue)
                    {
                        logger.LogError("Date value missing from timesheet entry when in daily timesheet mode.");
                        return;
                    }

                    if (!document["duration"].HasValue)
                    {
                        logger.LogError("Duration value missing from timesheet entry when in daily timesheet mode.");
                        return;
                    }

                    // Generate timestamps for daily entry mode based on date and duration
                    DateTime date = (DateTime)document["date"];
                    DateTime startTimestamp = new DateTime(date.Year, date.Month, date.Day, dateDefaultHour, 0, 0);
                    DateTime endTimestamp = startTimestamp.AddMilliseconds((double)document["duration"]);

                    document["starttimestamp"] = startTimestamp;
                    document["endtimestamp"] = endTimestamp;
                }
                else
                {
					entryLogger.LogDebug("Normalizing timesheet data (timesheet entry mode)");

					if (document["parent"][DBQuery.Id].HasValue)
					{
						entryLogger.LogDebug("Child timesheet entry is being created or updated. Find and update data from parent.");

						// This is a child entry. Get the parent
						DBDocument parent = DBDocument.FindOne("timesheetentry", document["parent"][DBQuery.Id]);

                        if (!document["duration"].HasValue)
                        {
                            logger.LogError("Duration value missing from timesheet entry detail.");
                            return;
                        }

                        // Get relevat data from parent.
                        document["date"] = parent["date"];

                        DateTime startTimestamp = (DateTime)parent["starttimestamp"];

                        document["starttimestamp"] = startTimestamp;
                        document["user"] = parent["user"];
                        document["project"] = parent["project"];
                        document["endtimestamp"] = startTimestamp.AddMilliseconds((int)document["duration"]);

                        entryLogger.LogDebug("Parent timesheet data updated.", "Date: " + parent["date"], "User: " + parent["user"], "Project: " + parent["project"]);

					}
					else
					{

						// This is a base entry. Generate date and duration for base entries
						DateTime startTimestamp = (DateTime)document["starttimestamp"];
						DateTime endTimestamp = (DateTime)document["endtimestamp"];

						TimeSpan duration = endTimestamp.Subtract(startTimestamp);
						document["duration"] = (decimal)duration.TotalMilliseconds;

						startTimestamp = startTimestamp.ToLocalTime();
						DateTime date = new DateTime(startTimestamp.Year, startTimestamp.Month, startTimestamp.Day, 0, 0, 0, DateTimeKind.Utc);
						document["date"] = date;

						entryLogger.LogDebug("Base timesheet entry is being created or updated. Added date and duration.", "Date: " + document["date"], "Duration: " + document["duration"]);

					}
				}
            }
			else if (collectionName == "allocationentry" && document["starttimestamp"].HasValue)
			{
				// Generate date and duration for allocation
				DateTime startTimestamp = (DateTime)document["starttimestamp"];

				// Duration is only sensible if there is endtimestamp
				if (document["endtimestamp"].HasValue)
				{
					DateTime endTimestamp = (DateTime)document["endtimestamp"];
					TimeSpan duration = endTimestamp.Subtract(startTimestamp);
					document["duration"] = (decimal)duration.TotalMilliseconds;
				}

				startTimestamp = startTimestamp.ToLocalTime();
				DateTime date = new DateTime(startTimestamp.Year, startTimestamp.Month, startTimestamp.Day, 0, 0, 0, DateTimeKind.Utc);
				document["date"] = date;

				entryLogger.LogDebug("Allocation is being created or updatd. Add date and duration.", "Date: " + document["date"], "Duration: " + document["duration"]);
			}
			else if (collectionName == "articleentry" && document["timestamp"].HasValue)
			{
				// Create date for article
				DateTime startTimestamp = (DateTime)document["timestamp"];
				startTimestamp = startTimestamp.ToLocalTime();
				DateTime date = new DateTime(startTimestamp.Year, startTimestamp.Month, startTimestamp.Day, 0, 0, 0, DateTimeKind.Utc);
				document["date"] = date;

				entryLogger.LogDebug("Article is being created or updatd. Add date.", "Date: " + document["date"]);
			}
		}

		/// <summary>
		/// If the updated document is a parent document for other documents, these documents must be updated too.
		/// </summary>
		/// <param name="document"></param>
		/// <param name="collectionName"></param>
		private void UpdateRelatedTimesheetEntries(DataTree document, string collectionName)
		{
			// Check preconditions for actually having child documents.
			if (timeTrackingMode &&
				collectionName == "timesheetentry" &&
				document[DBQuery.Id].HasValue &&
				!document["parent"][DBQuery.Id].HasValue)
			{
				// Update dates for all related timesheetentries (formerly details) if this is an update

				DBQuery childTimesheetEntries = new DBQuery("tro", "getchildtimesheets");
				childTimesheetEntries.AddParameter("parent", new ObjectId(document[DBQuery.Id]));

				DBResponse response = childTimesheetEntries.Find();

                foreach (DBDocument childEntry in response.FirstCollection)
                {
                    try
                    {
                        childEntry["date"] = document["date"];
                        childEntry["project"][DBQuery.Id] = document["project"][DBQuery.Id];
                        childEntry["user"][DBQuery.Id] = document["user"][DBQuery.Id];

                        DateTime startTimestamp = (DateTime)document["starttimestamp"];

                        childEntry["starttimestamp"] = startTimestamp;
                        childEntry["endtimestamp"] = startTimestamp.AddMilliseconds((int)childEntry["duration"]);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError("Failed to update related TSE.", ex, "Child: " + childEntry[DBQuery.Id], "Parent: " + document[DBQuery.Id]);
                    }
                }

                response.FirstCollection.UpdateDatabase(new DBCallProperties() { DisableEventFiring = true, SkipValidityChecks = true, SkipDefaultValues = true });
			}
		}

		private void CacheDataToTimesheetEntries(DataTree document, string collectionName)
        {
			if ((collectionName == "timesheetentry" || collectionName == "dayentry" || collectionName == "absenceentry" || collectionName == "articleentry")
				&& Runtime.CurrentActionCall.Action != "doapprovework"
				&& Runtime.CurrentActionCall.Action != "approveworkmanager")
			{
				entryLogger.LogDebug("Caching data to entry");

				// Set CLA contract and internal worker status for timesheet entries to 
				// enable querying based on these statuses. Do not trouble the backend when accepting entries.
				DataTree user = DBDocument.FindOne("user", document["user"][DBQuery.Id]);
                DataTree project = null;

                if (user == null)
				{
					entryLogger.LogWarning("User not found when caching entry data", "Collection: " + document[DBQuery.CollectionName], "Id: " + document[DBQuery.Id]);
					return;
				}

				if (document["project"].HasValue)
                    project = DBDocument.FindOne("project", document["project"]);

                if (!user["clacontract"].Empty && user["clacontract"].Count > 0)
                    document["clacontract"] = user["clacontract"];
                    
                document["internalworker"] = (bool)user["internalworker"];

                if (project != null)
                {
                    if (!project["profitcenter"].Empty && project["profitcenter"].Count > 0)
                        document["profitcenter"] = project["profitcenter"];

                    if (!project["projectmanager"].Empty && project["projectmanager"].Count > 0)
                        document["projectmanager"] = project["projectmanager"];

					entryLogger.LogDebug("Applying project data to entry", "Profitcenter: " + project["profitcenter"], "Project manager: " + project["projectmanager"]);
                }

                if (!user["profitcenter"].Empty && user["profitcenter"].Count > 0)
                    document["userprofitcenter"] = user["profitcenter"];
				else
					document["userprofitcenter"].Remove();


				if (!user["businessarea"].Empty && user["businessarea"].Count > 0)
					document["userbusinessarea"] = user["businessarea"];
				else
					document["userbusinessarea"].Remove();


				if (!user["functionalarea"].Empty && user["functionalarea"].Count > 0)
                    document["userfunctionalarea"] = user["functionalarea"];
				else
					document["userfunctionalarea"].Remove();

				entryLogger.LogDebug("Applying user data to entry", "Profitcenter: " + user["profitcenter"], "BusinessArea" + user["businessarea"], "Functional area" + user["functionalarea"]);

                // Cache whether entry counts towards regular hours
                if (document["timesheetentrydetailpaytype"].Exists 
                    && !timesheetUtils.GetPayTypesThatCountAsRegularHours().Result.Contains(document["timesheetentrydetailpaytype"][DBQuery.Id]))
                        document["countsasregularhours"] = false;
                else
                    document["countsasregularhours"] = true;
            }
            else if (collectionName == "assetentry")
            {
                if (document["project"].HasValue)
                {
                    DataTree project = DBDocument.FindOne("project", document["project"]);
                    if (project != null)
                    {
                        if (!project["profitcenter"].Empty && project["profitcenter"].Count > 0)
                            document["profitcenter"] = project["profitcenter"];

                        if (!project["projectmanager"].Empty && project["projectmanager"].Count > 0)
                            document["projectmanager"] = project["projectmanager"];
                    }
                }

                DataTree asset = DBDocument.FindOne("asset", document["asset"]);

                if (asset != null) {
                    if (!asset["profitcenter"].Empty && asset["profitcenter"].Count > 0)
                        document["assetprofitcenter"] = asset["profitcenter"];

					entryLogger.LogDebug("Caching asset entry profitcenter", "Asset profitcenter: " + asset["profitcenter"]);
                }
            }
        }

        public void AutoWorkerApproveEntriesForManagement(DataTree document)
        {
            string collectionName = document["__collectionname"];

            // Managers don't need to approve hours logged to others as their own.
            if (collectionName == "timesheetentry" || collectionName == "absenceentry" || collectionName == "dayentry" || collectionName == "articleentry")
            {
                if (document["user"].Exists && !string.IsNullOrEmpty(document["user"]) &&
				   Runtime.Security.IsCurrentUserMemberOfAccessGroup("autoapprovework"))
                {
                    if ((string)document["user"] != (string)Runtime.SessionManager.CurrentUser["_id"])
                    {
						entryLogger.LogDebug("Automatically worker approving manager's TSE", document[DBQuery.Id]);
                        document["approvedbyworker"] = true;
                    }
                }
            }
            else if (collectionName == "assetentry")
            {
                if (document["asset"].Exists && !string.IsNullOrEmpty(document["asset"]) &&
					Runtime.Security.IsCurrentUserMemberOfAccessGroup("autoapprovework"))
                {
                    document["approvedbyworker"] = true;
					entryLogger.LogDebug("Automatically worker approving manager's asset entry", document[DBQuery.Id]);
				}
			}
        }

		/// <summary>
		/// When creating booking for others the owner of document must be the target user. Creator remains the one
		/// who created the document;
		/// </summary>
		/// <param name="document"></param>
		/// <returns></returns>
		public void SetUserAsOwner(DataTree document)
		{
			string collectionName = document["__collectionname"];

			if (collectionName == "timesheetentry" || collectionName == "absenceentry" || collectionName == "dayentry" || collectionName == "articleentry")
			{
				if (document["user"].Exists && !string.IsNullOrEmpty(document["user"]))
					document["owner"] = document["user"];

				entryLogger.LogDebug("Setting user as owner of created or updated document", "User: " + document["user"]);
			}
		}

		public void MakeModificationsForCopyOperation(DataTree document)
		{
			if ((bool)document["__copyoperation"])
			{
				// If worker copies an entry it shouldnever be approved by default
				if ((int)Runtime.SessionManager.CurrentUser["level"] == 1)
					document["approvedbyworker"] = false;

				// If worker or manager copies an entry it should never be manager approved by default
				if ((int)Runtime.SessionManager.CurrentUser["level"] < 4)
					document["approvedbymanager"] = false;

				document["exported_ax"] = false;
				document["exportfailurecount_ax"] = 0;
				document["exporttimestamp_ax"].Remove();
				document["exported_visma"] = false;
				document["exporttimestamp_visma"].Remove();
			}
		}

		public void RemovingDocumentBefore(DataTree document, ref bool cancel)
        {
			entryLogger.LogDebug("Removing doucment: " + document[DBQuery.Id]);
            string collectionName = document["__collectionname"];

            DataTree documentToRemove = DBDocument.FindOne(collectionName, document[DBQuery.Id]);

            ValidatedDocument validatedDocument = new ValidatedDocument(documentToRemove, Runtime.Schema[0][collectionName]);
            validatedDocument.DataValid = true;
            ValidateNotAccepted(validatedDocument, collectionName);

			if (!validatedDocument.DataValid)
			{
				entryLogger.LogDebug("Not removing document because it has been accepted", "Collection: " + document[DBQuery.CollectionName], "Id: " + document[DBQuery.Id]);
				cancel = true;
			}
		}

		public void RemovingDocumentAfter(DataTree documentObject, Task<RCResponse> response)
		{
			TimesheetEntryDeleteCascade(documentObject, response);
		}

		public void TimesheetEntryDeleteCascade(DataTree documentObject, Task<RCResponse> response)
		{
			// Remove all childred of removed timesheetentry
			if (documentObject[DBQuery.CollectionName] == "timesheetentry" && (bool)Runtime.Config["application"]["features"]["timetracking"])
			{
				entryLogger.LogDebug("Removing childeren after parent entry has been removed. Parent: " + documentObject[DBQuery.Id]);

				var childEntriesQuery = new DBQuery("tro", "getchildtimesheets");
				childEntriesQuery.AddParameter("parent", new ObjectId(documentObject[DBQuery.Id]));

				DBCollection childEntries = childEntriesQuery.Find().FirstCollection;

				entryLogger.LogDebug("Removing childeren after parent entry has been removed. Children found: " + childEntries.Count);

				childEntries.RemoveFromDatabase(new DBCallProperties() { DisableEventFiring = true });
			}
		}

		#endregion

		#region Utils

		/// <summary>
		/// Get reference to base pay type that is used for base pay types in timetracking mode.
		/// </summary>
		/// <returns></returns>
		private DBDocument GetBasePay()
		{
			if (basePayReference != null)
				return basePayReference;

			lock (basePayReferenceLockObject)
			{
				var getBasePay = new DBQuery("tro", "getbasepay");
				basePayReference = getBasePay.FindOneAsync().Result;

				return basePayReference;
			}
		}

        /// <summary>
        /// Get reference to travel time pay type that is used for travel time pay types in timetracking mode.
        /// </summary>
        /// <returns></returns>
        private DBDocument GetTravelTimePayType()
        {
            if (travelTimeReference != null)
                return travelTimeReference;

            lock (travelTimeReferenceLockObject)
            {
                var getTravelTime = new DBQuery("tro", "gettraveltime");
                travelTimeReference = getTravelTime.FindOneAsync().Result;

                return travelTimeReference;
            }
        }

        private bool IsProjectActiveForEntry(DataTree entry)
        {
            DataTree project = DBDocument.FindOne("project", (string)entry["project"]);

            if (project != null &&
                project["status"] != ProjectStateInProgress &&
                (bool)Runtime.Config["application"]["features"]["resourcing"]["hidenotinprogress"])
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Timed Tasks

        private bool AutoApproveWork(ILogger logger, Runtime runtime, TimedTask task)
        {
            var autoApproveWork = new RCMessage("tro_autoapprovework");
            Runtime.SendRemoteMessage(autoApproveWork);
            return true;
        }

		public DataTree GetMonitorData()
		{
			var results = new DataTree();

			results["homescreenopened"] = homeScreenOpened;
			results["approveworkviewopened"] = approveWorkViewOpened;
			results["approveworkmanagerviewopened"] = approveWorkManagerViewOpened;
			results["hrviewopened"] = hrViewOpened;
			results["settingsviewopened"] = settingsViewOpened;
			results["managementviewopened"] = managementViewOpened;
			results["myplacesviewopened"] = myPlacesViewOpened;
			results["favouriteusersviewopened"] = favouriteusersViewOpened;

			results["timesheetentriesadded"] = timesheetEntriesAdded;
			results["absenceentriesadded"] = absenceEntriesAdded;
			results["expenseentriesadded"] = expenseEntriesAdded;
			results["articleentriesadded"] = articleEntriesAdded;
			results["assetentriesadded"] = assetEntriesAdded;

			results["workerapprovalrequests"] = workerApprovalRequests;
			results["managerapprovalrequests"] = managerApprovalRequests;
			results["managerfilterwork"] = managerFilterWork;

			return results;
		}

		#endregion
	}
}

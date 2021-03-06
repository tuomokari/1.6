﻿/**
 * Loads work data (TSEs, absences, articles...)
 */
$.widget("workdatawidget.workdatawidget", $.core.base, {
	options: {
		historyDateKey: ""
    },
    /**
     * Fired by jQuery when the widget is created.
     */
    _create: function () {
    	this._uiState = {};
    	this._model = {};
    	this._uiState.firstRender = true;
    	this._firstRefresh = true;
    },
	/**
	 * Fired by MC2 when widget is created. Implementation specific initialization code should
	 * sbe run here
	 */
    _initWidget: function () {

    	this._model.currentUserId = $("body").attr("data-user");

    	this._setupWidgetEvents();

    	this._getPersistedValues();

    	this._model.userClaContract = this.options.userclacontract;
    },
    _getPersistedValues: function()    
    {
    	// Default first to date from history and secondarily to current date
    	this._model.selectedDateKey = sessionStorage.getItem(this._model.currentUserId + this.element.attr("id") + "_selectedday");
    	if (!this._model.selectedDateKey)
    		this._model.selectedDateKey = this.getKeyFromDate(new Date());

    	// Default first to date from history and secondarily to current date
    	this._model.defaultProject = sessionStorage.getItem(this._model.currentUserId + "_defaultproject");

		// See if we have last entered item data and it's not the last seen item
    	if (this.options.lastnewid && this.options.lastnewid != sessionStorage.getItem(this._model.currentUserId + "_lastnewid") &&
    		this.options.lastnewcollectionname) {

			// Set item to session storage to prevent displaying it again
    		sessionStorage.setItem(this._model.currentUserId + "_lastnewid", this.options.lastnewid);
    		sessionStorage.setItem(this._model.currentUserId + "lastnewcollectionname", this.options.lastnewcollectionname);

			// Set item in model to enable showing it (one time).
    		this._model.lastShownNewId = sessionStorage.getItem($("body").attr("data-user") + "_lastnewid");
    		this._model.lastShownNewCollection = sessionStorage.getItem($("body").attr("data-user") + "lastnewcollectionname");
		}
    },
	/**
	 * Setup widget's events.
	 */
    _setupWidgetEvents: function () {
    	var base = this;
    },
    /**
     * Refresh all data in the model from the server.
     * 
     * @param options: configures how and what data should be acquired
	 *
     * @note: You should not do any dom manipulation here. All DOM manipulation needs
     *        to go to the render method.
     */
    refreshData: function (options) {
    	var base = this;
    	if (!options)
    		options = {};

    	if (options.startDate && options.endDate) {
    		this._model.startDate = options.startDate;
    		this._model.endDate = options.endDate;
    	}
    	else if (!this._model.startDate && !this._model.endDate) {
			// No dates in options or mode. This is likely the first (redundant) refresh.
    		return;
		}

    	this._model.days = {};
    	this._generateDaysForRange();
    	this._determineNextAndPreviousDay();

		// Clean up new document info for future updates
    	if (!this._firstRefresh) {
    		this._model.lastShownNewId = null;
    		this._model.lastShownNewCollection = null;
    		this._model.lastShownNewDocument = null;
    		this._model.lastShownNewDocumentDateKey = null;
		}

    	// Create an array of promises to wait before calling base and potentially rendering.
    	var promises = [];

    	var url = getAjaxUrl("workdatawidget", "getEntryData");
    	url = addParameterToUrl(url, "start", UTCISOString(base._model.startDate));
    	url = addParameterToUrl(url, "end", UTCISOString(base._model.endDate));

    	var deferred = new jQuery.Deferred();
    	var baseDataPromise = deferred.promise();

    	$.ajax({
    	    url: url,
    	    dataType: "json",
    	    success: function (data, status, xhr) {
    		    base._model.timesheetEntries = new Array();
    		    base._model.absenceEntries = new Array();
    		    base._model.dayEntries = new Array();
    		    base._model.assetEntries = new Array();
    		    base._model.allocationEntries = new Array();
    		    base._model.articleEntries = new Array();

    		    if (data.timesheetentry)
    			    base._model.timesheetEntries = data.timesheetentry;

    		    if (data.absenceentry)
    			    base._model.absenceEntries = data.absenceentry;

    		    if (data.dayentry)
    			    base._model.dayEntries = data.dayentry;

    		    if (data.articleentry)
    			    base._model.articleEntries = data.articleentry;

    		    if (data.allocationentry) {
    		        base._model.allocationEntries = data.allocationentry;
    		        base._model.allocationEntries.sort(base._allocationSortFunction);
                }
    		    if (data.assetentry)
    			    base._model.assetEntries = data.assetentry;

    		    base._model.timesheetEntriesById = {};
    		    base._model.absenceEntriesById = {};
    		    base._model.dayEntriesById = {};
    		    base._model.assetEntriesById = {};
    		    base._model.allocationEntriesById = {};
    		    base._model.articleEntriesById = {};

    		    base._addEntriesFromArrayToObject(base._model.timesheetEntries, base._model.timesheetEntriesById);
    		    base._addEntriesFromArrayToObject(base._model.absenceEntries, base._model.absenceEntriesById);
    		    base._addEntriesFromArrayToObject(base._model.dayEntries, base._model.dayEntriesById);
    		    base._addEntriesFromArrayToObject(base._model.assetEntries, base._model.assetEntriesById);
    		    base._addEntriesFromArrayToObject(base._model.allocationEntries, base._model.allocationEntriesById);
    		    base._addEntriesFromArrayToObject(base._model.articleEntries, base._model.articleEntriesById);
    		    deferred.resolve();
    	    }
    	});

    	var payTypesPromise = restApi.find("workdatawidget", "paytypes");

    	payTypesPromise.done(function (data) {
    		if (!data.timesheetentrydetailpaytype)
    			throw ("No paytypes found");

    		base._model.payTypes = data.timesheetentrydetailpaytype;

    		base._model.payTypesById = {};

    		for (var i = 0; i < base._model.payTypes.length; i++) {
    			var payType = base._model.payTypes[i];

    			base._model.payTypesById[payType._id] = payType;
    		}

    		base._model.dayEntryTypes = data.dayentrytype;

    		base._model.dayEntryTypesById = {};

    		for (var i = 0; i < base._model.dayEntryTypes.length; i++) {
    			var payType = base._model.dayEntryTypes[i];

    			base._model.dayEntryTypesById[payType._id] = payType;
    		}
    	});

    	promises.push(baseDataPromise);
    	promises.push(payTypesPromise);

    	$.when.apply($, promises).done(function () {

    		base._cacheDataById();

    		base._convertWorkDataDates();

    		base._splitWorkDataToDays();

    		base._addTimedEntries();

    		base._splitAssetDataForAssets();

    		base._countDailyTotals();

    		base._sortEntries();

    		base._findEntryToHighlight();

    		$(base.element).trigger("datarefreshed");

    		base._firstRefresh = false;
    	});
    },
    _addEntriesFromArrayToObject: function(documentArray, documentObject) {
    	for (var i = 0; i <documentArray.length; i++) {
    		var document = documentArray[i];

    		if (!document._id)
    			continue;

    		documentObject[document._id] = document;
    	}
    },
    /**
	 * Save information aobut next and previous day. If we are at start or end of date range
	 * next/prev date will contain null.
	 */
    _determineNextAndPreviousDay: function() {    	
    	var currentDate = this.getDateFromKey(this._model.selectedDateKey);
    	var nextDateKey = this.getKeyFromDate(new moment(currentDate).add(1, "days").toDate());
    	var prevDateKey = this.getKeyFromDate(new moment(currentDate).add(-1, "days").toDate());

    	this._model.nextDateKey = (this._model.days[nextDateKey])? nextDateKey : null;
    	this._model.prevDateKey = (this._model.days[prevDateKey])? prevDateKey : null;
    },
    _cacheDataById: function() {
		// Only cache what's needed. More types can be added here.
    	this._cacheCollectionById("allocationEntries");
	},
	_cacheCollectionById: function(collectionName) {
		var collection = this._model[collectionName];

		var collectionById = {};
		this._model[collectionName + "ById"] = collectionById;
		for (var i = 0; i < collection.length; i++) {
			var entry = collection[i];
			collectionById[entry._id] = entry;
		}
	},
	setCurrentDateAsCopied: function() {
		this._model.copiedDateKey = this._model.selectedDateKey;
	},
    setSelectedDate: function (date) {
    	this.setSelectedDateKey(getKeyFromDate(date));
    },
    setSelectedDateKey: function (dateKey) {
        this._model.selectedDateKey = dateKey;
        sessionStorage.setItem($("body").attr("data-user") + this.element.attr("id") + "_selectedday", dateKey);
        this._determineNextAndPreviousDay();
        $(this.element).trigger("selecteddatechanged", dateKey);
    },
    scrollHorizontalWorkView: function () {
        $(this.element).trigger("scrollrequest");
    },
    /*
	 * Get string representig date part of timestamp
	 */
    getKeyFromDate: function (date) {
        var yearPart = date.getFullYear().toString();
        var monthPart = (date.getMonth() + 1).toString(); // + 1 to take care of zero based month
        var dayPart = date.getDate().toString();

        // Pad to max number of digits
        return yearPart + (monthPart[1] ? monthPart : "0" + monthPart[0]) + (dayPart[1] ? dayPart : "0" + dayPart[0]);
    },
    getDateFromKey: function (key) {
    	var year = parseInt(key.substr(0, 4));
    	var month = parseInt(key.substr(4,2));
    	var date = parseInt(key.substr(6,2));

    	return new Date(year, month - 1, date); // - 1 to take care of zero based month
    },
	/*
	 * Start given allocation and trigger refresh.
	 */
    startProject: function(allocationId) {
    	this._model.allocationEntriesById[allocationId]["status"] = "In progress";
    	$(this.element).trigger("datarefreshed");
    },
	/*
	 * End given allocation and trigger refresh.
	 */
    endProject: function (allocationId) {
    	this._model.allocationEntriesById[allocationId]["status"] = "Done";
    	$(this.element).trigger("datarefreshed");
    },

	/*
	 * Converty ISO date strings to JS dates.
	 */
    _convertWorkDataDates: function () {
    	var base = this;
    	
    	base._convertDates(base._model.timesheetEntries)
    	base._convertDates(base._model.absenceEntries)
    	base._convertDates(base._model.dayEntries)
    	base._convertDates(base._model.assetEntries)
    	base._convertDates(base._model.articleEntries)
    	base._convertDates(base._model.allocationEntries)
    },
	/*
	 * Converty ISO date strings to JS dates.
	 */
	_convertDates: function(dateEntryArray) {
		for (var i = 0; i < dateEntryArray.length; i++) {
			var entry = dateEntryArray[i];

			if (entry.date)
				entry.date = getDateFromIsoString(entry.date);

			if (entry.starttimestamp)
				entry.starttimestamp = getDateFromIsoString(entry.starttimestamp);

			if (entry.endtimestamp)
				entry.endtimestamp = getDateFromIsoString(entry.endtimestamp);

			if (entry.created)
				entry.created = getDateFromIsoString(entry.created);
		}
	},
	/*
	 * Prefill day data with empty dates to provide consistenst data in case no entries are
	 * present for given day
	 */
	_generateDaysForRange: function() {
		var base = this;
		var current = moment(base._model.startDate);
		var end = moment(base._model.endDate);

		var todaysKey = this.getKeyFromDate(new Date());

		while (current.diff(end) <= 0) {
			current = current.add(1, "days");
			
			// Initialize day.
			var day = {};
			day.date = new Date(current.toDate().getTime());
			day.totalWorkHours = 0;
			day.totalAbsenceHours = 0;
			day.totalAllHours = 0;
			day.overtime50Hours = 0;
			day.overtime100Hours = 0;
			day.overtime150Hours = 0;
			day.overtime200Hours = 0;
			day.timesheetEntries = new Array();
			day.allocationEntries = new Array();
			day.absenceEntries = new Array();
			day.timedEntries = new Array();
			day.dayEntries = new Array();
			day.articleEntries = new Array();
			day.assetEntries = new Array();
			day.projects = new Array();
			day.projectsObject = {};
			day.extras = new Array();
			day.extrasById = {};
			day.key = base.getKeyFromDate(current.toDate());
			day.hasUnapprovedEntries = false;
			day.hasUnapprovedCurrentUserCreatedEntries = false;
			day.hasEntries = false;
			day.assets = new Array();
			day.assetsById = {};
			
			if (day.key === todaysKey)
				day.isToday = true;

			base._model.days[day.key] = day;
		}

	},
    _splitWorkDataToDays: function() {
    	this._splitDataWithDatesToDays(this._model.timesheetEntries, "timesheetEntries");
    	this._splitDataWithDatesToDays(this._model.absenceEntries, "absenceEntries");
    	this._splitDataWithDatesToDays(this._model.dayEntries, "dayEntries");
    	this._splitDataWithDatesToDays(this._model.articleEntries, "articleEntries");
    	this._splitDataWithDatesToDays(this._model.assetEntries, "assetEntries");
    	this._splitDataWithDatesToDays(this._model.allocationEntries, "allocationEntries");
    },
	/**
	 * Get entries and arrange them to dates.
	 */
    _splitDataWithDatesToDays: function(entries, type) {

    	for(var i = 0; i < entries.length; i++) {
    		var entry = entries[i];
    		if (!entry.date)
    			continue;

    		var key = this.getKeyFromDate(entry.date);

    		var day = this._model.days[key];
    		day[type].push(entry);
    	}
    },
    _addTimedEntries: function () {
    	var days = this._getDaysArray();

    	for (var i = 0; i < days.length; i++) {
    		var day = days[i];

    		// Timesheet entries
    		for (var j = 0; j < day.timesheetEntries.length; j++) {
    			day.timedEntries.push(day.timesheetEntries[j])
    		}

    		// Absence entries
    		for (var j = 0; j < day.absenceEntries.length; j++) {
    			day.timedEntries.push(day.absenceEntries[j])
    		}
    	}
    },
    _splitAssetDataForAssets: function() {
    	var days = this._getDaysArray();

    	for (var i = 0; i < days.length; i++) {
    		var day = days[i];

    		for (var j = 0; j < day.assetEntries.length; j++) {
    			var assetEntry = day.assetEntries[j];

    			if (!assetEntry.asset)
    				continue;

    			if (!day.assetsById[assetEntry.asset._id]) {
    				var asset = {};
    				asset.totalHours = 0;
    				asset._id = assetEntry.asset._id;
    				asset.__displayname = assetEntry.asset.__displayname;
    				asset.hours = new Array();
    				day.assetsById[assetEntry.asset._id] = asset;
    				day.assets.push(asset);
    			}

    			var asset = day.assetsById[assetEntry.asset._id];
    			asset.hours.push(assetEntry);
    		}

    		// Sort hours for each asset per day
    		for (var j = 0; j < day.assets.length; j++) {
    			var asset = day.assets[j];
    			asset.hours.sort(this._timestampSortFunction);
    		}
    	}
    },
    _sortEntries: function () {
    	var days = this._getDaysArray();

    	for (var i = 0; i < days.length; i++) {
    		var day = days[i];

			this._sortTimedEntries(day);
	    	this._sortAllocations(day);
    	}
    },
    _findEntryToHighlight: function() {
    	if (this._model.lastShownNewCollection == "timesheetentry") {
    		this._model.lastShownNewDocument = this._model.timesheetEntriesById[this._model.lastShownNewId];
    	} else if (this._model.lastShownNewCollection == "dayentry") {
    		this._model.lastShownNewDocument = this._model.dayEntriesById[this._model.lastShownNewId];
    	} else if (this._model.lastShownNewCollection == "articleentry") {
    		this._model.lastShownNewDocument = this._model.articleEntriesById[this._model.lastShownNewId];
    	} else if (this._model.lastShownNewCollection == "absenceentry") {
    		this._model.lastShownNewDocument = this._model.absenceEntriesById[this._model.lastShownNewId];
    	} else if (this._model.lastShownNewCollection == "assetentry") {
    		this._model.lastShownNewDocument = this._model.assetEntriesById[this._model.lastShownNewId];
    	}

    	if (this._model.lastShownNewDocument) {
    		this._model.lastShownNewDocumentDateKey = this.getKeyFromDate(this._model.lastShownNewDocument.date);
    	}
    },
    _timestampSortFunction: function (a, b) {
        if (!a.starttimestamp && !b.starttimestamp)
            return 0;

        if (!a.starttimestamp)
            return -1;

        if (!b.starttimestamp)
            return 1;

        if (a.starttimestamp == b.starttimestamp)
            return 0;

        return (a.starttimestamp < b.starttimestamp) ? -1 : 1;
    },
    _allocationSortFunction: function (a, b) {
        // Sort primarily by project due date and secondarily by identifier.
        if (a.project.duedate || b.project.duedate)
        {
            if (!a.project.duedate)
                return 1;

            if (!b.project.duedate)
                return -1;
        }

        if (a.project.duedate == b.project.duedate) {
            var idA = parseInt(a.project.identifier)
            var idB = parseInt(b.project.identifier)
            if (!idA)
                return -1;

            if (!idB)
                return 1;

            if (idA == idB)
                return 0

            return (idA < idB) ? -1 : 1;
        }

        return (a.project.duedate < b.project.duedate) ? -1 : 1;
    },
    _endtimeSortFunction: function (a, b) {
        if (a.endtimestamp) {
            if (b.endtimestamp) {
                if (a.endtimestamp != b.endtimestamp) {
                    return (a.endtimestamp < b.endtimestamp) ? -1 : 1;
                }
            } else {
                return -1;
            }
        } else if (b.endtimestamp) {
            return 1;
        }

        if (!a.project.__displayname && !b.project.__displayname)
            return 0;

        if (!a.project.__displayname)
            return -1;

        if (!b.project.__displayname)
            return 1;

        if (a.project.__displayname == b.project.__displayname)
            return 0;

        return (a.project.__displayname < b.project.__displayname) ? -1 : 1;
    },
    _sortTimedEntries: function (day) {
    	day.timedEntries.sort(this._timestampSortFunction);
    },
    _sortAllocations: function (day) {
    	day.allocationEntries.sort(this._allocationSortFunction);
    },
	/**
	 * Count total values per day.
	 */
    _countDailyTotals: function () {

    	var days = this._getDaysArray();

    	for (var i = 0; i < days.length; i++) {
    		var day = days[i];

			// timesheet entries
    		for (var j = 0; j < day.timesheetEntries.length; j++)
    		{
    		    day.hasEntries = true;

    			var timesheetEntry = day.timesheetEntries[j];

    			if (!timesheetEntry.timesheetentrydetailpaytype) {
    				console.log("Paytype missing from TSE " + timesheetEntry._id);
    				continue;
    			}

    			var durationHours = this._millisecondsToHours(timesheetEntry.duration);
    			var payType = this._model.payTypesById[timesheetEntry.timesheetentrydetailpaytype._id];

    			if (features.timetracking) {
    				if (!timesheetEntry.parent)
    				{
    					day.totalWorkHours += durationHours;
    					day.totalAllHours += durationHours;
    				}
    			} else {
    				if (payType.countsasregularhours) {
    					day.totalWorkHours += durationHours;
    					day.totalAllHours += durationHours;
    				}
    			}

    			if (payType.isovertime50)
    				day.overtime50Hours += durationHours;
    			else if (payType.isovertime100)
    				day.overtime100Hours += durationHours;
    			else if (payType.isovertime150)
    				day.overtime150Hours += durationHours;
    			else if (payType.isovertime200)
    				day.overtime200Hours += durationHours;

    			// Abbreviations and icons for paytypes can be used to see what has been done today at a glance.
				// Prefer icon if it's present
    			if (payType.icon) {
    				var icon = "[icon]" + payType.icon

    				// Do not add icon for overtime
    				if (!day.extrasById[icon] && icon != "[icon]schedule") {
    					day.extras.push(icon);
    					day.extrasById[icon] = true;
    				}
				}
    			else if (payType.abbreviation) {

    				if (!day.extrasById[payType.abbreviation]) {
    					day.extras.push(payType.abbreviation);
    					day.extrasById[payType.abbreviation] = true;
    				}
    			}

    			if (!timesheetEntry.approvedbyworker) {
    				day.hasUnapprovedEntries = true;
    				if (timesheetEntry.creator._id == this._model.currentUserId)
    					day.hasUnapprovedCurrentUserCreatedEntries = true;
				}
    			else {
    				day.hasApprovedEntries = true;
    			}
			}

    		// absence entries
    		for (var j = 0; j < day.absenceEntries.length; j++) {

    		    day.hasEntries = true;

    			var absenceEntry = day.absenceEntries[j];

    			var durationHours = this._millisecondsToHours(absenceEntry.duration);
    			day.totalAllHours += durationHours;
    			day.totalAbsenceHours += durationHours;

    			if (!absenceEntry.approvedbyworker) {
    				day.hasUnapprovedEntries = true;
    				if (absenceEntry.creator._id == this._model.currentUserId)
    					day.hasUnapprovedCurrentUserCreatedEntries = true;
				}
    			else {
    				day.hasApprovedEntries = true;
    			}
			}

    		// article entries
    		for (var j = 0; j < day.articleEntries.length; j++) {

    		    day.hasEntries = true;

    			var articleEntry = day.articleEntries[j];

    			if (!articleEntry.approvedbyworker) {
    				day.hasUnapprovedEntries = true;
    				if (articleEntry.creator._id == this._model.currentUserId)
    					day.hasUnapprovedCurrentUserCreatedEntries = true;
				}
    			else {
    				day.hasApprovedEntries = true;
    			}
			}

			// asset entries
    		for (var j = 0; j < day.assets.length; j++) {

    		    day.hasEntries = true;

    			var asset = day.assets[j];

    			for (var k = 0; k < asset.hours.length; k++) {
    				var assetEntry = asset.hours[k];

    				var durationHours = this._millisecondsToHours(assetEntry.duration);
    				asset.totalHours += durationHours;

    				if (!assetEntry.approvedbyworker) {
    					day.hasUnapprovedEntries = true;
    					if (assetEntry.creator._id == this._model.currentUserId)
    						day.hasUnapprovedCurrentUserCreatedEntries = true;
					}
    				else {
    					day.hasApprovedEntries = true;
    				}
				}
    		}


    		// expense entries
    		for (var j = 0; j < day.dayEntries.length; j++) {

    		    day.hasEntries = true;

    			var dayEntry = day.dayEntries[j];

    			if (!dayEntry.approvedbyworker) {
    				day.hasUnapprovedEntries = true;
    				if (dayEntry.creator._id == this._model.currentUserId)
    					day.hasUnapprovedCurrentUserCreatedEntries = true;
				}
    			else {
    				day.hasApprovedEntries = true;
    			}

    			var payType = this._model.dayEntryTypesById[dayEntry.dayentrytype._id];

    			if (payType.icon) {
    				var icon = "[icon]" + payType.icon
    				if (!day.extrasById[icon]) {
    					day.extras.push(icon);
    					day.extrasById[icon] = true;
    				}
    			}
    			else if (payType.abbreviation) {

    				if (!day.extrasById[payType.abbreviation]) {
    					day.extras.push(payType.abbreviation);
    					day.extrasById[payType.abbreviation] = true;
    				}
    			}
			}
		}
    },
	/**
	 * Split entries for project by day
	 */
    _splitEntriesForProjects: function (day) {
    	var days = this._getDaysArray();

    	for (var i = 0; i < days.length; i++) {
    		var day = days[i];

    		// timesheet entries
    		for (var j = 0; j < day.timesheetEntries.length; j++) {
    			var timesheetEntry = day.timesheetEntries[j];

    			if (!timesheetEntry.project)
    				continue;

    			this._verifyProjectExists(day, timesheetEntry.project._id)
    			day.projectsObject[timesheetEntry.project._id].timesheetEntries.push(timesheetEntry);
    		}

    		// expense entries
    		for (var j = 0; j < day.dayEntries.length; j++) {
    			var dayEntry = day.dayEntries[j];
    			this._verifyProjectExists(day, dayEntry.project._id)
    			day.projectsObject[dayEntry.project._id].dayEntries.push(dayEntry);
			}

			// article entries
    		for (var j = 0; j < day.articleEntries.length; j++) {
    			var articleEntry = day.articleEntries[j];
    			this._verifyProjectExists(day, articleEntry.project._id)
    			day.projectsObject[articleEntry.project._id].articleEntries.push(articleEntry);
			}

    		// asset entries
    		for (var j = 0; j < day.assetEntries.length; j++) {
    			var assetEntry = day.assetEntries[j];
    			this._verifyProjectExists(day, assetEntry.project._id)
    			day.projectsObject[assetEntry.project._id].assetEntries.push(assetEntry);
			}
    	}
    },
    _verifyProjectExists: function(day, id) {
    	if (!day.projectsObject[id]) {
		
    		var project = {};
    		project.timesheetEntries = new Array();
    		project.dayEntries = new Array();
    		project.articleEntries = new Array();
    		project.assetEntries = new Array();
    		day.projectsObject[id] = project;
    		day.projects.push(project);
		}
    },
    /**
    * Render data in the model and settings into view.
    * 
    * @note: should not make any changes into model or settings
    * *
    * @param options: configures how data should be rendered
    */
    render: function (options) {
    	if (!options) options = {};

    	if (this._uiState.firstRender) {
			// Code run only once when the widget is rendered for the first time.
    		this.element.removeClass("__hidden");
    		this._uiState.firstRender = false;
    	}
    },
	/*
	 * Get daily work data as an array
	 */
    _getDaysArray: function () {
    	var base = this;
    	var current = moment(base._model.startDate);
    	var end = moment(base._model.endDate);

    	var ret = new Array();

    	while (current.diff(end) < 0) {
    		currentDay = current.add("days", 1);

    		var day = base._model.days[base.getKeyFromDate(currentDay.toDate())];

    		ret.push(day);
    	}

		// Todo: cache value
    	return ret;
    },
    _millisecondsToHours: function (milliseconds) {
    	if (!milliseconds)
    		return 0;

    	return milliseconds / (3600000);
    }
});
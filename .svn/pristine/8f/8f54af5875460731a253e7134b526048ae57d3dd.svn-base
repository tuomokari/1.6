/**
 * capacityutilizationwidget widget
 */
$.widget("capacityutilizationwidget.capacityutilizationwidget", $.core.base, {
    options: {
    },

    /**
     * Fired by jQuery when the widget is created.
     */
    _create: function () {

        this._uiState = new Object;
        this._uiState.firstRender = true;
        this._uiState.viewType = "future";

        this._model = new Object();

        this._model.currentDate = moment(newDate());
        this._model.allocations = new Array();
        this._model.users = {};
        this._model.usersArray = new Array();
        this._model.timesheetEntries = new Array();
        this._model.absenceEntries = new Array();
    },
    _setupWidgetEvents: function () {
    	var base = this;
    	base.element.find("input[type=radio][name='capacityutilizationwidget_viewtype']").change(function () {
    		clearTimeout(base._uiState.uiTimeoutToken);
    		base._uiState.lastAction = null;

    		base._uiState.viewType = this.value;
    		base.refreshData(true);
    	});
    },
    /**
     * Refresh all data in the model from the server.
     * 
     * @param (bool) renderData True if data should be rendered after refreshing it.
     * 
     * @note: You should not do any dom manipulation here. All DOM manipulation needs
     *        to go to the render method.
     */
    refreshData: function (renderData) {

    	console.log("Refresing data (dailyresourcing)");

    	// Create an array of promises to wait before calling base and potentially rendering.
    	var promises = [];

    	var base = this;

    	if (!base._model.selectedProfitCenter) {
    		// No valid selection is possible for empty profitcenter. clear everything and skip sending request.
    		this._model.calendarEntries = new Array();
    		this.render();
    		return;
    	}

    	var rangeStart, rangeEnd;

    	if (this._uiState.viewType == "future") {

    		rangeStart = moment(this._model.currentDate);
    		rangeEnd = moment(rangeStart).add(this.options.numberofdays, 'days');

    	} else if (this._uiState.viewType == "past") {

    		rangeEnd = moment(this._model.currentDate);
    		rangeStart = moment(rangeStart).add(-this.options.numberofdays, 'days');
    	}

    	// Create an array of promises to wait before calling base and potentially rendering.
    	var promises = [];

    	var restApi = new RestApi();

    	if (this._uiState.viewType == "future") {
    		var futureCapacityUtilizationsPromise = restApi.find("capacityutilizationwidget", "allocationsbyuserprofitcenter",
			{
				start: rangeStart.toDate(),
				end: rangeEnd.toDate(),
				profitcenter: base._model.selectedProfitCenter,
			});

    		futureCapacityUtilizationsPromise.done(function (data) {
    			if (data.allocationentry) {
    				base._model.allocations = data.allocationentry;
    			}
    			else {
    				base._model.allocations.length = 0;
    			}

    			base.render({ noitemlistupdate: true });
    		});

    		promises.push(futureCapacityUtilizationsPromise);
    	} else if (this._uiState.viewType == "past") {
    		var workHistoryPromise = restApi.find("capacityutilizationwidget", "workhistory",
			{
				start: rangeStart.toDate(),
				end: rangeEnd.toDate(),
				profitcenter: base._model.selectedProfitCenter
			});

    		workHistoryPromise.done(function (data) {
    			if (data.timesheetentry) {
    				base._model.timesheetEntries = data.timesheetentry;
    			}
    			else {
    				base._model.timesheetEntries.length = 0;
    			}

    			if (data.absenceentry) {
    				base._model.absenceEntries = data.absenceentry;
    			}
    			else {
    				base._model.absenceEntries.length = 0;
    			}

    			base.render({ noitemlistupdate: true });
    		});

    		promises.push(workHistoryPromise);
    	}

    	var usersPromise = restApi.find("capacityutilizationwidget", "usersforprofitcenter",
		{
			profitcenter: base._model.selectedProfitCenter
		});

    	usersPromise.done(function (data) {
    		base._model.users = {};

    		if (!data.user) {
    			base._model.usersArray.length = 0;
    			return;
    		}
			
    		base._model.usersArray = data.user;

    		for (var i = 0; i < base._model.usersArray.length; i++) {
    			var user = base._model.usersArray[i];
    			base._model.users[user._id] = user;
    		}
    	});

    	promises.push(usersPromise);

		var base = this;

		$.when.apply($, promises).done(function () {
			if (base._uiState.viewType == "future")
				base._processAllocations();
			else if (base._uiState.viewType == "past")
				base._processEntries();

        	if (renderData)
        		base.render();
        });
    },
    _initWidget: function () {
    	this._setupWidgetEvents();
    	this._model.selectedProfitCenter = this.options.userprofitcenter;
    },
    _processEntries: function() {
    	var users = this._model.users;
    	var usersArray = this._model.usersArray;

    	var timesheetEntries = this._model.timesheetEntries;
    	var absenceEntries = this._model.absenceEntries;

    	for (var i = timesheetEntries.length - 1; i >= 0; i--) {

    		var timesheetEntry = timesheetEntries[i];

    		if (!timesheetEntry.user || !timesheetEntry.date || !users[timesheetEntry.user._id])
    			continue;

    		var user = users[timesheetEntry.user._id];

    		var dateString = this._getDateStringFromDate(getDateFromIsoString(allocation.date));

    		if (!users[user._id][dateString])
    			users[user._id][dateString] = { total: 0 };

    		var duration = Math.round(timesheetEntry.duration / 1000 / 6 / 6) / 100; // from milliseconds to hours with two decimals
    		users[user._id][dateString].total = users[user._id][dateString].total + duration;
    	}
    },
    _processAllocations: function() {
    	var users = this._model.users;
    	var usersArray = this._model.usersArray;

    	var allocations = this._model.allocations;

    	for (var i = allocations.length - 1; i >= 0; i--) {

    		var allocation = allocations[i];

    		if (!allocation.user || !allocation.date || !users[allocation.user._id])
    			continue;

    		var user = users[allocation.user._id];

			var dateString = this._getDateStringFromDate(getDateFromIsoString(allocation.date));

			if (!users[user._id][dateString])
				users[user._id][dateString] = { total: 0 };

    		var duration = Math.round(allocation.duration / 1000 / 6 / 6) / 100; // from milliseconds to hours with two decimals
    		users[user._id][dateString].total = users[user._id][dateString].total + duration;
    	}
    },
    /**
    * Render data in the model and settings into view.
    * 
    * @note: should not make any changes into model or settings
    */
    render: function () {
    	var days = this.options.numberofdays;

    	if (this._uiState.firstRender) {
    		this.element.removeClass("__hidden");
    	}

    	if (this._uiState.viewType == "future") {
    		this._renderFutureData();
    	}
    	else if (this._uiState.viewType == "past") {
    		this._renderPastData();
    	}
    },
    _renderFutureData: function() {
    	var days = this.options.numberofdays;

    	var data = new Array();
    	// Add table headers
    	for (var i = 0; i < days; i++) {

    		var date = new moment(this._model.currentDate).add(i, "days");

    		data.push("<th>" + date.format(txt("daymonthformat", "capacityutilizationwidget")) + "<th>");
    	}

    	var header = $(this.element).find(".utilizationtableheader");
    	header.html("").html("<th>" + txt("users", "capacityutilizationwidget") + "</th>" + data.join(""));

    	var usersArray = this._model.usersArray;
    	var body = $(this.element).find(".utilizationtablebody")

    	var data = new Array();

    	for (var i = 0; i < usersArray.length; i++) {
    		// Add rows for users
    		var user = usersArray[i];

    		data.push("<tr><td>" + user.firstname + " " + user.lastname + "</td>");

    		for (var j = 0; j < days; j++) {

    			var date = new moment(this._model.currentDate).add(j, "days");

    			var total = 0;
    			if (this._model.users[user._id][this._getDateStringFromDate(date.toDate())])
    				total = this._model.users[user._id][this._getDateStringFromDate(date.toDate())].total;

    			data.push("<td " + this._getClassForHours(user, total) + ">" + total + "<td>");
    		}

    		data.push("</tr>");
    	}

    	body.html("").html(data.join(""));
    },
    _renderPastData: function() {
    	var days = this.options.numberofdays;

    	var data = new Array();
    	// Add table headers
    	for (var i = days; i >=0 ; i--) {

    		var date = new moment(this._model.currentDate).add(-i, "days");

    		data.push("<th>" + date.format(txt("daymonthformat", "capacityutilizationwidget")) + "<th>");
    	}

    	var header = $(this.element).find(".utilizationtableheader");
    	header.html("").html("<th>" + txt("users", "capacityutilizationwidget") + "</th>" + data.join(""));

    	var usersArray = this._model.usersArray;
    	var body = $(this.element).find(".utilizationtablebody")

    	var data = new Array();

    	for (var i = 0; i < usersArray.length; i++) {
    		// Add rows for users
    		var user = usersArray[i];

    		data.push("<tr><td>" + user.firstname + " " + user.lastname + "</td>");

    		for (var j = 0; j < days; j++) {

    			var date = new moment(this._model.currentDate).add(j, "days");

    			var total = 0;
    			if (this._model.users[user._id][this._getDateStringFromDate(date.toDate())])
    				total = this._model.users[user._id][this._getDateStringFromDate(date.toDate())].total;

    			data.push("<td " + this._getClassForHours(user, total) + ">" + total + "<td>");
    		}

    		data.push("</tr>");
    	}

    	body.html("").html(data.join(""));
    },
	/**
	 * Return a date string from index of days from now to past or future
	 */
    _getDayWithIndex: function (index) {
    	var date = this._model.currentDate;
    },
	/**
	 * Return the date part of datetime as string
	 */
    _getDateStringFromDate: function(date) {
    	return date.getDate() + "." + date.getMonth() + "." + date.getFullYear();
    },
    _getClassForHours: function(user, total) {
    	var classHtml = "";

    	var completedlimit = this.options.completedlimit;
    	var overlimit = this.options.overlimit;

    	if (total > 0) {
    		if (total >= completedlimit) {
    			if (total <= overlimit) {
    				// Ok
    				classHtml = "style='background-color:green'";
    			} else {
					// Overtime
    				classHtml = "style='background-color:red'";
    			}

    		} else {
				// Some work but not full
    			classHtml = "style='background-color:orange'";
    		}
    	} else {
			// Total
    	}

    	return classHtml;
    }



});
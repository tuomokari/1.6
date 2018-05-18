/**
 * resourcingcalendarwidget widget
 */
$.widget("resourcingcalendarwidget.resourcingcalendarwidget", $.core.base, {
    options: {
    },

    /**
     * Fired by jQuery when the widget is created.
     */
    _create: function () {

        this._uiState = new Object;
        this._model = new Object();

		// Calendar entries are either allocations or projects
        this._model.calendarEntries = new Array();

		// Allocations of projects shown with current filtering
        this._model.filteredEntries = new Array();

    	// User entries. Filled based on allocations. Only has values when showing allocations.
        this._model.users = new Array();

    	// Projects. Filled based on allocations. Only has values when showing allocations.
        this._model.projects = new Array();

        this._model.currentDate = moment(newDate());

        this._uiState.firstRender = true;
        this._uiState.calendarRendered = false;
        this._uiState.showProjects = false;
        this._uiState.shownEntries = {};
        this._uiState.viewType = "projects";
        this._uiState.eventsFetched = false;
        this._setupWidgetEvents();
    },
    _initWidget: function () {
    	this._model.selectedProfitCenter = this.options.userprofitcenter;
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

    	console.log("Refresing data (resourcingcalendar)");

		// We don't immediately have any valid selection and don't need to get any data.
    	if (this._uiState.firstRender) {
    		this.render()
    		return;
    	}

    	// Create an array of promises to wait before calling base and potentially rendering.
        var promises = [];

		var base = this;

		if (!base._model.selectedProfitCenter) {
			// No valid selection is possible for empty profitcenter. clear everything and skip sending request.
			this._model.calendarEntries = new Array();
			this.render();
			return;
		}

		var monthStart = moment(this._model.currentDate).startOf("month");
		var monthEnd = moment(this._model.currentDate).endOf("month");


		if (this._uiState.viewType == "projects")
			queryName = "projectsbyprofitcenter";
		else
			queryName = "allocationsbyuserprofitcenter";

		var restApi = new RestApi();
		var calendarEntriesPromise = restApi.find("resourcingcalendarwidget", queryName,
		{
			start: monthStart.toDate(),
			end: monthEnd.toDate(),
			profitcenter: base._model.selectedProfitCenter,
		});

		calendarEntriesPromise.done(function (data) {
			if (data.allocationentry) {
				base._model.calendarEntries = data.allocationentry;
			} else if (data.project) {
				base._model.calendarEntries = data.project;
			}
			else
			{
				base._model.calendarEntries.length = 0;
			}

			base._processCalendarEntries();
			base._extractUsersAndProjectsFromData();
			base.render({ nocalendarupdate: true });

			window.setTimeout(function () {
				base.render({ noitemlistupdate: true });
			}, 0);
		});


    },
    _setupWidgetEvents: function () {

    	var base = this;

    	$(window).on("resize", function () {
    	    base.render();
    	});

    	base.element.find("select[name='resourcingcalendarwidget_profitcenter']").on("dropdownupdated", function () {
    		$(this).val(base._model.selectedProfitCenter);

    		// First data refresh when profit center is available.
    		base.refreshData(true);
    	});

    	base.element.find("select[name='resourcingcalendarwidget_profitcenter']").on("change", function (e) {
    		base._model.selectedProfitCenter = $(this).val();

    		console.log("resourcingcalendarwidget_profitcenter §§§");

    		// Clean all selections when changing profit center.
    		base._uiState.shownEntries = {};
    		base.refreshData(true);

    	    // also change dailyresourcing's profit center todo: revise using widget communications, unless triggered automatically
    		if (e.originalEvent !== undefined)
        		$(".dailyresourcingsetup select").val($(this).val()).trigger("change");

    	});

    	base.element.find('input[type=radio][name=resourcingcalendarwidget_viewtype]').change(function () {
    		clearTimeout(base._uiState.uiTimeoutToken);
    		base._uiState.lastAction = null;

    		var previousViewType = base._uiState.viewType;
    		base._uiState.viewType = this.value;

    		if (this.value == 'projects') {

    			base._uiState.viewType = this.value;
    			base.refreshData(true);
    		}
    		else { 
				// allocations by project or user use the same data
    			if (previousViewType == "projects")				
    				base.refreshData(true);
    			else
    				base.render();
    		}
    	});

    	var refreshDelay = 300;

    	base.element.find(".resourcingcalendarwidget_nextbutton").on("click", function () {
    		base._performFullCalendarAction("next", refreshDelay);
    	});

    	base.element.find(".resourcingcalendarwidget_previousbutton").on("click", function () {
    		base._performFullCalendarAction("prev", refreshDelay);
    	});

    	base.element.find(".resourcingcalendarwidget_todaybutton").on("click", function () {
    		base._performFullCalendarAction("today", refreshDelay);
    	});
    },
	/*
	 * Performs an action on fullcalendar with a delay. Does not stack calls and delays
	 * calls to UI. If same call is made multiple times calling delay is not
	 * reset. If different calls are made only last one is accepted and delay is reset
	 * every time.
	 */
    _performFullCalendarAction: function (action, refreshDelay) {
    	var base = this;

    	if (base._uiState.lastAction != action)
    		clearTimeout(base._uiState.uiTimeoutToken);
    	else
    		return;

    	base._uiState.lastAction = action;

    	base._uiState.uiTimeoutToken = setTimeout(function () {

    		if (action == "next") {
    			base._model.currentDate.add(1, 'months');
    		}
    		else if (action == "prev") {
    			base._model.currentDate.add(-1, 'months');
    		}
    		else if (action == "today") {
    			base._model.currentDate = moment(newDate());
    		}

    		base.refreshData(true);
    		base._uiState.lastAction = null;
    	}, refreshDelay);
    },
    _extractUsersAndProjectsFromData: function() {
    	this._model.projects = new Array();
    	this._model.users = new Array();

    	if (this._uiState.viewType == "project")
    		return;

    	var calendarEntries = this._model.calendarEntries;
		
    	var addedUsers = {};
    	var addedProjects = {};

    	for (var i = calendarEntries.length - 1; i >= 0; i--) {
    		var calendarEntry = calendarEntries[i];
			
    		if (calendarEntry.user && !addedUsers[calendarEntry.user._id]) {
    			addedUsers[calendarEntry.user._id] = true;
    			this._model.users.push(calendarEntry.user);
    		}

    		if (calendarEntry.project && !addedProjects[calendarEntry.project._id]) {
    			addedProjects[calendarEntry.project._id] = true;
    			this._model.projects.push(calendarEntry.project);
    		}
    	}
    },
	/**
	 * Modify calendarEntries to a format understood by fullcalenda
	 */
    _processCalendarEntries: function () {
    	var base = this;
    	var calendarEntries = this._model.calendarEntries;

    	var maxItemsToDisplayAtOnce = 10;
    	var showAllItemsImmediately = (calendarEntries.length <= maxItemsToDisplayAtOnce);

    	for (var i = calendarEntries.length - 1; i >= 0; i--) {
    		var calendarEntry = calendarEntries[i];

    		calendarEntry.enabled = false;

    		try
    		{
				// Show different names for different view types.
    			if (base._uiState.viewType == "projects") {

    				calendarEntry.title = calendarEntry.name;

    			} else if (base._uiState.viewType == "allocationsforusers") {
    				if (calendarEntry.project)
    					calendarEntry.title = calendarEntry.project.__displayname;
    				else
    					calendarEntry.title = txt("projectnotfound", "resourcingcalendarwidget");
				}
    			else {
    				if (calendarEntry.user)
    					calendarEntry.title = calendarEntry.user.__displayname;
    				else
    					calendarEntry.title = txt("usernotfound", "resourcingcalendarwidget");
    			}
    		}
    		catch(e)
    		{
    		}

			// If not start date is specified remove calendar entry.
    		if (!calendarEntry["projectstart"] && !calendarEntry["starttimestamp"]) {
    			calendarEntries.splice(i, 1);
    			continue;
    		}

    		var hasStart = false;
    		var hasEnd = false;

    		// Get start and end from entries and change ISO style timezone to moment timezone.
    		if (calendarEntry["projectstart"] || calendarEntry["projectend"]) {
    			if (calendarEntry["projectstart"]) {
    				hasStart = true;
    				calendarEntry.start = calendarEntry["projectstart"].replace("Z00", "+00:00");
    			}

    			if (calendarEntry["projectend"]) {
    				hasEnd = true;
    				calendarEntry.end = calendarEntry["projectend"].replace("Z00", "+00:00");
    			}
    		} else {
    			if (calendarEntry["starttimestamp"]) {
    				hasStart = true;
    				calendarEntry.start = calendarEntry["starttimestamp"].replace("Z00", "+00:00");
    			}

    			if (calendarEntry["endtimestamp"]) {
    				hasEnd = true;
    				calendarEntry.end = calendarEntry["endtimestamp"].replace("Z00", "+00:00");
    			}
    		}

	    	// Calendar entries with only one date part set are handled as allday.
    		if ((hasStart || hasEnd) && (!hasStart || !hasEnd))
    			calendarEntry["allDay"] = true;

    		if (showAllItemsImmediately)
    			base._uiState.shownEntries[calendarEntry._id] = true;
    	}

    	// Sort entries alphabetically based on title
    	calendarEntries.sort(function (a, b) {
    		if (a.title < b.title) return -1;
    		if (a.title > b.title) return 1;
    		return 0;
    	});
    },
    /**
    * Render data in the model and settings into view.
    */
    render: function (options) {

    	if (!options) options = {};

    	this.element.removeClass("__hidden");

    	var base = this;

    	if (!options.nocalendarupdate) {
    		// Add FullCalendar
    		if (this._uiState.firstRender) {
    			this._uiState.firstRender = false;
    		}
    		else {

    			if (!base._uiState.calendarRendered) {
    				base._uiState.calendarRendered = true;
    			} else {
    				this.element.find(".fullcalendar").fullCalendar('destroy');
    			}

    			var contentHeight = this.element.find(".resourcecalendar").height() - 10; // - 10 to make sure no scrollbars ever appear

    			this.element.find(".fullcalendar").fullCalendar({
    				lang: txt("languagecode", "core").substring(0, 2).toLowerCase(),
    				timezone: "local",
    				header: false,
    				contentHeight: contentHeight,
    				eventLimit: true,
    				dayClick: function (date) {
    				    var dailyResourcing = $(".dailyresourcingwidget").data("dailyresourcingwidget-dailyresourcingwidget");
    				    dailyResourcing._clearSelection();
                        dailyResourcing._performResourcingAction(new Date(date), 10);
    				    $(".__tabhead[rel='resourcing-tab-daily']").click(); // todo: this might need to be improved
    				},
    				views: {
    					agenda: {
    						eventLimit: 6
    					}
    				},
    				defaultDate: base._model.currentDate,
    				eventSources: [
						{
							events: function (start, end, timezone, callback) {
								base._model.filteredEntries.length = 0;

								if (base._uiState.viewType == "projects") {

									for (var i = 0; i < base._model.calendarEntries.length; i++) {
										var item = base._model.calendarEntries[i];

										if (base._uiState.shownEntries[item._id])
											base._model.filteredEntries.push(item);
									}
								} else if (base._uiState.viewType == "allocationsforproject") {

									for (var i = 0; i < base._model.calendarEntries.length; i++) {
										var item = base._model.calendarEntries[i];

										if (item.project && base._uiState.shownEntries[item.project._id])
											base._model.filteredEntries.push(item);
									}
								} else if (base._uiState.viewType == "allocationsforusers") {

									for (var i = 0; i < base._model.calendarEntries.length; i++) {
										var item = base._model.calendarEntries[i];

										if (item.user && base._uiState.shownEntries[item.user._id])
											base._model.filteredEntries.push(item);
									}
								}

								base._uiState.eventsFetched = true;

								callback(base._model.filteredEntries);
							}
						}
    				]
    			});
    		}
    	}

    	// Add events as a checkbox list
    	if (!options.noitemlistupdate) {
    		var calendarEntryList = this.element.find(".calendarentrylist");
    		calendarEntryList.html("");

    		if (base._uiState.viewType == "projects") {
    			base._renderProjectSwitches();
    		} else if (base._uiState.viewType == "allocationsforproject") {
    			base._renderProjectSwitchesForAllocations();
    		} else if (base._uiState.viewType == "allocationsforusers") {
    			base._renderUserSwitches();
    		}
    	}

    	updateScrollBars(base.element);
    	var currDate = new Date(this._model.currentDate)
    	this.element.find(".selecteddate").text(txt("month_" + currDate.getMonth()) + " " + currDate.getFullYear());
    },
    _renderUserSwitches: function ()
	{
    	var base = this;
    	var calendarEntryList = this.element.find(".calendarentrylist");

    	for (var i = 0; i < this._model.users.length; i++) {
    		var user = this._model.users[i];
    		var id = base.id + "_calendarentrylist_" + i;

    		var checked = (base._uiState.shownEntries[user._id]) ? " checked " : "";
    		calendarEntryList.append("<div class='__control __checkbox __switch'><input type='checkbox' id='" + id + "' class='__checkbox' data-identifier='" + user._id + "'" + checked + "></input><label for='" + id + "'>" + user.__displayname + "</label></div>")

    		this.element.find("#" + id).on("click", function () {
    			base._uiState.shownEntries[$(this).data("identifier")] = $(this).is(':checked');

    			clearTimeout(base._uiState.eventsListTimeoutToken);
    			var refreshDelay = 800;

    			base._uiState.eventsListTimeoutToken = setTimeout(function () {
    				base._uiState.lastAction = null;
    				base.render({ noitemlistupdate: true });
    			}, refreshDelay);
    		});
    	}
	},
	_renderProjectSwitchesForAllocations: function()
	{
		var base = this;

		var calendarEntryList = this.element.find(".calendarentrylist");

		for (var i = 0; i < this._model.projects.length; i++) {
			var project = this._model.projects[i];
			var id = base.id + "_calendarentrylist_" + i;

			var checked = (base._uiState.shownEntries[project._id]) ? " checked " : "";
			calendarEntryList.append("<div class='__control __checkbox __switch'><input type='checkbox' id='" + id + "' class='__checkbox' data-identifier='" + project._id + "'" + checked + "></input><label for='" + id + "'>" + project.__displayname + "</label></div>")

			this.element.find("#" + id).on("click", function () {
				base._uiState.shownEntries[$(this).data("identifier")] = $(this).is(':checked');

				clearTimeout(base._uiState.eventsListTimeoutToken);
				var refreshDelay = 800;

				base._uiState.eventsListTimeoutToken = setTimeout(function () {
					base._uiState.lastAction = null;
					base.render({ noitemlistupdate: true });
				}, refreshDelay);
			});
		}
	},

	_renderProjectSwitches: function () {
		var base = this;

		var calendarEntryList = this.element.find(".calendarentrylist");

		for (var i = 0; i < this._model.calendarEntries.length; i++) {
			var calendarEntry = this._model.calendarEntries[i];
			var id = base.id + "_calendarentrylist_" + i;

			var checked = (base._uiState.shownEntries[calendarEntry._id]) ? " checked " : "";
			calendarEntryList.append("<div class='__control __checkbox __switch'><input type='checkbox' id='" + id + "' class='__checkbox' data-identifier='" + calendarEntry._id + "'" + checked + "></input><label for='" + id + "'>" + calendarEntry.title + "</label></div>")

			this.element.find("#" + id).on("click", function () {
				base._uiState.shownEntries[$(this).data("identifier")] = $(this).is(':checked');

				clearTimeout(base._uiState.eventsListTimeoutToken);
				var refreshDelay = 800;

				base._uiState.eventsListTimeoutToken = setTimeout(function () {
					base._uiState.lastAction = null;
					base.render({ noitemlistupdate: true });
				}, refreshDelay);
			});
		}
	}
});
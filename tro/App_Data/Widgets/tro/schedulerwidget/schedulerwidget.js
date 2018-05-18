/**
 * schedulerwidget widget
 */
$.widget("schedulerwidget.schedulerwidget", $.core.base, {
    options: {
    },

    /**
     * Fired by jQuery when the widget is created.
     */
    _create: function () {

        this._uiState = new Object;
        this._model = new Object();

        // Allocations of projects shown with current filtering
        this._model.filteredEntries = new Array();

        // User entries. Filled based on allocations. Only has values when showing allocations.
        this._model.users = new Array();

        // Projects. Filled based on allocations. Only has values when showing allocations.
        this._model.projects = new Array();

        this._model.currentDate = moment(newDate());

        this._uiState.firstRender = true;
        this._uiState.calendarRendered = false;
        this._uiState.shownEntries = {};
        this._uiState.viewType = "personnel";
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
            this._model.projects = new Array();
            this._model.users = new Array();
            this._model.allocations = new Array();
            this.render();
            return;
        }

        var monthStart = moment(this._model.currentDate).startOf("month");
        var monthEnd = moment(this._model.currentDate).endOf("month");

        // retaining placeholders for future addition of a project-based view
        if (this._uiState.viewType == "personnel")
            queryName = "allocationsbyuserprofitcenter";

        var restApi = new RestApi();
        var calendarEntriesPromise = restApi.find("schedulerwidget", queryName,
		{
		    start: monthStart.toDate(),
		    end: monthEnd.toDate(),
		    profitcenter: base._model.selectedProfitCenter,
		});

        calendarEntriesPromise.done(function (data) {
            if (data.project && data.user && data.allocationentry) {
                base._model.projects = data.project;
                base._model.users = data.user;
                base._model.allocations = data.allocationentry;
            }
            else {
                base._model.projects.length = 0;
                base._model.users.length = 0;
                base._model.allocations.length = 0;
            }

            base._processPersonnel();
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

        base.element.find("select[name='schedulerwidget_profitcenter']").on("dropdownupdated", function () {
            $(this).val(base._model.selectedProfitCenter);

            // First data refresh when profit center is available.
            base.refreshData(true);
        });

        base.element.find("select[name='schedulerwidget_profitcenter']").on("change", function (e) {
            base._model.selectedProfitCenter = $(this).val();

            // Clean all selections when changing profit center.
            base._uiState.shownEntries = {};
            base.refreshData(true);

            // also change dailyresourcing's profit center todo: revise using widget communications, unless triggered automatically -- should update profitcenters of all widgets!
            if (e.originalEvent !== undefined)
                $(".dailyresourcingsetup select").val($(this).val()).trigger("change");

        });

        var refreshDelay = 300;

        base.element.find(".schedulerwidget_nextbutton").on("click", function () {
            base._performFullCalendarAction("next", refreshDelay);
        });

        base.element.find(".schedulerwidget_previousbutton").on("click", function () {
            base._performFullCalendarAction("prev", refreshDelay);
        });

        base.element.find(".schedulerwidget_todaybutton").on("click", function () {
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
    /**
	 * Modify calendarEntries to a format understood by fullcalendar
	 */
    _processPersonnel: function () {
        var base = this;
        var personnel = this._model.users;
        var allocations = this._model.allocations;

        for (var i = personnel.length - 1; i >= 0; i--) {
            var schedulerPerson = personnel[i];

            schedulerPerson.enabled = false;

            try {
                schedulerPerson.title = schedulerPerson.lastname + " " + schedulerPerson.firstname;
                schedulerPerson.id = schedulerPerson._id;
            }
            catch (e) {
            }

            base._uiState.shownEntries[schedulerPerson._id] = true;
        }

        for (var i = allocations.length - 1; i >= 0; i--) {
            var schedulerAllocation = allocations[i];

            schedulerAllocation.enabled = false;

            try {
                schedulerAllocation.title = schedulerAllocation.project.__displayname;
                schedulerAllocation.resourceId = schedulerAllocation.user._id;
            }
            catch (e) {
            }


            // If not start date is specified remove calendar entry.
            if (!schedulerAllocation["projectstart"] && !schedulerAllocation["starttimestamp"]) {
                allocations.splice(i, 1);
                continue;
            }

            var hasStart = false;
            var hasEnd = false;

            // Get start and end from entries and change ISO style timezone to moment timezone.
            if (schedulerAllocation["projectstart"] || schedulerAllocation["projectend"]) {
                if (schedulerAllocation["projectstart"]) {
                    hasStart = true;
                    schedulerAllocation.start = schedulerAllocation["projectstart"].replace("Z00", "+00:00");
                }

                if (schedulerAllocation["projectend"]) {
                    hasEnd = true;
                    schedulerAllocation.end = schedulerAllocation["projectend"].replace("Z00", "+00:00");
                }
            } else {
                if (schedulerAllocation["starttimestamp"]) {
                    hasStart = true;
                    schedulerAllocation.start = schedulerAllocation["starttimestamp"].replace("Z00", "+00:00");
                }

                if (schedulerAllocation["endtimestamp"]) {
                    hasEnd = true;
                    schedulerAllocation.end = schedulerAllocation["endtimestamp"].replace("Z00", "+00:00");
                }
            }

            // Calendar entries with only one date part set are handled as allday.
            if ((hasStart || hasEnd) && (!hasStart || !hasEnd))
                schedulerAllocation["allDay"] = true;

            base._uiState.shownEntries[schedulerAllocation._id] = true;
        }

        // Sort entries alphabetically based on title
        personnel.sort(function (a, b) {
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
                    schedulerLicenseKey: 'CC-Attribution-NonCommercial-NoDerivatives',
                    resourceAreaWidth: 230,
                    defaultDate: '2016-01-07',
                    editable: true,
                    aspectRatio: 1.5,
                    scrollTime: '00:00',
                    header: {
                        left: 'promptResource today prev,next',
                        center: 'title',
                        right: 'timelineDay,timelineThreeDays,agendaWeek,month'
                    },
                    defaultView: 'timelineDay',
                    views: {
                        timelineThreeDays: {
                            type: 'timeline',
                            duration: { days: 3 }
                        }
                    },
                    resourceLabelText: 'Rooms',
                    resources: base._model.users,
                    events: function (start, end, timezone, callback) {
                        base._model.filteredEntries.length = 0;

                            for (var i = 0; i < base._model.allocations.length; i++) {
                                var item = base._model.allocations[i];
                                item.start = "2016-0-18T09:00:00";
                                base._model.filteredEntries.push(item);
                            }
                    },
                    //events: [
                    //    { id: '1', resourceId: 'b', start: '2016-01-07T02:00:00', end: '2016-01-07T07:00:00', title: 'event 1' },
                    //    { id: '2', resourceId: 'c', start: '2016-01-07T05:00:00', end: '2016-01-07T22:00:00', title: 'event 2' },
                    //    { id: '3', resourceId: 'd', start: '2016-01-06', end: '2016-01-08', title: 'event 3' },
                    //    { id: '4', resourceId: 'e', start: '2016-01-07T03:00:00', end: '2016-01-07T08:00:00', title: 'event 4' },
                    //    { id: '5', resourceId: 'f', start: '2016-01-07T00:30:00', end: '2016-01-07T02:30:00', title: 'event 5' }
                    //]
                    //lang: txt("languagecode", "core").substring(0, 2).toLowerCase(),
                    //timezone: "local",
                    //header: false,
                    //contentHeight: contentHeight,
                    //eventLimit: true,
                    //dayClick: function (date) {
                    //    var dailyResourcing = $(".dailyresourcingwidget").data("dailyresourcingwidget-dailyresourcingwidget");
                    //    dailyResourcing._clearSelection();
                    //    dailyResourcing._performResourcingAction(new Date(date), 10);
                    //    $(".__tabhead[rel='resourcing-tab-daily']").click(); // todo: this might need to be improved
                    //},
                    //views: {
                    //    agenda: {
                    //        eventLimit: 6
                    //    }
                    //},
                    //defaultDate: base._model.currentDate,
                    //eventSources: [
					//	{
					//	    events: function (start, end, timezone, callback) {
					//	        base._model.filteredEntries.length = 0;

					//	        if (base._uiState.viewType == "projects") {

					//	            for (var i = 0; i < base._model.calendarEntries.length; i++) {
					//	                var item = base._model.calendarEntries[i];

					//	                if (base._uiState.shownEntries[item._id])
					//	                    base._model.filteredEntries.push(item);
					//	            }
					//	        } else if (base._uiState.viewType == "allocationsforproject") {

					//	            for (var i = 0; i < base._model.calendarEntries.length; i++) {
					//	                var item = base._model.calendarEntries[i];

					//	                if (item.project && base._uiState.shownEntries[item.project._id])
					//	                    base._model.filteredEntries.push(item);
					//	            }
					//	        } else if (base._uiState.viewType == "allocationsforusers") {

					//	            for (var i = 0; i < base._model.calendarEntries.length; i++) {
					//	                var item = base._model.calendarEntries[i];

					//	                if (item.user && base._uiState.shownEntries[item.user._id])
					//	                    base._model.filteredEntries.push(item);
					//	            }
					//	        }

					//	        base._uiState.eventsFetched = true;

					//	        callback(base._model.filteredEntries);
					//	    }
					//	}
                    //]
                });
            }
        }

        // Add events as a checkbox list
        if (!options.noitemlistupdate) {
            var calendarEntryList = this.element.find(".calendarentrylist");
            calendarEntryList.html("");

            // todo: here we need to have the draggable project templates instead of switches
            base._renderProjectSwitches();
        }

        updateScrollBars(base.element);
        var currDate = new Date(this._model.currentDate)
        // todo: update text display
        this.element.find(".selecteddate").text(txt("month_" + currDate.getMonth()) + " " + currDate.getFullYear());
    },
    _renderProjectSwitches: function () {
        var base = this;

        var projects = this.element.find(".calendarentrylist");

        for (var i = 0; i < this._model.projects.length; i++) {
            var project = this._model.projects[i];
            var id = base.id + "_calendarentrylist_" + i;

            var checked = (base._uiState.shownEntries[project._id]) ? " checked " : "";
            projects.append("<div class='__control __checkbox __switch'><input type='checkbox' id='" + id + "' class='__checkbox' data-identifier='" + project._id + "'" + checked + "></input><label for='" + id + "'>" + project.name + "</label></div>")

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
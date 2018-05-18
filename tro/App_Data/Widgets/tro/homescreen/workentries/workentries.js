/**
 * workentries widget
 */
$.widget("workentries.workentries", $.core.base, {
	options: {
	},

	/**
     * Fired by jQuery when the widget is created.
     */
	_create: function () {

		this._uiState = {};
		this._model = {};
		this._uiState.firstRender = true;
	},
	/**
	 * Fired by MC2 when widget is created. Implementation specific initialization code should
	 * be run here
	 */
	_initWidget: function () {
		this._dataSource = widgets[this.options.datasource];
		
		this._uiState.selectedEntryType = sessionStorage.getItem(this.element.attr("id") + "_selectedentrytype");
		if (!this._uiState.selectedEntryType)
			this._uiState.selectedEntryType = "hours";

		this._setupWidgetEvents();
	},
	/**
	 * Sets the type of selected antry, "hours", "expenses", "articles"
	 * or ("assetselector_" + assetId)
	 */
	_setSelectedEntryType: function(selectedEntryType) {
		this._uiState.selectedEntryType = selectedEntryType;
		sessionStorage.setItem(this.element.attr("id") + "_selectedentrytype", selectedEntryType);
	},
	/**
	 * Setup widget's events.
	 */
	_setupWidgetEvents: function () {
		var base = this;

		$(this._dataSource.element).on("datarefreshed", function () {
		    base.render();
		});

		$(this._dataSource.element).on("selecteddatechanged", function () {
		    base.render();
		});

		$(this.element).find(".approvebutton").on("click", function (e) {
			e.stopPropagation();
			e.preventDefault();

			var confirmText = txt("approvedayprompt", "workentries");

			if (confirm(confirmText)) {
				var url = getAjaxUrl("workentries", "approveday");
				url = addParameterToUrl(url, "selectedday", base._dataSource._model.selectedDateKey);

				$.ajax({
					url: url,
					success: function (data, status, xhr) {
						base._dataSource.refreshData();
					}
				});
			}
		});

		$(this.element).find(".copybutton").on("click", function (e) {
			e.stopPropagation();
			e.preventDefault();

			base._dataSource.setCurrentDateAsCopied();
			base._renderCopyControls();
		});

		$(this.element).find(".pastebutton").on("click", function (e) {
			e.stopPropagation();
			e.preventDefault();

			var confirmText = (base._uiState.futureDaySelected)? txt("pasteconfirmationfuture", "workentries") : txt("pasteconfirmation", "workentries");

			var sourceDate = base._dataSource.getDateFromKey(base._dataSource._model.copiedDateKey);
			var targetDate = base._dataSource.getDateFromKey(base._dataSource._model.selectedDateKey);

			confirmText = confirmText.replace("{0}", formatDateShort(sourceDate));
			confirmText = confirmText.replace("{1}", formatDateShort(targetDate));

			if (confirm(confirmText)) {
				var url = getAjaxUrl("workentries", "pasteday");
				url = addParameterToUrl(url, "sourceday", base._dataSource._model.copiedDateKey);
				url = addParameterToUrl(url, "targetday", base._dataSource._model.selectedDateKey);

				$.ajax({
					url: url,
					success: function (data, status, xhr) {
					    if (data === "filtered") {
                            alert(txt("filtered", "workentries"))
					    }
						base._dataSource.refreshData();
					}
				});
			}
		});

		$(this.element).find(".datecontrols .previous").on("click", function (e) {
			if (base._dataSource._model.prevDateKey)
			    base._dataSource.setSelectedDateKey(base._dataSource._model.prevDateKey);
    			base._dataSource.scrollHorizontalWorkView();
		});

		$(this.element).find(".datecontrols .next").on("click", function (e) {
		    if (base._dataSource._model.nextDateKey) {
		        base._dataSource.setSelectedDateKey(base._dataSource._model.nextDateKey);
	            base._dataSource.scrollHorizontalWorkView();
		    }
		});

		$(this.element).find(".datecontrols .today").on("click", function (e) {
		    base._dataSource.setSelectedDateKey(base._dataSource.getKeyFromDate(new Date()));
		    base._dataSource.scrollHorizontalWorkView();
		});

    	$(this.element).find(".addentrybutton").on("click", function (e) {
    		e.stopPropagation();
    		e.preventDefault();

    		var entryDialogContents = base.element.find(".workentryactionbuttons").removeClass("__widgettemplate").clone();

    		var start = null;
    		var end = null;

    		if (features.timetracking) {
    			// Start after last timed entry for today or if there are no timed entries then send no start and end
				// and use default (start now)
    			var selectedDay = base._dataSource._model.days[base._dataSource._model.selectedDateKey];

    			var lastEntry = base._getLastPrimaryEntry();

				// Only set default date if last entry's end date is still today. Go with default otherwise.
    			if (lastEntry) {
    				var start = lastEntry.endtimestamp;
    				var end = new Date(start.getTime() + (60 * 60 * 1000)); // Default to 1 hour after start

    				// If end is tomorrow (or later) use default start and end.
    				if (lastEntry.starttimestamp.toDateString() !== lastEntry.endtimestamp.toDateString()) {
    					start = null;
    					end = null;
    				}
    			}
    		}

			// If there is no favored start or end date user selected day and current time and end time one hour after that
    		if (!start || !end) {
    			var now = new Date();
    			start = base._dataSource.getDateFromKey(base._dataSource._model.selectedDateKey)
    			start.setHours(now.getHours());
    			start.setMinutes(now.getMinutes());

    			var end = new Date(start.getTime() + (60 * 60 * 1000)); // Default to 1 hour after start
    		}

    		if (base._uiState.futureDaySelected) {
    			entryDialogContents.find(".expense").hide();
    			entryDialogContents.find(".article").hide();
    			entryDialogContents.find(".asset").hide();
    			entryDialogContents.find(".worktime").hide();
    			entryDialogContents.find(".socialproject").hide(); // for both work entry and expense
    		} 

    		entryDialogContents.find("a").each(function () {
    			var url = $(this).attr("href");
    			url = addParameterToUrl(url, "selecteddatekey", base._dataSource._model.selectedDateKey);

    			if (base._dataSource._model.defaultProject)
    				url = addParameterToUrl(url, "workscheduleprojectid", base._dataSource._model.defaultProject);

    			if (start && end) {
    				url = addParameterToUrl(url, "start", UTCISOString(start));
    				url = addParameterToUrl(url, "end", UTCISOString(end));
				}

    			$(this).attr("href", url);
    		});

    		entryDialogContents.find(".allocations").on("click", function (e) {
    		    $(window).data("dialog").close();
    		    document.getElementById("homescreensection-allocations").click();
    		});

    		$(window).dialog({
    			object: entryDialogContents.get()[0],
    			minWidth: "200px",
    			height: "auto",
    			width: "auto",
    			maxWidth: "250px",
    			closeButton: false,
    			customScrolling: false
    		});
    	});
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

    	if (!options) options = {};    	
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
    	var base = this;

    	// Determine if a new entry was just added. This info with document id can be used to highlight the new
		// entry.
    	this._uiState.hasNewEntry = this._uiState.lastHighlightedEntryId != this._dataSource._model.lastShownNewId;

    	if (this._uiState.firstRender) {
			// Code run only once when the widget is rendered for the first time.
    	    this.element.removeClass("__hidden");

    	    // entry type scrolling
    	    base.scroll = new IScroll(base.element.find(".totalswrapper").get()[0], {
    	        scrollX: true,
    	        scrollY: false,
    	        deceleration: 0.005
    	    });

            base.scroll.on('scrollEnd', function () {
    	        base.element.removeClass("__disabled");
    	    });

    	    base.scroll.on('scrollStart', function () {
    	        base.element.addClass("__disabled");
    	    });

    		this._uiState.firstRender = false;
    	}

    	this._renderNextAndPreviousControls();

    	var selectedDay = this._dataSource._model.days[this._dataSource._model.selectedDateKey];

    	this._renderDay(selectedDay);

    	this._renderCopyControls();

    	this._uiState.lastHighlightedEntryId = this._dataSource._model.lastShownNewId;

    },
    _renderCopyControls: function() {
    	if (this._dataSource._model.copiedDateKey) {
    		$(this.element).find(".pastebutton").removeClass("__disabled");
    	} else {
    		$(this.element).find(".pastebutton").addClass("__disabled");
    	}
    },
    _renderNextAndPreviousControls: function() {
    	if (this._dataSource._model.nextDateKey)
    	    $(this.element.find(".datecontrols .next")).removeClass("__disabled");
    	else
    	    $(this.element.find(".datecontrols .next")).addClass("__disabled");

    	if (this._dataSource._model.prevDateKey)
    	    $(this.element.find(".datecontrols .previous")).removeClass("__disabled");
    	else
    	    $(this.element.find(".datecontrols .previous")).addClass("__disabled");

    },
    _renderDay: function (day) {
    	$(this.element).find(".entrycontainer").html("");

    	this._uiState.futureDaySelected = (moment(day.date) > moment().add(1, "days").startOf("day")) ? true : false;

    	if (day.date) {
    	    $(this.element).find(".date .long").text(txt("day_" + day.date.getDay()) + " " + formatDateShort(day.date));
    	    $(this.element).find(".date .short").text(txt("day_abbreviated_" + day.date.getDay()) + " " + formatDateShort(day.date));
    	}
    	else {
    	    $(this.element).find(".date .long").text("");
    	    $(this.element).find(".date .short").text("");
    	}

    	this._enablePrimaryButtons(day);

    	this._adjustSelectedEntryType(day);

		this._renderEntryTypeSelectors(day);

		this._renderAssetSelectors(day);

    	if (this._uiState.selectedEntryType == "hours") {
    		this._renderTimedEntries(day);
    	}
    	else if (this._uiState.selectedEntryType == "expenses") {
    		this._renderExpenses(day);
    	}
    	else if (this._uiState.selectedEntryType == "articles") {
    		this._renderArticles(day);
    	}
    	else if (this._uiState.selectedEntryType == "none") {
    		this._renderNothing();
    	}
    	else if (this._uiState.selectedEntryType.substr(0, "assetselector_".length) == "assetselector_") {
    		this._renderAssetEntries(day, this._uiState.selectedEntryType.substr("assetselector_".length));
    	}
        
    	this.scroll.refresh();

        // select worklist if there are no entries
    	if (!day.hasEntries)
    	    document.getElementById("homescreensection-allocations").checked = "checked";
        else
            document.getElementById("homescreensection-entries").checked = "checked";

    },
    _enablePrimaryButtons: function (day) {
        if (day.hasUnapprovedCurrentUserCreatedEntries)
            $(this.element).find(".approvebutton").removeClass("__disabled");
        else
            $(this.element).find(".approvebutton").addClass("__disabled");

        if (this._uiState.futureDaySelected && !features.allowfutureabsences)
            $(this.element).find(".addentrybutton").addClass("__disabled");
        else
            $(this.element).find(".addentrybutton").removeClass("__disabled");

    },
    _renderAssetSelectors: function (day) {
    	var base = this;

    	$(this.element).find(".assettotal").remove();

    	for (var i = 0; i < day.assets.length; i++) {
    		var asset = day.assets[i];
    		var assetSelectorClass = "assetselector_" + asset._id;
    		var assetSelector = $("<div class='totalselection " + assetSelectorClass + " assettotal' data-amount='" + asset.__displayname.substr(0,6) + "'><i class='material-icons'>local_shipping</i></div>");
    		assetSelector.data("assetid", asset._id);

    		$(this.element).find(".totals").append(assetSelector);

    		if (base._uiState.selectedEntryType == "assetselector_" + asset._id) {
    			$(base.element).find(".totalselection").removeClass("active");
    			assetSelector.addClass("active");
    		}

    		assetSelector.on("click tap", function () {
    		    var assetId = $(this).data("assetid");
    			if (base._uiState.selectedEntryType == "assetselector_" + assetId)
    				return;

    			base._setSelectedEntryType("assetselector_" + assetId);
    			base.render();
    		});
    	}
    },
	/**
	 * Change selected entyr type according to changing conditions (availability, last created item etc.)
	 */
    _adjustSelectedEntryType: function (day) {
    	var base = this;

    	// Adjust selection to the newly created entry if exists and is effective today
    	if (this._dataSource._model.selectedDateKey == this._dataSource._model.lastShownNewDocumentDateKey &&
			this._uiState.hasNewEntry) {
    		var highlightColelction = this._dataSource._model.lastShownNewDocument.__collectionname;

    		if (highlightColelction == "timesheetentry" || highlightColelction == "absenceentry") {
    			this._setSelectedEntryType("hours");
    		}
    		else if (highlightColelction == "dayentry") {
    			this._setSelectedEntryType("expenses");
    		}
    		else if (highlightColelction == "articleentry") {
    			this._setSelectedEntryType("articles");
    		}
    		else if (highlightColelction == "assetentry") {
    			// Determine the asset to select
    			var assetEntryType = "assetselector_" + this._dataSource._model.lastShownNewDocument.asset._id;
    			this._setSelectedEntryType(assetEntryType);    			
    		}
    	}

    	// If nothing is selected, attempt to select hours firt.
    	if (this._uiState.selectedEntryType == "none") {

    		this._uiState.selectedEntryType = "hours";

    		// In case no hours are present revert to other types by default
    		if (day.timedEntries.length == 0) {
    			$(this.element).find(".totaltimesheetentries").addClass("__disabled");

    			var found = false;

    			if (day.assetEntries.length > 0 && this._uiState.selectedEntryType.substr(0, "assetselector_".length) == "assetselector_") {
    				// There are some assets and an asset is selected. See if selected asset is available for today.
    				found = true;

    				for (var i = 0; i < day.assetEntries.length; i++) {
    					if ("assetselector_" + day.assetEntries[i].asset._id == this._uiState.selectedEntryType) {
    						continue;
    					}
    				}
    			}
    			else if (day.dayEntries.length > 0) {
    				this._uiState.selectedEntryType = "expenses";
    				found = true;
    			}
    			else if (day.articleEntries.length > 0) {
    				this._uiState.selectedEntryType = "articles";
    				found = true;
    			}

    			// If nothing is found, default to none.
    			if (!found) {
    				this._uiState.selectedEntryType = "none";
    			}
    		}
    		else {
    			$(this.element).find(".totaltimesheetentries").removeClass("__disabled");
    		}
    	}

    	if (day.dayEntries.length == 0) {
    		$(this.element).find(".totalexpenses").addClass("__disabled");
    		// Revert to hours if there's nothing to show.
    		if (this._uiState.selectedEntryType == "expenses") {
    			this._setSelectedEntryType("hours");
    		}
    	}
    	if (day.articleEntries.length == 0) {
    		if (this._uiState.selectedEntryType == "articles")
    			this._setSelectedEntryType("hours");
    	}

    	// Revert to hours if asset was selected bu it's not available for this day
    	if (this._uiState.selectedEntryType.substr(0, "assetselector_".length) == "assetselector_" &&
			!day.assetsById[this._uiState.selectedEntryType.substr("assetselector_".length)]) {
    		this._setSelectedEntryType("hours");
    	}
    },
    _renderEntryTypeSelectors: function (day) {
    	var base = this;

    	if (!this._uiState.selectorEventsSet) {
    		$(this.element).find(".totalexpenses").on("click tap", function (event) {
    			event.stopPropagation();

    			if (base._uiState.selectedEntryType == "expenses")
    				return;

    			base._setSelectedEntryType("expenses");
    			base.render();
    		});

    		$(this.element).find(".totalarticles").on("click tap", function (event) {
    			event.stopPropagation();

    			if (base._uiState.selectedEntryType == "articles")
    				return;

    			base._setSelectedEntryType("articles");
    			base.render();
    		});

    		$(this.element).find(".totaltimesheetentries").on("click tap", function (event) {
    			event.stopPropagation();

    			if (base._uiState.selectedEntryType == "hours")
    				return;

    			base._setSelectedEntryType("hours");
    			base.render();
    		});

    		this._uiState.selectorEventsSet = true;
    	}


    	if (day.dayEntries.length == 0) {
    		$(this.element).find(".totalexpenses").addClass("__disabled");
    	}
    	else {
    		$(base.element).find(".totalexpenses").removeClass("__disabled");

    		if (this._uiState.selectedEntryType == "expenses") {
    			$(base.element).find(".totalselection").removeClass("active");
    			$(base.element).find(".totalexpenses").addClass("active");
    		}
    	}

    	if (day.articleEntries.length == 0) {
    		$(this.element).find(".totalarticles").addClass("__disabled");
    	} else {
    		$(this.element).find(".totalarticles").removeClass("__disabled");

    		if (this._uiState.selectedEntryType == "articles") {
    			$(base.element).find(".totalselection").removeClass("active");
    			$(base.element).find(".totalarticles").addClass("active");
    			$(this.element).find(".maintitle").removeClass("noentries");
    			$(base.element).find(".maintitle").text(txt("articles", "workentries"));
    		}
    	}


    	// In case no hours are present revert to other types by default
    	if (day.timedEntries.length > 0) {
    		$(this.element).find(".totaltimesheetentries").removeClass("__disabled");

    		if (this._uiState.selectedEntryType == "hours") {
    			$(base.element).find(".totalselection").removeClass("active");
    			$(base.element).find(".totaltimesheetentries").addClass("active");
    		}
		}
    },
    _renderTimedEntries: function(day) {
    	$(this.element).find(".totalhours").attr("data-duration", "" + day.totalAllHours);
    	$(this.element).find(".dataview").addClass("showmeter");
    	$(this.element).find(".totalhours").css("visibility", "visible");
    	$(this.element).find(".totalhours").css("display", "");
    	$(this.element).find(".maintitle").removeClass("noentries");
    	$(this.element).find(".maintitle").text(txt("hours", "workentries"));

    	var previousEntry = null;

    	for (var i = 0; i < day.timedEntries.length; i++) {
    		var entry = day.timedEntries[i];

    		// Do not create card for details
    		if (entry.parent)
    			continue;

    		var entryRow = this._createWorkEntryRow(entry);

    		if (entry.project)
    			entryRow.setTitle(entry.project.__displayname);
			else if (entry.timesheetentrydetailpaytype)
				entryRow.setTitle(entry.timesheetentrydetailpaytype.__displayname);
			else if (entry.absenceentrytype)
				entryRow.setTitle(entry.absenceentrytype.__displayname);

    		if (features.timetracking)
    			entryRow.setTime(entry.starttimestamp, entry.endtimestamp)

    		if (features.dailyentries) {
    			entryRow.setDuration(entry.duration)
    			if (entry.timesheetentrydetailpaytype)
    				entryRow.setPayType(entry.timesheetentrydetailpaytype.__displayname)
    			else
    				entryRow.setPayType("");
    		}

    		if (entry.approvedbyworker)
    			entryRow.setApproved(true);

    		if (entry.istraveltime)
    		    entryRow.setTravelTime(true);

    		if (entry.absenceentrytype || !features.timetracking)
    			entryRow._setDetailsButton(false);
			else
    			this._renderDayDetails(day, entry, entryRow);		

    		if (this._uiState.hasNewEntry && this._dataSource._model.lastShownNewId == entry._id)
    			entryRow.setNewEntry(true);

    		// Check for idle time
    		if (features.timetracking &&  previousEntry) {
    			if (previousEntry.endtimestamp.getTime() < entry.starttimestamp.getTime()) {
					// idle time found add idle time card
    				var idleEntryRow = this._createIdleEntryRow(entry);
    				var duration = entry.starttimestamp - previousEntry.endtimestamp;
    				idleEntryRow.setDuration(this._durationToHours(duration));
    				idleEntryRow.setDates(previousEntry.endtimestamp, entry.starttimestamp);

    				$(this.element).find(".entrycontainer").append(idleEntryRow.element);
				}
    		}

    		$(this.element).find(".entrycontainer").append(entryRow.element);

    		previousEntry = entry;
    	}

    	if (day.timedEntries.length == 0)
    		$(this.element).find(".totaltimesheetentries").addClass("__disabled");
    	else
    		$(this.element).find(".totaltimesheetentries").removeClass("__disabled");
    },
    _renderAssetEntries: function (day, assetId) {
    	var asset = day.assetsById[assetId];

    	$(this.element).find(".totalhours").attr("data-duration", "" + asset.totalHours);
    	$(this.element).find(".dataview").addClass("showmeter");
    	$(this.element).find(".totalhours").css("visibility", "visible");
    	$(this.element).find(".totalhours").css("display", "");
    	$(this.element).find(".maintitle").removeClass("noentries");
    	$(this.element).find(".maintitle").text(asset.__displayname);

    	if (!asset)
    		return;

    	for (var i = 0; i < asset.hours.length; i++) {
    		var entry = asset.hours[i];

    		var entryRow = this._createAssetEntryRow(entry);

    		if (entry.project)
    			entryRow.setTitle(entry.project.__displayname);

    		if (features.timetracking)
    			entryRow.setTime(entry.starttimestamp, entry.endtimestamp)

    		if (features.dailyentries)
    			entryRow.setDuration(entry.duration)

    		if (entry.approvedbyworker)
    			entryRow.setApproved(true);

    		if (this._uiState.hasNewEntry && this._dataSource._model.lastShownNewId == entry._id)
    			entryRow.setNewEntry(true);

    		$(this.element).find(".entrycontainer").append(entryRow.element);
    	}
    },
    _renderExpenses: function (day) {
    	$(this.element).find(".totalhours").attr("data-duration", "");
    	$(this.element).find(".dataview").removeClass("showmeter");
    	$(this.element).find(".totalhours").css("visibility", "hidden");
    	$(this.element).find(".totalhours").css("display", "");
    	$(this.element).find(".maintitle").removeClass("noentries");
    	$(this.element).find(".maintitle").text(txt("expenses", "workentries"));

    	for (var i = 0; i < day.dayEntries.length; i++) {
    		var entry = day.dayEntries[i];

    		if (!entry.dayentrytype)
    			continue;

    		var entryRow = this._createExpenseEntryRow(entry);

    		entryRow.setTitle(entry.dayentrytype.__displayname);
    		entryRow.setAmount(entry.amount);

    		if (entry.project)
    			entryRow.setProject(entry.project.__displayname);
    		
    		if (entry.approvedbyworker)
    			entryRow.setApproved(true);

    		if (this._uiState.hasNewEntry && this._dataSource._model.lastShownNewId == entry._id) 
    			entryRow.setNewEntry(true);

    		$(this.element).find(".entrycontainer").append(entryRow.element);
    	}
    },
    _renderArticles: function (day) {
    	$(this.element).find(".totalhours").attr("data-duration", "");
    	$(this.element).find(".dataview").removeClass("showmeter");
    	$(this.element).find(".totalhours").css("visibility", "hidden");
    	$(this.element).find(".totalhours").css("display", "");

    	for (var i = 0; i < day.articleEntries.length; i++) {
    		var entry = day.articleEntries[i];

    		if (!entry.article)
    			continue;

    		var entryRow = this._createArticleEntryRow(entry);

    		entryRow.setTitle(entry.article.__displayname);
    		entryRow.setAmount(entry.amount);

    		if (entry.project)
    			entryRow.setProject(entry.project.__displayname);

    		if (entry.approvedbyworker)
    			entryRow.setApproved(true);

    		if (this._uiState.hasNewEntry && this._dataSource._model.lastShownNewId == entry._id) 
    			entryRow.setNewEntry(true);

    		$(this.element).find(".entrycontainer").append(entryRow.element);
    	}
    },
    _renderNothing: function (day) {
    	$(this.element).find(".totalhours").attr("data-duration", "");
    	$(this.element).find(".dataview").removeClass("showmeter");
    	$(this.element).find(".totalhours").css("visibility", "hidden");
    	$(this.element).find(".maintitle").text(txt("notentries", "workentries"));
    	$(this.element).find(".maintitle").addClass("noentries");
    	$(this.element).find(".totalhours").css("display", "none");
    },
	/**
	 * Render day's details other than overtime.
	 */
    _renderDayDetails: function(day, parent, entryRow){
    	if (!parent._id)
    		return;

    	for (var i = 0; i < day.timesheetEntries.length; i++) {
    		var timesheetEntry = day.timesheetEntries[i];

    		if (timesheetEntry.parent && timesheetEntry.parent._id == parent._id && timesheetEntry.timesheetentrydetailpaytype) {
    			payType = this._dataSource._model.payTypesById[timesheetEntry.timesheetentrydetailpaytype._id];

    			var unit = "";
    			var amount = "";

    			if (payType.hasprice) {
    				unit = txt("unit_euro");
    				amount = timesheetEntry.price;
    			}

    			else {
    				unit = txt("unit_hours");
    				amount = this._durationToHours(timesheetEntry.duration);
				}

    			entryRow.addDetail(timesheetEntry.timesheetentrydetailpaytype.__displayname, amount, unit, timesheetEntry);
    		}
    	}    
    },
    _createIdleEntryRow: function(document) {
    	var base = this;
    	var idleEntryRowElement = $(".workentries_templates .idleentryrow").clone().removeClass("__widgettemplate");
    	var idleEntryRow = {
    		element: idleEntryRowElement,

    		document: document,
    		setDuration: function (duration) {
    			$(this.element).find(".idle").attr("data-duration", duration);
    		},
    		setDates: function (start, end) {
    			this.start = new Date(start.getTime());
    			this.end = new Date(end.getTime());
    		}
    	}
    	
    	$(idleEntryRowElement).find(".idle").on("click", function (e) {
    		e.stopPropagation();
    		e.preventDefault();

    		var entryDialogContents = base.element.find(".workentryactionbuttons").removeClass("__widgettemplate").clone();

    		entryDialogContents.find(".expense").hide();
    		entryDialogContents.find(".article").hide();
    		entryDialogContents.find(".asset").hide();
    		
    		entryDialogContents.find("a").each(function () {

    			url = $(this).attr("href");
    			url = addParameterToUrl(url, "selecteddatekey", base._dataSource._model.selectedDateKey);
    			url = addParameterToUrl(url, "start", UTCISOString(idleEntryRow.start));
    			url = addParameterToUrl(url, "end", UTCISOString(idleEntryRow.end));

    			$(this).attr("href", url);
    		});

    		$(window).dialog({
    			object: entryDialogContents.get()[0],
    			minWidth: "200px",
    			height: "auto",
    			width: "auto",
    			maxWidth: "350px",
    			closeButton: false
    		});
    	});

    	return idleEntryRow;
    },
    _createWorkEntryRow: function (document) {
    	var base = this;
    	var workEntryRowElement = $(".workentries_templates .timeentryrow").clone().removeClass("__widgettemplate");

    	var workEntryRow = {
    		element: workEntryRowElement,

			document: document,

    		setTime: function (start, end) {
    			$(this.element).find(".start").text((start) ? base._getTimePart(start) : "")

    			var endHtml = "";

    			if (end) {
    				$(this.element).find(".end").html(base._getTimePart(end));
					// Check to see whether start and end are on the same date
    				if (start.toDateString() === end.toDateString()) {
    					$(this.element).find(".enddate").css("display", "none");
    				} else {
    					$(this.element).find(".enddate").css("display", "flex");
    					$(this.element).find(".enddate>span").text(formatDateShort(end));
    				}
    			}
    			else {
    				$(this.element).find(".enddate").css("display", "none");
    			}
			},
    		setDuration: function (duration) {

				// Convert to hours with two decimals
    			var duration = (duration / (1000 * 60 * 60)).toFixed(2);

    			$(this.element).find(".duration").html(duration + txt("unit_hours"));
    			$(this.element).find(".enddate").css("display", "none");
    		},
    		setTitle: function (title) {
				$(this.element).find(".title").text(title);
    		},
    		setTravelTime: function (isTravelTime) {
    		    if (isTravelTime)
    		        this.element[0].classList.add("traveltime");
    		    else
    		        this.element[0].classList.remove("traveltime");
    		},
    		setPayType: function (payType) {
    			$(this.element).find(".paytype").text(payType);
    		},
    		setNewEntry: function (enabled) {
    		    if (enabled)
    		        $(this.element).addClass("newentry");
    		    else
    		        $(this.element).removeClass("newentry");
			},
			addDetail: function (description, amount, unit, detailentry) {
			    var detail = $("<div class='workentryrowdetail'><div class='description'><span class='unit'>" + amount + unit + "</span> " + description + "</div>" +
			    	"<div class='detailrowactions'><a class='__button __small __modest editbutton'><i class='material-icons'>edit</i></a>" +
			    	"<a class='__button __small __warning __modest removebutton'><i class='material-icons'>delete_forever</i></a></div></div>");

				$(this.element).find(".workentryrowdetails").append(detail);

				detail.find(".editbutton").on("click tap", function (e) {
					e.stopPropagation();
					e.preventDefault();

					// Go go around issue of templates using ids we remove the template
					var editDetailDialogContents = base.element.find(".editdetails").removeClass("__widgettemplate");

					base._filterDetailPayTypes(editDetailDialogContents);

					editDetailDialogContents.find("input[name='duration']").val(detailentry.duration);
					createTimespan(editDetailDialogContents.find(".__timespanfieldcontrol")[0]);

					editDetailDialogContents.find(".__dialog-cancel").off();
					editDetailDialogContents.find(".__dialog-ok").off();

					editDetailDialogContents.find(".__dialog-cancel").on("click", function () {
						$(window).data("dialog").close();
					});

					var payTypeSelect = editDetailDialogContents.find("select[name='timesheetentrydetailpaytype']");

					payTypeSelect.find("select[name='timesheetentrydetailpaytype']").change(function () {
						base._onDetailPayTypeChange(editDetailDialogContents);
					});

					payTypeSelect.val(detailentry.timesheetentrydetailpaytype._id);
					base._onDetailPayTypeChange(editDetailDialogContents);

					editDetailDialogContents.find(".__dialog-ok").on("click", function () {

						var duration = editDetailDialogContents.find("input[name='duration']").val();
						var price = editDetailDialogContents.find("input[name='price']").val();
						var payTypeSelect = editDetailDialogContents.find("select[name='timesheetentrydetailpaytype']");

						var payType = base._dataSource._model.payTypesById[payTypeSelect.val()];

						if (!payType) {
							alert(txt("required_paytype", "workentries"));
							return false;
						}

						if (!payType.noduration && (duration == 0 || !duration)) {
							alert(txt("required_duration", "workentries"));
							return false;
						}

						if (payType.hasprice && (price == 0 || !price)) {
							alert(txt("required_price", "workentries"));
							return false;
						}

						// edit detail

						var url = getAjaxUrl("workentries", "editdetail");

						if (!payType.noduration && duration && duration != 0)
							url = addParameterToUrl(url, "duration", duration);

						if (payType.hasprice && price && price != 0)
							url = addParameterToUrl(url, "price", price);

						url = addParameterToUrl(url, "paytype", payTypeSelect.val());
						url = addParameterToUrl(url, "id", detailentry._id);

						$.ajax({
							url: url,
							success: function (data, status, xhr) {
								base._dataSource.refreshData();

								// Update paytype favourites
								var favouritesData = getRelationDropdownFavourites(payTypeSelect[0]);

								var favouriteName = $(payTypeSelect).val();
								var displayName = $(payTypeSelect).find("option:selected").text();
								favouritesData.addFavourite(favouriteName, displayName);
							}
						});

						$(window).data("dialog").close();
					});

					$(window).dialog({
						object: editDetailDialogContents.get()[0],
						minWidth: "50px",
						height: "auto",
						width: "auto",
						maxWidth: "350px",
						initMc2Controls: true,
						closeButton: false,
						closeable: false,
						hideElementsOnClose: false,
						customScrolling: false,
						enableDefaultClick: true
					});
				});

				detail.find(".removebutton").on("click tap", function () {
					var confirmText = txt("removeprompt_detail", "workentries");
					confirmText = confirmText.replace("{0}", description);
					confirmText = confirmText.replace("{1}", workEntryRowElement.find(".title").text());

					if (confirm(confirmText)) {
						restApi.delete(detailentry.__collectionname, detailentry._id).done(function () {
							base._dataSource.refreshData();
						});
					}
				});
			},
			_setDuration: function (type, duration) {
				throw("Not implemented");
			},
			_setDetailsButton: function (enabled) {
				if (enabled)
					$(this.element).find(".adddetailbutton").show();
				else
					$(this.element).find(".adddetailbutton").hide();
			},
			setApproved: function (isApproved) {
			    if (isApproved)
			        $(this.element).addClass("approved");
			    else
			        $(this.element).removeClass("approved");
    		},
    		enableActions: function(addDtail, edit, remove) {
    		}
		};

    	this._enableTouchEvents(workEntryRowElement);

    	$(workEntryRowElement).find(".removebutton").on("click tap", function (e) {
    		e.stopPropagation();
    		e.preventDefault();

    		var confirmText = txt("removeprompt", "workentries");
    		confirmText = confirmText.replace("{0}", workEntryRowElement.find(".title").text());

    		if (confirm(confirmText)) {
    			restApi.delete(document.__collectionname, document._id).done(function () {
    				base._dataSource.refreshData();
    			});
    		}
    	});

    	$(workEntryRowElement).find(".adddetailbutton").on("click tap", function (e) {
    		e.stopPropagation();
    		e.preventDefault();

			// Go go around issue of templates using ids we remove the template
    		var addDetailDialogContents = base.element.find(".adddetails").removeClass("__widgettemplate");

    		base._filterDetailPayTypes(addDetailDialogContents);

    		addDetailDialogContents.find("input[name='duration']").val(document.duration);
    		createTimespan(addDetailDialogContents.find(".__timespanfieldcontrol")[0]);

    		addDetailDialogContents.find(".__dialog-cancel").off();
    		addDetailDialogContents.find(".__dialog-ok").off();

    		addDetailDialogContents.find(".__dialog-cancel").on("click", function () {
    			$(window).data("dialog").close();
    		});

    		addDetailDialogContents.find("select[name='timesheetentrydetailpaytype']").change(function () {
    			base._onDetailPayTypeChange(addDetailDialogContents);
    		});

    		addDetailDialogContents.find("select[name='timesheetentrydetailpaytype']").trigger("change");

    		addDetailDialogContents.find(".__dialog-ok").on("click", function () {

    			var duration = addDetailDialogContents.find("input[name='duration']").val();
    			var price = addDetailDialogContents.find("input[name='price']").val();

    			var payTypeSelect = addDetailDialogContents.find("select[name='timesheetentrydetailpaytype']");

    			var payType = base._dataSource._model.payTypesById[payTypeSelect.val()];

    			if (!payType) {
    				alert(txt("required_paytype", "workentries"));
    				return false;
    			}

    			if (!payType.noduration && (duration == 0 || !duration)) {
    				alert(txt("required_duration", "workentries"));
    				return false;
    			}

    			if (payType.hasprice && (price == 0  || !price)) {
    				alert(txt("required_price", "workentries"));
    				return false;
    			}

    			// adddetail

    			var url = getAjaxUrl("workentries", "adddetail");

    			if (!payType.noduration && duration && duration != 0)
    				url = addParameterToUrl(url, "duration", duration);

    			if (payType.hasprice && price && price != 0)
    				url = addParameterToUrl(url, "price", price);

    			url = addParameterToUrl(url, "paytype", payTypeSelect.val());
    			url = addParameterToUrl(url, "parent", document._id);

    			$.ajax({
    				url: url,
    				success: function (data, status, xhr) {
    					base._dataSource.refreshData();

    					// Update paytype favourites
    					var favouritesData = getRelationDropdownFavourites(payTypeSelect[0]);

    					var favouriteName = $(payTypeSelect).val();
    					var displayName = $(payTypeSelect).find("option:selected").text();
    					favouritesData.addFavourite(favouriteName, displayName);
    				}
    			});

    			$(window).data("dialog").close();
    		});

    		$(window).dialog({
    			object: addDetailDialogContents.get()[0],
    			minWidth: "50px",
    			height: "auto",
    			width: "auto",
    			maxWidth: "350px",
    			initMc2Controls: true,
    			closeButton: false,
    			closeable: false,
    			hideElementsOnClose: false,
    			customScrolling: false,
    			enableDefaultClick: true
    		});
    	});

    	$(workEntryRowElement).on("click", function (e) {
    		e.stopPropagation();
    		e.preventDefault();
    	});

    	workEntryRowElement.find(".editbutton, .amountcontainer").on("click tap", function (e) {
    		e.stopPropagation();
    		e.preventDefault();

    		var url;
    		if (document.absenceentrytype) {
    			url = getAjaxUrl("tro", "absenceentry");
    		}
    		else {
    			url = getAjaxUrl("tro", "timesheetentry");
    		}

    		url = addParameterToUrl(url, "actiontype", "modify");
    		url = addParameterToUrl(url, "id", document._id);

    		window.location.href = url;
    	});

    	workEntryRowElement.find(".addbutton").on("click tap", function (e) {
    		e.stopPropagation();
    		e.preventDefault();

    		base._addNewEntryBasedOnExisting(document);
    	});

    	return workEntryRow;

    },
    _onDetailPayTypeChange: function (dialogContents) {
    	var selectedPayType = this._dataSource._model.payTypesById[dialogContents.find("select[name='timesheetentrydetailpaytype']").val()];

    	var duration = dialogContents.find("input[name='duration']");
    	if (!selectedPayType || selectedPayType.noduration) {
    		duration.parent().hide();
    	} else {
    		createTimespan(duration.parent().get()[0]);
    		duration.parent().show();
    	}

    	var price = dialogContents.find("input[name='price']");
    	if (selectedPayType && selectedPayType.hasprice) {
    		price.parent().show();
    	} else {
    		price.parent().hide();
    	}
    },
    _createExpenseEntryRow: function (document) {
    	var base = this;
    	var expenseEntryRowElement = $(".workentries_templates .expenseentryrow").clone().removeClass("__widgettemplate");

    	var expenseEntryRow = {
    		element: expenseEntryRowElement,

    		document: document,

    		setTitle: function (title) {
    			$(this.element).find(".title").text(title);
    		},
    		setProject: function (project) {
    			$(this.element).find(".project").text(project);
    		},
    		setAmount: function (amount) {
    			$(this.element).find(".amount").text(amount);
    		},
    		setApproved: function (isApproved) {
    			if (isApproved)
    				$(this.element).addClass("approved")
    			else
    				$(this.element).removeClass("approved")
    		},
    		setNewEntry: function (enabled) {
    			if (enabled)
    				$(this.element).addClass("newentry")
    			else
    				$(this.element).removeClass("newentry")
    		},
    	};

    	$(expenseEntryRowElement).find(".removebutton").on("click tap", function (e) {
    		e.stopPropagation();
    		e.preventDefault();

    		var confirmText = txt("removeprompt", "workentries");
    		confirmText = confirmText.replace("{0}", expenseEntryRowElement.find(".title").text());

    		if (confirm(confirmText)) {
    			restApi.delete(document.__collectionname, document._id).done(function () {
    				base._dataSource.refreshData();
    			});
    		}
    	});

    	expenseEntryRowElement.find(".editbutton, .amountcontainer").on("click tap", function (e) {
    		e.stopPropagation();
    		e.preventDefault();

    		e.stopPropagation();
    		e.preventDefault();

    		var url = getAjaxUrl("tro", "dayentry");
    		url = addParameterToUrl(url, "actiontype", "modify");
    		url = addParameterToUrl(url, "id", document._id);

    		window.location.href = url;
    	});

    	expenseEntryRowElement.find(".addbutton, .amountcontainer").on("click tap", function (e) {
    		e.stopPropagation();
    		e.preventDefault();

    		base._addNewEntryBasedOnExisting(document);
    	});

    	this._enableTouchEvents(expenseEntryRowElement);


    	return expenseEntryRow;
    },
    _createArticleEntryRow: function (document) {
    	var base = this;
    	var articleEntryRowElement = $(".workentries_templates .articleentryrow").clone().removeClass("__widgettemplate");

    	var articleEntryRow = {
    		element: articleEntryRowElement,

    		document: document,

    		setTitle: function (title) {
    			$(this.element).find(".title").text(title);
    		},
    		setProject: function (project) {
    			$(this.element).find(".project").text(project);
    		},
    		setAmount: function (amount) {
    			$(this.element).find(".amount").text(amount);
    		},
    		setApproved: function (isApproved) {
    			if (isApproved)
    				$(this.element).addClass("approved")
    			else
    				$(this.element).removeClass("approved")
    		},
    		setNewEntry: function (enabled) {
    			if (enabled)
    				$(this.element).addClass("newentry")
    			else
    				$(this.element).removeClass("newentry")
    		},
    	};

    	$(articleEntryRowElement).find(".removebutton").on("click tap", function (e) {
    		e.stopPropagation();
    		e.preventDefault();

    		var confirmText = txt("removeprompt", "workentries");
    		confirmText = confirmText.replace("{0}", articleEntryRowElement.find(".title").text());

    		if (confirm(confirmText)) {
    			restApi.delete(document.__collectionname, document._id).done(function () {
    				base._dataSource.refreshData();
    			});
    		}
    	});

    	$(articleEntryRowElement).find(".editbutton, .amountcontainer").on("click tap", function (e) {
    		e.stopPropagation();
    		e.preventDefault();

    		var url = getAjaxUrl("tro", "articleentry");
    		url = addParameterToUrl(url, "actiontype", "modify");
    		url = addParameterToUrl(url, "id", document._id);

    		window.location.href = url;
    	});

    	articleEntryRowElement.find(".addbutton").on("click tap", function (e) {
    		e.stopPropagation();
    		e.preventDefault();

    		base._addNewEntryBasedOnExisting(document);
    	});

    	this._enableTouchEvents(articleEntryRowElement);

    	return articleEntryRow;
    },
    _createAssetEntryRow: function (document) {
    	var base = this;
    	var assetEntryRowElement = $(".workentries_templates .assetentryrow").clone().removeClass("__widgettemplate");

    	var assetEntryRow = {
    		element: assetEntryRowElement,

    		document: document,

    		setTime: function (start, end) {
    			$(this.element).find(".start").text((start) ? base._getTimePart(start) : "")
    			$(this.element).find(".end").text((end) ? base._getTimePart(end) : "")
    		},
    		setTitle: function (title) {
    			$(this.element).find(".title").text(title);
    		},
    		setDuration: function (type, duration) {
    			throw ("Not implemented");
    		},
    		setProject: function (project) {
    			$(this.element).find(".project").text(project);
    		},
    		setApproved: function (isApproved) {
    			if (isApproved)
    				$(this.element).addClass("approved")
    			else
    				$(this.element).removeClass("approved")
    		},
    		setNewEntry: function (enabled) {
    			if (enabled)
    				$(this.element).addClass("newentry")
    			else
    				$(this.element).removeClass("newentry")
    		},
    	};

    	$(assetEntryRowElement).find(".removebutton").on("click tap", function (e) {
    		e.stopPropagation();
    		e.preventDefault();

    		var confirmText = txt("removeprompt", "workentries");
    		confirmText = confirmText.replace("{0}", assetEntryRowElement.find(".title").text());

    		if (confirm(confirmText)) {
    			restApi.delete(document.__collectionname, document._id).done(function () {
    				base._dataSource.refreshData();
    			});
    		}
    	});

    	$(assetEntryRowElement).on("click tap", function (e) {
    		e.stopPropagation();
    		e.preventDefault();

    		var url = getAjaxUrl("tro", "assetentry");
    		url = addParameterToUrl(url, "actiontype", "modify");
    		url = addParameterToUrl(url, "id", document._id);

    		window.location.href = url;
    	});

    	assetEntryRowElement.find(".addbutton").on("click tap", function (e) {
    		e.stopPropagation();
    		e.preventDefault();

    		base._addNewEntryBasedOnExisting(document);
    	});

    	this._enableTouchEvents(assetEntryRowElement);

    	return assetEntryRow;
    },
    _addNewEntryBasedOnExisting: function (document) {
    	var base = this;

    	// Should not be possible. Cannot add entries for future work.
    	if (base._uiState.futureDaySelected) {
    	    messageBoxConfirm(txt("futureentriesnotallowed", "workentries"), false, "info");
    	    return false;
    	}

    	var entryDialogContents = $(".homescreenprojects_templates .addbuttons").removeClass("__widgettemplate").clone(); // §§§ todo: use proper reference to parent widget / other widget!!!

    	var start = null;
    	var end = null;

    	// Todo: move to it's own method to avoid copy/paste code
    	if (features.timetracking) {
    		// Start after last timed entry for today or if there are no timed entries then send no start and end
    		// and use default (start now)
    		var selectedDay = base._dataSource._model.days[base._dataSource._model.selectedDateKey];

    		var lastEntry = base._getLastPrimaryEntry();

    		// Only set default date if last entry's end date is still today. Go with default otherwise.
    		if (lastEntry) {
    			var start = lastEntry.endtimestamp;
    			var end = new Date(start.getTime() + (60 * 60 * 1000)); // Default to 1 hour after start

    			// If end is tomorrow (or later) use default start and end.
    			if (lastEntry.starttimestamp.toDateString() !== lastEntry.endtimestamp.toDateString()) {
    				start = null;
    				end = null;
    			}
    		}
    	}

    	// If there is no favored start or end date user selected day and current time and end time one minuts after that
    	if (!start || !end) {
    		var now = new Date();
    		start = base._dataSource.getDateFromKey(base._dataSource._model.selectedDateKey)
    		start.setHours(now.getHours());
    		start.setMinutes(now.getMinutes());

    		var end = new Date(start.getTime() + (60 * 60 * 1000)); // Default to 1 hour after start
    	}

    	entryDialogContents.find("a").each(function () {
    		var url = $(this).attr("href");
    		url = addParameterToUrl(url, "selecteddatekey", base._dataSource._model.selectedDateKey);

    		if (document.project)
    			url = addParameterToUrl(url, "workscheduleprojectid", document.project._id);

    		if (start && end) {
    			url = addParameterToUrl(url, "start", UTCISOString(start));
    			url = addParameterToUrl(url, "end", UTCISOString(end));
    		}

    		$(this).attr("href", url);
    	});

    	$(window).dialog({
    		object: entryDialogContents.get()[0],
    		minWidth: "200px",
    		height: "auto",
    		width: "auto",
    		maxWidth: "250px",
    		closeButton: false,
    		customScrolling: false
    	});
    },
    _filterDetailPayTypes: function (target) {
    	var personClaContract = "[none]";
    	if (this._dataSource._model.userClaContract)
    		personClaContract = this._dataSource._model.userClaContract;

    	// Clear and repopulate paytype selection
    	var payTypeSelect = $(target).find("select[name='timesheetentrydetailpaytype']");

    	payTypeSelect.html("");

    	// Add favourites
    	var favouritesData = getRelationDropdownFavourites(payTypeSelect[0]);
    	if (favouritesData) {

    		var optionGroup = $("<optgroup label='" + txt("recent") + "'></optgroup>");

    		var favouritesFound = false;

    		for (var i = 0; favouritesData.favourites && i < favouritesData.favourites.length; i++) {
    			var favourite = favouritesData.favourites[i];

    			if (!this._dataSource._model.payTypesById[favourite.name])
    				continue;

    			var payType = this._dataSource._model.payTypesById[favourite.name];

    			var option = $("<option value='" + payType._id + "'>" + payType.name + "</option>")
                    .data("countsasregularhours", payType.countsasregularhours)
                    .data("isovertime50", payType.isovertime50)
                    .data("isovertime100", payType.isovertime100)
                    .data("isovertime150", payType.isovertime150)
                    .data("isovertime200", payType.isovertime200)
                    .data("issocialpaytype", payType.issocialpaytype)

    			optionGroup.append(option);
    			favouritesFound = true;
    		}

    		if (favouritesFound)
    			$(payTypeSelect).append(optionGroup);
    	}

    	for (var i = 0; i < this._dataSource._model.payTypes.length; i++) {

    		var payType = this._dataSource._model.payTypes[i];

    		var claContractFound = false;

    		if (payType.enabledclacontracts !== undefined) {
    			for (var j = 0; j < payType.enabledclacontracts.length; j++) {
    				if (payType.enabledclacontracts[j]._id === personClaContract) {
    					claContractFound = true;
    					break;
    				}
    			}
    		}

			// Todo: add support for social projects if needed.

    		if (claContractFound && !payType.isbasepaytype)
    			payTypeSelect.append($("<option value='" + payType._id + "'>" + payType.name + "</option>")
                    .data("countsasregularhours", payType.countsasregularhours)
                    .data("isovertime50", payType.isovertime50)
                    .data("isovertime100", payType.isovertime100)
                    .data("isovertime150", payType.isovertime150)
                    .data("isovertime200", payType.isovertime200)
                    .data("issocialpaytype", payType.issocialpaytype)
                );
    	}
    },
    _getTimePart: function (date) {
    	var padding = "00"
		// hh:mm
    	return date.getHours() + ":" + padding.substr(0, padding.length - ("" + date.getMinutes()).length) + date.getMinutes();
    },
    _createWorkEntryIdleRow: function () {
    	var base = this;
    	var workEntryIdleRowElement = $(".workentries_templates .workentryidlerow").clone().removeClass("widgettemplate");

    	var workEntryIdleRow = {
    		element: workEntryIdleRowElement,

    		setDuration: function (duration) {
    		},

    	};

    	workEntryIdleRowElement.on("click tap", function () {
    		// Create entry
    	});

    	return workEntryIdleRow;
    },
	/**
	 * Convert duration from milliseconds to hours with two decimals
	 */
    _durationToHours: function (durationMilliseconds) {
    	return Math.round(durationMilliseconds / 1000 / 6 / 6) / 100;
    },
	/*
	 * Return last TSE or absence for today.
	 */
    _getLastPrimaryEntry: function() {
    	var selectedDay = this._dataSource._model.days[this._dataSource._model.selectedDateKey];

    	for (var i = selectedDay.timedEntries.length - 1; i >= 0; i--) {
    		var entry = selectedDay.timedEntries[i];
    		if (!entry.parent)
    			return entry;
    	}

    	return null;
    },
    _enableTouchEvents: function (entryRowElement) {
        $(entryRowElement).on("click tap", function (e) {
            e.stopPropagation();
            e.preventDefault();
            var el = this;

            var siblings = Array.prototype.filter.call(el.parentNode.children, function (child) {
                return child !== el;
            });

            Array.prototype.forEach.call(siblings, function (el, i) {
                el.classList.remove("__active");
            });

            // only show buttons on mobile
            if (this.classList.contains("touch")) {
                this.classList.toggle("__active");
            }

        });

        $(entryRowElement).on("touchstart", function (e) {
            this.classList.add("touch");
            $(this).off("touchstart");
        });

        // swipe functionality
        $(entryRowElement).on("swipeleft", function (e) {
            this.lastTouchX = 0;
            if (!isNullOrUndefined(event.button))
                return false;
            e.stopPropagation();
            if (!$(this).hasClass("__active")) { //skip if active
                collapseAllCards(this);
                $(entryRowElement).on("touchmove.temporary", function (e) { // check delta and cancel if direction changed
                    var delta = this.lastTouchX - event.touches[0].clientX;
                    this.lastTouchX = event.touches[0].clientX;
                    if (delta < 0)
                        $(this).removeClass("__active");
                    else if (delta > 0)
                        showControls(this);
                });
            }
        });

        // Helper functions
        function showControls(el) {
            el.classList.add("__active");
            collapseOtherCards(el);
        }

        function collapseOtherCards(el) {
            $(el).parents(".entrycontainer").find(".workentryrow.__active").not(el).removeClass("__active");
        }

        function collapseAllCards(el) {
            $(el).parents(".entrycontainer").find(".workentryrow.__active").removeClass("__active");
        }
    }
});
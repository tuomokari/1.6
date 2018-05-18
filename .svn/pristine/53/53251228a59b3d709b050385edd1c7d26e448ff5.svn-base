/**
 * totalwork widget
 */
$.widget("totalwork.totalwork", $.core.base, {
    options: {
    },

    /**
     * Fired by jQuery when the widget is created.
     */
    _create: function () {

    	this._uiState = {};
    	this._model = {};
    	this._uiState.firstRender = true;
    	this._model.firstRefresh = true;
    	this._uiState.totalsSelected = false;
    },
	/**
	 * Fired by MC2 when widget is created. Implementation specific initialization code should
	 * be run here
	 */
    _initWidget: function () {
    	this._setupWidgetEvents();

		// Refresh data if totals are selected initially.
    	var selectedTab = $(".__tabhead.__active").find("i").first().text();
    	if (selectedTab == "show_chart") {
    		this.refreshData();
    		this._uiState.totalsSelected = true;
    	}

    	this._determineExtraTypes();

    	this.render();
    },
	/**
	 * Setup widget's events.
	 */
    _setupWidgetEvents: function () {
    	var base = this;
    	$(".__tabhead").on("click", function (event) {
			// Find out 
    		icon = $(this).find("i").first().text();

    		if (icon == "show_chart") {
    			base._uiState.totalsSelected = true;
    			if (!base._model.dataRefreshed)
    				base.refreshData();
    		}
    		else
    		{
    			base._uiState.totalsSelected = false;
    		}

    		base.render({ visibilityonly: true });
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

    	if (this._model.firstRefresh) {
    		this._model.firstRefresh = false;
    		return;
    	}

        // Create an array of promises to wait before calling base and potentially rendering.
        var promises = [];

        var filtersDiv = $("." + this.options.filterdivclass);

        var totalsPromise = app.call("totalwork", "gettotals", {
        	userfilter: filtersDiv.attr("data-userfilter"),
        	profitcenterfilter: filtersDiv.attr("data-profitcenterfilter"),
        	assetfilter: filtersDiv.attr("data-assetfilter"),
        	projectfilter: filtersDiv.attr("data-projectfilter"),
        	resourceprofitcenterfilter: filtersDiv.attr("data-resourceprofitcenterfilter"),
        	resourcebusinessarea: filtersDiv.attr("data-resourcebusinessarea"),
        	resourcefunctionalarea: filtersDiv.attr("data-resourcefunctionalarea"),
        	managerprojectsfilter: filtersDiv.attr("data-managerprojectsfilter"),
        	payrollperiodfilter: filtersDiv.attr("data-payrollperiodfilter"),
        	favouriteusersfilter: filtersDiv.attr("data-favouriteusersfilter"),
        	showonlyentriesnotaccepted: filtersDiv.attr("data-showonlyentriesnotaccepted"),
        	showdaterange: filtersDiv.attr("data-showdaterange"),
        	daterangestart: (filtersDiv.attr("data-daterangestart"))? getDateFromIsoString(filtersDiv.attr("data-daterangestart")) : null,
        	daterangeend: (filtersDiv.attr("data-daterangeend"))? getDateFromIsoString(filtersDiv.attr("data-daterangeend")): null,
        });

        totalsPromise.done(function (data) {
        	base._model.totals = data;
        	base._model.toomanyresults = false;
        });

        totalsPromise.fail(function (data) {
        	base._model.totals = null;

        	if (data && data.responseText == "too many results") {
        		base._model.toomanyresults = true;
			}
        });

        promises.push(totalsPromise);

		var base = this;

		$.when.apply($, promises).always(function () {
			base.render();
		});

		this._model.dataRefreshed = true;
    },
	/**
	 * Find out which additional total types are available based on what's in html table header
	 */
    _determineExtraTypes: function () {
    	var base = this;
    	this._model.extratypes = new Array();

    	$(this.element).find(".totaltype").each(function (i, element) {
			
    		var extraType = {
    			type: $(element).attr("data-totaltype")
    		}

			base._model.extratypes.push(extraType);
    	});
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

    	if (this._model.toomanyresults) {
    		$(this.element).find(".toomanyresultsmessage").show();
    		$(this.element).find(".managertotalstable").hide();
    	} else {
    		$(this.element).find(".toomanyresultsmessage").hide();
    		$(this.element).find(".managertotalstable").show();
		}

    	if (this._uiState.firstRender) {
			// Code run only once when the widget is rendered for the first time.
    		this.element.removeClass("__hidden");
    		this._uiState.firstRender = false;
    		return;
    	}

    	if (this._uiState.totalsSelected) {
    		$(".submit_approvemanager_form").hide();
    		$(".export_excel").hide();
        }
    	else {
    		$(".submit_approvemanager_form").show();
    		$(".export_excel").show();
        }

    	if (options.visibilityonly)
    		return;

    	this._renderTotals();

		// Only render sum line if there are more than one users' results
    	if (this._model.totals && this._model.totals.length > 1)
    		this._renderSumLine();
    },
    _renderTotals: function () {
    	var totals = this._model.totals;

    	$(this.element).find(".totalstablecustomheader").remove();
    	$(this.element).find(".totalstablerow").remove();

    	if (!totals)
    		return;

    	var totalsBody = $(this.element).find(".totalstablebody");
    	var additionalHeaders = {};

    	for (var i = 0; i < totals.length; i++) {
    		var userTotals = totals[i];
    		this.renderTotalsLine(userTotals, totalsBody, additionalHeaders);
    	}
    },
    renderTotalsLine: function (userTotals, totalsBody, additionalHeaders) {

    	if (!userTotals.totalworkinghours)
    		userTotals.totalworkinghours = "";

    	if (!userTotals.totalabsences)
    		userTotals.totalabsences = "";

    	if (!userTotals.overtime)
    		userTotals.overtime = "";

    	if (!userTotals.socialproject)
    		userTotals.socialproject = "";

    	var totalHours = 0;
    	if (userTotals.totalabsences)
    		totalHours += userTotals.totalabsences;

    	if (userTotals.totalworkinghours)
    		totalHours += userTotals.totalworkinghours;

    	var userTotalsLine = "<tr><th class='person'>" + userTotals.__displayname + "</th>" +
		"<td class='regular_hours_content'>" + userTotals.totalworkinghours + "</td>" +
		"<td class='absences_content'>" + userTotals.totalabsences + "</td>" +
		"<td class='overtime_content'>" + userTotals.overtime + "</td>" +
    	"<td class='totalhours_content'>" + totalHours + "</td>";

    	if (features.enablesocialproject)
    		userTotalsLine += "<td class='socialproject_content'>" + userTotals.socialproject + "</td>";

    	for (var i = 0; i < this._model.extratypes.length; i++) {
    		var extraType = this._model.extratypes[i];

    		var extraValue = "";
    		if (userTotals.expensetypes && userTotals.expensetypes[extraType.type])
    			extraValue = userTotals.expensetypes[extraType.type];
			
    		userTotalsLine += "<td>" + extraValue + "</td>";
    	}

    	if (userTotals.expensetypes) {
    		Object.keys(userTotals.expensetypes).forEach(function (key, index) {

    		});
    	}

    	userTotalsLine += "</tr>";

    	totalsBody.append($(userTotalsLine));
    },
    _renderSumLine: function () {
    	var thead = $(this.element).find(".totalstablehead");
    	var tbody = $(this.element).find(".totalstablebody");

    	var sumLineHtml = "<tr><td></td>";

    	thead.find("th").each(function (i, header) {
			// Skip name column
    		if (i != 0) {
    			var sum = 0;
    			tbody.find("tr").each(function (i2, row) {
    				var value = $(row).children().eq(i).text();
    				valueFloat = parseFloat(value);

    				if (!isNaN(valueFloat))
    					sum += valueFloat;
    			});

    			sumLineHtml += "<td>" + sum + "</td>";
    		}
    	});

    	sumLineHtml += "</tr>";

    	tbody.append($(sumLineHtml));
	}

});
$.widget("payrollexportwidget.payrollexportwidget", $.core.base, {
    options: {
    },

    _create: function () {
        this._uiState = new Object;
        this._model = new Object();

        this._uiState.firstRender = true;
        this._setupEvents();
        this._setupTimedTasks();

    },
    _setupTimedTasks: function () {
        var base = this;
        window.setInterval(function () {
            // Use setInterval. With 10 seconds intervals we are not concerned wiht overlap
            base.render(true);
        }, 10000);
    },
    _setupEvents: function () {
        var base = this;
        $(".button_filter").click(
            function (event) {
                event.preventDefault();
                $(".__listview-wrapper.approveworklist, .__listview-wrapper.approveworklist, .__listview-wrapper.approveabsenceentrieslist").attr("page", "0")
                base.render();
            });
        $(".submit_exportpayroll").click(
            function (event) {
            	event.preventDefault();

            	var result = confirm(txt("confirmexport", "payrollexportwidget"));
            	if (!result)
            		return;

            	base._addExport("csv");
            });
        $(".submit_exporttoexcel").click(
            function (event) {
            	event.preventDefault();

            	var result = confirm(txt("confirmexporttoexcel", "payrollexportwidget"));
            	if (!result)
            		return;

            	base._addExport("excel");
            });
        $(".submit_revertpayroll").click(
           function (event) {
				event.preventDefault();

           		var result = confirm(txt("confirmrevert", "payrollexportwidget"));
           		if (!result)
           			return;

           		var user = base.element.find("input[name='userfilter']").val();
           		var payrollPeriod = base.element.find("input[name='payrollperiodfilter']").val();
           		var showOnlyEntriesNotAcceptedByManager = base.element.find("input[name='showonlyentriesnotacceptedbymanager']").is(':checked');
           		var showOnlyEntriesNotAcceptedByWorker = base.element.find("input[name='showonlyentriesnotacceptedbyworker']").is(':checked');
           		var showOnlyExportedEntries = base.element.find("input[name='showonlyexportedentries']").is(':checked');

           		var url = getAjaxUrl("payrollexportwidget", "revert");

           		url = addParameterToUrl(url, "user", user);
           		url = addParameterToUrl(url, "payrollperiod", payrollPeriod);
           		url = addParameterToUrl(url, "onlyentriesnotacceptedbymanager", showOnlyEntriesNotAcceptedByManager);
           		url = addParameterToUrl(url, "onlyentriesnotacceptedbyworker", showOnlyEntriesNotAcceptedByWorker);
           		url = addParameterToUrl(url, "onlyexportedentries", showOnlyExportedEntries);

           		$.ajax({
           			url: url,
           			async: true,
           			success: function (data, status, xhr) {
           				base.render(true);
           				base.render(false);
           			}
           		});

           		clearSearchfilterSelection($("#payrollperiodfilter"));
           });

        // Redirect new button to dialog
        $(".__addbutton").each(function () {
            $(this).click(function (event) {
                event.preventDefault();
                event.stopPropagation();

                var href = $(this).attr("href") + "&isdialog=true";

                $(document.body).dialog({
                    url: href,
                    maxHeight: "90%",
                    height: "90%",
                    width: "90%",
                    maxWidth: "90%",
                    identifier: "entryedit"
                });
            });
        });

        $(".__listview-wrapper").on("listviewshown", function () {
            $(this).find(".__listviewrow").each(function() {
                $(this).click(function (event) {
                    event.preventDefault();
                    event.stopPropagation();

                    var href = $(this).attr("href") + "&isdialog=true";

                    $(document.body).dialog({
                        url: href,
                        maxHeight: "90%",
                        height: "90%",
                        width: "90%",
                        maxWidth: "90%",
                        identifier: "entryedit"

                    });
                });
            });
        });

        $(window).on("dialogclosing", function (event, identifier) {
            if (identifier === "entryedit") {
                base.refreshData(true);
            }
        });
    },
    _addExport: function (target) {
    	var base = this;

    	var payrollPeriod = base.element.find("input[name='payrollperiodfilter']").val();
    	var user = base.element.find("input[name='userfilter']").val();
    	var showOnlyEntriesNotAcceptedByManager = base.element.find("input[name='showonlyentriesnotacceptedbymanager']").is(':checked');
    	var showOnlyEntriesNotAcceptedByWorker = base.element.find("input[name='showonlyentriesnotacceptedbyworker']").is(':checked');
    	var showOnlyExportedEntries = base.element.find("input[name='showonlyexportedentries']").is(':checked');

    	var url = getAjaxUrl("payrollexportwidget", "export");

    	url = addParameterToUrl(url, "payrollperiod", payrollPeriod);
    	url = addParameterToUrl(url, "user", user);
    	url = addParameterToUrl(url, "target", target);
    	url = addParameterToUrl(url, "onlyentriesnotacceptedbymanager", showOnlyEntriesNotAcceptedByManager);
    	url = addParameterToUrl(url, "onlyentriesnotacceptedbyworker", showOnlyEntriesNotAcceptedByWorker);
    	url = addParameterToUrl(url, "onlyexportedentries", showOnlyExportedEntries);

    	$.ajax({
    		url: url,
    		async: true,
    		success: function (data, status, xhr) {
    			base.render(true);
    			base.render(false);
    		}
    	});

    	clearSearchfilterSelection($("#payrollperiodfilter"));
    	clearSearchfilterSelection($("#userfilter"));
    },
    refreshData: function (renderData, refreshLists) {

        var promises = [];

        var base = this;

        // Call super when all data updates have been completed.
        $.when.apply($, promises).done(function () {
            base._super(renderData);
        });
    },

    _setListviewParameters: function (listview, user, payrollPeriod, showOnlyEntriesNotAcceptedByManager,
        showOnlyEntriesNotAcceptedByWorker, showOnlyExportedEntries) {
        var extraparams = 
            "&userfilter=" + user +
            "&payrollperiodfilter=" + payrollPeriod +
            "&showonlyentriesnotacceptedbymanager=" + showOnlyEntriesNotAcceptedByManager +
            "&showonlyentriesnotacceptedbyworker=" + showOnlyEntriesNotAcceptedByWorker +
            "&showonlyexportedentries=" + showOnlyExportedEntries;


        listview.attr("extraparams", extraparams);
    },
    render: function (exportsOnly) {
        this.element.removeClass("__hidden");

        if (this._uiState.firstRender) {
            this._uiState.firstRender = false;
            showListView(this.element.find(".payrollexportlist"));
            return;
        }

        if (exportsOnly) {
            showListView(this.element.find(".payrollexportlist"));
        } else {
            this._refreshListviews();
            this._setActionEnabledState();
            showListView(this.element.find(".payrollexportlist"));
        }
    },
    _refreshListviews: function () {
    	var user = this.element.find("input[name='userfilter']").val();
        var payrollPeriod = this.element.find("input[name='payrollperiodfilter']").val();

        var showOnlyEntriesNotAcceptedByManager = this.element.find("input[name='showonlyentriesnotacceptedbymanager']").is(':checked');
        var showOnlyEntriesNotAcceptedByWorker = this.element.find("input[name='showonlyentriesnotacceptedbyworker']").is(':checked');
        var showOnlyExportedEntries = this.element.find("input[name='showonlyexportedentries']").is(':checked');

        this._setListviewParameters(this.element.find(".approveworklist"), user, payrollPeriod, showOnlyEntriesNotAcceptedByManager, showOnlyEntriesNotAcceptedByWorker, showOnlyExportedEntries)
        this._setListviewParameters(this.element.find(".approvedailyentreislist"), user, payrollPeriod, showOnlyEntriesNotAcceptedByManager, showOnlyEntriesNotAcceptedByWorker, showOnlyExportedEntries)
        this._setListviewParameters(this.element.find(".approveabsenceentrieslist"), user, payrollPeriod, showOnlyEntriesNotAcceptedByManager, showOnlyEntriesNotAcceptedByWorker, showOnlyExportedEntries)

        showListView(this.element.find(".approveworklist"));
        showListView(this.element.find(".approvedailyentreislist"));
        showListView(this.element.find(".approveabsenceentrieslist"));
    },
    _setActionEnabledState: function () {
    	var user = this.element.find("input[name='userfilter']").val();
        var payrollPeriod = this.element.find("input[name='payrollperiodfilter']").val();

        var showOnlyEntriesNotAcceptedByManager = this.element.find("input[name='showonlyentriesnotacceptedbymanager']").is(':checked');
        var showOnlyEntriesNotAcceptedByWorker = this.element.find("input[name='showonlyentriesnotacceptedbyworker']").is(':checked');
        var showOnlyExportedEntries = this.element.find("input[name='showonlyexportedentries']").is(':checked');

        var exportEnabled = true;
        var revertEnabled = true;
        var excelEnabled = true;

    	// Action requires single person or payroll period
        if (isEmpty(user) && isEmpty(payrollPeriod))
        {
            exportEnabled = false;
            revertEnabled = false;
            excelEnabled = false;
        }

        // No actions entries not accepted
        if (showOnlyEntriesNotAcceptedByManager || showOnlyEntriesNotAcceptedByWorker)
        {
            exportEnabled = false;
            revertEnabled = false;
        }

        //No actions for invidual user withóut payroll period
        if (!isEmpty(user) && isEmpty(payrollPeriod))
        {
            exportEnabled = false;
            revertEnabled = false;
            excelEnabled = false;
        }

        if (showOnlyExportedEntries)
            exportEnabled = false;
        else
            revertEnabled = false;

        if (exportEnabled)
            $(".submit_exportpayroll").removeClass("__disabled");
        else
            $(".submit_exportpayroll").addClass("__disabled");

        if (revertEnabled)
            $(".submit_revertpayroll").removeClass("__disabled");
        else
            $(".submit_revertpayroll").addClass("__disabled");

        if (excelEnabled)
        	$(".submit_exporttoexcel").removeClass("__disabled");
        else
        	$(".submit_exporttoexcel").addClass("__disabled");
    }
});
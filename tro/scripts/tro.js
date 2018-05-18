﻿$(function () {

	// Setup tro

    troSetup();

    function troSetup() {

        setupStartEvents();
        performViewSpecificActions();

        $(function () {
            $(document).tooltip();
        });
    }

    function setupStartEvents() {

        setupUpdateEndDatesBasedOnStartDates();
        setupMobileListviewClick();
        setupProjectDetailsDialog();
        setupWorkerApprovalView();
        setupManagerApprovalView();
        setupHrView();
    }

    function setupUpdateEndDatesBasedOnStartDates() {

        // Update end day in timesheet entry if start day is changed
        $("body").on("change", "[name='__starttimestamp_day']", function (e) {

            $("[name='__endtimestamp_day']").val(
                $("[name = '__starttimestamp_day']").val()
                ).change();
        });

        // Update end month in timesheet entry if start month is changed
        $("body").on("change", "[name='__starttimestamp_month']", function (e) {

            $("[name='__endtimestamp_month']").val(
                $("[name = '__starttimestamp_month']").val()
                ).change();
        });

        // Update end year in timesheet entry if start year is changed
        $("body").on("change", "[name='__starttimestamp_year']", function (e) {

            $("[name='__endtimestamp_year']").val(
                $("[name = '__starttimestamp_year']").val()
                ).change();
        });
    }

    function setupMobileListviewClick() {

        // mobile listview item click to work from other than just the text. todo: revise this and potentially graduate to core
        $("body").on("click", ".__listviewtable tr", function (e) {
            // only fire event if "empty space" is clicked
            if (e.target != this) return;

            // If the TD has no a tag it's not intended to work as a link
            if ($(this).find("a").length == 0)
                return;

            var url = $(this).find("a").first().attr("href");
            window.location.href = url;
        });
    }

    function setupProjectDetailsDialog() {
        $("#ts_activeprojectinfo").dialog({
            selector: "#ts_activeprojectdetails",
            height: "90%",
            width: "90%",
            maxWidth: "640px",
            maxHeight: "90%"
        });
    }

    function setupWorkerApprovalView() {
        jQuery('body').on('listviewshown', function (e, listViewWrapper) {

            if ($("body").data("action") === "approvework") {
                $(listViewWrapper).find("[data-approvedbyworker]").each(function () {
                    var tr = $(this).parents("tr").first();
                    tr.addClass("approved");

                    // hide checkbox
                    tr.find("input").first().hide();
                });
            }
        });

        $("body").on("click", ".approvework_daterange", function (event) {
        	event.preventDefault();
			// Temporary fix after __off CSS style started eating clicks
            //$(".approvework_day").addClass("__off");
            //$(".approvework_currentperiod").addClass("__off");
            $(this).removeClass("__off");
            $(".approvework-day-element").hide();
            $(".approvework-daterange-element").show();
            $("[name='approvework_filter_type']").val("daterange");
            $("#approvework_approve_button").hide();

            $(".approvework_show").click();

        });

        $("body").on("click", ".approvework_currentperiod", function (event) {
        	// Temporary fix after __off CSS style started eating clicks
        	//$(".approvework_day").addClass("__off");
            //$(".approvework_daterange").addClass("__off");
            $(this).removeClass("__off");
            $(".approvework-day-element").hide();
            $(".approvework-daterange-element").hide();
            $("[name='approvework_filter_type']").val("notaccepted");
        });
    }

    function setupManagerApprovalView() {
        jQuery('body').on('listviewshown', function (e, listViewWrapper) {

            if ($("body").data("action") === "approveworkmanager") {
                $(listViewWrapper).find("[data-approvedbymanager]").each(function () {
                    var tr = $(this).parent().parent().parent();
                    tr.addClass("approved");

                    // hide approve checkbox for approved hours
                    tr.find("input").first().parent().hide();
                });
            }
        });

        $(".submit_approvemanager_form").click(
            function (event) {
                event.preventDefault();

                var message = this.getAttribute("data-message");
                var result = confirm(message);

                if (!result) {
                    return;
                }

                var extraparams = this.getAttribute("data-extraparams");

                submitApprovemanagerForm(this, extraparams);
            });

        $("#approvemanager_showdaterange").each(setApproveManagerDateRangeVisibility);
        $("#approvemanager_showdaterange").change(setApproveManagerDateRangeVisibility);

        function setApproveManagerDateRangeVisibility() {
            if ($(this).is(":checked")) {
                $("#approveworkmanager_start").show();
                $("#approveworkmanager_end").show();
            } else {
                $("#approveworkmanager_start").hide();
                $("#approveworkmanager_end").hide();
            }
        }
    }

    function setupHrView() {
        $("#hr_showonlyexportedentries").change(function () {
            disableHRActions();
        });

        $("#hr_userfilter-field").change(function () {
            disableHRActions();
        });

        $("#hr_payrollperiodfilter-field").change(function () {
            disableHRActions();
        });

        $("#hr_showonlyentriesnotacceptedbymanager").change(function () {
            disableHRActions();
        });

        $("#hr_showonlyentriesnotacceptedbyworker").change(function () {
            disableHRActions();
        });

        $("#hr_showonlyexportedentries").change(function () {
            disableHRActions();
        });

        $(".submit_exportvisma").click(
            function (event) {
                event.preventDefault();

                var message = this.getAttribute("data-message");
                var result = confirm(message);

                if (!result) {
                    return;
                }

                var extraparams = this.getAttribute("data-extraparams");

                submitApprovemanagerForm(this, extraparams);
            });

        $(".submit_cancelvismaexport").click(
            function (event) {
                event.preventDefault();

                var message = this.getAttribute("data-message");
                var result = confirm(message);

                if (!result) {
                    return;
                }

                var extraparams = this.getAttribute("data-extraparams");

                submitCancelVismaExport(this, extraparams);
            });
    }


    function submitApprovemanagerForm(el, extraparams) {
        var form = $(el).parents("form");

        var action = form.attr("action");

        if (action.indexOf("?") != -1)
            action += "&";
        else
            action += "?";

        form.attr("action", action + "actiontype=approve");
        form.submit();
    }

    function submitCancelVismaExport(el, extraparams) {
        var form = $(el).parents("form");

        var action = form.attr("action");

        if (action.indexOf("?") != -1)
            action += "&";
        else
            action += "?";

        form.attr("action", action + "actiontype=cancel");
        form.submit();
    }

    // Expense entries

    function performViewSpecificActions()
    {
        hideProjectRelationsFromBasicUser();
        hideCrudButtonsForApprovedEntries();

    }

    function hideProjectRelationsFromBasicUser()
    {
        if ($("body").data("controller") == "tro" && $("body").data("action") == "project"
            && getUserLevel() < 3) {
            $(".__panel").find(".__panel").hide();
        }            
    }

    /**
     * Hides buttons related to creating, updating or deleting items that have been already accepted. 
     */
    function hideCrudButtonsForApprovedEntries()
    {
        var action = $("body").data("action");
        var actionType = $("body").data("actiontype");
        var userLevel = parseInt($("body").data("userlevel"));

        if (actionType != "view")
            return;

        if (action !== "timesheetentry" && action !== "assetentry"
            && action !== "absenceentry" && action !== "dayentry" && action !== "articleentry")
            return;

        // Find out if items have been approved by user
        var acceptedByWorker = $("body").find("[data-approvedbyworker]").size() != 0;
        var acceptedByManager = $("body").find("[data-approvedbymanager]").size() != 0;

        var hideCrudButtons = false;

        var preventRelationItemSelect = false;

        if ((userLevel == 1 || userLevel == 2 || userLevel == 6) && acceptedByWorker) {
            hideCrudButtons = true;
            preventRelationItemSelect = true;
        }

        if (userLevel == 3 && acceptedByManager) {
            hideCrudButtons = true;
            preventRelationItemSelect = true;
        }

        if (hideCrudButtons)
        {
            $("body").find("[data-actiontype='remove']").hide();
            $("body").find("[data-actiontype='modify']").hide();
            $("body").find("[data-actiontype='add']").hide();
        }

        if (preventRelationItemSelect)
        {
            jQuery('body').on('listviewshown', function (e, listViewWrapper) {
                $(".__listviewtable a").attr("href", "javascript:");
            });
        }
    }

    function disableHRActions()
    {
        var exportButton = $(".submit_exportvisma").first();
        exportButton.prop("disabled", true);
        exportButton.addClass("__disabled");

        var cancelButton = $(".submit_cancelvismaexport").first();
        cancelButton.prop("disabled", true);
        cancelButton.addClass("__disabled", true);
    }

    function showTimesheetEntryDetails(source) {
		// §§§ TODO: Needs to use related tiemsheetentries instead of details.
    	return;

        // fetch data
        var originalRow = $(source).parents("tr").first();
        var timesheetEntryId = originalRow.data("relation");
        
        var elem = document.createElement("div");
        elem.className = "dummy timesheetentrydetails";
        elem.setAttribute("data-rel", timesheetEntryId);
        $(elem).data("originalposition", originalRow.offset().top);
        $(elem).data("originalrowheight", originalRow.outerHeight());
        $(elem).data("originalcellwidth", $(source).outerWidth());
        $(elem).data("panellimit", $("#__applicationview_inner").offset().left + $("#__applicationview_inner").outerWidth());
        elem = $(elem);
        var tempElem = elem.clone().css({
            visibility: "hidden",
            position: "absolute"
        }).appendTo("body");
        var itemOriginalHeight = tempElem.outerHeight();
        var itemOriginalWidth = tempElem.outerWidth();
        tempElem.remove();
        elem.css({
            position: "absolute",
            display: "block",
            top: elem.data("originalposition") - itemOriginalHeight,
            width: itemOriginalWidth,
            opacity: 0
        });

        elem.css("left", $("body").width() - itemOriginalWidth - 20 + "px"); // -20 for margin
        elem.fadeIn(50);
        elem.html("<i style='display: none' class='fa fa-circle-o-notch fa-spin'></i>");
        elem.appendTo("body");
        elem.find("i").delay(300).fadeIn(250);

        var ajaxUrl = getAjaxUrl("tro", "timesheetentrydetaildata");
        ajaxUrl = ajaxUrl + "&timesheetentryid=" + timesheetEntryId;

        timesheetEntryDetailData = $.ajax({
            dataType: "json",
            url: ajaxUrl,
            success: function (data, status, xhr) {
                var resourcepanel = $("[data-rel='" + timesheetEntryId + "']");
                resourcepanel.html("");
                if (data.item != "null") {
                    for (var key in data.item) {
                        var item = data.item[key];
                        resourcepanel.append("<p>" +
                            item.timesheetentrydetailpaytype.name +
                            "&nbsp;<b>" +
                            getTimeFromMilliseconds(item.duration) +
                            "</b></p>");
                    }
                }

                if (data.parentitem.istraveltime == "true") {
                    resourcepanel.append("<p>" + txt("schema_timesheetentry_istraveltime") + "</p>");
                }

                // calculate and fix position
                var panelHeight;
                var panelWidth;
                var resourcePanelTemp = resourcepanel.clone().css({
                    height: "auto",
                    visibility: "hidden",
                    position: "absolute"
                }).appendTo("body").each(function () {
                    panelHeight = $(this).outerHeight();
                    panelLeft = $(this).offset().left;
                    var top = resourcepanel.data("originalposition");
                }).remove();
                resourcepanel.css({
                    "height": panelHeight,
                    "top": top - panelHeight,
                    opacity: 1
                });
            }
        });
    }
});

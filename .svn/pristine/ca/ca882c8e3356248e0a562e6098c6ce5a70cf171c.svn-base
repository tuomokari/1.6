//var uploadedImages = 0;

// force blockdiv removal on browser back to fix safari iOS issues with spinner and blockscreen
// todo: check if this is necessary
window.onpageshow = function (event) {
    if (event.persisted) {
        $("body").removeClass("__blockscreen");
    }
};

/**
 * Array containing each widget.
 */
var widgets = {};
var widgetsArray = new Array();

/**
 * Array containing each  intarface provider object (widget, name)
 */
var interfaceProvidersArray = new Array();

/**
 * Object containing arrays for each interface provider type. Each array contains widgets.
 */
var interfaceProviders = {}

$(function () {

    // variables
    var applicationView = $("#__applicationview_inner");

    initCorePlugins();

    initWidgets();

    $.ajaxSetup({ cache: false });

    initFields(document.body);

    setupCoreEvents();

    //#region formlogic

    processViewFormFields(document.body);
    processLocalizedValues();

    // Default action tabs load listview content when first clicked
    $(".__defaultformtabs").each(function () {
        var tabs = $(this);

        // Load listview inside tab when clicked
        $(this).find(".__tabhead").on("click", function () {
            var tabindex = $(this).index();
            var defaultActionListView = tabs.find(".__tab").eq(tabindex).find(".__defaultactionlistview").first();
            var wasClicked = defaultActionListView.attr("data-clicked");

            if (wasClicked != "true") {
                defaultActionListView.attr("data-clicked", "true");

                showListView(defaultActionListView);
            }
        });

        var historyTabIndex = tabs.attr("data-historytabindex");

        // Load listview based on history state
        if (historyTabIndex) {
            tabIndex = parseInt(historyTabIndex, 10);

            var defaultActionListView = tabs.find(".__tab").eq(tabIndex).find(".__defaultactionlistview").first();

            defaultActionListView.attr("data-clicked", "true");
            showListView(tabs.find(".__tab").eq(tabIndex).find(".__defaultactionlistview").first());
        }
    });



    //#endregion

    // Default action
    function addRelatedElement() {
        elementid = $(this).attr("elementid")
    }

    function initWidgets() {
        $("[data-widget='true']").each(function () {

            var widgetName = $(this).data('widgetname');
            var widgetNamespace = $(this).data('widgetnamespace');

            // Link the widget to it's JavaScript
            $(this)[widgetName]();

            var widget = $(this).data(widgetNamespace + "-" + widgetName);


            // Add data attributes

            var data = $(this).data();

            for (var itemKey in data) {
                if (!data.hasOwnProperty(itemKey))
                    continue;

                widget.options[itemKey] = data[itemKey];
            }

            widgetsArray.push(widget);

            // Create a shortcut to widgets based on their element's identifier.
            if (this.id)
                widgets[this.id] = widget;
        });

        runWidgetInitFunctions();
        refreshWidgetData();
    }

    function runWidgetInitFunctions() {
        for (var i = 0; i < widgetsArray.length; i++) {

            var widget = widgetsArray[i];

            if (widget._initWidget !== undefined)
                widget._initWidget();
        }
    }

    function refreshWidgetData() {
        for (var i = 0; i < widgetsArray.length; i++) {

            var widget = widgetsArray[i];

            if ($(widget.element).attr("data-initialrefresh") == "true")
                widget.refreshData(true, true);
        }
    }

    function initCorePlugins() {

        // idempotence
        $(".__singleuse").singleUse();

        // datepicker
        $(".__datepicker").datepicker({
            weekHeader: txt("week_short"),
            dayNamesMin: [txt("sunday_short"), txt("monday_short"), txt("tuesday_short"), txt("wednesday_short"), txt("thursday_short"), txt("friday_short"), txt("saturday_short")],
            monthNames: [txt("january"), txt("february"), txt("march"), txt("april"), txt("may"), txt("june"), txt("july"), txt("august"), txt("september"), txt("october"), txt("november"), txt("december")],
            showWeek: true,
            firstDay: 1, // todo: get from locale
            onClose: function (date, inst) {
                var fieldControl = $(this).parents(".__datetimefieldcontrol");
                fieldControl.find(".__day").val(inst.selectedDay).change();
                fieldControl.find(".__month").val(inst.selectedMonth).change();
                fieldControl.find(".__year").val(inst.selectedYear).change();

                updateDateTimeString(fieldControl.get()[0], false, false);
            }
        });

        $(".__datepicker_toggle").click(function () {
            var dateString = $(this).parents(".__datetimefieldcontrol").find(".__datestring").val();
            var currDate = getDateFromIsoString(dateString); // todo: do we need timeaccuracy?
            if (isValidDate(currDate)) {
                $(this).parents(".__datetimefieldcontrol").find(".__datepicker").datepicker("setDate", currDate);
            }
            $(this).parents(".__datetimefieldcontrol").find(".__datepicker").datepicker("show");
        });

        // Accordion panel
        $(".__accordion").each(function () {
            var args = {};
            if ($(this).hasClass("__closeother")) {
                args.closeOther = true;
            }
            if ($(this).hasClass("__expand")) {
                args.expandAll = true;
            }
            if ($(this).hasClass("__initial")) {
                args.initial = true;
                args.initialindex = $(this).attr("initial");
            }
            $(this).toggleablePanel(args);
        });

        // Dialogs
        $(".__dialog_open").dialog();

        // Tabs
        $(".__tabs").each(function () {

            // Get default initial tab from tab data attribute
            var initial = $(this).attr("data-initial");

            if (initial)
                initial = parseInt(initial);
            else
                initial = 0;

            $(this).tabs({
                initial: initial,
                usehistory: true
            });
        })
    }

    function setupCoreEvents() {

        // flexpanels
        $(".__flexpanel-wrapper").each(function () {
            flexPanel(this);
        });

        // disable clicks from disabled checkboxes (needed because of IE10 that doesn't support pointer-events: none)
        $("input[data-disabled='true']").on("click", function (e) {
            e.preventDefault();
            e.stopPropagation();
            return false;
        });
        // toggle switch checkboxes with a swipe
        $(".__checkbox.__switch+label").on("swiperight", function (e) {
            console.log("swipe, active");
            e.preventDefault();
            e.stopPropagation();
            document.getSelection().removeAllRanges();

            $(this).prev().prop("checked", true);
        });
        $(".__checkbox.__switch+label").on("swipeleft", function (e) {
            console.log("swipe, inactive");
            e.preventDefault();
            e.stopPropagation();
            document.getSelection().removeAllRanges();

            $(this).prev().prop("checked", false);
        });
        $(".__checkbox.__switch+label").on("click", function (e) {
            console.log("mouseup");
            e.preventDefault();
            e.stopPropagation();

            document.getSelection().removeAllRanges();
            $(this).prev().prop("checked", !$(this).prev().prop("checked"));
        });

        setupButtonConfirmationMessages();

        //#region form submit

        $("form").submit(function (e) {
            //$("body").data("blockscreen").show({ // todo: update to new blockscreen with proper timing
            //    shade: true,
            //    shadeDelay: 600,
            //    spinner: true,
            //    spinnerDelay: 0
            //});
            if ($(this).data("submitted") != true) {
                $(this).data("submitted", true);
            }
            else {
                return false;
            }
        });

        //#endregion

        // back button
        $(".__icon-back").on("click", function () {
            window.location.href = "/main.aspx?controller=navigation&action=previous&historytoken=" + $("body").data("historytoken");
        });

        // logo home button
        $("#__menubar_logo").click(function () {
            window.location.href = "/";
        });

        $("#__menubutton").on("click tap", function (e) {
            e.stopPropagation();
            e.preventDefault();
            toggleMenu();
        });

        $("#__mobilemenu .__menuitem a").on("click touchstart", function (e) {
            $(this).parents("li").first().addClass("__hover");
        });

        $("#__mobilemenu .__menuitem a").on("touchend", function (e) {
            $(this).parents("li").first().removeClass("__hover");
        });

        $("#__mobilemenu .__menuitem a").click(function (e) {
            e.preventDefault();
            toggleMenu();
            var href = $(this).attr("href");
            setTimeout(function () {
                window.location.href = href;
            }, 120); // must correspond to animation length in css
        });

        // Items that cancel if we are in a dialog iframe
        $(".__closedialog").on("click", function (e) {
            e.stopPropagation();
            closeParentDialog();
        });

        // Adds a new item within one to many relation field panel
        $("input.__core-addrelatedelement").click(addRelatedElement);

        // listviews

        // Remove item counter when listview starts updating
        jQuery('body').on('listviewupdating', function (e, listViewWrapper) {
            var index = $(listViewWrapper).parents(".__tab").first().index();
            $(listViewWrapper).parents(".__tabs").first().find(".__tabhead").eq(index).find(".__itemcounter").remove();
        });

        jQuery('body').on('listviewshown', function (e, listViewWrapper) {
            var totalRecords = $(listViewWrapper).children().first().data("totalrecords");
            var noResults = $(listViewWrapper).find(".__noresults");
            var isDefaultForm = $(".__defaultformtabs").length != 0

            var index = $(listViewWrapper).parents(".__tab").first().index();

            $(listViewWrapper).parents(".__tabs").first().find(".__tabhead").eq(index).find(".__itemcounter").remove();

            if (isDefaultForm)
                ; // Do not show results for default forms.
            else if (typeof totalRecords !== "undefined" && totalRecords != -1)
                $(listViewWrapper).parents(".__tabs").first().find(".__tabhead").eq(index).append("<span class='__itemcounter'>" + totalRecords + "</span>");
            else if (noResults.length > 0)
                $(listViewWrapper).parents(".__tabs").first().find(".__tabhead").eq(index).append("<span class='__itemcounter'>0</span>");
            else
                $(listViewWrapper).parents(".__tabs").first().find(".__tabhead").eq(index).append("<span class='__itemcounter'>" + txt("error_error", "core") + "</span>");

            $(listViewWrapper).find(".__listviewheadercheckbox input").on("click", function () {
                if ($(this).prop("checked")) {
                    listViewWrapper.find(".__listviewcheckbox input").prop("checked", false).click();
                }
                else {
                    listViewWrapper.find(".__listviewcheckbox input").prop("checked", true).click();
                }
            });

            $(listViewWrapper).find(".__listviewcheckbox input").change(function (e) {
                e.preventDefault();
                if ($(this).prop("checked")) {

                    $(this).parents("tr").first().addClass("__selected");
                }
                else {
                    $(this).parents("tr").first().removeClass("__selected");
                }
            });

            $(listViewWrapper).find(".__listviewtools_more").click(function (e) {
                $(this).parents(".__listviewtools").find(".__listviewtools_more_fields").slideToggle();
            });

        });

    }
});

function flexPanel(el) {
    var panel = $(el);
    var panelHandle = panel.find(".__flexpanel-handle");
    panel.find(".__flexpanel-handle").on("staticClick", function () {
        $(this).prev().toggleClass("collapsed");
    });
    //panel.hasClass("collapsed").find(".__flexpanel-left").on("click", function () {
    //    panel.removeClass("collapsed");
    //});

    // scrolling
    if (panel.hasClass("individualscrollbars")) {

        panel.find(".__flexpanel-column").each(function () {

            var panelElement = this;

            $(this).data("scrollbar", new IScroll(this, {
                scrollbars: true,
                mouseWheel: true,
                fadeScrollbars: true,
                resizeScrollbars: true,
                interactiveScrollbars: true,
                bounce: false,
                preventDefaultException: {
                    tagName: /^(INPUT|TEXTAREA|BUTTON|SELECT|LABEL)$/i
                }
            }));

            // Shows the scrollbars when mouse pointer in vicinity -- todo: this should be made generic (it is a missing functionality of IScroll :( 
            var scrollBar = $(this).data("scrollbar");

            $(this).data("scrollbar-rects", scrollBar.indicators[0].wrapper.getClientRects()[0]);

            var rects = $(this).data("scrollbar-rects");

            $(document).on('mousemove', function (e) {
                rects = $(panelElement).data("scrollbar-rects");
                if (e.clientX > rects.left - 20 &&
                    e.clientX < rects.right + 20 &&
                    e.clientY > rects.top - 20 &&
                    e.clientY < rects.bottom + 20 &&
                    $(panelElement).is(":not(.scrolling-locked)"))
                    $(scrollBar.indicators[0].wrapper).addClass("show");
                else
                    $(scrollBar.indicators[0].wrapper).removeClass("show");
            });

            $(window).on("resize", function () {
                $(panelElement).data("scrollbar-rects", scrollBar.indicators[0].wrapper.getClientRects()[0]);
            });

        });
    }

    panel.draggable = panel.find(".__flexpanel-handle").draggabilly({
        axis: "x",
        containment: panel
    });

    panel.draggable.on("dragStart", function () {
        $(this).addClass("active");
    });

    panel.draggable.on("dragEnd", function () {
        var panelOffset = $(this).offset().left;
        var panelCurrentWidth = $(this).prev().offset().left;
        var panelWidth = panelOffset - panelCurrentWidth;
        if (panelWidth < 0)
            panelWidth = 0;
        $(this).removeClass("active");
        $(this).prev().removeClass("collapsed");
        $(this).prev().css({
            minWidth: panelWidth + "px",
            width: panelWidth + "px",
            flexBasis: 0
        });
        $(this).attr("style", "");
        updateScrollBars(panel);

    });
}

function initFields(rootElement) {
    $(rootElement).each(function () {

        // datetime

        $(this).find(".__datetimefieldcontrol").each(function () {
            var currentControl = this;
            $(currentControl).find("select").each(function () {
                $(this).change(function () {
                    if ($(this).parentsUntil(".__control").hasClass("__datetime"))
                        updateDateTimeString(currentControl, true, true);
                    else
                        updateDateTimeString(currentControl, true, false);
                });

            });

            // Initially update datetime only if value exists.
            if (!isEmpty($(this).find("__datetimevalue").val())) {
                if ($(this).parentsUntil(".__control").hasClass("__datetime") || $(this).find(".__datetimecontrolwrapper").hasClass("__datetime"))
                    setTimeout(function () { updateDateTimeString(currentControl, true, true); }, 0);
                else
                    setTimeout(function () { updateDateTimeString(currentControl, true, false); }, 0);

            } else {
                // Unselect current year. This is a bit of a hack and should be redone when convertig date picker to a widget.
                $(this).find("select").val("");
            }

            createDateTimeString(currentControl);
        });

        // timespan

        $(this).find(".__timespanfieldcontrol").each(function () {
            var currentControl = this;
            $(currentControl).find("select").each(function () {
                $(this).change(function () {
                    updateTimespan(currentControl);
                });
            });

            createTimespan(currentControl);
            updateTimespan(currentControl);
        });

        // numbers

        $(this).find(".__numberfieldcontrol").each(function () {
            var currentControl = this;
            $(currentControl).find("input#__displayvalue_" + currentControl.id).each(function () {
                $(this).change(function () {
                    updateNumber(currentControl);
                });
            });

            createNumber(currentControl);
        });

        // booleans

        $(this).find(".__checkbox").each(function () {
            var currentControl = this;
            if ($(this).attr("data-historyenabled")) {
                $(this).change(function () {
                    updateNumber(currentControl);
                });

            }
        });

        // Relation dropdown

        $(this).find(".__relationdropdown, .__selectiondropdown").each(function () {
            if ($(this).data("noinitialupdate") !== true)
                updateRelationDropdown(this);

            setupRelationDropdownFavourites(this);
        });

        function setupRelationDropdownFavourites(relationDropdown) {
            var favouritesData = getRelationDropdownFavourites(relationDropdown);

            if (!favouritesData)
                return null;

            $(relationDropdown).parents("form").first().on("submit", function () {

                var favouriteName = $(relationDropdown).val();
                var displayName = $(relationDropdown).find("option:selected").text();

                favouritesData.addFavourite(favouriteName, displayName);
            });
        }

        function updateRelationDropdown(currentControl) {
            var extraInfoName = currentControl.name + "_hiddenfield";
            var extraInfo = $("#" + extraInfoName);

            var collection = extraInfo.attr("data-collection");
            var selectedValue = extraInfo.attr("data-value");

            var filterController = extraInfo.attr("data-filtercontroller");
            var filterAction = extraInfo.attr("data-filteraction");

            var extraparams = "&collection=" + collection + "&historytoken=" + $("body").data("historytoken");

            if (filterAction !== undefined && filterController !== undefined && filterController !== "" && filterAction !== "")
                extraparams += "&filtercontroller=" + filterController + "&filteraction=" + filterAction;

            var ajaxUrl = getAjaxUrl("form", "relationdropdown") + extraparams;

            $.ajax({
                dataType: "json",
                url: ajaxUrl,
                async: false,
                cache: false,
                success: function (data, status, xhr) {
                    if (!verifyLoginStatus(data)) return;

                    var favouritesData = getRelationDropdownFavourites(currentControl);

                    // Add favourites
                    if (favouritesData) {

                        var optionGroup = $("<optgroup label='" + txt("recent") + "'></optgroup>");

                        var favouritesFound = false;

                        for (var i = 0; favouritesData.favourites && i < favouritesData.favourites.length; i++) {
                            var favourite = favouritesData.favourites[i];

                            if (!data[favourite.name])
                                continue;

                            var item = data[favourite.name];

                            var option =
								$("<option></option>")
									.attr("value", favourite.name)
									.text(item.name);

                            optionGroup.append(option);
                            favouritesFound = true;
                        }

                        if (favouritesFound)
                            $(currentControl).append(optionGroup);
                    }

                    var option =
                        $("<option></option>")
                        .attr("value", "")
                        .text("");

                    $(currentControl).append(option);

                    // Extract items from object to array
                    var items = new Array();
                    for (var itemKey in data) {
                        if (!data.hasOwnProperty(itemKey))
                            continue;

                        var object = {
                            name: data[itemKey].name,
                            __value: itemKey
                        }

                        items.push(object);
                    }

                    // Sort the array
                    items.sort(function (a, b) {
                        if (a.name > b.name) return 1;
                        else if (a.name < b.name) return -1;
                        else return 0
                    });

                    // Append to select
                    for (var i = 0; i < items.length; i++) {
                        var item = items[i];

                        // Do not show dropdown entries with empty name
                        if (item.name == "")
                            continue;

                        var option =
                            $("<option></option>")
                            .attr("value", item.__value)
                            .text(item.name)


                        $(currentControl).append(option);
                    }

                    if (selectedValue != "")
                        $(currentControl).val(selectedValue);

                    $(currentControl).trigger("dropdownupdated");
                }
            });
        }
    });
}

function mobile() {
    var mq = window.matchMedia("only screen and (max-width: 750px)");
    return mq.matches;
}

function createDateTimeString(element) {
    $(element).find(".__cleardatefield").bind("click", function (e) {
        e.preventDefault();
        $(element).find("select").each(function () {
            $(this).children("option:eq(0)").prop("selected", true);

            var dateValueField = document.getElementById("__datevalue" + element.id);
            $(dateValueField).val("");

        });
    });
    $(element).find(".__setdatefieldnow").bind("click", function (e) {
        e.preventDefault();
        $(element).find(".__cleardatefield").click();
        if ($(this).parents(".__datetimecontrolwrapper ").hasClass("__datetime"))
            updateDateTimeString(element, true, true);
        else
            updateDateTimeString(element, true, false);
    });

    var dateValueField = document.getElementById("__datevalue" + element.id);
    var currDateString = dateValueField.value;
    if (currDateString.length < 26)
        return false;

    var timeAccuracy = parseInt($(element).attr("data-timeaccuracy"), 10);
    var currDate = getDateFromIsoString(currDateString, timeAccuracy);

    // End dates in daterange are displayed one day before the actual date's end.
    if ($(element).attr("data-isenddate") === "true") {
        currDate.setDate(currDate.getDate() - 1);
    }

    var lengthCorrectedHourString = currDate.getHours().toString();
    var lengthCorrectedMinuteString = currDate.getMinutes().toString();
    var lengthCorrectedSecondString = currDate.getSeconds().toString();

    if (lengthCorrectedHourString.length < 2)
        lengthCorrectedHourString = "0" + lengthCorrectedHourString;


    if (lengthCorrectedMinuteString.length < 2)
        lengthCorrectedMinuteString = "0" + lengthCorrectedMinuteString;

    if (lengthCorrectedSecondString.length < 2)
        lengthCorrectedSecondString = "0" + lengthCorrectedSecondString;

    var dateValues = {
        "year": currDate.getFullYear(),
        "month": currDate.getMonth(),
        "day": currDate.getDate(),
        "hour": lengthCorrectedHourString,
        "minute": lengthCorrectedMinuteString,
        "seconds": lengthCorrectedSecondString,
        "milliseconds": currDate.getMilliseconds(),
        "timezone": currDate.getTimezoneOffset()
    };

    $(element).find("select").each(function () {
        $(this).val(dateValues[$(this).data("datepart")]);
    });

    // Set dateValueField to new value in case there was any rounding.
    UTCISOString(currDate);
}

function UTCISOString(date) {
    var dateString = date.toISOString();
    return dateString + "00";
}

function UTCISODateString(date) {
    var monthString = date.getMonth() + 1; // getMonth is zero based
    var dateString = date.getDate();
    if (monthString < 10)
        monthString = "0" + monthString;
    if (dateString < 10)
        dateString = "0" + dateString;
    var dateString = date.getFullYear() + "-" + monthString + "-" + dateString + "T00:00:00.000Z00";
    return dateString;
}

function updateDateTimeString(element, setRelated, updateTime) {
    var dateValueField = document.getElementById("__datevalue" + element.id);

    var isEndDate = ($(element).attr("data-isenddate") === "true");

    var currDate = newDate();

    var timeAccuracy = parseInt($(element).attr("data-timeaccuracy"), 10);
    currDate = roundToTimeAccuracy(currDate, timeAccuracy);

    if (updateTime) {
        var dateValues = {
            "year": currDate.getFullYear(),
            "month": currDate.getMonth(),
            "day": currDate.getDate(),
            "hour": currDate.getHours(),
            "minute": currDate.getMinutes(),
            "seconds": "00",
            "milliseconds": "000",
            "timezone": currDate.getTimezoneOffset()
        };
    }
    else {
        var dateValues = {
            "year": currDate.getFullYear(),
            "month": currDate.getMonth(),
            "day": currDate.getDate(),
            "hour": "00",
            "minute": "00",
            "seconds": "00",
            "milliseconds": "000",
            "timezone": currDate.getTimezoneOffset()
        };
    }

    var elementId = element.id;

    $(element).find("select").each(function () {
        var currValue = $(this).val();
        if (currValue != null && currValue != "") {
            if (currValue < 10 && ($(this).data("datepart") == "hour" || $(this).data("datepart") == "minute"))
                currValue = "0" + currValue;
            dateValues[$(this).data("datepart")] = $(this).val();
        }
        else if (setRelated) {
            var relatedValue = dateValues[$(this).data("datepart")];
            if (relatedValue < 10 && ($(this).data("datepart") == "hour" || $(this).data("datepart") == "minute"))
                relatedValue = "0" + relatedValue;
            $(this).val(relatedValue);
        }
    });

    var dateFieldDate = new Date(
        dateValues["year"],
        dateValues["month"],
        parseInt(dateValues["day"], 10),
        dateValues["hour"],
        dateValues["minute"],
        dateValues["seconds"],
        dateValues["milliseconds"]);

    dateFieldDate = new Date(dateFieldDate.getTime());

    // For time ranges the end date's actual value is one higher than actually shown. Therefore when
    // initially setting the value we substract one day from the shown value and then add one when the
    // value is updated. To server we always send the correct timestamp.
    if (isEndDate) {
        dateFieldDate.setDate(dateFieldDate.getDate() + 1);
    }

    if (updateTime)
        dateValueField.value = UTCISOString(dateFieldDate);
    else
        dateValueField.value = UTCISODateString(dateFieldDate);
}

function createTimespan(el) {
    var timespanValueField = document.getElementById("__value" + el.id);

    var timespanValue = timespanValueField.value;

    var timespanInt = 0;

    if (timespanValue !== "")
        timespanInt = parseInt(timespanValue, 10);

    var timeAccuracy = parseInt($(el).attr("data-timeaccuracy"), 10);

    var hour = Math.floor(timespanInt / (1000 * 60 * 60));
    var minutes = Math.floor((timespanInt - hour * 1000 * 60 * 60) / (1000 * 60));

    var roundedMinutes = Math.round(minutes / timeAccuracy) * timeAccuracy;

    if (hour < 10)
        hour = "0" + hour;

    if (roundedMinutes < 10)
        roundedMinutes = "0" + roundedMinutes;

    var elId = el.id;

    var minuteField = document.getElementById("__minute" + elId);
    var hourField = document.getElementById("__hour" + elId);

    $(hourField).val(hour);
    $(minuteField).val(roundedMinutes);
}

function updateTimespan(el) {
    var elementId = el.id;
    var minuteField = document.getElementById("__minute" + elementId);
    var hourField = document.getElementById("__hour" + elementId);
    var timespanValueField = document.getElementById("__value" + elementId);

    var hours = 0;
    var minutes = 0;

    if (hourField.value != "")
        hours = parseInt(hourField.value, 10);

    if (minuteField.value != "")
        minutes = parseInt(minuteField.value, 10);

    timespanValueField.value = hours * 1000 * 60 * 60 + minutes * 1000 * 60;
}

function createNumber(el) {
    var numberValueField = document.getElementById("__value" + el.id);

    var numberValue = numberValueField.value;
    var decimals = parseInt(numberValueField.getAttribute("data-decimals"), 10);

    var number = 0;

    if (numberValue !== "")
        number = parseFloat(numberValue);

    if (isNaN(number))
        number = 0;

    number = number.toFixed(decimals);

    var locale = getCurrentUserLocale();

    var displayNumber = formatFloat(number, decimals);
    $(document.getElementById("__displayvalue_" + el.id)).val(displayNumber);
    numberValueField.value = number;
}

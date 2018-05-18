function closeParentDialog() {
	window.parent.$("body").data("dialog").close();
}

// Javascript helpers and extensions

function isOverflowed(element, axis) {
	switch (axis) {
		case "x":
			return element.scrollWidth > element.clientWidth;
			break;

		case "y":
			return element.scrollHeight > element.clientHeight;
			break;

		case "xy":
			return element.scrollHeight > element.clientHeight || element.scrollWidth > element.clientWidth;
			break;
	}
}

/*
 * Make deep copy of an object. Supports arrays, strings, numbers and booleans.
 */
function deepCopy(objectToCopy) {
	var returnObject = new Object();


	if (typeof (objectToCopy) == "object") {
		if (Object.prototype.toString.call(objectToCopy) === "[object Array]") {
			returnObject = new Array();
		}

		for (var member in objectToCopy) {
			if (typeof (objectToCopy[member]) == 'object') {

				if (objectToCopy[member] === null)
					returnObject[member] = null;
				else
					returnObject[member] = deepCopy(objectToCopy[member]);

			} else if (typeof (objectToCopy[member]) == 'string' || typeof (objectToCopy[member]) == 'number') {

				returnObject[member] = objectToCopy[member];

			} else if (typeof (objectToCopy[member]) == 'boolean') {

				((objectToCopy[member] == true) ? returnObject[member] = true : returnObject[member] = false);
			}
		}
	}

	return returnObject;
}

function getUserLevel() {
	return parseInt($("body").data("userlevel"), 10);
}

function getAjaxUrl(controller, action) {
	return "/main.aspx?controller=" + controller + "&action=" + action + "&historytoken=" + $("body").data("historytoken");
}

function addParameterToUrl(url, parameterName, parameterValue) {
	var separator = "&";

	if (url.indexOf("?") == -1)
		separator = "?";

	return url + separator + parameterName + "=" + encodeURIComponent(parameterValue);
}

function isEmpty(value) {
	if (value === undefined || value === null || value === "")
		return true;
	else
		return false;
}

function roundToTimeAccuracy(dateTime, timeAccuracy) {
	// Round to desired minute accuracy

	second = 0;

	var minutes = dateTime.getUTCMinutes();

	date = new Date(Date.UTC(dateTime.getUTCFullYear(), dateTime.getUTCMonth(), dateTime.getUTCDate(), dateTime.getUTCHours(), minutes, 0));

	var roundedMinutes = Math.round(minutes / timeAccuracy);

	// Substract minutes and add rounded minutes.
	date = new Date(date.getTime() - minutes * 60000 + roundedMinutes * timeAccuracy * 60000);

	return date;
}

function getDateFromIsoString(isoString, timeAccuracy) {
	if (isEmpty(isoString))
		return false;

	try {
		isoString = isoString.trim();

		var year = parseInt(isoString.substring(0, 4), 10);
		var month = parseInt(isoString.substring(5, 7), 10) - 1; // -1 to move range to 0 - 11
		var day = parseInt(isoString.substring(8, 10), 10);
		var hour = parseInt(isoString.substring(11, 13), 10);
		var minute = parseInt(isoString.substring(14, 16), 10);
		var second = parseInt(isoString.substring(17, 19), 10);

		var date;

		if (typeof timeAccuracy !== "undefined" && !isNaN(timeAccuracy)) {
			date = new Date(Date.UTC(year, month, day, hour, minute, second));

			date = roundToTimeAccuracy(date, timeAccuracy);

		}
		else {
			date = new Date(Date.UTC(year, month, day, hour, minute, second));
		}

		return date;
	} catch (ex) {
		return false;
	}
}

function processLocalizedValues() {
	$(".__localize_datetime").each(function () {
		processDateValueField(this, "datetime");
	});

	$(".__localize_date").each(function () {
		processDateValueField(this, "date");
	});

	$(".__localize_time").each(function () {
		processTimeValueField(this);
	});

	$(".__localize_decimal").each(function () {
		formatFloat(this, this.attr("data-decimals"));
	});
}

function processListviewFields(data) {
	var jdata = $(data);

	// Process datetime fields

	jdata.find(".__listview_datetime").each(function () {
		if ($(this).hasClass("__datetimetype_date"))
			processDateValueField(this, "date");
		else if ($(this).hasClass("__datetimetype_time"))
			processDateValueField(this, "time")
		else
			processDateValueField(this, "datetime")
	});

	return jdata;
}

function processViewFormFields(rootElement) {
	$(rootElement).each(function () {
		$(this).find(".__displayform_datetime").each(function () {
			if ($(this).hasClass("__datetimetype_date"))
				processDateValueField(this, "date");
			else if ($(this).hasClass("__datetimetype_time"))
				processDateValueField(this, "time")
			else
				processDateValueField(this, "datetime")

			$(this).addClass("__show");
		});
	});
}

function processDateValueField(el, dateTimeType) {

	var valueElem = null;

	if ($(el).find("a").length)
		valueElem = $(el).find("a:first");
	else
		valueElem = $(el);

	valueDate = getDateFromIsoString(valueElem.text(), parseInt($(el).attr("data-timeaccuracy"), 10));

	// End dates of daterange are displayed as current date minus one. The 
	// human readable version of end date says that 12th of december as an
	// end date includes that date. As a timestamp it would be the start of
	// that date. Tehrefore substract 1 from date to get to the human readable
	// format.
	if (($(el).attr("data-isenddate") === "true"))
		valueDate.setDate(valueDate.getDate() - 1)

	if (isValidDate(valueDate)) {
		if (dateTimeType == "datetime")
			valueElem.text(formatDateTime(valueDate));
		else if (dateTimeType == "date")
			valueElem.text(formatDate(valueDate));
		else if (dateTimeType == "time")
			valueElem.text(formatTime(valueDate));
	}
	else
		valueElem.text("");
}

function processTimeValueField(el) {
	var valueElem = null;

	if ($(el).find("a").length)
		valueElem = $(el).find("a:first");
	else
		valueElem = $(el);

	valueTime = getDateFromIsoString(valueElem.text(), parseInt($(el).attr("data-timeaccuracy"), 10));

	if (isValidDate(valueTime)) {
		valueElem.text(formatTime(valueTime));
	}
	else
		valueElem.text("");
}

function getLocale(locale) {
	var fi = d3.locale({
		"decimal": ",",
		"thousands": " ",
		"grouping": [3],
		"currency": ["", "€"],
		"dateTime": "%a %b %e %X %Y",
		"date": "%d.%m.%Y",
		"time": "%H:%M:%S",
		"periods": ["", ""],
		"days": ["Sunnuntai", "Maanantai", "Tiistai", "Keskiviikko", "Torstai", "Perjantai", "Lauantai"],
		"shortDays": ["Su", "Ma", "Ti", "Ke", "To", "Pe", "La"],
		"months": ["Tammikuu", "Helmikuu", "Maaliskuu", "Huhtikuu", "Toukokuu", "Kesäkuu", "Heinäkuu", "Elokuu", "Syyskuu", "Lokakuu", "Marraskuu", "Joulukuu"],
		"shortMonths": ["Tammi", "Helmi", "Maalis", "Huhti", "Touko", "Kesä", "Heinä", "Elo", "Syys", "Loka", "Marras", "Joulu"]
	});

	if (locale == "fi")
		return fi;
}

function formatFloat(value, decimals) {
	var numberFormat = getCurrentUserLocale().numberFormat("." + decimals + "f");

	return numberFormat(value);
}

function parseFloatLocal(value) {
	value = value.replace(",", ".");
	return parseFloat(value);
}

function formatDate(date) {
	var locale = getCurrentUserLocale();
	var format = locale.timeFormat("%x");

	return format(date);
}

function formatDateShort(date) {
	var locale = getCurrentUserLocale();
	var format = locale.timeFormat("%-d.%-m.");

	return format(date);
}

function formatTime(date) {
	var locale = getCurrentUserLocale();
	var format = locale.timeFormat("%H:%M");

	return format(date);
}

function formatDateTime(date) {
	var locale = getCurrentUserLocale();
	var format = locale.timeFormat("%x %H:%M");

	return format(date);
}

function getCurrentUserLocale() {
	return getLocale("fi");
}

function isValidDate(date) {
	if (Object.prototype.toString.call(date) === "[object Date]") {
		if (isNaN(date.getTime()))
			return false;
		else
			return true;
	}
	else
		return false;
}

function getFirstProperty(obj) {
	for (var itemKey in obj) {
		if (!obj.hasOwnProperty(itemKey))
			continue;

		return obj[itemKey];
	}
}

function verifyLoginStatus(data) {
	// If login response is requested
	if (data == "4FE698E2069844429E5FFFEC3C7C958D") {
		location.href = "/main.aspx";
		return false;
	}

	return true;
}

function getFirstDayOfWeek(d) {
	d = new Date(d);
	var day = d.getDay(),
        diff = d.getDate() - day + (day == 0 ? -6 : 1); // adjust when day is sunday todo: make a parameter
	return new Date(d.setDate(diff));
}

function getUrlParameter(sParam) {
	var sPageURL = window.location.search.substring(1);
	var sURLVariables = sPageURL.split('&');
	for (var i = 0; i < sURLVariables.length; i++) {
		var sParameterName = sURLVariables[i].split('=');
		if (sParameterName[0] == sParam) {
			return sParameterName[1];
		}
	}
}

function setUrlParameter(paramName, newVal) {
	var url;
	if (window.location.href.indexOf(paramName) != -1) {
		var tmpRegex = new RegExp("(" + paramName + "=)[a-z|0-9]+", 'ig');
		url = window.location.href.replace(tmpRegex, '$1' + newVal);
		history.replaceState({}, document.title, url);
	}
	else {
		var deliminator = "?";
		if (window.location.href.indexOf("?") != -1)
			deliminator = "&"
		url = window.location.href + deliminator + paramName + "=" + newVal;
		history.replaceState({}, document.title, url);
	}
}

/*
 * Sets a history state for this page in the server. State parameter should be an object
 * and it can contain child values.
 */
function setHistoryState(identifier, state) {

	if (!identifier)
		throw ("Setting history state requires an identifier.")

	if (!state)
		throw ("History state must be an object containing the history state");

	$.ajax
    ({
    	type: "POST",
    	//the url where you want to sent the userName and password to
    	url: getAjaxUrl("history", "setstate") + "&identifier=" + identifier,
    	dataType: 'json',
    	async: true,
    	data: JSON.stringify(state),
    	success: function () {
    	},
    	failed: function (data) {
    		console.log("Failed to set history state: " + data);
    	}

    })
}

// Get duration from milliseconds
function getTimeFromMilliseconds(s) {
	var ms = s % 1000;
	s = (s - ms) / 1000;
	var secs = s % 60;
	s = (s - secs) / 60;
	var mins = s % 60;
	var hrs = (s - mins) / 60;

	if (mins < 10) mins = "0" + mins;
	//if (hrs < 10) hrs = "0" + hrs;

	return hrs + ':' + mins;
}

// get weeknumber
Date.prototype.getWeekNumber = function () {
	var d = new Date(+this);
	d.setHours(0, 0, 0);
	d.setDate(d.getDate() + 4 - (d.getDay() || 7));
	return Math.ceil((((d - new Date(d.getFullYear(), 0, 1)) / 8.64e7) + 1) / 7);
};

// Number

function updateNumber(el) {
	var elId = el.id;
	var displayField = document.getElementById("__displayvalue_" + elId);
	var numberValueField = document.getElementById("__value" + el.id);

	var number;
	if (displayField.value != "")
		number = parseFloatLocal(displayField.value);

	numberValueField.value = number;

	createNumber(el);
}

// Boolean

function updateBoolean(el) {
	var elId = el.id;
	var numberValueField = document.getElementById("__value" + el.id);
	numberValueField.value = number;

	// Save selection to history. If use comes back to this version of page in history the same resolved result will be shown.
	var historyData = new Object();
	historyData.historyvalue = resultId;
	historyData.historydisplayvalue = displayName;
	setHistoryState("__searchfilter_state" + searchFilterWrapper.attr("id"), historyData);


	createNumber(el);
}

// get translated string

function txt(str, controller) {
	try {
		var language = $("body").data("language");

		if (controller === undefined)
			controller = $("body").data("controller");

		var returnString = "";
		returnString = window.translations[controller][language][str];

		if (typeof returnString === "undefined") {
			controller = "core";
			returnString = window.translations[controller][language][str];
		}
	}
	catch (e) { }

	if (typeof returnString === "undefined") {
		returnString = "$" + str;
	}

	return returnString;
}

// center
function center(el) {

	var elWidth = $(el).outerWidth();
	var elHeight = $(el).outerHeight()

	$(el).css("left", (($(window).width() / 2 - elWidth / 2) + "px"));
	$(el).css("top", (($(window).height() / 2 - elHeight / 2) + "px"));
}

// center to parent
function centerToContainer(el, containerEl) {

	var elWidth = $(el).outerWidth();
	var elHeight = $(el).outerHeight()

	$(el).css("left", (($(containerEl).width() / 2 - elWidth / 2) + "px"));
	$(el).css("top", (($(containerEl).height() / 2 - elHeight / 2) + "px"));
}

// Object manipulation
function isNullOrUndefined(obj) {
	return (obj == null || typeof obj === "undefined")
}

// lock and release scrolling
function lockScrolling() {
	$("#__applicationview").css("top", (0 - $(window).scrollTop()) + "px");
	console.log(0 - $(window).scrollTop() + "px");
}

function releaseScrolling() {
	var scrollPos = Math.abs($("#__applicationview").offset().top);
	$("#__applicationview").css("top", "");
	$(window).scrollTop(scrollPos);
}


// form confirmation messages
function setupButtonConfirmationMessages() {
	$("body").on("click", "input[data-confirmationmessage][data-confirmationmessage!=''], a.__button[data-confirmationmessage][data-confirmationmessage != '']", function (e) {
		var message = this.getAttribute("data-confirmationmessage");
		if (!confirm(message)) {
			e.preventDefault();
			return false;
		}

		return true;
	});
}

function updateScrollBars(container) { //todo: make support for other than flexpanels
	container.find(".__flexpanel-column").each(function () {
		if (typeof ($(this)).data("scrollbar") !== "undefined") {
			$(this).data("scrollbar").refresh();
			$(this).data("scrollbar-rects", $(this).data("scrollbar").indicators[0].wrapper.getClientRects()[0]);
		}
	});
}

function getScrollbarWidth() {
	var outer = document.createElement("div");
	outer.style.visibility = "hidden";
	outer.style.width = "100px";
	outer.style.msOverflowStyle = "scrollbar"; // needed for WinJS apps

	document.body.appendChild(outer);

	var widthNoScroll = outer.offsetWidth;
	// force scrollbars
	outer.style.overflow = "scroll";

	// add innerdiv
	var inner = document.createElement("div");
	inner.style.width = "100%";
	outer.appendChild(inner);

	var widthWithScroll = inner.offsetWidth;

	// remove divs
	outer.parentNode.removeChild(outer);

	return widthNoScroll - widthWithScroll;
}

function messageBoxConfirm(message, cancellable, messageType) {

	var deferred = $.Deferred();
	var returnValue = "";

	if (typeof cancellable === "undefined")
		cancellable = false;

	if (typeof message === "undefined")
		return;

	if (typeof messageType === "undefined")
		messageType = "none";

	var element = document.createElement("DIV");
	element.className = "__messagebox-body";
	var messageBoxMessage = $("<div class='__messagebox-message'></div>");
	var messageBoxControls = $("<div class='__messagebox-controls'></div>");
	var messageBoxIcon = $("<div class='__messagebox-icon'></div>");
	var messageBoxText = $("<div class='__messagebox-text'>" + message + "</div>");
	var messageBoxIconOk = $("<i class='fa fa-check-circle'></i>");
	var messageBoxIconWarning = $("<i class='fa fa-exclamation-triangle'></i>");
	var messageBoxIconError = $("<i class='fa fa-exclamation-circle'></i>");
	var messageBoxIconInfo = $("<i class='fa fa-info-circle'></i>");
	var messageBoxIconQuestion = $("<i class='fa fa-question-circle'></i>");
	var messageBoxOkButton = $("<button class='__messagebox-ok __ok __button'>" + txt("ok") + "</button>");
	var messageBoxCancelButton = $("<button class='__messagebox-cancel __warning __button'>" + txt("cancel") + "</button>");

	// ok button
	messageBoxOkButton.on("click", function () {
		deferred.resolve(true);
		close();
	});

	// cancel button
	if (cancellable) {
		messageBoxCancelButton.on("click", function () {
			deferred.resolve(false);
			close();
		});
	}

	// submit prompt
	function close() {
		$(window).data("dialog").close();
	}

	switch (messageType) {
		case "error":
			messageBoxIcon.addClass("__messagebox-confirm-error");
			messageBoxIcon.append(messageBoxIconError);
			break;
		case "ok":
			messageBoxIcon.addClass("__messagebox-confirm-ok");
			messageBoxIcon.append(messageBoxIconOk);
			break;
		case "warning":
			messageBoxIcon.addClass("__messagebox-confirm-warning");
			messageBoxIcon.append(messageBoxIconWarning);
			break;
		case "info":
			messageBoxIcon.addClass("__messagebox-confirm-info");
			messageBoxIcon.append(messageBoxIconInfo);
			break;
		case "question":
			messageBoxIcon.addClass("__messagebox-confirm-question");
			messageBoxIcon.append(messageBoxIconQuestion);
			break;
		default:
	}

	if (messageType != "none")
		messageBoxMessage.append(messageBoxIcon);

	messageBoxMessage.append(messageBoxText);

	$(element).append(messageBoxMessage);

	messageBoxControls.append(messageBoxOkButton);

	if (cancellable) {
		messageBoxControls.append(messageBoxCancelButton);
		messageBoxControls.addClass("__right");
	}

	$(element).append(messageBoxControls);

	$(window).on("dialogclosed", $(".__dialog").data("dialog"), function () {
		deferred.resolve(false);
	});

	$(window).dialog({
		object: element,
		closeable: false,
		closeButton: false,
		width: "auto",
		height: "auto"
	});

	return deferred.promise();
}

function messageBoxPrompt(message, cancellable, validates, validateMessage) {

	var deferred = $.Deferred();
	var returnValue = "";

	if (typeof cancellable === "undefined")
		cancellable = false;

	if (typeof message === "undefined")
		return;

	if (typeof validates === "undefined") {
		validates = function (value) {
			return value != "";
		}
	}
	if (typeof validateMessage === "undefined")
		validateMessage = "invalid";

	var element = document.createElement("DIV");
	element.className = "__messagebox-body";
	var messageBoxMessage = $("<div class='__messagebox-message'></div>");
	var messageBoxControls = $("<div class='__messagebox-controls'></div>");
	var messageBoxText = $("<div class='__messagebox-text'>" + message + "</div>");
	var messageBoxForm = document.createElement("FORM");
	var messageBoxInputWrapper = $("<div class='__messagebox-inputwrapper'></div>");
	var messageBoxInput = $("<input type='text' class='__messagebox-input' />");
	var messageBoxValidateMessage = $("<div class='__messagebox-validatemessage'>" + validateMessage + "</div>");
	var messageBoxOkButton = $("<button class='__messagebox-ok __ok __button'>" + txt("ok") + "</button>");
	var messageBoxCancelButton = $("<button class='__messagebox-cancel __warning __button'>" + txt("cancel") + "</button>");

	// field
	messageBoxInput.on("change", function () {
		$(this).removeClass("__invalid");
		$(element).find(".__messagebox-validatemessage").hide();
	});

	// form
	$(messageBoxForm).on("submit", function (e) {
		e.preventDefault();
		deliverValue();
	});

	// ok button
	messageBoxOkButton.on("click", function () {
		deliverValue();
	});

	// cancel button
	if (cancellable) {
		messageBoxCancelButton.on("click", function () {
			deferred.resolve(false);
			close();
		});
	}

	// submit prompt
	function close() {
		$(window).data("dialog").close();
	}

	function deliverValue() {
		returnElement = $(element).find(".__messagebox-input");
		returnValue = returnElement.val();

		if (validates(returnValue)) {
			deferred.resolve(returnValue);
			close();
		}
		else {
			returnElement.addClass("__invalid");
			$(element).find(".__messagebox-validatemessage").show();
		}
	}

	messageBoxMessage.append(messageBoxText);

	$(element).append(messageBoxMessage);

	$(messageBoxInputWrapper).append(messageBoxInput);
	$(messageBoxInputWrapper).append(messageBoxValidateMessage);
	$(messageBoxForm).append(messageBoxInputWrapper);
	element.appendChild(messageBoxForm);

	messageBoxControls.append(messageBoxOkButton);
	if (cancellable) {
		messageBoxControls.append(messageBoxCancelButton);
		messageBoxControls.addClass("__right");
	}

	$(element).append(messageBoxControls);

	$(window).on("dialogcreated", $(".__dialog").data("dialog"), function () {
		$(".__dialog").find("input").first().focus(); // todo: find a way to reference properly this singular instance
	});

	$(window).on("dialogclosed", $(".__dialog").data("dialog"), function () {
		deferred.resolve(false);
	});

	$(window).dialog({
		object: element,
		closeable: cancellable,
		closeButton: false,
		width: "auto",
		height: "auto"
	});

	return deferred.promise();
}

//// Examples for firing messageboxes
//function testprompt() {
//    messageBoxPrompt("Prompt", true, function (value) { return value.length > 6 ; }, "Too short.")
//    .then(function (input) {
//        if (input === false)
//            console.log("false");
//        else
//            console.log(input);
//    })
//}

//function testconfirm() {
//    messageBoxConfirm("Confirm", false, "error")
//    .then(function (answer) {
//        if (answer)
//            console.log("true");
//        else
//            console.log("false");
//    });
//}

function getParameterByName(name) {
	name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
	var regex = new RegExp("[\\?&]" + name + "=([^&#]*)"),
        results = regex.exec(location.search);
	return results === null ? "" : decodeURIComponent(results[1].replace(/\+/g, " "));
}

// Gets favourites data for relation dropdown
function getRelationDropdownFavourites(relationDropdown) {
	var extraInfo = $("#" + relationDropdown.name + "_hiddenfield");
	if (!$(extraInfo).attr("data-favourites"))
		return;

	var maxFavourites = false;

	if ($(extraInfo).attr("data-maxfavourites"))
		maxFavourites = parseInt($(extraInfo).attr("data-maxfavourites"));

	// Use 5 as default
	if (!maxFavourites)
		maxFavourites = 5;

	var userId = $("body").attr("data-user");
	var relationName = $(relationDropdown).attr("name");

	var collection = extraInfo.attr("data-collection");


	if (!userId || !relationName || !collection) {
		console.log("required infromation missing to enable relation dropdown favourites. UserId: " + userId + ", relationName: " + relationName + ", collection: " + collection);
		return null;
	}

	var favourites = storedFavourites.getFavouritesData(relationName + "__" + collection + "__" + getUrlParameter("action"), maxFavourites);
	favourites.maxFavourites = maxFavourites;

	return favourites;
}

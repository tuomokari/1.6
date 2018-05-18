// Static variables

// Only one search filter request at a time should be active.
var searchfilter_request = null;

function clearSearchfilterSelection(searchFilterWrapper) {
	searchFilterWrapper.data("currentSelection", "");

	var elemId = $(searchFilterWrapper).attr("id");
	elemId = elemId.substring(0, elemId.indexOf("-"));
	var displayField = $("#" + elemId + "-field");
	var filterField = $(searchFilterWrapper).find(".__searchfilter_filterfield");

	// Clear any existing values indicating that the selection is resolved
	searchFilterWrapper.attr("resolved", "");
	$("#" + searchFilterWrapper.attr("targetid")).val("");
	displayField.removeClass("__resolved");
	displayField.val("");
	filterField.removeClass("__resolved");
	filterField.val("");
}

$(function () {
	setupSearchFilter();

	function setupSearchFilter() {

		var defaultDelay = 300;

		setupSearchFilterEvents();

		$.each($("input.__searchfilter_filterfield"), function () {
			var elemId = $(this).attr("id");
			elemId = elemId.substring(0, elemId.indexOf("-"));
			var searchFilterWrapper = $("#" + elemId);
			var displayField = $("#" + elemId + "-field");


			// Initially set previous display value as current display value.
			searchFilterWrapper.attr("previousvalue", displayField.val());

			searchFilterWrapper.on("updatehistorystate", function () {
				var elemId = $(searchFilterWrapper).attr("id");
				elemId = elemId.substring(0, elemId.indexOf("-"));
				var displayField = $("#" + elemId + "-field");

				saveStateToHistory(searchFilterWrapper, $(searchFilterWrapper).attr("data-containingitemid"), displayField.val());
			});

			var targetField = $("#" + searchFilterWrapper.attr("targetid"));

			var targetValue = targetField.val();

			if (targetValue !== "") {
				searchFilterWrapper.attr("resolved", "true");
				$(this).addClass("__resolved");
			}

			if (searchFilterWrapper.attr("resolved") === "true") {
				$(this).addClass("__resolved");
			}

			// set delay for submitting queries
			var delay = parseInt(searchFilterWrapper.data("delay"));
			if (isNaN(delay)) {
				searchFilterWrapper.data("delay", defaultDelay);
			}

			if (searchFilterWrapper.attr("data-history") === "true") {
				// Apply history state if it exists and history is enabled.            
				var historyValue = searchFilterWrapper.attr("data-historyvalue");
				var historyDisplayValue = searchFilterWrapper.attr("data-historydisplayvalue");

				if (historyValue !== "") {
					searchFilterWrapper.attr("resolved", "true");
					$(this).addClass("__resolved");

					targetField.val(historyValue);
					displayField.val(historyDisplayValue);
				}
			}
		});
	}

	function setupSearchFilterEvents() {
		$("input[type='button'].__searchfilter").on("click", delaySubmitSearchFilter);
		$("input.__searchfilter_filterfield").on("keyup focus", delaySubmitSearchFilter);
		$("input.__searchfilter_filterfield").on("keydown", cancelKeys);
		$("input.__searchfilter_filterfield").on("keyup", itemSelection);
		$("input.__searchfilter_filterfield").on("blur", hideSearchResults);
		$("input.__searchfilter_filterfield").on("cleared", delaySubmitSearchFilter);
		$("input.__searchfilter_filterfield").on("mouseup", function (e) {
			var $input = $(this),
				oldValue = $input.val();

			if (oldValue == "") return;

			// When this event is fired after clicking on the clear button
			// the value is not cleared yet. We have to wait for it.
			setTimeout(function () {
				var newValue = $input.val();

				if (newValue == "") {
					$input.trigger("cleared");
				}
			}, 1);
		});
	}

	function setupSearchFilterResults(elemId) {
		$(".__searchfilter_resultitem").mousedown(selectResultItem);
		$(".__searchfilter_resultitem").attr("elemid", elemId);
		$("#" + $("#" + elemId).attr("resultid") + " .__panel").css({
			width: ($("#" + elemId + " input").outerWidth()) + "px",
			maxHeight: $(window).outerHeight() - ($("#" + elemId + " input")[0].getBoundingClientRect().top + $("#" + elemId + " input").outerHeight() + 10) + "px" // + 10 for margin
		});
		var fieldElement = $("#" + elemId + "-field");
		$(".__searchfilterresult .__panel").on("scrollstart.__searchfilter", function () {
			fieldElement.data("scrolling", true);
		});

		$(".__searchfilterresult .__panel").on("scroll.__searchfilter", function () {
			fieldElement.focus();
			clearTimeout(window.resetFocusTimeOut);
			clearTimeout(window.removeSearchFilterResult);
			clearTimeout(window.searchFilterQueryTimeout);
		});

		$(".__searchfilterresult .__panel").on("scrollstop.__searchfilter", function () {
			fieldElement.data("scrolling", false);
		});
	}

	// Selects one item from the list of choices loaded from server.
	// Activated by mouse click.
	function selectResultItem(e) {
		e.stopPropagation();
		e.preventDefault();

		var elemId = $(this).attr("elemid");

		var searchFilterWrapper = $("#" + elemId);

		var displayField = $("#" + elemId + "-field");

		var displayName = getDisplayNameFromRow($(this));

		displayField.val(displayName);
		searchFilterWrapper.attr("resolved", "true");
		displayField.addClass("__resolved");

		searchFilterWrapper.attr("previousvalue", displayField.val());

		var result = searchFilterWrapper.attr("resultid");

		$("#" + result).html("");
		searchFilterWrapper.data("currentSelection", null);

		$("#" + searchFilterWrapper.attr("targetid")).val($(this).attr("resultid"));

		saveStateToHistory(searchFilterWrapper, $(this).attr("resultid"), displayName);

		searchFilterWrapper.trigger("resolved");
	}

	// Hides the search results view after a small timeout.
	function hideSearchResults(e) {

		var elemId = $(this).attr("id");

		if (elemId == null)
			return;

		elemId = elemId.substring(0, elemId.indexOf("-"));

		var searchFilterWrapper = $("#" + elemId);
		var result = searchFilterWrapper.attr("resultid");

		// Required because blur fires before click and we need to get the click target when selecting items.
		window.removeSearchFilterResult = setTimeout(function () {

			var displayField = $("#" + elemId + "-field");

			$("#" + result).html("");

			//remove active class to field
			displayField.removeClass("__active");
			if (!displayField.hasClass("__resolved"))
				displayField.val("");

			searchFilterWrapper.data("currentSelection", null);
			$("#" + elemId + "-field").data("scrolling", false);
		}, 200);
	}

	// Builds a display name from row's columns. Only columns wiht namefield attribute are used.
	function getDisplayNameFromRow(row) {
		var first = true;
		var displayName = "";

		row.children('td').each(function () {
			var td = $(this)

			if (td.attr("namefield") !== "") {
				if (first) {
					first = false;
				} else {
					displayName += " ";
				}

				displayName += td.text();
			}
		});

		return $.trim(displayName.replace("\n", ""));
	}

	function cancelKeys(e) {
		var keyValue = e.which;
		if (keyValue === 13)
			e.preventDefault();

	}

	function itemSelection(e) {

		var keyValue = e.which;

		var elemId = $(this).attr("id");
		elemId = elemId.substring(0, elemId.indexOf("-"));
		var searchFilterWrapper = $("#" + elemId);
		var resultArea = $("#" + searchFilterWrapper.attr("resultid"));
		var displayField = $("#" + elemId + "-field");

		var selectedItem = searchFilterWrapper.data("currentSelection");

		// esc
		if (keyValue === 27) {
			e.preventDefault();
			$(searchFilterWrapper).find(".__searchfilter_filterfield").blur();
		}
			// up
		else if (keyValue === 38) {
			e.preventDefault();
			var itemId = selectPreviousResultItem(selectedItem, resultArea);
			searchFilterWrapper.data("currentSelection", itemId);
		}
			// down
		else if (keyValue === 40) {
			e.preventDefault();
			var itemId = selectNextResultItem(selectedItem, resultArea);
			searchFilterWrapper.data("currentSelection", itemId);
			// enter
		} else if (keyValue === 13) {
			e.preventDefault();

			var resolved = resolveSelection(selectedItem, searchFilterWrapper);

			if (!resolved)
				resolveSelectionIfUnambiguous(searchFilterWrapper);

			searchFilterWrapper.data("currentSelection", "");
		}
		else if (keyValue === 9) {
			var resolved = resolveSelection(selectedItem, searchFilterWrapper);

			if (!resolved)
				resolveSelectionIfUnambiguous(searchFilterWrapper);

			searchFilterWrapper.data("currentSelection", "");
		}
	}

	function selectNextResultItem(currentItemId, resultItems) {

		var nextItem;

		if (!currentItemId) {
			nextItem = resultItems.find(".__searchfilter_resultitem:first");
		}
		else {
			var currentItem = $("#" + currentItemId);
			currentItem.removeClass("__activerow");
			nextItem = currentItem.next();
		}

		if (nextItem === null || nextItem.prop("class") !== "__searchfilter_resultitem") {
			return null;
		}
		else {
			var container = nextItem.parents(".__panel");

			var containerScrollTop = container.scrollTop();
			var itemTop = nextItem.offset().top;
			var containerTop = container.offset().top;
			var rowHeight = nextItem.outerHeight();

			itemOffset = itemTop + containerScrollTop - containerTop - rowHeight;

			nextItem.parents(".__panel").scrollTop(itemOffset);
			nextItem.addClass("__activerow");
			return nextItem.attr("id");
		}
	}

	function selectPreviousResultItem(currentItemId, resultItems) {

		var prevItem;
		if (!currentItemId) {
			prevItem = resultItems.find(".__searchfilter_resultitem:last");
		}
		else {
			currentItem = $("#" + currentItemId);
			currentItem.removeClass("__activerow");
			prevItem = currentItem.prev();
		}


		if (prevItem === null || prevItem.prop("class") !== "__searchfilter_resultitem") {
			return null;
		}
		else {
			var container = prevItem.parents(".__panel");

			var containerScrollTop = container.scrollTop();
			var itemTop = prevItem.offset().top;
			var containerTop = container.offset().top;
			var rowHeight = prevItem.outerHeight();

			itemOffset = itemTop + containerScrollTop - containerTop - rowHeight;

			prevItem.parents(".__panel").scrollTop(itemOffset);
			prevItem.addClass("__activerow");
			return prevItem.attr("id");
		}
	}

	function resolveSelection(itemId, searchFilterWrapper) {
		if (itemId === null)
			return false;

		var selectedItem = $("#" + itemId);

		if (selectedItem.prop("tagName") !== "TR")
			return false;

		var elemId = searchFilterWrapper.attr("id");

		var displayField = $("#" + elemId + "-field");

		var displayName = getDisplayNameFromRow(selectedItem);
		var resultId = selectedItem.attr("resultid");

		displayField.val(displayName);
		searchFilterWrapper.attr("resolved", "true");
		displayField.addClass("__resolved");

		searchFilterWrapper.attr("previousvalue", displayField.val());

		var result = searchFilterWrapper.attr("resultid");

		$("#" + result).html("");

		//remove active class to field
		displayField.removeClass("__active");

		searchFilterWrapper.data("currentSelection", null);

		$("#" + searchFilterWrapper.attr("targetid")).val(resultId);

		saveStateToHistory(searchFilterWrapper, resultId, displayName);

		searchFilterWrapper.trigger("resolved");
		return true;
	}

	/**
     * Resolves the selection if there is only one possible selection and currently not resolved
     */
	function resolveSelectionIfUnambiguous(searchFilterWrapper) {
		var resultArea = $("#" + searchFilterWrapper.attr("resultid"));
		var firstItem = resultArea.find(".__searchfilter_resultitem:first");
		var lastItem = resultArea.find(".__searchfilter_resultitem:last");

		// if only one item
		if (firstItem.attr("id") === lastItem.attr("id") && firstItem.attr("id") !== null) {
			resolveSelection(firstItem.attr("id"), searchFilterWrapper);
		}
		else {
			$(searchFilterWrapper).find(".__searchfilter_filterfield").blur();
		}
	}

	function saveStateToHistory(searchFilterWrapper, resultId, displayName) {
		if (searchFilterWrapper.attr("data-history") === "true") {
			// Save selection to history. If use comes back to this version of page in history the same resolved result will be shown.
			var historyData = new Object();
			historyData.historyvalue = resultId;
			historyData.historydisplayvalue = displayName;

			var formValue = new Object();
			formValue.formvalue = resultId;

			setHistoryState("__searchfilter_state" + searchFilterWrapper.attr("id"), historyData);
			setHistoryState("__form_" + document.getElementById(searchFilterWrapper.attr("targetid")).name, formValue);
		}
	}
	// Delays the submitting the searchfilter result
	function delaySubmitSearchFilter(e) {
		e.stopPropagation();
		e.preventDefault();

		var elemId = $(this).attr("id");
		elemId = elemId.substring(0, elemId.indexOf("-"));
		var searchFilterWrapper = $("#" + elemId);
		var delay = searchFilterWrapper.data("delay");

		// hide warning text
		searchFilterWrapper.next(".__form_validatemessage").remove();

		clearTimeout(window.searchFilterQueryTimeout);

		window.searchFilterQueryTimeout = setTimeout(function () {
			submitSearchFilter(e, elemId, searchFilterWrapper);
		}, delay);
	}


	// Sends a searchfilter query to the server.
	function submitSearchFilter(e, elemId, searchFilterWrapper) {
		e.stopPropagation();

		// cancel if scrolling
		if ($(this).data("scrolling"))
			return;

		var displayField = $("#" + elemId + "-field");

		// remove events
		$(".__searchfilterresult .__panel").off(".__searchfilter");

		var keyValue = e.which;
		if (keyValue === 38 || keyValue === 40 || keyValue === 13)
			return;

		var controller = searchFilterWrapper.attr("controllername");
		var action = searchFilterWrapper.attr("action");
		var extraparams = getAttrOrDefault(searchFilterWrapper, "extraparams", "");
		var immediate = searchFilterWrapper.attr("immediate");
		var result = searchFilterWrapper.attr("resultid");
		var displayFieldValue = displayField.val();
		var documentsperpage = getAttrOrDefault(searchFilterWrapper, "documentsperpage", "");
		var filtercontroller = getAttrOrDefault(searchFilterWrapper, "filtercontroller", "");
		var filteraction = getAttrOrDefault(searchFilterWrapper, "filteraction", "");
		var containingitemid = getAttrOrDefault(searchFilterWrapper, "containingitemid", "");

		//add active class to field
		displayField.addClass("__active");

		// Return if value hasn't changed and the field is resolved
		if (displayFieldValue === searchFilterWrapper.attr("previousvalue") && displayFieldValue !== "" && displayField.hasClass("__resolved")) {
			return;
		}

		searchFilterWrapper.attr("previousvalue", displayFieldValue);

		// Clear any existing values indicating that the selection is resolved
		searchFilterWrapper.attr("resolved", "");
		$("#" + searchFilterWrapper.attr("targetid")).val("");
		displayField.removeClass("__resolved");

		saveStateToHistory(searchFilterWrapper, "", "");

		if (e.type === "keypress" && e.which !== "13" && immediate === "false")
			return;

		var term = displayFieldValue;

		var ajaxUrl = getAjaxUrl(controller, action);

		if (searchfilter_request !== null)
			searchfilter_request.abort();

		if (extraparams != "")
			extraparams = "&" + extraparams;

		if (documentsperpage != "")
			extraparams = extraparams + "&documentsperpage=" + documentsperpage;

		if (filtercontroller != "" && filteraction != "")
			extraparams = extraparams + "&filtercontroller=" + filtercontroller + "&filteraction=" + filteraction;

		if (containingitemid != "")
			extraparams = extraparams + "&itemid=" + containingitemid;

		searchfilter_request = $.ajax({
			dataType: "html",
			url: ajaxUrl,
			cache: false,
			data: "terms=" + encodeURIComponent(term) + extraparams,
			success: function (data, status, xhr) {
				if (!verifyLoginStatus(data)) return;

				var results = $("#" + result);
				if (data != "")
					results.html(data);
				else if (encodeURIComponent(term) == "")
					results.html("<div class='__searchfilter-empty __panel __searchfilterresult __mobilepadding'><div class='__searchfilter-result-display'>" + txt("searchfilter_empty") + "</div></div>");
				else
					results.html("<div class='__searchfilter-noresults __panel __searchfilterresult __mobilepadding'><div class='__searchfilter-result-display'>" + txt("searchfilter_noresults") + "</div></div>");
				results.data("fieldid", elemId);
				results.addClass("__searchfilter-resultwrapper");

				setupSearchFilterResults(elemId);
			}
		});

		if (e.type === "keypress" && immediate === "false")
			$(this).blur();
	}
});
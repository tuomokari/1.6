/**
 * projectlead widget
 */
$.widget("projectleadmodule.projectleadmodule", $.core.base, {
    options: {
    },

	/**
	* Fired by jQuery when the widget is created.
	*/
    _create: function () {

    	this._uiState = {};
    	this._model = {};
    	this._uiState.firstRender = true;
    	this.firstRefresh = true;
    	this.render();

    },
    /**
	* Fired by MC2 when widget is created. Implementation specific initialization code should
	* be run here
	*/
    _initWidget: function () {
    	this._setupWidgetEvents();

    	if (this.options.viewtype != "modify") {
    		// Hide edit and copy buttons
    		$("[data-actiontype='modify']").hide();
    		$(".copybutton").hide();
    	}
    },
    /**
	* Setup widget's events.
	*/
    _setupWidgetEvents: function () {
    	var base = this;
    	$(".button_generate_report").click(
			function (event) {
				base.refreshData();
				base.render();
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
    	var base = this;
    	if (!options)
    		options = {};

    	if (base.firstRefresh) {
    		base.firstRefresh = false;
    		return;
    	}

    	base._model.waitingForData = true;

    	base._model.report = null;

    	// Create an array of promises to wait before calling base and potentially rendering.
    	var promises = [];

    	var url = getAjaxUrl("projectleadmodule", "getprojectleadreport");
    	url = addParameterToUrl(url, "user", $("body").attr("data-user"));
    	if ($("input[name='userfilter']").length > 0)
    		url = addParameterToUrl(url, "userfilter", $("input[name='userfilter']").val());

    	if ($("input[name='userlist']").length > 0)
    		url = addParameterToUrl(url, "userlist", $("input[name='userlist']").val());

    	url = addParameterToUrl(url, "project", base.options.projectid);
    	url = addParameterToUrl(url, "payrollperiod", $("input[name='payrollperiodfilter']").val());
    	url = addParameterToUrl(url, "startdate", $("input[name='startdate']").val());
    	url = addParameterToUrl(url, "enddate", $("input[name='enddate']").val());

    	var reportPromise = $.ajax({
			dataType: "json",
    		url: url
    	});

    	reportPromise.done(function (data) {
    		try {
    			base._model.report = data.handlers.trohelpershandler.report;
    			base._processReports();
    			base._model.waitingForData = false;
    			base._model.error = false;
    		}
    		catch (e) {
    			alert(e);
    			base._model.error = true;
    		}
    	});

    	promises.push(reportPromise);

    	$.when.apply($, promises).done(function () {
    		base.render();
    	});
    },
    _processReports: function () {
    	var base = this;

    	var documentTypes = ["timesheetentry", "dayentry", "articleentry"];

    	documentTypes.forEach(function (documentType) {
    		base._processReport(documentType);
    		base._sortReport(documentType);
    	});
    },
    _sortReport: function(documentType) {
    	var base = this;
    	var documents = this._model.report[documentType];
    	documents.sort(function (a, b) {
    		var dateA = base._getDateFromDocument(a);
    		var dateB = base._getDateFromDocument(b);

    		var order = dateB - dateA;

    		if (order == 0)
    			order = base._getUserFromDocument(a).__displayname < base._getUserFromDocument(b).__displayname ? 1 : -1;

    		return order;
    	});
    },
    _getDateFromDocument: function(doc) {
    	if (doc.payroll && doc.payroll.date)
    		return doc.payroll.date;

    	if (doc.manager && doc.manager.date)
    		return doc.manager.date;

    	if (doc.worker && doc.worker.date)
    		return doc.worker.date;

    	return undefined;
    },
    _getUserFromDocument: function (doc) {
    	if (!doc.payroll.user.empty)
    		return doc.payroll.user;

    	if (!doc.manager.user.empty)
    		return doc.manager.user;

    	if (!doc.worker.user.empty)
    		return doc.worker.user;

    	return { __displayname: "" };
    },
    _processReport: function (documentType) {
    	if (!this._model.report[documentType]) {
    		this._model.report[documentType] = new Array();
    		return;
    	}

    	var documents = this._model.report[documentType];

    	if (!documents)
    		return;

    	for (var i = 0; i < documents.length; i++) {
    		var document = documents[i];
    		this._processDocument(document);
    	}
    },
    _processDocument: function (document) {
    	if (!document.worker)
    		document.worker = { empty: true }

    	if (!document.manager)
    		document.manager = { empty: true }

    	if (!document.payroll)
    		document.payroll = { empty: true }

    	this.processDocumentFragment(document.worker, document);
    	this.processDocumentFragment(document.manager, document);
    	this.processDocumentFragment(document.payroll, document);
    },
    processDocumentFragment: function (fragment, document) {
    	if (!fragment.user)
    		fragment.user = { empty: true, __displayname: "" }

    	if (!fragment.__user)
    		fragment.__user = { empty: true, __displayname: "" }

    	if (!fragment.paytype)
    		fragment.paytype = { empty: true, __displayname: "" }

    	if (!fragment.article)
    		fragment.article = { empty: true, __displayname: "" }

    	if (!fragment.duration)
    		fragment.duration = 0;

    	if (!fragment.amount)
    		fragment.amount = 0;

    	if (!fragment.note)
    		fragment.note = "";

    	if (fragment.note.length > 150)
    		fragment.note = fragment.note.substr(0, 147) + "...";

    	if (fragment.date)
    		fragment.date = getDateFromIsoString(fragment.date);

    	if (fragment.__datetime)
    		fragment.__datetime = getDateFromIsoString(fragment.__datetime);

    	if (fragment.__deleted) {
    		document.__deleted = true;
    		fragment.user = { empty: true, __displayname: "" }
    		fragment.paytype = { empty: true, __displayname: "" }
    		fragment.article = { empty: true, __displayname: "" }
    		fragment.amount = "";
    		fragment.duration = "";
    		fragment.date = null;
    		fragment.note = txt("deleted", "projectleadmodule");
    	}
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

    	if (this._model.error) {
    		$(".button_generate_report").removeClass("__disabled");
    		$(".timesheetentriestab").html("<h2>" + txt("error", "projectleadmodule") + "</h2>");
    		$(".dayentriestab").html("<h2>" + txt("error", "projectleadmodule") + "</h2>");
    		$(".articlestab").html("<h2>" + txt("error", "projectleadmodule") + "</h2>");
    		$(".button_generate_report").removeClass("__disabled");
    		return;
		}

    	if (this._model.waitingForData) {
    		$(".timesheetentriestab").html("<h2>" + txt("generatingreport", "projectleadmodule") + "</h2>");
    		$(".dayentriestab").html("<h2>" + txt("generatingreport", "projectleadmodule") + "</h2>");
    		$(".articlestab").html("<h2>" + txt("generatingreport", "projectleadmodule") + "</h2>");
    		$(".button_generate_report").addClass("__disabled");
    		return;
    	} else
    	{
    		$(".button_generate_report").removeClass("__disabled");
    	}

    	this.element.removeClass("__hidden");
    	this.renderReport();
    },
    renderReport: function () {
    	if (!this._model.report)
    		return;

    	$(".timesheetentriestab").html("<table class='projectleadmoduletable' id='timesheetentriestable'><tr>" +
    		"<th class='headercell'>" + txt("worker", "projectleadmodule") + "</th>" +
    		"<th class='headercell'>" + txt("hours", "projectleadmodule") + "</th>" +
    		"<th class='headercell'>" + txt("paytype", "projectleadmodule") + "</th>" +
    		"<th class='headercell'>" + txt("note", "projectleadmodule") + "</th>" +
    		"<th class='headercell'>" + txt("updater", "projectleadmodule") + "</th>" +
    		"<th class='headercell'>" + txt("entrydate", "projectleadmodule") + "</th>" +
    		"<th class='headercell'>" + txt("stage", "projectleadmodule") + "</th>" +
			"</tr></table>");

    	$(".dayentriestab").html("<table class='projectleadmoduletable' id='dayentriestable'><tr>" +
    		"<th class='headercell'>" + txt("worker", "projectleadmodule") + "</th>" +
    		"<th class='headercell'>" + txt("amount", "projectleadmodule") + "</th>" +
    		"<th class='headercell'>" + txt("paytype", "projectleadmodule") + "</th>" +
    		"<th class='headercell'>" + txt("note", "projectleadmodule") + "</th>" +
    		"<th class='headercell'>" + txt("updater", "projectleadmodule") + "</th>" +
    		"<th class='headercell'>" + txt("entrydate", "projectleadmodule") + "</th>" +
    		"<th class='headercell'>" + txt("stage", "projectleadmodule") + "</th>" +
			"</tr></table>");

    	$(".articlestab").html("<table class='projectleadmoduletable' id='articleentriestable'><tr>" +
    		"<th class='headercell'>" + txt("worker", "projectleadmodule") + "</th>" +
    		"<th class='headercell'>" + txt("amount", "projectleadmodule") + "</th>" +
    		"<th class='headercell'>" + txt("article", "projectleadmodule") + "</th>" +
    		"<th class='headercell'>" + txt("note", "projectleadmodule") + "</th>" +
    		"<th class='headercell'>" + txt("updater", "projectleadmodule") + "</th>" +
    		"<th class='headercell'>" + txt("entrydate", "projectleadmodule") + "</th>" +
    		"<th class='headercell'>" + txt("stage", "projectleadmodule") + "</th>" +
			"</tr></table>");

    	if (!this._renderDocuments("timesheetentry", $("#timesheetentriestable"), this._renderHourData))
    		$(".timesheetentriestab").html("<h2>" + txt("noresults", "projectleadmodule") + "</h2>");

    	if (!this._renderDocuments("dayentry", $("#dayentriestable"), this._renderExpenseData))
    		$(".dayentriestab").html("<h2>" + txt("noresults", "projectleadmodule") + "</h2>");

    	if (!this._renderDocuments("articleentry", $("#articleentriestable"), this._renderArticleData))
    		$(".articlestab").html("<h2>" + txt("noresults", "projectleadmodule") + "</h2>");
    },
    _renderDocuments: function (documentType, table, renderFunction) {
		var documents = this._model.report[documentType];

    	if (!documents || documents.length == 0)
    		return false;

    	var previousDate = undefined;
    	for (var i = 0; i < documents.length; i++) {
    		var doc = documents[i];

    		date = this._getDateFromDocument(doc);

    		if (!previousDate || date.getTime() != previousDate.getTime())
    			this._renderDayRow(date, table);

    		this._renderRow(doc, renderFunction);
    		previousDate = date;
    	}

    	return true;
    },
    _renderDayRow: function (date, table) {
    	table.find("tr").last().after(
			"<tr><td colspan=8><b>" + formatDate(date) + "</b></td></tr>");
    },

    _renderHourData: function (document, previousLevelDocument, base) {
    	if (document.empty)
    		return;

		// clone document so we can update it.
    	document = jQuery.extend({}, document);

		// only show changed data if this is an update
    	var showRow = base._determineShownFields(document, previousLevelDocument);

    	if (showRow) {
    		var table = $("#timesheetentriestable");

    		table.find("tr").last().after("<tr>" +
				"<td>" + ((document.user) ? document.user.__displayname : "") + "</td>" +
				"<td>" + ((document.duration) ? base._durationToHours(document.duration) : "") + "</td>" +
				"<td>" + ((document.paytype) ? document.paytype.__displayname : "") + "</td>" +
				"<td class='note'>" + ((document.note) ? document["note"] + "</td>" : "") +
				"<td>" + ((document.__user) ? document.__user.__displayname : "") + "</td>" +
				"<td>" + ((document.__datetime) ? formatDate(document.__datetime) : "") + "</td>" +
				"<td class='stage'></td>" +
				"</tr>");

    		return table.find("tr").last();
		}
    },
    _renderExpenseData: function (document, previousLevelDocument, base) {
    	if (document.empty)
    		return;

    	// clone document so we can update it.
    	document = jQuery.extend({}, document);

    	// only show changed data if this is an update
    	var showRow = base._determineShownFields(document, previousLevelDocument);

    	if (showRow) {
    		var table = $("#dayentriestable");

    		table.find("tr").last().after("<tr>" +
				"<td>" + ((document.user) ? document.user.__displayname : "") + "</td>" +
				"<td>" + document.amount + "</td>" +
				"<td>" + ((document.paytype) ? document.paytype.__displayname : "") + "</td>" +
				"<td class='note'>" + ((document.note) ? document["note"] + "</td>" : "") +
				"<td>" + ((document.__user) ? document.__user.__displayname : "") + "</td>" +
				"<td>" + ((document.__datetime) ? formatDate(document.__datetime) : "") + "</td>" +
				"<td class='stage'></td>" +
				"</tr>");

    		return table.find("tr").last();
		}
    },
    _renderArticleData: function (document, previousLevelDocument, base) {
    	if (document.empty)
    		return;

    	// clone document so we can update it.
    	document = jQuery.extend({}, document);

    	// only show changed data if this is an update
    	var showRow = base._determineShownFields(document, previousLevelDocument);

    	if (showRow) {
    		var table = $("#articleentriestable");

    		table.find("tr").last().after("<tr>" +
				"<td>" + ((document.user) ? document.user.__displayname : "") + "</td>" +
				"<td>" + document.amount + "</td>" +
				"<td>" + ((document.article) ? document.article.__displayname : "") + "</td>" +
				"<td class='note'>" + ((document.note) ? document["note"] + "</td>" : "") +
				"<td>" + ((document.__user) ? document.__user.__displayname : "") + "</td>" +
				"<td>" + ((document.__datetime) ? formatDate(document.__datetime) : "") + "</td>" +
				"<td class='stage'></td>" +
				"</tr>");

    		return table.find("tr").last();
		}
    },
    _renderRow: function (document, dataRenderFunction) {
    	if (!document.worker) return;
    	var row1 = dataRenderFunction(document.worker, null, this);

    	if (document.manager.approvedbymanager) {
    		row1.find(".stage").text(txt("stage_manager_approved", "projectleadmodule"));
    	}
    	else if (document.worker.approvedbyworker) {
    		row1.find(".stage").text(txt("stage_worker_approved", "projectleadmodule"));
		}
    	else {
    		row1.addClass("row-unapproved");
    		row1.find(".stage").text(txt("stage_worker_unapproved", "projectleadmodule"));
		}

    	if (!document.manager) return;

    	var row2 = dataRenderFunction(document.manager, document.worker, this);
    	if (row2) {
    		row2.addClass("row-changed");
    		row2.find(".stage").text(txt("stage_manager", "projectleadmodule"));
    		row2.find(".note").text(txt("modified", "projectleadmodule"));

    		if (document.__deleted && row1)
    			row1.addClass("row-deleted");
		}

    	if (!document.payroll) return;

    	var row3 = dataRenderFunction(document.payroll, document.manager, this);
    	if (row3) {
    		row3.addClass("row-changed");
    		row3.find(".stage").text(txt("stage_payroll", "projectleadmodule"));
    		row3.find(".note").text(txt("modified", "projectleadmodule"));
    		if (document.__deleted && row1)
    			row1.addClass("row-deleted");
    		if (document.__deleted && row2)
    			row2.addClass("row-deleted");
		}
    },
    _determineShownFields: function (document, previousLevelDocument) {
    	var showRow = false;
    	if (previousLevelDocument) {
    		for (var id in document) {
    			if (!document.hasOwnProperty(id))
    				continue;

    			if (id == "__user" || id == "__datetime" || id == "_id" || id == "approvedbymanager" || id == "approvedbyworker")
    				continue;

    			if (JSON.stringify(document[id]) === JSON.stringify(previousLevelDocument[id]))
    				document[id] = "";
    			else
    				showRow = true;
    		}
    	}
    	else {
    		showRow = true;
    	}
    	return showRow;
    },
    _durationToHours: function (durationMilliseconds) {
    	return Math.round(durationMilliseconds / 1000 / 6 / 6) / 100;
    },
});
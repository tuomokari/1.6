/**
 * Widget to display details for timesheetentries in approve work manager and worker.
 */
$.widget("approveworkhelper.approveworkhelper", $.core.base, {
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
    	this._setupWidgetEvents();
    },
	/**
	 * Setup widget's events.
	 */
    _setupWidgetEvents: function () {
    	var base = this;

    	if (!this.options.listclass) {
    		console.log("No list class specified for approveworkhelper.");
    	}

    	$("." + this.options.listclass).on("listviewshown", function () {

    		if (!base.options.renderdetails) {
    			// If details are not rendered there is no need for paytypes and detail data.
    			base.render();
    			return;
    		}

    		// Details are rendered, this is a TSE.

    		var promises = [];

    		var identifiers = new Array();
    		// Get all ids from listview
    		$(this).find("tr").each(function () {
    			if ($(this).attr("data-relation"))
    				identifiers.push($(this).attr("data-relation"));
    		});

    		if (identifiers.length > 0) {
    			// Query details data

    			var detailsPromise = new jQuery.Deferred();
    			promises.push(detailsPromise);

    			var url = getAjaxUrl("approveworkhelper", "getdetailsdata");
    			url = addParameterToUrl(url, "detailids", identifiers.join(","));

    			$.ajax({
    				url: url,
    				dataType: "json",
    				success: function (data, status, xhr) {
    					base._model.details = data;

    					detailsPromise.resolve();
    				}
    			});
    		}

    		var payTypesPromise = restApi.find("approveworkhelper", "paytypes");
    		promises.push(payTypesPromise);

    		payTypesPromise.done(function (result) {
    			base._model.payTypes =  result.timesheetentrydetailpaytype;

    			base._model.payTypesById = {};

    			for (var i = 0; i < base._model.payTypes.length; i++) {
    				var payType = base._model.payTypes[i];

    				base._model.payTypesById[payType._id] = payType;
    			}
    		});


    		$.when.apply($, promises).done(function () {
    			base.render();
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

    	if (base.options.renderdetails)
    		base._renderDetails();

    	base._renderAcceptedStates();

    },
    _renderAcceptedStates: function () {

        if (this.options.viewtype == "worker") {
            $("." + this.options.listclass).find("tr").each(function () {
                var approvedByManager = $(this).find("td[data-schemaname='approvedbyworker']");
                if (approvedByManager.find("span").attr("data-boolvalue") == "true") {
                    $(this).addClass("approved");
                }
                var approvedByManager = $(this).find("td[data-schemaname='approvedbymanager']");
                if (approvedByManager.find("span").attr("data-boolvalue") == "true") {
                    $(this).addClass("approved");
                }
            });
        } else {
            // Set accepted style for manager approved TSEs
            $("." + this.options.listclass).find("tr").each(function () {
                var approvedByManager = $(this).find("td[data-schemaname='approvedbymanager']");
                if (approvedByManager.find("span").attr("data-boolvalue") == "true") {
                    $(this).addClass("approved");
                }
            });
        }
    },
    _renderDetails: function () {
    	var base = this;
    	// Iterate TSE list
    	$("." + this.options.listclass).find("tr").each(function () {

    		// find correct details
    		var id = $(this).attr("data-relation");

    		var extras = new Array();
    		var extrasById = {};

    		var details = new Array();

    		for (var i = 0; i < base._model.details.length; i++) {
    			var detail = base._model.details[i];

    			if (detail.parent && detail.parent._id == id) {
    				var payType = base._model.payTypesById[detail.timesheetentrydetailpaytype._id];

    				details.push(detail);

    				if (payType.icon) {
    					var icon = "[icon]" + payType.icon
    					if (!extrasById[icon]) {
    						extras.push(icon);
    						extrasById[icon] = true;
    					}
    				}
    				else if (payType.abbreviation) {

    					if (!extrasById[payType.abbreviation]) {
    						extras.push(payType.abbreviation);
    						extrasById[payType.abbreviation] = true;
    					}
    				}
    			}
    		}

    		var extrasHtml = "";

    		for (var i = 0; i < extras.length; i++) {
    			var extra = extras[i];

    			if (i > 0)
    				extrasHtml += " ";

    			if (extra.substr(0, "[icon]".length) == "[icon]") {
    				var icon = extra.substr("[icon]".length);
    				extrasHtml += "<i class='material-icons'>" + icon + "</i>";
    			} else {
    				extrasHtml += extra;
    			}
    		}

    		var detailsHtml = "<table><tbody>";

    		for (var i = 0; i < details.length; i++) {
    			var detail = details[i];

    			var durationHours = base._millisecondsToHours(detail.duration);

    			detailsHtml += "<tr><th>" + detail.timesheetentrydetailpaytype.__displayname + "</th><td>" + durationHours + "h " + "</td></tr>";
    		}

    		detailsHtml += "</tbody></table>";

    		var details = $(this).find("td[data-schemaname='details']");
    		details.addClass("entrydetails");

    		details.html(extrasHtml + detailsHtml);
    	});
    },
	_millisecondsToHours: function (milliseconds) {
	if (!milliseconds)
		return 0;

	return milliseconds / (3600000);
}

});
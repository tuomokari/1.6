/**
 * approveworkresultshelper widget
 */
$.widget("approveworkresultshelper.approveworkresultshelper", $.core.base, {
    options: {
    },

    /**
     * Fired by jQuery when the widget is created.
     */
    _create: function () {
    	this._uiState = { exportId: 0 };
    	this._model = {};
    	this._uiState.firstRender = true;
    },
	/**
	 * Fired by MC2 when widget is created. Implementation specific initialization code should
	 * be run here
	 */
    _initWidget: function () {
        this._setupWidgetEvents()
    },
	/**
	 * Setup widget's events.
	 */
    _setupWidgetEvents: function () {
        var base = this;
        $(".exportLinkContainer").on("click", function (event) {
            base._uiState.showCsvLink = false
            base._uiState.exporting = false
            base.render()
        })

        $(".__tabhead").on("click", function(event) {
            base._uiState.showCsvLink = false
            base._uiState.exporting = false
            base.render()
        })

        $(".export_excel").on("click", function (event) {
            // Find the correct list view
            var activeTab = $(".approveworktab.__active")
            var activeList = activeTab.find(".__listview-wrapper")

            // Get filter parameters from the list
            var controller = activeList.attr("listviewcontroller")
            var action = activeList.attr("listviewaction")
            var collection = activeList.attr("collection")
            var extraParams = activeList.attr("extraparams")

            var url = getAjaxUrl(controller, action)
            url = addParameterToUrl(url, "collection", collection)
            url = addParameterToUrl(url, "viewcontroller", "listview")
            url = addParameterToUrl(url, "viewaction", "listviewresults")
            url = addParameterToUrl(url, "orderby", "__default")
            url = addParameterToUrl(url, "ascending", "false")
            url = addParameterToUrl(url, "documentsperpage", "50000")
            url = addParameterToUrl(url, "page", "0")
            url = addParameterToUrl(url, "saveAsCsv", "true")
            url = addParameterToUrl(url, "csvFieldsStr", base._getCsvFieldsForDocumenType(collection))
                
            url += extraParams

            base._uiState.exportId++
            var exportId = base._uiState.exportId

            $.ajax({
                url: url,
                type: "GET",
                dataType: "json",
                success: function (data, status, xhr) {
                    // Return silently if this is a stale load an export abandoned
                    // by user action
                    if (base._uiState.exportId != exportId) return

                    if (data.success) {
                        var fileName = data.fileName;
                        var address = "/public/csv/" + data.fileName;
                        base._uiState.showCsvLink = true
                        var link = $(".exportLinkContainer");
                        link.attr("href", address);
                        base._uiState.exporting = false
                        base.render()
                    }
                    else {
                        base._uiState.exporting = false
                        base.render()
                    }
                    base._model.details = data;
                    detailsPromise.resolve();
                },
            });

            base._uiState.exporting = true
            base._uiState.showCsvLink = false
            base.render()
        })
    	jQuery('body').on('listviewshown', function (e, listViewWrapper) {
    	    if (listViewWrapper.hasClass("approvework_timesheetentry")) {
    	        $(".approvework_timesheetentry").find('th[data-propertyschema="note"]').each(function () {
    	            $(this).removeClass("__hidden")
    	            $(this).insertAfter($(this).parent().children()[3])
    	        });
    	        $(".approvework_expenseentry").find('td[data-schemaname="route"]').each(function () {
    	            $(this).removeClass("__hidden")
                    var link = $(this).children().first()
                    $(this).css("max-width", "20px");
                    link.css("text-overflow", "ellipsis");
                    link.css("overflow", "hidden");

                    var text = $(this).find('div[data-widgetname="routefieldwidget"]').attr("data-value")
                    link.text(text)
                    link.data("fulltext", text);
                    link.attr("title", text);
    	        });
    	        $(".approvework_timesheetentry").find('td[data-schemaname="note"]').each(function () {
    	            var text = $(this).text();
    	            $(this).data("fulltext", text);
    	            $(this).attr("title", text);
    	            $(this).css("max-width", "50px");

    	            var link = $(this).children().first()
    	            link.css("text-overflow", "ellipsis");
    	            link.css("overflow", "hidden");

    	            $(this).removeClass("__hidden")
    	            $(this).insertAfter($(this).parent().children()[3])
    	        });
            }

    	    if (listViewWrapper.hasClass("approvework_expenseentry")) {
    	        $(".approvework_expenseentry").find('th[data-propertyschema="price"]').each(function () {
    	            $(this).removeClass("__hidden")
    	            $(this).insertAfter($(this).parent().children()[4])
    	        });
    	        $(".approvework_expenseentry").find('th[data-propertyschema="note"]').each(function () {
    	            $(this).removeClass("__hidden")
    	            $(this).insertAfter($(this).parent().children()[3])
    	        });
    	        $(".approvework_expenseentry").find('td[data-schemaname="price"]').each(function () {
    	            $(this).removeClass("__hidden")
    	            $(this).insertAfter($(this).parent().children()[4])
    	        });

    	        $(".approvework_expenseentry").find('th[data-propertyschema="route"]').each(function () {
    	            $(this).removeClass("__hidden")
    	        });
    	        $(".approvework_expenseentry").find('td[data-schemaname="note"]').each(function () {
    	            var text = $(this).text();
    	            $(this).data("fulltext", text);
    	            $(this).attr("title", text);
    	            $(this).css("max-width", "50px");

    	            var link = $(this).children().first()
    	            link.css("text-overflow", "ellipsis");
    	            link.css("overflow", "hidden");

    	            $(this).removeClass("__hidden")
    	            $(this).insertAfter($(this).parent().children()[3])
    	        });
            }
    	});
    },
    _getCsvFieldsForDocumenType: function(documentType) {
        if (documentType === "timesheetentry") {
            return "user,note,timesheetentrydetailpaytype,date,duration,project"
        }
        else if (documentType === "dayentry") {
            return "user,note,dayentrytype,note,date,price,project,projectcategory"
        }
        else if (documentType === "absenceentry") {
            return "user,absenceentrytype,date,duration"
        }
        else if (documentType === "articleentry") {
            return "user,article,amount,project"
        }
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

        // Create an array of promises to wait before calling base and potentially rendering.
        var promises = [];

		var base = this;

		$.when.apply($, promises).done(function () {
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
        var base = this

    	if (!options) options = {};

    	if (this._uiState.firstRender) {
			// Code run only once when the widget is rendered for the first time.
    		this._uiState.firstRender = false;
    	}

    	var link = $(".exportLinkContainer");
    	if (this._uiState.showCsvLink)
    	    link.removeClass("__hidden");
        else
    	    link.addClass("__hidden");

    	var exporting = $(".exporting_excel");
    	var exportingButton = $(".export_excel");

    	if (this._uiState.exporting) {
    	    exporting.removeClass("__hidden")
    	    exportingButton.addClass("__disabled")

    	    if (!this._uiState.exportInterval) {
    	        this._uiState.exportInterval = setInterval(function () {
    	            var exportingProgress = $(".exporting_excel_progress");
    	            exportingProgress.text(exportingProgress.text() + ".")
    	        }, 1000)
    	    }
        }
        else {
    	    exporting.addClass("__hidden");
    	    exportingButton.removeClass("__disabled")

    	    if (this._uiState.exportInterval) {
    	        clearInterval(this._uiState.exportInterval)
    	        this._uiState.exportInterval = undefined
    	        var exportingProgress = $(".exporting_excel_progress");
    	        exportingProgress.text("")
    	    }
        }

    }
});
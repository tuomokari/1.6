/**
 * userformwidget widget
 */
$.widget("userformwidget.userformwidget", $.core.base, {
    options: {
    },

    /**
     * Fired by jQuery when the widget is created.
     */
    _create: function () {

        this._uiState = { exportId: 0, firstRender: true };
    	this._model = {};

    },
	/**
	 * Fired by MC2 when widget is created. Implementation specific initialization code should
	 * be run here
	 */
    _initWidget: function () {
        var base = this
        this._setupWidgetEvents();
        $(".__externalrelation_project").remove();
        $(".__externalrelation_timesheetentry").remove();
        $(".__externalrelation_dayentry").remove();
        $(".__externalrelation_allocationentry").remove();
        $(".__externalrelation_articleentry").remove();
        $(".__externalrelation_absenceentry").remove();
        $(".__externalrelation_favouriteusers").remove();

        $(".__export_excel").removeClass("__hidden")

        // create some balls
        if (this.options.viewtype == "display") {
            var panelTools = document.createElement("div");
            panelTools.className = "__paneltools";

            var selectedUser = getParameterByName("id");
            var selectedUserName = document.querySelector("[data-propertyschemaname='firstname']").textContent.trim() + " " + document.querySelector("[data-propertyschemaname='lastname']").textContent.trim();

            var url = "/main.aspx?controller=tro&action=approveworkmanager&filtered=true&selecteduser=" + selectedUser + "&selectedusername=" + selectedUserName;

            var entryTypes = ["schedule", "layers", "euro_symbol", "do_not_disturb"];
            if (features.assets)
                entryTypes.push("local_shipping");

            entryTypes.forEach(function (entryType) {
                var button = createPanelToolButton(txt(entryType, "userformwidget"), entryType, url + "&entrytype=" + entryType);
                panelTools.appendChild(button);
            });

            var userPanel = document.querySelector("#__applicationview .__panel");
            userPanel.insertBefore(panelTools, userPanel.firstChild);

            function createPanelToolButton(labelText, icon, url) {
                var buttonContainer = document.createElement("div");
                buttonContainer.className = "__floatbutton2";

                var label = document.createElement("div");
                label.className = "label";
                label.textContent = labelText;

                var button = document.createElement("a");
                button.className = "floatbuttonelement";
                button.href = url;

                var iconElement = document.createElement("i");
                iconElement.className = "material-icons";
                iconElement.textContent = icon;

                button.appendChild(iconElement);
                buttonContainer.appendChild(label);
                buttonContainer.appendChild(button);

                return buttonContainer;
            }

            setTimeout(function() { base.render() }, 0)
        }
    },
	/**
	 * Setup widget's events.
	 */
    _setupWidgetEvents: function () {
        var base = this;
        jQuery('body').on('listviewshown', function (e, listViewWrapper) {
            var collection = listViewWrapper.attr("collection")

            if (collection === "timesheetentry") {
                listViewWrapper.find('th[data-propertyschema="note"]').each(function () {
                    $(this).removeClass("__hidden")
                    $(this).insertAfter($(this).parent().children()[3])
                });
            listViewWrapper.find('td[data-schemaname="note"]').each(function () {
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

            if (collection === "dayentry") {
                listViewWrapper.find('th[data-propertyschema="price"]').each(function () {
                    $(this).removeClass("__hidden")
                    $(this).insertAfter($(this).parent().children()[4])
                });

                listViewWrapper.find('td[data-schemaname="price"]').each(function () {
                    $(this).removeClass("__hidden")
                    $(this).insertAfter($(this).parent().children()[4])
                });

                listViewWrapper.find('th[data-propertyschema="route"]').each(function () {
                    $(this).removeClass("__hidden")
                });
                listViewWrapper.find('td[data-schemaname="route"]').each(function () {
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
                listViewWrapper.find('th[data-propertyschema="note"]').each(function () {
                    $(this).removeClass("__hidden")
                    $(this).insertAfter($(this).parent().children()[3])
                });
                listViewWrapper.find('td[data-schemaname="note"]').each(function () {
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

        // Export

        var base = this;
        $(".__exportLinkContainer").on("click", function (event) {
            base._uiState.showCsvLink = false
            base._uiState.exporting = false
            base.render()
        })

        $(".__tabhead").on("click", function(event) {
            base._uiState.showCsvLink = false
            base._uiState.exporting = false
            base.render()
        })
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
        if (!options) options = {};
        var base = this;

    	if (base._uiState.firstRender) {
			// Code run only once when the widget is rendered for the first time.
    		base.element.removeClass("__hidden");
    		base._uiState.firstRender = false;

            // Event needs to bet here or it's too early not binding correctly
    		$(".__export_excel").on("click", function (event) {
    		    event.preventDefault()

    		    var activeTab = $(".__tab.__active")
    		    // Find the correct list view
    		    var activeList = activeTab.find(".__listview-wrapper") 

    		    // Get filter parameters from the list
    		    var controller = "listview"
    		    var action = "showlistview"
    		    var relationId = activeList.attr("relationid")
    		    var relation = activeList.attr("relation")
    		    var collection = activeList.attr("collection")

    		    var url = getAjaxUrl(controller, action)
    		    url = addParameterToUrl(url, "relationid", relationId)
    		    url = addParameterToUrl(url, "collection", collection)
    		    url = addParameterToUrl(url, "orderby", "__default")
    		    url = addParameterToUrl(url, "ascending", "false")
    		    url = addParameterToUrl(url, "documentsperpage", "50000")
    		    url = addParameterToUrl(url, "localcollection", collection)
    		    url = addParameterToUrl(url, "viewcontroller", "listview")
    		    url = addParameterToUrl(url, "viewaction", "listviewresults")
    		    url = addParameterToUrl(url, "page", "0")
    		    url = addParameterToUrl(url, "relation", relation)
    		    url = addParameterToUrl(url, "islocalrelation", "false")
    		    url = addParameterToUrl(url, "saveAsCsv", "true")
    		    url = addParameterToUrl(url, "csvFieldsStr", base._getCsvFieldsForDocumenType(collection))


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
    		                var link = $(".__exportLinkContainer");
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
    	}

    	var activeTab = $(".__tab.__active")
    	var link = activeTab.find(".__exportLinkContainer");
    	if (base._uiState.showCsvLink)
    	    link.removeClass("__hidden");
    	else
    	    link.addClass("__hidden");

    	var exporting = activeTab.find(".__exporting_excel");
    	var exportingButton = activeTab.find(".__export_excel");

    	var activeList = activeTab.find(".__listview-wrapper") 
    	var collection = activeList.attr("collection")

    	if (base._getCsvFieldsForDocumenType(collection)) {
    	    exportingButton.removeClass("__hidden")
    	} else {
    	    exportingButton.addClass("__hidden")
    	}

    	if (base._uiState.exporting) {
    	    exporting.removeClass("__hidden")
    	    exportingButton.addClass("__disabled")

    	    if (!base._uiState.exportInterval) {
    	        base._uiState.exportingIndex = 0
    	        base._uiState.exportInterval = setInterval(function () {
    	            var activeTab = $(".__tab.__active")
    	            var exportingProgress = activeTab.find(".__exporting_progress");

    	            base._uiState.exportingIndex++
                    var dots = []
    	            for (var i = 0; i < base._uiState.exportingIndex; i++) {
                        dots.push(".")
    	            }

    	            exportingProgress.text(dots.join(""))
    	        }, 1000)
    	    }
    	}
    	else {
    	    exporting.addClass("__hidden");
    	    exportingButton.removeClass("__disabled")

    	    if (base._uiState.exportInterval) {
    	        clearInterval(this._uiState.exportInterval)
    	        base._uiState.exportInterval = undefined
    	        var exportingProgress = $(".userformwidget_exporting_progress");
    	        exportingProgress.text("")
    	    }
    	}
    },
    _getCsvFieldsForDocumenType: function (documentType) {
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

        return ""
    }
});
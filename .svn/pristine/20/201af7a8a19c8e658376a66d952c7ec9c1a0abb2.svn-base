/**
 * MC2 Widget.
 * 
 * Todo: Add description here.
 */
$.widget("routefieldwidget.routefieldwidget", $.core.base, {
    options: {
    },
    show: function() {
        this.element.show();
    },
    hide: function(){
        this.element.hide();
    },
    /**
     * Fired by jQuery when the widget is created.
     */
    _create: function () {

        /* You can use uiState object to maintain the current state of widget's UI. For example
         * you could set parameter _uiState.minimized = true to indicate that the widget's UI is
         * minimized. uiState should not contain any data, all data should go to _model.
         */
        this._uiState = new Object;
        this._uiState.firstRender = true;

        /* Model contains the data of your widget. For example if your widget counts apples
         * model can contain the number of apples. Model should not contain any data specific 
         * to the UI of the widget.
         */
        this._model = new Object();

        this._setupWidgetEvents();
        this._setupRouteForExpenses();

    },

    _setupWidgetEvents: function () {
        var locationDropdown = this.element.find("select[name='location']");

        var base = this;

        locationDropdown.change(function () {
            var locationValue = $("select[name='location'] option:selected").text();

            if ($("select[name='location'] option:selected").val() == "addlocation") {
                base._addNewLocation();
            }
            else {
                base._addLocationToRoute(locationValue);
            }
        });

        // Add "new location" selection after dropdown has updated.
        locationDropdown.on("dropdownupdated", function () {
            locationDropdown.append($('<option>', {
                value: "addlocation",
                text: txt("addlocation", "routefieldwidget")                
            }));
            base.render();
        });
    },
    _addLocationToRoute: function (name)
    {
        if (name == "")
            return;

        var routeValue = $("textarea[name='route']").val();

        if (routeValue !== "")
            routeValue += ", ";

        routeValue += name;
        $("textarea[name='route']").val(routeValue);
    },
    _addNewLocation: function () {
        var newLocation = prompt(txt("inputlocation", "routefieldwidget"), "");
        
        // make sure a valid value was entered
        if (newLocation === "" || isNullOrUndefined(newLocation))
            return false;

        var deferred = new jQuery.Deferred();

        var url = getAjaxUrl("routefieldwidget", "addnewlocation");

        url = addParameterToUrl(url, "name", newLocation);

        var base = this;
        $.ajax({
            dataType: "text",
            url: url,
            async: true,
            type: "GET",
            success: function (data, status, xhr) {

                // Add new value
                $("select[name='location'] option").eq(1).before($("<option></option>").val(data).text(newLocation));

                // Select empty value
                $("select[name='location']").val("");

                base._addLocationToRoute(newLocation);
                deferred.resolve();
            }
        });

        return deferred;
    },
    _setupRouteForExpenses: function () {
        var base = this;
        $("select[name='dayentrytype']").change(function () { base.render(); });
        $("select[name='dayentrytype']").on("dropdownupdated", function ()
        {
        	base.render();
        });
	},
    _updateRouteVisibility: function () {
        var payTypeId = $("select[name='dayentrytype']").val();

        var restApi = new RestApi();
        var base = this;
        restApi.getDocument("dayentrytype", payTypeId).done(function (dayEntryType) {
            if (dayEntryType.hasroute) {
            	base.element.show();
            	$("input[name='requireroute']").val("true");
            } else {
                base.element.hide();
                $("textarea[name='route']").val("");
                $("input[name='requireroute']").val("");
			}
        });
    },
    _setupRouteAsMandatory: function() {
    	if (this._uiState.firstRender == true) {
    		var label = $("textarea[name='route']").parents(".__control").find(".__required-asterisk").removeClass("__hidden");
    	}
    },
    refreshData: function (renderData, initialUpdate) {
        var promises;

        var base = this;

        // Call super when all data updates have been completed.
        $.when.apply($, promises).done(function () {
            base._super(renderData);
        });
	},
    /**
    * Render data in the model and settings into view.
    * 
    * @note: should not make any changes into model or settings
    */
    render: function () {
    	this._updateRouteVisibility();
    	this._setupRouteAsMandatory();

    	this._uiState.firstRender = false;
    }
});
/**
 * entryintegrationwidget widget. Used for all entries (timesheet, absence etc...) to display integration related data.
 */
$.widget("entryintegrationwidget.entryintegrationwidget", $.core.base, {
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
    	this._model.integrationEvents = new Array();
    	this._setupWidgetEvents();
    },
	/**
	 * Setup widget's events.
	 */
    _setupWidgetEvents: function () {
    	var base = this;
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

    	// Create an array of promises to wait before calling base and potentially rendering.
    	var promises = [];

    	// Get data missing from either end of the 	
    	var integrationDataPromise = restApi.find("entryintegrationwidget", "integrationevents",
				{
					id: new ObjectId(getParameterByName("id")),
				});
		
    	var base = this;
    	integrationDataPromise.done(function (data) {
    		if (data.integrationevent)
    			base._model.integrationEvents = data.integrationevent;
    	});

    	promises.push(integrationDataPromise);

    	var base = this;
    	$.when.apply($, promises).done(function () {
    		base.render();
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

    	var integrationEventsHtml = "";

    	for (var i = 0; i < this._model.integrationEvents.length; i++)
    	{
    		var integrationEvent = this._model.integrationEvents[i];

    		if (integrationEventsHtml)
    			integrationEventsHtml += " ";

    		integrationEventsHtml += "<a href='/app/tro/integrationevent?actiontype=view&id=" + integrationEvent._id + "'>["  + integrationEvent.status +  "]</a>";
    	}

    	$(".__displayformrow").last().after("<tr><th>" + txt("integrationevents", "entryintegrationwidget") + "</th><td>" + integrationEventsHtml + "</td>")
    }
});
/**
 * assetentrywidget widget
 */
$.widget("assetentrywidget.assetentrywidget", $.core.base, {
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

    	// Save project selection status to history. This is needed to enable default assets
    	// to be shown based on project data.
    	window.setTimeout(function() {$("div[data-name='project']").trigger("updatehistorystate") }, 0);

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
    }
});
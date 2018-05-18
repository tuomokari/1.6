/**
 * homescreenwidget widget
 */
$.widget("homescreenwidget.homescreenwidget", $.core.base, {
    options: {
    	pastDays: 16,
    	futureDays: 6
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

    	var now = newDate();

    	this._model.startDate = moment(now).add("days", -this.options.pastDays);
    	this._model.endDate = moment(now).add("days", this.options.futureDays);

    	widgets[this.element[0].id + "_workdatawidget"].refreshData({ startDate: this._model.startDate.toDate(), endDate: this._model.endDate.toDate() })

		this.render();
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

    	if (this._uiState.firstRender) {
			// Code run only once when the widget is rendered for the first time.
    		this.element.removeClass("__hidden");
    		this._uiState.firstRender = false;
    	}
    }
});
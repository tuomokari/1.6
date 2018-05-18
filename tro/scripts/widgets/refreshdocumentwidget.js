/**
 * refreshdocumentwidget widget
 */
$.widget("refreshdocumentwidget.refreshdocumentwidget", $.core.base, {
    options: {
    },
	/**
	 * Fired by MC2 when widget is created. Implementation specific initialization code should
	 * be run here
	 */
    _initWidget: function () {
    	this._uiState = {};
    	this._model = {};
    	this._uiState.firstRender = true;
    	this._setupWidgetEvents();
    },
	/**
	 * Setup widget's events.
	 */
    _setupWidgetEvents: function () {
    	var base = this;

    	this._on(this.element.find("[name='refreshbutton']"), {
    		click: "_refreshDisplayNames"
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

    	if (this._uiState.firstRender) {
			// Code run only once when the widget is rendered for the first time.
    		this.element.removeClass("__hidden");
    		this._uiState.firstRender = false;
    	}
    },
	_refreshDisplayNames: function () {

		var collection = this.element.find("[name='collectiondropdown']").val();
		var skip = this.element.find("[name='skip']").val();
		var limit = this.element.find("[name='limit']").val();

		var url = getAjaxUrl("refreshdocumentwidget", "refreshdisplaynames");
		url = addParameterToUrl(url, "collection", collection);
		url = addParameterToUrl(url, "skip", skip);
		url = addParameterToUrl(url, "limit", limit);

		var base = this;
		$.ajax({
			dataType: "JSON",
			url: url,
			async: true,
			type: "GET",
			success: function (data, status, xhr) {

				base.model.results = data;
				base.model.dirty = true;

				base.uistate.lava = false;
				base.render();
			}
		});

		this.uistate.lava = true;
		this.render();
	}
});
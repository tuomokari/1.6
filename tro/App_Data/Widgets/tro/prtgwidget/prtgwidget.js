/**
 * prtgwidget widget
 */
$.widget("prtgwidget.prtgwidget", $.core.base, {
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
    	this._model.svgData = "";

    	var base = this;

    	this.render();
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
    	var base = this;

        // Create an array of promises to wait before calling base and potentially rendering.
        var promises = [];

        var deferredSvg = jQuery.Deferred();
        promises.push(deferredSvg.promise());

        var url = getAjaxUrl("prtgwidget", "getprtggraph");
        url = addParameterToUrl(url, "graphgroup", this.options.graphgroup);
        url = addParameterToUrl(url, "graphidentifier", this.options.graphidentifier);

        $.ajax({
        	url: url,
        	async: true,
        	success: function (data, status, xhr) {
        		base._model.svgData = data;
        		deferredSvg.resolve();
        	}
        });

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

    	if (this._uiState.firstRender) {
			// Code run only once when the widget is rendered for the first time.
    		this.element.removeClass("__hidden");
    		this._uiState.firstRender = false;
    	}
    	else
    	{
    		$(this.element).find(".content").html("<img src='data:image/svg+xml;base64," + this._model.svgData + "' />");
    	}
    }
});
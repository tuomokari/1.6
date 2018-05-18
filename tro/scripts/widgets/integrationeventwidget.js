/**
 * integrationeventwidget widget
 */
$.widget("integrationeventwidget.integrationeventwidget", $.core.base, {
    options: {
    },

    /**
     * Fired by jQuery when the widget is created.
     */
    _create: function () {
    },
	/**
	 * Fired by MC2 when widget is created. Implementation specific initialization code should
	 * be run here
	 */
    _initWidget: function () {
    	this._uiState = {};
    	this._model = {};
    	this._setupWidgetEvents();
    	this._uiState.firstRender = true;
    	this.render();
    },
	/**
	 * Setup widget's events.
	 */
    _setupWidgetEvents: function () {
    	var base = this;
    },
    refreshData: function (options) {

    	if (!options) options = {};

        // Create an array of promises to wait before calling base and potentially rendering.
        var promises = [];

		var base = this;

		$.when.apply($, promises).done(function () {
        });
    },
    render: function (options) {
    	if (!options) options = {};

    	if (this._uiState.firstRender) {
    		this.element.removeClass("__hidden");

    		// Get data from form. Note that this is based on dom tree for compatibility
			// with earlier versions.
    		var id = $(".__displayformrow").first().find("td").text();
    		var collectionname = $(".__displayformrow").eq(1).find("td").text();

    		var content = "<a href='/app/tro/" + collectionname + "?actiontype=view&id=" + id + "'>" + txt("originaldocument", "integrationeventwidget") + "</a>"

    		$(".__displayformrow").first().find("th").first().html(txt("originaldocument", "integrationeventwidget"));
    		$(".__displayformrow").first().find("td").first().html(content);
    		$(".__displayformrow").eq(1).hide();

    		this._uiState.firstRender = false;
    	}
    }
});
/**
 * resourcingwidget widget
 */
$.widget("resourcingwidget.resourcingwidget", $.core.base, {
    options: {
    },

    /**
     * Fired by jQuery when the widget is created.
     */
    _create: function () {

        this._uiState = new Object;
        this._model = new Object();

        this._setupWidgetEvents();
    },
    _setupWidgetEvents: function () {

        var base = this;

        //refresh scrollbars todo: this function should be handled elsewhere optimally
        base.element.find(".__tabhead").on("click", function () {
            // update scrollBars
            updateScrollBars(base.element);
        });

    },

    /**
     * Refresh all data in the model from the server.
     * 
     * @param (bool) renderData True if data should be rendered after refreshing it.
     * 
     * @note: You should not do any dom manipulation here. All DOM manipulation needs
     *        to go to the render method.
     */
    refreshData: function (renderData) {

        // Create an array of promises to wait before calling base and potentially rendering.
        var promises = [];

	var base = this;


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
        this.element.removeClass("__hidden");
    }
});
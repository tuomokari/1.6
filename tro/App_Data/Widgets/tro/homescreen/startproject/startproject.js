/**
 * startproject widget
 */
$.widget("startproject.startproject", $.core.base, {
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
    	this._dataSource = widgets[this.options.datasource];
    },
	/**
	 * Setup widget's events.
	 */
    _setupWidgetEvents: function () {
    	var base = this;

    	this.element.find(".startproject").on("click", function (e) {
    		e.stopPropagation();
    		base._startProject();
    	});

    	this.element.find(".startprojectbutton").on("click", function (e) {
    		e.stopPropagation();
    		var dialogContents = base.element.find(".startprojectdialogcontents");

			// Clear existing content
    		dialogContents.find("input[name='project']").val("");
    		dialogContents.find("input[name='__searchfilterinput-project']").val("");

    		var startProjectDialogContents = base.element.find(".startprojectdialogcontents").show();
    		$(window).dialog({
    			object: startProjectDialogContents.get()[0],
    			minWidth: "50px",
    			height: "130px",
    			width: "75%",
    			maxWidth: "550px",
    			initMc2Controls: true,
    			closeButton: false,
    			hideElementsOnClose: true,
    			customScrolling: false,
    			customClass: "startprojectdialog"
    		});
    	});    	
    },
    _startProject: function () {
    	var base = this;
    	var selectedProjectId = $("input[name='project']").val();

    	if (selectedProjectId == "") {
    		$(".workschedulewidget_startproject").removeClass("__disabled");
    		return;
    	}

    	var url = getAjaxUrl("startproject", "startworkonproject");
    	url = addParameterToUrl(url, "projectid", selectedProjectId);

    	$.ajax({
    		url: url,
    		success: function (data, status, xhr) {
    			base._dataSource.refreshData();
    			$(window).data("dialog").close();
    		}
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
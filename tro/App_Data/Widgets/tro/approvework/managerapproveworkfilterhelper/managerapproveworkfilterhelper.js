/**
 * managerapproveworkfilterhelper widget
 */
$.widget("managerapproveworkfilterhelper.managerapproveworkfilterhelper", $.core.base, {
    options: {
    },

    /**
     * Fired by jQuery when the widget is created.
     */
    _create: function () {

    	this._uiState = {};
    	this._model = {};
    	this._uiState.firstRender = true;

    	var selectedUser = getParameterByName("selecteduser");
    	var selectedUserName = getParameterByName("selectedusername");
    	var entrytype = getParameterByName("entrytype");
    	var filtered = getParameterByName("filtered");

		// If we get selected user, set it's value as default to searchfilter
    	if (selectedUser) {
    		$("#approveworkmanager_userfilter-field").val(selectedUserName);
    		$("#approveworkmanager_userfilter_target").val(selectedUser);
    		$("input[name='showonlyentriesnotaccepted']").prop("checked", false);
    	}

        // expand filters if no selection
    	if (filtered!="true") {
    	    $(".__accordion .__panel").addClass("__active");
    	}

        // If we get entry type, open the corresponding tab
    	if (entrytype) {
    	    $(".__tab-icon:contains(" + entrytype + ")").click();
    	}
    },
	/**
	 * Fired by MC2 when widget is created. Implementation specific initialization code should
	 * be run here
	 */
    _initWidget: function () {
    	var selectedUser = getParameterByName("selecteduser");
    	if (selectedUser) {
    		$("form").submit();
    	}
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
    }
});
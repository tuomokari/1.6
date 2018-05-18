/**
 * querytesterwidget widget
 */
$.widget("querytesterwidget.querytesterwidget", $.core.base, {
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

    	this._model.parameters = new Array();
    	this._model.parameterId = 0;
    },
	/**
	 * Setup widget's events.
	 */
    _setupWidgetEvents: function () {
    	var base = this;

    	this._on(this.element.find("[name='addparameter']"), {
    		click: function () {
    			base._addParameter();
    			base.render();
    		}
    	});

    	this.element.on("keypress", function (e) {
    		if (e.keyCode == 13) {
    			base._executeQuery();				
    		}
    	});

    	this._on(this.element.find("[name='executequery']"), {
    		click: function () {
    			base._executeQuery();
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

        // Create an array of promises to wait before calling base and potentially rendering.
        var promises = [];

		var base = this;

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
    		return;
    	}

    	this._renderMissingParameters();

    	this._clearRemovedParameters();
    },
    _renderMissingParameters: function() {
    	var parameterTemplate = $(this.element).find(".querytesterwidget_parameter.widgettemplate");

    	for (var i = 0; i < this._model.parameters.length; i++) {
    		var parameter = this._model.parameters[i];

    		var parameterDom = $(this.element).find("[data-identifier=" + parameter.identifier + "]");

    		if (parameterDom.length == 0)
    			this._renderParameter(parameter.identifier);
			
    	}
    },
    _clearRemovedParameters: function() {
    	var base = this;
    	this.element.find(".parameters").children().each(function () {
    		var identifier = $(this).attr("data-identifier");

    		var found = false;
    		for (var i = 0; i < base._model.parameters.length; i++) {
    			var parameter = base._model.parameters[i];

    			if (parameter.identifier == identifier) {
    				found = true;
    				break;
    			}
    		}

    		if (!found)
    			$(this).remove();
    	});
    },
    _renderParameter: function(id) {
    	var parameterTemplate = this.element.find(".querytesterwidget_parameter.widgettemplate");

    	var parameter = parameterTemplate.clone();
    	parameter.removeClass("widgettemplate");
    	parameter.removeClass("__hidden");
    	parameter.attr("data-identifier", id);

    	var base = this;
    	parameter.find("[name='remove']").on("click", function () {
    			base._removeParameter(id);
    			base.render();
    	});

    	var parametersArea = $(this.element).find(".parameters");
    	parametersArea.append(parameter);
    },
    _addParameter: function () {
    	var parameter = new Object();

    	parameter.identifier = this._model.parameterId;
    	this._model.parameterId += 1;

    	this._model.parameters.push(parameter);
    },
    _removeParameter: function (identifier) {

    	for (var i = 0; i < this._model.parameters.length; i++) {
    		var parameter = this._model.parameters[i];

    		if (parameter.identifier == identifier) {
    			this._model.parameters.splice(i, 1)
    			break;
    		}
    	}
    },
    _executeQuery: function () {

    	var parameters = {};

    	this.element.find(".parameters").children().each(function () {

    		var type = $(this).find("[name='type']").val();
    		var value = $(this).find("[name='value']").val();
    		var name = $(this).find("[name='name']").val();

    		if (type == "int") {
    			value = parseInt(value);
    		} else if (type == "float") {
    			value = parseFloat(value);
    		} else if (type == "date") {
    			value = new Date(value);
    		}else if (type == "identifier") {
    			value = new ObjectId(value);
    		}else if (type == "bool") {
    			value = (value === "true")? true : false;
    		}

    		parameters[name] = value;
    	});


    	var queryController = this.element.find("[name='querycontroller']").val();
    	var queryName = this.element.find("[name='queryname']").val();

    	var base = this;

    	base.element.find("[name='results']").val("");
    	promise = restApi.find(queryController, queryName, parameters);

    	promise.done(function (data) {
    		base.element.find("[name='results']").val("Call succeeded:" + "\n\n" + JSON.stringify(data));
    	});

    	promise.fail(function (data) {
    		base.element.find("[name='results']").val("Call failed:"+ "\n\n" + JSON.stringify(data));
    	});

    }


});
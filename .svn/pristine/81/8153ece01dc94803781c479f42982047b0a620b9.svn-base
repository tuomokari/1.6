function DBDocument() {

}

function RestApi() {

	this.findOne = function (collection, identifier) {
		if (arguments > 2)
			throw "Too many parameters for GetDocument";

		var deferred = new jQuery.Deferred();


		url = "/doc/" + collection + "/" + identifier;

		$.ajax({
			dataType: "json",
			url: url,
			cache: false,
			async: "true",
			success: function (data, status, xhr) {
				deferred.resolve(data);
			},
			failure: function (data, status, xhr) {
				deferred.reject(data)
			}
		});

		return deferred.promise();
	}

	/**
     * Finds and returns a JSON array of documents based on query and parameters. Queries are defined in
     * server documents with file ending .query inside the App_data folder.
     * 
     * @param (string) controller Name of the query's controller. This is usually name of the product or
     *        or plugin.
     * @param (string) query Name of the query that is used to find the results. 
     * @param (object) parameters Query parameters in object notation. Object should consist of prameter
     *        name and value pairs.
     * 
     * @return (Promise) A promise object of a JSON result with data that was found.
     * 
     * @throws if the query parameters are incorrect.
     */
	this.find = function (controller, query, parameters) {
		var deferred = new jQuery.Deferred();

		if (typeof controller == "undefined" || typeof query == "undefined" || typeof queryController == "parameters")
			throw ("Missing parameter");

		var url = "/find/" + controller + "/" + query;

		if (typeof parameters === "object") {
			for (var parameterName in parameters) {
				if (!parameters.hasOwnProperty(parameterName))
					continue;

				url = addFindParameterToUrl(url, parameterName, parameters[parameterName]);
			}
		}

		$.ajax({
			dataType: "json",
			url: url,
			cache: false,
			async: "true",
			success: function (data, status, xhr) {
				deferred.resolve(data);
			},
			error: function (data, status, xhr) {
				deferred.reject(data)
			}
		});

		return deferred.promise();
	}

	function addFindParameterToUrl(url, paremeterName, parameter) {
		var type = typeof (parameter);

		if (type === "string") {
			url = addParameterToUrl(url, paremeterName, "string:" + parameter);
		}
		else if (type === "number") {
			if (isNaN(parameter))
				throw ("Attempted to find with parameter value NaN. Please make sure you assign finite actual (not NaN) numbers as parameters.");

			if (parameter === Number.POSITIVE_INFINITY || parameter === Number.NEGATIVE_INFINITY)
				throw ("Attempted to find with infinite value. Please make sure you assign finite actual (not NaN) numbers as parameters.");

			url = addParameterToUrl(url, paremeterName, "number:" + parameter);
		}
		else if (type === "boolean") {
			value = "false";
			if (parameter === true)
				value = "true";

			url = addParameterToUrl(url, paremeterName, "bool:" + value);
		}
		else if (type === "object") {
			var objectType = Object.prototype.toString.call(parameter);

			if (objectType === "[object Date]") {
				if (isNaN(parameter.getTime()))
					throw "Attempted to find with invalid Date value.";

				url = addParameterToUrl(url, paremeterName, "date:" + UTCISOString(parameter));
			}
			else if (parameter.hasOwnProperty("objectId")) {
				url = addParameterToUrl(url, paremeterName, "objectid:" + parameter.objectId);
			}
		}
		else if (type === "undefined") {
			url = addParameterToUrl(url, paremeterName, "null");
		}

		return url;
	}

	this.delete = function (collection, identifier) {

		if (!collection || !identifier)
			throw ("Missing parameter");

		var deferred = new jQuery.Deferred();

		var url = "/doc/" + collection + "/" + identifier;

		$.ajax({
			dataType: "json",
			url: url,
			type: 'DELETE',
			cache: false,
			async: "true",
			success: function (data, status, xhr) {
				deferred.resolve(data);
			},
			error: function (data, status, xhr) {
				deferred.reject(data)
			}
		});

		return deferred.promise();
	}


	// Obsolete
	this.getDocument = function (collection, identifier) {
		return this.findOne(collection, identifier);
	}
}

function ObjectId(objectId) {
	this.objectId = objectId;
}

var restApi = new RestApi();
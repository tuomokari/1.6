function App() {
    this.call = function (controller, action, parameters) {
        var deferred = new jQuery.Deferred();

        if (typeof controller == "undefined" || typeof action == "undefined")
            throw ("Missing parameter");

        var url = "/app/" + controller + "/" + action;

        if (typeof parameters === "object") {
            for (var parameterName in parameters) {
                if (parameterName === null || !parameters.hasOwnProperty(parameterName))
                    continue;

                var parameter = parameters[parameterName];

                if (parameter === null)
                    continue;

                var type = typeof (parameter);

                if (type === "string") {
                    url = addParameterToUrl(url, parameterName, parameter);
                }
                else if (type === "number") {
                    if (isNaN(parameter))
                        throw ("Attempted to call server with NaN number.");

                    if (parameter === Number.POSITIVE_INFINITY || parameter === Number.NEGATIVE_INFINITY)
                        throw ("Attempted to call server with infinite value.");

                    url = addParameterToUrl(url, parameterName, parameter);
                }
                else if (type === "boolean") {
                    url = addParameterToUrl(url, parameterName, parameter);
                }
                else if (type === "object") {
                    var objectType = Object.prototype.toString.call(parameter);

                    if (objectType === "[object Date]") {
                        if (isNaN(parameter.getTime()))
                            throw "Attempted to call server with invalid Date value.";

                        url = addParameterToUrl(url, parameterName, UTCISOString(parameter));
                    }
                    else if (parameter.hasOwnProperty("objectId")) {
                        url = addParameterToUrl(url, parameterName, parameter.objectId);
                    }
                }
                else if (type === "undefined") {
                    // do not add parameter
                }
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
}

var app = new App();
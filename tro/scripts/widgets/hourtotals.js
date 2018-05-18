/**
 * MC2 Widget.
 * 
 * Todo: Add description here.
 */
$.widget("hourtotals.hourtotals", $.core.base, {
    options: {
        mode: "",
        startdate: null,
        enddate: null
    },

    /**
     * Fired by jQuery when the widget is created.
     */
    _create: function () {

        /* You can use uiState object to maintain the current state of widget's UI. For example
         * you could set parameter _uiState.minimized = true to indicate that the widget's UI is
         * minimized. uiState should not contain any data, all data should go to _model.
         */
        this._uiState = new Object;

        /* Model contains the data of your widget. For example if your widget counts apples
         * model can contain the number of apples. Model should not contain any data specific 
         * to the UI of the widget.
         */
        this._model = {
            totalTimesheetHours: 0,
            totalAbsenceHours: 0
        };
    },

    /**
     * Refresh all data in the model from the server.
     * 
     * @param (bool) renderData True if data should be rendered after refreshing it.
     * 
     * @note: You should not do any dom manipulation here. All DOM manipulation needs
     *        to go to the render method.
     */
    refreshData: function (doRender) {

        // Create an array of promises to wait before calling base and potentially rendering.
        var promises = [];

		var base = this;
         promises.push(this.getHourTotals());

        // Call super when all data updates have been completed.
         $.when.apply($, promises).done(function () {
             if (doRender) {
                base.render();
            }
        });
	},

    /**
    * Render data in the model and settings into view.
    * 
    * @note: should not make any changes into model or settings
    */
    render: function () {
        this.element.find(".hourtotals_totaltimesheethours_result").text(this.formatHuors(this._model.totalTimesheetHours));
        this.element.find(".hourtotals_totalabsencehours_result").text(this.formatHuors(this._model.totalAbsenceHours));

    },
    getHourTotals: function () {
        if (this.options.mode == "notaccepted")
            return this.gettotalHours();
        else if (this.options.mode == "daterange")
            return this.getTotalHoursByRange();
    },
    getTotalHoursByRange: function () {

        var restApi = new RestApi();

        var deferred = restApi.find("hourtotals", "totalsforuser_bydate",
            {
                user: new ObjectId($("body").data("user")),
                rangestart: getDateFromIsoString(this.options.startdate),
                rangeend: getDateFromIsoString(this.options.enddate)
            });

        var base = this;


        deferred.done(function (data) {

            var totalTimesheetHours = 0;
            var totalAbsenceHours = 0;

            if (data.timesheetentry != null) {
                for (var i = 0; i < data.timesheetentry.length; i++) {
                    var timesheetEntry = data.timesheetentry[i];

                    var start = getDateFromIsoString(timesheetEntry.starttimestamp);
                    var end = getDateFromIsoString(timesheetEntry.endtimestamp);

                    // duration in hours
                    var duration = (end - start) / 1000 / 60 / 60;

                    if (duration > 0)
                        totalTimesheetHours += duration;
                }

                base._model.totalTimesheetHours = totalTimesheetHours
            }

            if (data.absenceentry != null) {
                for (var i = 0; i < data.absenceentry.length; i++) {
                    var absenceentry = data.absenceentry[i];

                    var start = getDateFromIsoString(absenceentry.starttimestamp);
                    var end = getDateFromIsoString(absenceentry.endtimestamp);

                    // duration in hours
                    var duration = (end - start) / 1000 / 60 / 60;

                    if (duration > 0)
                        totalAbsenceHours += duration;
                }

                base._model.totalAbsenceHours = totalAbsenceHours
            }

        });

        return deferred;
    },
    gettotalHours: function() {
        var restApi = new RestApi();
        var deferred = restApi.find("hourtotals", "totalsforuser_unapproved",
            {
                user: new ObjectId($("body").data("user")),
            });

        var base = this;

        deferred.done(function (data) {

            var totalTimesheetHours = 0;
            var totalAbsenceHours = 0;

            if (data.timesheetentry != null) {
                for (var i = 0; i < data.timesheetentry.length; i++) {
                    var timesheetEntry = data.timesheetentry[i];

                    var start = getDateFromIsoString(timesheetEntry.starttimestamp);
                    var end = getDateFromIsoString(timesheetEntry.endtimestamp);

                    // duration in hours
                    var duration = (end - start) / 1000 / 60 / 60;

                    if (duration > 0)
                        totalTimesheetHours += duration;
                }

                base._model.totalTimesheetHours = totalTimesheetHours
            }

            if (data.absenceentry != null) {
                for (var i = 0; i < data.absenceentry.length; i++) {
                    var absenceentry = data.absenceentry[i];

                    var start = getDateFromIsoString(absenceentry.starttimestamp);
                    var end = getDateFromIsoString(absenceentry.endtimestamp);

                    // duration in hours
                    var duration = (end - start) / 1000 / 60 / 60;

                    if (duration > 0)
                        totalAbsenceHours += duration;
                }

                base._model.totalAbsenceHours = totalAbsenceHours
            }

        });

        return deferred;
    },
    formatHuors : function (totalHours)
    {
        var hours = Math.floor(totalHours)
        
        var minutes = Math.floor((totalHours - hours) * 60);

        var ret = "";

        if (hours > 0)
            ret = hours + " " + txt("unit_hours");

        if (minutes > 0)
        {
            if (ret != "")
                ret += " ";

            ret += minutes + " " + txt("unit_minutes");
        }

        return ret;
    }
});
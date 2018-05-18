/**
 * calendarwidget widget
 */
$.widget("calendarwidget.calendarwidget", $.core.base, {
    options: {
    },

    /**
     * Fired by jQuery when the widget is created.
     */
    _create: function () {

        this._uiState = new Object;
        this._model = new Object();

        this._uiState.firstRender = true;
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

        if (this._uiState.firstRender) {
            this._uiState.firstRender = false;
            this.element.fullCalendar({
                lang: txt("languagecode", "core").substring(0, 2).toLowerCase(),
                events: [
                {
                    title: 'Ikkunan vaihto',
                    start: '2015-12-01'
                },
                {
                    title: 'Vesitornin purku',
                    start: '2015-12-07',
                    end: '2015-12-19'
                },
                {
                    id: 999,
                    title: 'Porakaivon putsaus',
                    start: '2015-12-09T16:00:00'
                },
                {
                    id: 999,
                    title: 'Töhryjen poisto',
                    start: '2015-12-16T16:00:00'
                },
                {
                    title: 'Isotehoimurointi',
                    start: '2015-12-11',
                    end: '2015-12-13'
                },
                {
                    title: 'Seinän siirto',
                    start: '2015-12-12T10:30:00',
                    end: '2015-12-12T12:30:00'
                },
                {
                    title: 'Ovimattojen vaihto',
                    start: '2015-12-12T12:00:00'
                },
                {
                    title: 'Töhryn poisto',
                    start: '2015-12-12T14:30:00'
                },
                {
                    title: 'Remeleitten vaihto',
                    start: '2015-12-12T17:30:00'
                },
                {
                    title: 'Valojen tarkastus',
                    start: '2015-12-12T20:00:00'
                },
                {
                    title: 'Autojen huollot',
                    start: '2015-12-13T07:00:00'
                },
                {
                    title: 'Hanojen tsekkaus ja mittaukset',
                    url: 'http://google.com/',
                    start: '2015-12-28'
                }
                ]
            });

            //test event
            //this.addCalendarEvent({
            //    title: "Testi",
            //    start: UTCISOString(new Date())
            //});
            return;
        }
    },

    addCalendarEvent: function (args) { // todo: this don't work yet :)

        this.element.fullCalendar({
            events: [
                {
                    title: 'All Day Event',
                    start: '2015-12-01'
                },
                {
                    title: 'Long Event',
                    start: '2015-12-07',
                    end: '2015-12-10'
                },
                {
                    id: 999,
                    title: 'Repeating Event',
                    start: '2015-12-09T16:00:00'
                },
                {
                    id: 999,
                    title: 'Repeating Event',
                    start: '2015-12-16T16:00:00'
                },
                {
                    title: 'Conference',
                    start: '2015-12-11',
                    end: '2015-12-13'
                },
                {
                    title: 'Meeting',
                    start: '2015-12-12T10:30:00',
                    end: '2015-12-12T12:30:00'
                },
                {
                    title: 'Lunch',
                    start: '2015-12-12T12:00:00'
                },
                {
                    title: 'Meeting',
                    start: '2015-12-12T14:30:00'
                },
                {
                    title: 'Happy Hour',
                    start: '2015-12-12T17:30:00'
                },
                {
                    title: 'Dinner',
                    start: '2015-12-12T20:00:00'
                },
                {
                    title: 'Birthday Party',
                    start: '2015-12-13T07:00:00'
                },
                {
                    title: 'Click for Google',
                    url: 'http://google.com/',
                    start: '2015-12-28'
                }
            ]
        });
        this.element.fullCalendar("render");
    }
});
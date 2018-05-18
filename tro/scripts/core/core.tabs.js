(function ($) {
    $.tabs = function (el, options) {
        // To avoid scope issues, use 'base' instead of 'this'
        // to reference this class from internal events and functions.
        var base = this;

        // Access to jQuery and DOM versions of element
        base.$el = $(el);
        base.el = el;

        // Add a reverse reference to the DOM object
        base.$el.data("tabs", base);

        base.init = function () {

            base.options = $.extend({}, $.tabs.defaultOptions, options);

            // move head elements to header
            $(el).find(".__tabhead").appendTo($(el).find(".__tabsheader-wrapper"));

            $(el).find(".__tabhead").click(function () {

                $(this).siblings().removeClass("__active");
                $(this).addClass("__active");

                var tabindex = $(this).index();

                $(el).find(".__tab").removeClass("__active");
                $(el).find(".__tab").eq(tabindex).addClass("__active");

                var callerId = $(el).attr("data-callerid");

                var data = new Object();

                data.tabindex = tabindex;

                setHistoryState(callerId, data);

            });

            var tabIndex = base.options.initial;

            if (base.options.usehistory) {
                var historyTabIndex = $(el).attr("data-historytabindex");

                if (typeof historyTabIndex !== "undefined" && historyTabIndex != "") {
                    tabIndex = parseInt(historyTabIndex, 10);
                }
            }

            // add rel attributes, requires tabs to have an id attribute
            $(el).find(".__tabhead").each(function (index) {
                if ($(el).find(".__tab").eq(index).has("[id]")) {
                    $(this).attr("rel", $(el).find(".__tab").eq(index).attr("id"));
                }
            });

			if (tabIndex != -1)
			    $(el).find(".__tabhead").eq(tabIndex).click();

            $(el).show();
        };

        // Run initializer
        base.init();
    };

    $.tabs.defaultOptions = {
        initial: 0,
        usehistory: true
    };

    $.fn.tabs = function (options) {
        return this.each(function () {
            (new $.tabs(this, options));
        });
    };

})(jQuery);
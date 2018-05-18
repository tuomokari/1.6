(function ($) {
    $.singleUse = function (el, options) {

        // To avoid scope issues, use 'base' instead of 'this'
        // to reference this class from internal events and functions.
        var base = this;

        // Access to jQuery and DOM versions of element
        base.$el = $(el);
        base.el = el;

        // Add a reverse reference to the DOM object
        base.$el.data("singleUse", base);

        base.init = function () {

            base.options = $.extend({}, $.singleUse.defaultOptions, options);

            base.$el.on("click", function() {
                $(this).blur().addClass("__disabled");
            });
        };

        base.release = function (singleUse) {
            base.$el.removeClass("__disabled");
        }

        // Run initializer
        base.init();
    };

    //$.singleUse.defaultOptions = {
    //};

    $.fn.singleUse = function (options) {
        return this.each(function () {
            (new $.singleUse(this, options));
        });
    };

})(jQuery);
(function ($) {
    $.ui = function (el, options) {

        var base = this;

        base.$el = $(el);
        base.el = el;

        base.$el.data("ui", base);

        base.init = function () {
            base.options = $.extend({}, $.ui.defaultOptions, options);

            $(el).click(function () {
                


            });
        };

        base.showMenu = function () {
        };

        base.hideMenu = function () {
        };

        // Run initializer
        base.init();
    };

    $.ui.defaultOptions = {
    };

    $.fn.ui = function (options) {
        return this.each(function () {
            (new $.ui(this, options));
        });
    };

})(jQuery);
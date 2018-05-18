(function ($) {
    $.blockscreen = function (el, options) {

        // To avoid scope issues, use 'base' instead of 'this'
        // to reference this class from internal events and functions.
        var base = this;

        // Access to jQuery and DOM versions of element
        base.$el = $(el);
        base.el = el;

        // Add a reverse reference to the DOM object
        base.$el.data("blockscreen", base);

        base.init = function () {

            base.options = $.extend({}, $.blockscreen.defaultOptions, options);

            if (base.options.autoRelease)
                base.$el.on("click.__blockscreen", function () {
                    base.release();
                });

            if (base.options.showOnInit)
                base.show();
        };

        base.show = function (blockscreen) {
            base.el.classList.add("__blockscreen");
            if (base.options.shade)
                base.el.classList.add("__shade");
            if (base.options.spinner) {
                base.spin();
            }
        }

        base.spin = function (blockscreen) {
            window.setTimeout(function () {
                base.el.classList.add("__spinner");
            }, base.options.delay)
        }

        base.stopSpin = function (blockscreen) {
            base.el.classList.remove("__spinner");
        }

        base.release = function (blockscreen) {
            base.el.classList.remove("__blockscreen", "__spinner", "__shade");
            base.$el.off(".__blockscreen");
            base.options.onClose.call(this);
        }

        // Run initializer
        base.init();
    };

    $.blockscreen.defaultOptions = {
        showOnInit: true,
        shade: true,
        spinner: false,
        delay: 0,
        autoRelease: false,
        onClose: function () { } // callback for closing
    };

    $.fn.blockscreen = function (options) {
        return this.each(function () {
            (new $.blockscreen(this, options));
        });
    };

})(jQuery);
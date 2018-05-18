(function ($) {
    $.toggleablePanel = function (el, options) {
        // To avoid scope issues, use 'base' instead of 'this'
        // to reference this class from internal events and functions.
        var base = this;

        // Access to jQuery and DOM versions of element
        base.$el = $(el);
        base.el = el;

        // Add a reverse reference to the DOM object
        base.$el.data("toggleablePanel", base);

        base.init = function () {

            base.options = $.extend({}, $.toggleablePanel.defaultOptions, options);

            $(el).find(".__panel:first").siblings().addBack().find(".__panelhead").addClass("__panelsection").on("click", function (e) {
                if (!$(this).parents(".__panel").hasClass("__active")) {
                    if (base.options.closeOther) {
                        base.hideAll();
                    }
                    $(this).parents(".__panel").addClass("__active");
                }
                else
                    hideSection(this);
            });

            if (base.options.expandAll) {
                base.showAll();
            }

            if (base.options.initial) {
                base.show(base.options.initialindex);
            }

            // expand sections marked for expansion
            $(el).find(".__panel:first").siblings().addBack().each(function () {
                if ($(this).hasClass("expand")) {
                    $(this).find(".__panelhead").click();
                }
            });
        };

        base.showAll = function () {
            if (!base.options.closeOther) {
                $(el).find(".__panel:first").siblings().addBack().find(".__panelhead").click();
            }
        };

        base.show = function (index) {
            if (index > 0)
                index--; // since the collection begins from 0 but the values in metacode are given from 1
            $(el).find(".__panel:eq(" + index + ")").find(".__panelhead").click();
        };

        base.hideAll = function () {
            $(el).find(".__panel:first").siblings().removeClass("__active");
        };

        function hideSection(el) {
            $(el).parents(".__panel").removeClass("__active");
        };

        // Run initializer
        base.init();
    };

    $.toggleablePanel.defaultOptions = {
        closeOther: false,
        expandAll: false,
        initial: false,
        initialindex: 0
    };

    $.fn.toggleablePanel = function (options) {
        return this.each(function () {
            (new $.toggleablePanel(this, options));
        });
    };

})(jQuery);
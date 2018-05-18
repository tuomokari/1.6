(function ($) {
    $.flyout = function (el, options) {

        // To avoid scope issues, use 'base' instead of 'this'
        // to reference this class from internal events and functions.
        var base = this;

        // Access to jQuery and DOM versions of element
        base.$el = $(el);
        base.el = el;

        // Add a reverse reference to the DOM object
        base.$el.data("flyout", base);

        base.init = function () {

            base.options = $.extend({}, $.flyout.defaultOptions, options);

            $(el).on("mouseleave", function (e) {
                window.clearTimeout(flyoutTimer);
            });

            $(el).on("click", function () {
                window.clearTimeout(flyoutTimer);
                showFlyout();
            });

            $(el).on("mouseenter", function () {
                flyoutTimer = window.setTimeout(showFlyout, 500);
            });

            function showFlyout() {
                if ($(".__flyout").length)
                    return false;

                // show blockscreen
                // Todo: add new blockscreen
                //if ($(base.options.blockscreen))
                //    $("body").data("blockscreen").show();

                // create flyout

                var flyout = document.createElement("div");
                flyout.className = "__flyout";
                flyout.style.width = base.options.width;
                flyout.style.height = base.options.height;
                data = "controller=" + base.options.controller + "&action=" + base.options.action;

                $(el).data("originalparentelement", $(base.options.selector).parent());
                $(base.options.selector).appendTo(flyout).show();

                $(el).data("flyout").position(flyout);

                $(flyout).appendTo("body").stop(true, true).fadeIn({ duration: 50, queue: false });

                // bind temporary events
                $(flyout).mouseleave(function () {

                    // don't close flyout if any input field inside has focus
                    if ($(flyout).find("input:focus, select:focus").length)
                        return;

                    $(flyout).mouseenter(function () {
                        $(this).stop(true).css("opacity", "1");
                    });
                    $(this).delay(250).fadeOut(50, function () {
                        $(el).data("flyout").close(".__flyout");
                    });
                });
                $(window).bind("resize", function () {
                    $(el).data("flyout").position(flyout);
                });

                // close flyout if clicked outside (requires blockscreen, this is also for touchenabled devices)
                // todo: update to new blockscreen
                //$(".__blockscreen").bind("click touchstart", function () {
                //    $(el).data("flyout").close(flyout);
                //})
            }

        };

        base.position = function (flyout) {

            var flyoutWidth = $(flyout).outerWidth();
            var flyoutHeight = $(flyout).outerHeight();

            var sourceX = $(el).offset().left + $(el).outerWidth();
            var sourceY = $(el).offset().top;
            $(flyout).css("position", "absolute");

            if (sourceX - flyoutWidth < 0) {
                var width = $(window).width() - 20;
                $(flyout).css("width", width + "px");
                $(flyout).css("left", 10 + "px");
            }
            else {
                $(flyout).css("left", sourceX - flyoutWidth + "px");
            }
            $(flyout).css("top", sourceY - flyoutHeight + base.$el.outerHeight() + "px"); // todo: calculate wether to put up or down, and +48 to overlap the bottom icon
        }

        base.close = function (flyout) {
            $(flyout).children(":first").hide().appendTo($(el).data("originalparentelement"));
            $(flyout).remove();

            // todo: add new blockscreen
            //if ($(base.options.blockscreen))
            //    $("body").data("blockscreen").hide();
        }
        // Run initializer
        base.init();
    };

    $.flyout.defaultOptions = {
        selector: null,
        width: "640px",
        height: "480px",
        event: "click",
        blockscreen: false
    };

    $.fn.flyout = function (options) {
        return this.each(function () {
            (new $.flyout(this, options));
        });
    };

})(jQuery);
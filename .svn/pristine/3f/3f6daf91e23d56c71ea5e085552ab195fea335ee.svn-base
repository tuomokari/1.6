(function ($) {
    $.listview = function (el, options) {
        // To avoid scope issues, use 'base' instead of 'this'
        // to reference this class from internal events and functions.
        var base = this;

        // Access to jQuery and DOM versions of element
        base.$el = $(el);
        base.el = el;

        // Add a reverse reference to the DOM object
        base.$el.data("listview", base);

        base.init = function () {
            base.options = $.extend({}, $.listview.defaultOptions, options);

            var baseid = $(el).attr("id");
            setupListViewResults(baseid);

            $(el).parents(".__listview-wrapper").siblings(".__listviewactions.fixed").remove();
            base.listviewActions = $(el).parents(".__listview-wrapper").siblings(".__listviewactions");

            base.listviewActionsTop = base.listviewActions.offset().top;

            base.floatingListviewActions = base.listviewActions.clone().addClass("fixed").data("floating", "true").insertAfter(base.listviewActions);
            $(window).on("scroll.listview resize.listview", function () {

                // calculate menubar height depending on smartphone/full
                if (mobile())
                    menubarHeight = 0;
                else
                    menubarHeight = $("#__menubar").height();

                if ($(el).hasClass("__noresults"))
                    return false;

                base.listviewActionsTop = base.listviewActions.offset().top; // update position, if number of items or other objects on page have cause elements to shift

                if ($(window).scrollTop() > base.listviewActionsTop - menubarHeight && $(window).scrollTop() < $(el).find(".__listviewtable").offset().top + $(el).find(".__listviewtable").height() - menubarHeight) {
                    $(".__listviewactions.fixed.show").not(base.floatingListviewActions).removeClass("show");
                    base.floatingListviewActions.addClass("show");
                    base.listviewActions.css("visibility", "hidden");
                }
                else {
                    base.floatingListviewActions.removeClass("show");
                    base.listviewActions.css("visibility", "visible");
                }
            });

            $(el).find(".__listview_checkbox input").on("click", function () {
                if ($(this).parents(".__listviewtable").find("input:checked").length > 0) {
                    base.floatingListviewActions.addClass("__items-selected");
                    base.listviewActions.addClass("__items-selected");
                }
                else {
                    base.clearSelection();
                }
            });

            base.clearSelection = function () {
                base.floatingListviewActions.removeClass("__items-selected");
                base.listviewActions.removeClass("__items-selected");
                $(el).parents(".__listviewtable").find("input:checked").prop("checked", false);
            }


            base.clearSelection();

            function setupListViewResults(baseid) {
                var container = $('#' + baseid);
                var next = $('#' + baseid + '-next');
                var previous = $('#' + baseid + '-previous');

                next.click(nextPage);
                previous.click(previousPage);

            }

            function nextPage(e) {
                var container = $('#' + baseid);
                var listViewWrapper = container.parent();
                listViewWrapper.addClass('__listview-unload');

                var page = parseInt(listViewWrapper.attr('page'), 10);

                listViewWrapper.attr('page', page + 1);

                showListView(listViewWrapper);
            }

            function previousPage(e) {
                var container = $('#' + baseid);
                var listViewWrapper = container.parent();
                listViewWrapper.addClass('__listview-unload');

                var page = parseInt(listViewWrapper.attr('page'), 10);

                listViewWrapper.attr('page', Math.max(page - 1, 0));

                showListView(listViewWrapper);
            }

        };

        // Run initializer
        base.init();
    };

    $.listview.defaultOptions = {
        page: "",
        sorfield: "",
        sortdir: "",
        filters: null
    };

    $.fn.listview = function (options) {
        return this.each(function () {
            (new $.listview(this, options));
        });
    };

})(jQuery);
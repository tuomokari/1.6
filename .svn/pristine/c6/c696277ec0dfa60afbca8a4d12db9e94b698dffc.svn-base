(function ($) {
    $.dialog = function (el, options) {

        // determine whether to open the dialog immediately
        var immediate;
        var applicationView = "#__applicationview";

        switch (el) {
            case document.body: // body is not allowed, so the functionality is transferred to window for immediate triggering
            case window:

                // if there are any dialogs already open, cancel process
                if (dialogOpen())
                    return;

                immediate = true;
                break;

            default:
                immediate = false;
        }

        // To avoid scope issues, use 'base' instead of 'this'
        // to reference this class from internal events and functions.
        var base = this;

        // Access to jQuery and DOM versions of element, use el and $el to reference the element from which the dialog is triggered. This is mainly for events.
        base.$el = $(el);
        base.el = el;

        // Add a reverse reference to the DOM object
        base.$el.data("dialog", base);

        // defaults
        base.closeButtonSize = 40;
        base.animationDuration = 75;

        base.init = function () {

            base.options = $.extend({}, $.dialog.defaultOptions, options);

            // The dialog is able to display either
            // * an element already created in DOM or
            // * by loading either an url via ajax or
            // * loading an MC2 action via ajax

            // determine dialog type
            var dialogType;

            if ($(base.options.selector).length > 0)
                dialogType = "dom";
            else if (base.options.object instanceof HTMLElement)
                dialogType = "object";
            else if (base.options.url != "")
                dialogType = "url";
            else if (base.options.mc2url)
                dialogType = "mc2";
            else if (base.options.run != "")
                dialogType = "function";
            else
                return;

            // Determine how the dialog is supposed to be triggered

            if (immediate)
                createDialog(dialogType);
            else {
                $(el).on(base.options.eventType, function (e) { // This is the default event that creates the dialog.

                    e.stopPropagation(); // Prevent potential parent element events from firing

                    if (dialogOpen())
                        return;

                    $(el).addClass("__passthrough"); // while the dialog is open, the trigger element events are disabled via css class

                    clearTimeout(window.dialogClickHandler); // the actual dialog display is deferred to avoid them opening multiple times
                    window.dialogClickHandler = setTimeout(function () {
                        createDialog(dialogType);
                    }, 50);
                });
            }
        }

        base.close = function (skipCallBack) {
            clearTimeout(window.dialogClickHandler);

            dialog = $(this).data("dialog");

            $(dialog).trigger("dialogclosing", base.options.identifier);

            skipCallBack = (typeof skipCallBack === "undefined") ? false : skipCallBack;

            $(dialog).addClass("__closed");

            if (!base.options.duplicateSourceElement) {
                var contentElements = $(dialog).children(".__dialog-content").children();

                if (base.options.hideElementsOnClose)
                    contentElements.hide();

                contentElements.appendTo($(dialog).data("originalparentelement"));
            }

            $("body").removeClass("__dialogopen __blockscreen");
            $("html").attr("data-dialogopen", "");
            $("html").attr("data-overflow", "");
            $("body").off(".__dialog");
            $(window).off(".__dialog");
            $(applicationView).off(".__dialog");
            $(dialog).trigger("dialogclosed", base.options.identifier);
            $(dialog).remove();

            releaseScrolling();

            $(el).removeClass("__passthrough");

            if (!skipCallBack)
                base.options.onClose.call(this);
        }

        function createDialog(dialogType) {

            // if there are any dialogs already open, cancel process
            if ($("body").hasClass("__dialogopen")) {
                return;
            }

            // check if overflow
            if ($(window).height() < $(document).height()) {
                $("html").attr("data-overflow", "true");
            }

            lockScrolling();

            $("body").addClass("__dialogopen __blockscreen");
            $("html").attr("data-dialogopen", "true");

            // make sure menu is closed and that there are no blockscreens
            $("body").removeClass("__menuopen");

            $(dialog).trigger("dialogcreating", base.options.identifier);

            // create dialog
            var dialog = document.createElement("div"); // dialog outer wrapper
            dialog.className = "__dialog";
            if (options.customClass)
                dialog.classList.add(options.customClass);
            dialog.style.width = base.options.width;
            dialog.style.height = base.options.height;
            if (base.options.maxWidth)
                dialog.style.maxWidth = base.options.maxWidth;
            if (base.options.maxHeight)
                dialog.style.maxHeight = base.options.maxHeight;
            if (base.options.minWidth)
                dialog.style.minWidth = base.options.minWidth;
            if (base.options.minHeight)
                dialog.style.minHeight = base.options.minHeight;

            var dialogBody = document.createElement("div"); // dialog content wrapper
            dialogBody.className = "__dialog-content";

            // closing functions closeable: false automatically disables also the close button. Dialog is still closeable through calling the method
            if (base.options.closeable) {

                $(document).on("keyup.__dialog", function (e) {
                    switch (e.keyCode) {  // esc
                        case 27: $(el).data("dialog").close();
                    }
                });
            }
            dialog.appendChild(dialogBody);

            switch (dialogType) {

                case "mc2":
                    var dialogContent = document.createElement("div");
                    dialog_request = $.ajax({
                        dataType: "html",
                        url: base.options.mc2url,
                        async: true,
                        cache: false,
                        success: function (data, status, xhr) {
                            if (!verifyLoginStatus(data)) return;

                            data = base.options.preprocess($(data));

                            $(dialogContent).append(data);

                            window.initDialogTimeout = setTimeout(function () {
                                initFields(dialogContent);
                                processViewFormFields(dialogContent);
                            }, 10);
                        }
                    });

                    dialogBody.appendChild(dialogContent);
                    src = base.options.source;

                    break;

                case "dom": // any dom selector

                    $(dialog).data("originalparentelement", $(base.options.selector).parent()); // saving the original location of the dom element

                    if (base.options.duplicateSourceElement)
                        $(base.options.selector).clone().addClass("__dialog-cloned").appendTo(dialogBody).show();
                    else
                        $(base.options.selector).appendTo(dialogBody).show(); // dom element is moved inside the dialog to make sure there are no problems caused by duplicating it

                    break;

                case "object": // any object

                    $(dialog).data("originalparentelement", $(base.options.object).parent()); // saving the original location of the dom element

                    if (base.options.duplicateSourceElement)
                        $(base.options.object).clone().addClass("__dialog-cloned").appendTo(dialogBody);
                    else
                        $(base.options.object).appendTo(dialogBody); // dom element is moved inside the dialog to make sure there are no problems caused by duplicating it

                    break;

                case "url": // url loaded into an iframe
                    var dialogContent = document.createElement("iframe");
                    dialogContent.setAttribute("src", base.options.url);
                    $(dialog).addClass("__iframe");
                    dialogBody.appendChild(dialogContent);

                    break;

                case "function": // function delivered with the dialog call
                    var result = base.options.run(base);

                    if (result instanceof HTMLElement) // make sure result is HTML
                        dialogBody.appendChild(result);
                    else
                        base.close();

                    break;

                default:
                    base.close();

                    break;
            }

            //determine where the dialog element will be inserted
            var targetContainer = (base.options.targetContainer instanceof HTMLElement) ? base.options.targetContainer : document.body;

            targetContainer.appendChild(dialog);

            $(base).data("dialog", dialog);

            window.appendDialogTimeout = setTimeout(function () {
                dialog.classList.add("__open");

                // iScroll for all but iframe. This is disabled in case the dialog shows an iframe due to issues
                if (dialogType != "url" && base.options.customScrolling) {

                    dialog.classList.add("__customscroll");

                    base.scrollBar = new IScroll(dialogBody, {
                        scrollbars: true,
                        mouseWheel: true,
                        fadeScrollbars: true,
                        interactiveScrollbars: true,
                        resizeScrollbars: true,
                        click: base.options.enableDefaultClick,
                        preventDefaultException: { tagName: /^(INPUT|TEXTAREA|BUTTON|SELECT|LABEL)$/i } // adds support for label elements and makes exceptions case-insensitive
                    });

                    // Shows the scrollbars when mouse pointer in vicinity -- todo: this should be made generic (it is a missing functionality of IScroll :( 

                    base.rects = base.scrollBar.indicators[0].wrapper.getClientRects()[0];
                    $(document).on('mousemove.__dialog', function (e) {
                        if (e.clientX > base.rects.left - 20 &&
                            e.clientX < base.rects.right + 20 &&
                            e.clientY > base.rects.top - 20 &&
                            e.clientY < base.rects.bottom + 20)
                            $(base.scrollBar.indicators[0].wrapper).addClass("show");
                        else
                            $(base.scrollBar.indicators[0].wrapper).removeClass("show");
                    });

                    $(window).on("resize.__dialog", function () {
                        base.rects = base.scrollBar.indicators[0].wrapper.getClientRects()[0];
                    });
                }

            }, 100);

            // create printbutton
            if (base.options.enablePrint) {
                var printButton = document.createElement("div"); // close button
                printButton.className = "__printbutton";
                printButton.style.width = base.options.width;
                if (base.options.maxWidth)
                    printButton.style.maxWidth = base.options.maxWidth;
                printButton.innerHTML = "<div class='__floatbutton'><i class='material-icons'>print</i></div>";

                $(printButton).click(function () {
                    window.print();
                });

                dialog.insertBefore(printButton, dialog.firstChild);
            }

            // create closebutton
            if (base.options.closeButton) {
                var closeButton = document.createElement("div"); // close button
                closeButton.className = "__closebutton";
                closeButton.style.width = base.options.width;
                if (base.options.maxWidth)
                    closeButton.style.maxWidth = base.options.maxWidth;
                closeButton.innerHTML = "<div class='__floatbutton'><i class='material-icons'>close</i></div>";

                $(closeButton).click(function () {
                    $(el).data("dialog").close();
                });

                dialog.insertBefore(closeButton, dialog.firstChild);
            }

            // bind temporary events
            if (base.options.disableKeyboard) {
                $(applicationView).on("keydown.__dialog", function (e) { // prevent application view keyboard events
                    e.preventDefault();
                    return;
                });
            }
            if (base.options.smartClose) {
                $(dialog).on("click.__dialog", ".__dialog-cancel", function (e) {
                    e.preventDefault();
                    $(el).data("dialog").close();
                });
            }

            // closing the dialog
            if (base.options.closeable) {
                $(applicationView).on("click.__dialog", function (e) {
                    $(el).data("dialog").close();
                });
            }

            // ready
            if (base.options.closeButton)
                $(closeButton).addClass("__show");

            if (base.options.enablePrint)
                $(printButton).addClass("__show");

            $(dialog).trigger("dialogcreated", base.options.identifier);
        }

        // Run initializer
        base.init();
    }

    function dialogOpen() {
        if ($("body").hasClass("__dialogopen"))
            return true;
        return false;
    }

    $.dialog.defaultOptions = {
        source: "main.aspx", // todo: what is this and why?
        selector: null, // jQuery-supported selector
        object: null, // html element
        url: "", // will be loaded into an iframe
        mc2url: "", // url to local application
        width: "640px",
        minWidth: "100px",
        maxWidth: "640px",
        height: "480px",
        minHeight: "100px",
        maxHeight: "480px",
        run: "", // code to run, returned htmlElement will be inserted in dialog
        preprocess: function (data) { return data; }, // function to preprocess data loaded via mc2url
        onClose: function () { }, // callback for closing
        closeButton: true, // upper right corner close button
        closeable: true, // esc-key and background click closing
        targetContainer: "", // locates the dialog into a specified element, BETA
        blockScreen: true, // will the background be blocked and shaded
        eventType: "click", // event to trigger the dialog from the target element
        hideElementsOnClose: true, // if opened via selector and move, will the contents be hidden again after closing. Useful if the data shown was already visible
        duplicateSourceElement: false, // duplicates the selector instead of moving
        disableKeyboard: true, // disables keyboard events on app view while dialog open
        enablePrint: false, // shows print button
        position: "", // top, center(default), right, bottom or left side
        smartClose: true, // any child element with class "__cancel" will close the dialog
        identifier: null, // Identifier that will be passed to any triggered events
        customScrolling: false, // Uses the iscroll plugin to handle scrollbars, disable if performance problems occurs. This is always disabled for iframe dialogs
        enableDefaultClick: false, // useful for enabling links etc on dialogs with custom scrolling
        skipDelay: true, // shows and hides dialog and blockscreen without delay. To be used in case no animations are defined that require special timing
        customClass: false // give the element a custom class for styling or behavior
    };

    $.fn.dialog = function (options) {
        return this.each(function () {
            (new $.dialog(this, options));
        });
    };

})(jQuery);
/**
 * Single-page logic for timesheetentry, absence and expense(dayentry) types
 */
$.widget("entryformhelperwidget.entryformhelperwidget", $.core.base, {
    options: {
    },

    _create: function () {
        this._uiState = new Object;
        this._model = new Object();

        this._uiState.firstRender = true;
    },
    _initWidget: function () {
        if (this.options.viewtype == "display") {
        	this.render();
        }

    },
    refreshData: function (renderData) {
    },
    _setupWidgetEvents: function () {
    },
    /**
    * Render data in the model and settings into view.
    * 
    * @note: should not make any changes into model or settings
    */
    render: function () {
        this._renderApproveButton();
        this._uiState.firstRender = false;
    },
    _renderApproveButton: function () {
        // Don't show approve button for edit
        if (this.options.viewtype !== "display")
            return

        // Only render approve button once
        if (!this._uiState.firstRender)
            return

        // Don't render approve button for workers or assistant HR
        let level = parseInt($("body").data("userlevel"))
        if (level < 3 || level > 5)
            return

        var approvedByWorkerSpan = $("td[data-propertyschemaname='approvedbyworker']").children().first();
        var approvedByManagerSpan = $("td[data-propertyschemaname='approvedbymanager']").children().first();
        var approvedByWorker = approvedByWorkerSpan.attr("data-boolvalue")
        var approvedByManager = approvedByManagerSpan.attr("data-boolvalue")

        // Don't render approve button if already approved or if worker hasn't approved yet.
        if (approvedByWorker == "false" || approvedByManager == "true")
            return

        // Create approve button by using existing copy button from default form
        var copyButton = $(".__copybutton");
        var approveButton = copyButton.clone();
        var language = $("body").data("language");
        approveButton.text(window.translations.tro[language].approve)
        approveButton.attr("herf", "");
        approveButton.click(function (event) {
            event.preventDefault()
            message = window.translations.entryformhelperwidget[language].confirm_approve_single_entry
            if (!confirm(message)) 
                return false;

            // Get the URL of regular approve function
            var url = getAjaxUrl("tro", "approveworkmanager");

            // Specify action type to approve
            url = addParameterToUrl(url, "actiontype", "approve");

            // Create FormData object to mimic the usual approve form
            var formData = new FormData();

            // Form fields must start with __listitem_ to be processed as approved entry ids.
            // The id we want is in querystring id parameter
            formData.append("__listitem_", getParameterByName("id"));
            console.warn("id", getParameterByName("id"))
            console.warn("url", url)

            $.ajax({
                type: "POST",
                url: url,
                processData: false,
                contentType: false,
                data: formData,
                // Reload page after success.
                success: function () { location.reload(); },
            })
        })

        // Insert approve button after copy button
        copyButton.after(approveButton);
    }
});
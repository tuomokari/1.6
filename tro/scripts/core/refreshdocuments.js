$.widget("admin.refreshdocuments", {
    options: {
    },

    /**
     * Fired by jQuery when the widget is created.
     */
    _create: function () {

        this.model = new Object();

        this.uistate = new Object();

        this._on(this.element.find("[name='refreshbutton']"), {
            click: "_refreshDisplayNames"
        });
    },

    /**
     * Refresh all data in the model from the server.
     * 
     * @param (bool) renderData True if data should be rendered after refreshing it.
     * 
     * @note: should not do any dom manipulation
     */
    refreshData: function (renderData) {
    },

    /**
     * Render data in the model and settings into view.
     * 
     * @note: should not make any changes into modle or settingns
     */
    render: function () {

        if (this.uistate.lava)
            this.element.find("#lavalamp").removeClass("__hidden");
        else
            this.element.find("#lavalamp").addClass("__hidden");

        if (!this.model.dirty)
            return;

        var results = this.model.results;
        var resultStrArray = [];

        resultStrArray.push("Documents processed: " + results.documentsprocessed.length + " \n");

        for (i = 0; i < results.documentsprocessed.length; i++)
        {
            resultStrArray.push(results.documentsprocessed[i].id + "\n");
        }

        resultStrArray.push("Documents failed: " + results.documentsfailed.length + "\n");

        for (i = 0; i < results.documentsfailed.length; i++) {
            resultStrArray.push(results.documentsfailed[i].id + "\n");
        }

        this.element.find("[name='results']").val(resultStrArray.join(""));

        this.model.dirty = false;
    },
    _refreshDisplayNames: function () {

        var collection = this.element.find("[name='collectiondropdown']").val();
        var skip = this.element.find("[name='skip']").val();
        var limit = this.element.find("[name='limit']").val();

        var url = getAjaxUrl("admin", "refreshdisplaynames");
        url = addParameterToUrl(url, "collection", collection);
        url = addParameterToUrl(url, "skip", skip);
        url = addParameterToUrl(url, "limit", limit);

        var base = this;
        $.ajax({
            dataType: "JSON",
            url: url,
            async: true,
            type: "GET",
            success: function (data, status, xhr) {

                base.model.results = data;
                base.model.dirty = true;

                base.uistate.lava = false;
                base.render();
            }
        });

        this.uistate.lava = true;
        this.render();
    }
});
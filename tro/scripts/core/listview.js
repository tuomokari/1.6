function showListView(listViewWrapper) {
    var elemId = listViewWrapper.attr("id");

    var controller = listViewWrapper.attr("controller");
    var collection = listViewWrapper.attr("collection");

    var orderBy = getAttrOrDefault(listViewWrapper, "orderby", "");
    var ascending = getAttrOrDefault(listViewWrapper, "ascending", "");
    var documentsperpage = getAttrOrDefault(listViewWrapper, "documentsperpage", "");
    var page = getAttrOrDefault(listViewWrapper, "page", "");
    var relation = getAttrOrDefault(listViewWrapper, "relation", "");
    var relationid = getAttrOrDefault(listViewWrapper, "relationid", "");
    var islocalrelation = getAttrOrDefault(listViewWrapper, "islocalrelation", "true");
    var localcollection = getAttrOrDefault(listViewWrapper, "localcollection", "");
    var listviewcontroller = getAttrOrDefault(listViewWrapper, "listviewcontroller", "listview");
    var listviewaction = getAttrOrDefault(listViewWrapper, "listviewaction", "showlistview");
    var extraparams = getAttrOrDefault(listViewWrapper, "extraparams", "");
    var itemSelection = getAttrOrDefault(listViewWrapper, "data-itemselection", false);
    var terms = getAttrOrDefault(listViewWrapper, "data-terms", "");
    var controller = getAttrOrDefault(listViewWrapper, "data-controller", "listview");
    var action = getAttrOrDefault(listViewWrapper, "data-action", "listviewresults");
    var async = getAttrOrDefault(listViewWrapper, "data-async", "true");
    var allowremove = getAttrOrDefault(listViewWrapper, "data-allowremove", "true");
    var includeTotals = getAttrOrDefault(listViewWrapper, "data-includetotals", "false");

    if (allowremove == "true")
        itemSelection = true

    var data = "collection=" + collection + "&orderby=" + orderBy + "&ascending=" + ascending +
        "&documentsperpage=" + documentsperpage + "&page=" + page + "&relation=" + relation + "&relationid=" + relationid +
        "&islocalrelation=" + islocalrelation + "&localcollection=" + localcollection + "&viewcontroller=" + controller +
        "&viewaction=" + action + "&itemselection=" + itemSelection + "&includetotals=" + includeTotals + extraparams;

    if (terms !== "")
        data += "&terms=" + encodeURIComponent(terms);

    var ajaxUrl = getAjaxUrl(listviewcontroller, listviewaction);

    var listViewWrapper = $("#" + elemId);

    listViewWrapper.trigger("listviewupdating", [listViewWrapper]);

    searchfilter_request = $.ajax({
        dataType: "html",
        url: ajaxUrl,
        data: data,
        cache: false,
        async: "true",
        success: function (data, status, xhr) {
            if (!verifyLoginStatus(data)) return;

            data = processListviewFields(data);

            listViewWrapper.html(data);

            listViewWrapper.removeClass("__listview-unload");

            setListviewEvents(listViewWrapper);

            listViewWrapper.find("div").first().listview();

            listViewWrapper.trigger("listviewshown", [listViewWrapper]);
        }
    });
}

function setSortClasses(listViewWrapper) {
    listViewWrapper.find(".__listviewheadercell").each(function () {
        var orderBy = listViewWrapper.attr("orderby");

        if (orderBy === undefined)
            return;

        var ascending = listViewWrapper.attr("ascending");

        if (ascending !== "true")
            ascending = "false";

        var headerSchemaName = $(this).attr("data-propertyschema");

        if (headerSchemaName == orderBy) {
            if (ascending == "true")
                $(this).addClass("__sortasc");
            else
                $(this).addClass("__sortdesc");

            $(this).attr("ascending", ascending);
        }
        else {
            $(this).removeClass("__sortasc");
            $(this).removeClass("__sortdesc");
        }
    });
}

function setorderBy(listViewWrapper) {
    listViewWrapper.find(".__sorted").removeClass("__sorted");
    listViewWrapper.find(".__listviewheadercell").each(function () {
        var orderBy = listViewWrapper.attr("orderby");

        if (orderBy === undefined)
            return;

        var headerSchemaName = $(this).attr("data-propertyschema");

        if (headerSchemaName == orderBy) {

            var columnIndex = $(this).index();

            listViewWrapper.find("tr").each(
                function () {
                    $(this).find("td").eq(columnIndex).addClass("__sorted");
                });
        }
    });
}

function setListviewEvents(listViewWrapper) {

    setSortClasses(listViewWrapper);

    // Sort
    listViewWrapper.find(".__listviewheadercell").off().on("click", function () {
        listViewWrapper.attr("orderby", $(this).attr("data-propertyschema"));

        ascending = $(this).attr("ascending");

        if (ascending == "true")
            ascending = "false";
        else
            ascending = "true";

        $(this).attr("ascending", ascending);
        listViewWrapper.attr("ascending", ascending);
        showListView(listViewWrapper);
    });

    setorderBy(listViewWrapper);

    // Search
    var searchButton = document.getElementById(listViewWrapper.attr("id") + "__listviewsearch_button");
    $(searchButton).off().on("click", function (e) {

        e.preventDefault();

        var terms = document.getElementById(listViewWrapper.attr("id") + "__listviewsearch");
        listViewWrapper.attr("data-terms", terms.value);

        // Reset page to zero when filtering data.
        listViewWrapper.attr("page", 0);

        showListView(listViewWrapper);
    });


    var searchField = document.getElementById(listViewWrapper.attr("id") + "__listviewsearch");
    $(searchField).off().on("keydown", function (e) {

        if (e.which != 13)
            return;

        e.preventDefault();

        listViewWrapper.attr("data-terms", this.value);
        showListView(listViewWrapper);
    });

}

function getAttrOrDefault(element, attrName, defaultValue) {
    attrValue = element.attr(attrName);

    if (attrValue === undefined)
        attrValue = element.data(attrName);

    if (attrValue === undefined)
        return defaultValue;
    else
        return attrValue;
}

$(function () {
    setupListView();

    function setupListView() {

        var toggleDuration = 100;

        $(".__listviewtoolstoggler").each(function () {
            var tools = $(this).next();
            tools.find(".__listviewtools_toggleallcolumns>input").on("click", function (e) {
                tools.nextUntil("__listview-wrapper").toggleClass("__showallcolumns"); //todo: make sure this always finds the element, connect better
            });

            $(this).on("click", function () {
                $(this).toggleClass("show");
                tools.toggleClass("__active").next().toggleClass("__active").find("input:first").each(function () {
                    if (tools.hasClass("__active"))
                        $(this).focus();
                });
            });
        });

        $("div.__listview-wrapper").each(function () {

            var listViewWrapper = $(this);

            var elemId = listViewWrapper.attr("id");

            if ($(this).data("noinitialupdate") != true) {
                showListView(listViewWrapper);
            }

        });
    }
});

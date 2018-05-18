function toggleMenu(hideBlockScreen) {

    if (typeof hideBlockScreen === "undefined")
        hideBlockScreen = true;

    if ($("body").hasClass("__menuopen")) {
        if (hideBlockScreen) {
            $("body").removeClass("__blockscreen");
            $("#__applicationview").off(".__menuopen");
        }
        $("body").removeClass("__menuopen");
        $("html").data("menuopen", "");
        releaseScrolling();
    }
    else {
        lockScrolling();
        $("html").data("menuopen", "true");
        $("body").addClass("__menuopen");

        $("#__applicationview").on("click.__menuopen swiperight.__menuopen", function (e) {
            toggleMenu();
        });
    }
}
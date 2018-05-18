$.fn.getSize = function () {
    var $wrap = $("<div />").appendTo($("body"));
    $wrap.css({
        "position": "absolute !important",
        "visibility": "hidden !important",
        "display": "block !important"
    });

    $clone = $(this).clone().css({
        height: "auto",
        width: "auto",
        overflow: "visible"
    }).appendTo($wrap);


    sizes = {
        "width": $clone.width(),
        "height": $clone.height()
    };

    $wrap.remove();

    return sizes;
};
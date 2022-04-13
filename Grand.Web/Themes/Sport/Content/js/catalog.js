function MobileSibeBar() {
    var LeftSide = $(".generalLeftSide.SideBar");
    var RightSide = $(".generalSideRight");
    var SideBar = $(".SideBarMobile");
    var SideBarMobile = $(".SideBarMobile .generalLeftSide.SideBar");
    if (window.matchMedia('(max-width: 991px)').matches) {
        if (!$(SideBarMobile).length > 0) {
            $(LeftSide).prependTo(SideBar);
        }
    } else {
        if ($(SideBarMobile).length > 0) {
            $(SideBarMobile).insertBefore(RightSide);
        }
    }
}

function sortContainers() {
    var so = $(".sort-options");
    var sp = $(".sort-size");
    var LeftSide = $(".generalLeftSide");
    var stats = $("#items_statistics");
    if (window.matchMedia('(max-width: 768px)').matches) {
        $(so).prependTo(LeftSide);
        $(sp).prependTo(LeftSide);
    } else {
        $(so).insertBefore(stats);
        $(sp).insertBefore(stats);
    }
}

$(document).ready(function () {
    MobileSibeBar();
    sortContainers();

    $(window).resize(function () {
        MobileSibeBar();
        sortContainers();
    });
});
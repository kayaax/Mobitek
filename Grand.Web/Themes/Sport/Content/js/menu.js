// main menu system

function mainMenuReplace() {
    if (window.matchMedia('(max-width: 991px)').matches) {
        $('.menu-container .main-menu').addClass("navbar-nav").prependTo('#mobile_menu .navbar-collapse');
        Popper.Defaults.modifiers.computeStyle.enabled = false;
        $("#mobile_menu .nav-item.dropdown .dropdown-toggle").each(function () {
            $(this).on("click", function (e) {
                e.preventDefault();
                $(this).parent().addClass("show");
                $(this).parent().find(".dropdown-menu:first").addClass("show");
            });
        });
        $("#mobile_menu .nav-item.cat-back").each(function () {
            $(this).on("click", function (e) {
                e.preventDefault();
                $(this).parents(".dropdown:first").removeClass("show");
                $(this).parents(".dropdown-menu:first").removeClass("show");
            });
        });
    }
    else {
        $('#mobile_menu .navbar-collapse .main-menu').removeClass("navbar-nav").prependTo('.menu-container');
        Popper.Defaults.modifiers.computeStyle.enabled = true;
    }
}

function mainMenuDesktop() {
    if (window.matchMedia("(min-width: 992px)").matches) {

        var menu = $('.main-menu');
        var gallery = $('.main-menu .gallery');
        var nogallery = $('.main-menu .no-gallery.first-level');
        var other_top_links = $('.main-menu .other-top-links');
        var other_links_container = $('.header-bottom .other-links-container');

        $(other_top_links).prependTo(other_links_container);

        $(gallery).each(function () {
            var gallerypos = $("header").height();
            $(this).css('top', + gallerypos + "px");
        });

        $(nogallery).each(function () {
            var noGalleryLeft = $(this).parent().position().left - 15;
            var noGalleryTop = $("header").height();
            $(this).css('top', + noGalleryTop + "px");
            $(this).css('left', + noGalleryLeft + "px");
        });

        $('.main-menu .nav-item.dropdown > .dropdown-menu').each(function () {
            if ($(this).attr("data-promo") === "true") {
                if ($(this).data('order')) {
                    order = $(this).attr('data-order');
                } else {
                    order = "0";
                }
                if ($(this).data('promotop')) {
                    promoTop = $(this).attr("data-promotop");
                } else {
                    promoTop = "";
                }
                if ($(this).data('promomiddle')) {
                    promoMiddle = $(this).attr("data-promomiddle");
                } else {
                    promoMiddle = "";
                }
                if ($(this).data('promobottom')) {
                    promoBottom = $(this).attr("data-promobottom");
                } else {
                    promoBottom = "";
                }
                if ($(this).data('promobutton')) {
                    button = $(this).attr("data-promobutton");
                } else {
                    button = "SHOP NOW";
                }
                var url = $(this).attr('data-url');
                var picture = $(this).attr("data-picture");

                $(this).append("<li style='order:" + order + "' class='nav-item promo'><div class='promo-top'>" + promoTop + "</div><div class='promo-middle'>" + promoMiddle + "</div><div class='promo-bottom'>" + promoBottom + "</div><a class='btn-promo' href='" + url + "'>" + button + "</a><div class='picture' style='background-image:url(" + picture + ")'></div></li>");
            }
            $(".nav-item:not(.promo)", this).each(function () {
                var order = $(this).index();
                $(this).css('order', order);
            });
        });

        $(".main-menu > .dropdown").mouseenter(function () {
            $(this).addClass('show');
            $('.dropdown-menu:first', this).addClass('show');
            $(".backdrop-menu").addClass("show");
            //$('> .nav-link', this).attr('style', 'color:#fff;background:#292929;');
        });
        $(".main-menu > .dropdown").mouseleave(function () {
            $(this).removeClass('show');
            $('.dropdown-menu:first', this).removeClass('show');
            $('> .nav-link', this).removeAttr('style');
            $(".backdrop-menu").removeClass("show");
        });
    }
}

function MenuCatLinks() {
    if (window.matchMedia("(min-width: 992px)").matches) {
        $(".main-menu .nav-link").on("click", function () {
            var CatLink = $(this).attr("href");
            var backdrop = $(".backdrop-menu.show");
            $(".main-menu").find('.dropdown-menu').hide();
            $(".main-menu").find('.show').removeClass("show");
            $(backdrop).addClass("loading");
            window.location.href = CatLink;
        });
    }
}

$(document).ready(function () {

    mainMenuDesktop();
    mainMenuReplace();
    MenuCatLinks();

    $(window).resize(function () {
        mainMenuReplace();
    });
});
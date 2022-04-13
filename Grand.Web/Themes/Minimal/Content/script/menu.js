(function () {
    function $(selector, context) {
        context = context || document;
        return context["querySelectorAll"](selector);
    }

    function forEach(collection, iterator) {
        for (var key in Object.keys(collection)) {
            iterator(collection[key]);
        }
    }

    function showMenu(menu) {
        var menu = this;
        var ul = $("ul", menu)[0];

        if (!ul || ul.classList.contains("-visible")) return;

        menu.classList.add("-active");
        menu.classList.add("-last")
        ul.classList.add("-animating");
        ul.classList.add("-visible");
        setTimeout(function () {
            ul.classList.remove("-animating")
        }, 25);

        forEach(
            $("li.-hasSubmenu.-active", menu.parentElement.parentElement),
            function () {
                menu.parentElement.parentElement.classList.remove('-last');
            }
        );
    }

    function hideMenu(menu) {
        var menu = this;
        var ul = $("ul", menu)[0];

        if (!ul || !ul.classList.contains("-visible")) return;

        menu.classList.remove("-active");
        ul.classList.add("-animating");
        setTimeout(function () {
            ul.classList.remove("-visible");
            ul.classList.remove("-animating");
        }, 300);
    }

    function hideMenuMobile(menu) {
        var menu = this.parentElement.parentElement;
        var ul = $("ul", menu)[0];

        if (!ul || !ul.classList.contains("-visible")) return;

        menu.classList.remove("-active");
        ul.classList.add("-animating");
        setTimeout(function () {
            ul.classList.remove("-visible");
            ul.classList.remove("-animating");
        }, 300);
        menu.classList.remove('-last');
        menu.parentElement.parentElement.classList.add('-last');
    }

    function hideAllInactiveMenus(menu) {
        var menu = this;
        forEach(
            $("li.-hasSubmenu.-active:not(:hover)", menu.parent),
            function (e) {
                e.hideMenu && e.hideMenu();
            }
        );
    }

    function menuLoad() {
        if (991 < window.innerWidth) {
            forEach($(".Menu > ul li.-hasSubmenu"), function (e) {
                e.addEventListener("mouseenter", showMenu);
            });
            forEach($(".Menu > ul li"), function (e) {
                e.addEventListener("mouseleave", hideMenu);
            });
        } else {
            forEach($(".Menu > ul li.-hasSubmenu"), function (e) {
                e.addEventListener("click", showMenu);
            });
            forEach($(".Menu > ul li.-hasSubmenu > ul > .back"), function (e) {
                e.addEventListener("click", hideMenuMobile);
            });
        }
    }
    document.addEventListener("DOMContentLoaded", function () {
        window.addEventListener("resize", function () {
            menuLoad();
        });
    });

    window.addEventListener("load", function () {
        forEach($(".Menu > ul > li.-hasSubmenu"), function (e) {
            e.showMenu = showMenu;
            e.hideMenu = hideMenu;
        });
        setTimeout(function () {
            menuLoad();
        }, 300);
        document.addEventListener("click", hideAllInactiveMenus);
    });
})();

function stickyBox() {
    if (window.matchMedia('(min-width: 992px)').matches) {
        if ($(".product-details-page .overview").attr("data-sticky") == "true") {

            var boxh = $(".product-details-page .overview .product-simple-share").last().height();
            var ctoppos = $(".product-details-page .overview .product-simple-share").last().offset().top;
            var cbottompos = ctoppos + boxh;
            var galleryImg = $(".gallery .zoom img").attr("src");

            var boxt = $(".product-details-page.sticky-gallery .gallery").offset().top;
            var overviewboxh = $(".product-details-page.sticky-gallery .overview").height();

            var galleryH = $(".product-details-page .gallery").height();

            $(".product-details-page .gallery").css("height", galleryH);

            if ($(".product-details-page.sticky-gallery").attr("data-sticky") == "true") {
                $(window).scroll(function () {
                    if ($(window).scrollTop() > cbottompos) {
                        $(".product-details-page .overview").addClass("sticky").one();
                        $(".product-details-page .overview .sticky-img img").attr("src", galleryImg);
                        $(".product-details-page.sticky-gallery .overview-bottom").attr("style", "margin-top:" + overviewboxh + "px");
                    } else {
                        $(".product-details-page .overview").removeClass("sticky").one();
                        $(".product-details-page.sticky-gallery .overview-bottom").attr("style", "");
                    }
                    if ($(window).scrollTop() > boxt) {
                        $(".product-details-page.sticky-gallery .gallery").addClass("sticky").one();
                    } else {
                        $(".product-details-page.sticky-gallery .gallery").removeClass("sticky").one();
                    }
                });
            } else {
                $(window).scroll(function () {
                    if ($(window).scrollTop() > cbottompos) {
                        console.log(this);
                        $(".product-details-page .overview").addClass("sticky").one();
                        $(".product-details-page .overview .sticky-img img").attr("src", galleryImg);
                    } else {
                        $(".product-details-page .overview").removeClass("sticky").one();
                    }
                });
            }
            if ($(".product-details-page .gallery .gallery-top").length > 0) {
                var gallerySliderImg = $(".gallery .gallery-top .swiper-slide-active").attr("data-fullsize");
                $(".product-details-page .overview .sticky-img img").attr("src", gallerySliderImg);
            }
        }
    }
}
$(document).ready(function () {
    stickyBox();
});


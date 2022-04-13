function productInfo() {
    $('.product-box').each(function () {
        var PB_bottom_h = $('.product-info .bottom', this).height();
        $('.box-unvisible', this).css('margin-bottom', - PB_bottom_h);
    });
}

$(document).ready(function () {
    $(function () {
        $('.custom-pills li:first-child a').tab('show');
    });

    $('.custom-pills a[data-toggle="pill"]').on('shown.bs.tab', function (e) {
        productInfo();
    });

    $(".custom-pills li").each(function () {
        var indexof = $(this).index();
        if ($(".custom-tabs .tab-pane").eq(indexof).find("div").length < 1) {
            $(this).addClass("d-none");
        } else {
            $(this).removeClass("d-none");
        }
    });
    var Bestsellers = new Swiper('#Bestsellers', {
        speed: 400,
        autoplay: {
            delay: 5000,
        },
        loop: true,
        spaceBetween: 15,
        slidesPerView: 1,
        grabCursor: true,
        navigation: {
            nextEl: '#Bestsellers .swiper-circle-next',
            prevEl: '#Bestsellers .swiper-circle-prev',
        },
        pagination: {
            el: '#Bestsellers .swiper-pagination',
            type: 'bullets',
            clickable: true
        },
    });
});
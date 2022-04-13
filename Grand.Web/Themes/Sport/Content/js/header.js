function topBarSlider() {
    var TopBar = new Swiper('#TopBar', {
        slidesPerView: 1,
        loop: true,
        autoplay: {
            delay: 3000
        }
    });
}
$(document).ready(function () {
    topBarSlider();
});
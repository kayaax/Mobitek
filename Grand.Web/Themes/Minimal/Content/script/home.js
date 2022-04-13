var HomePageItems = Vue.extend({
    props: {
        HomePageItems: {
            type: Array,
            default: () => [
                {
                    HomePageCategories: {
                        items: [],
                        loading: true,
                    },
                    HomePageManufacturers: {
                        items: [],
                        loading: true,
                    },
                    HomePageProducts: {
                        items: [],
                        loading: true,
                        ProductsToShow: null,
                        NumberOfProducts: null,
                    },
                    CategoryFeaturedProducts: {
                        items: [],
                        loading: true,
                    },
                    ManufacturerFeaturedProducts: {
                        items: [],
                        loading: true,
                    },
                    HomePageBestSellers: {
                        items: [],
                        loading: true,
                        ProductsToShow: null,
                        NumberOfProducts: null,
                    },
                    RecommendedProducts: {
                        items: [],
                        loading: true,
                        ProductsToShow: null,
                        NumberOfProducts: null,
                    },
                    HomePageNewProducts: {
                        items: [],
                        loading: true,
                        ProductsToShow: null,
                        NumberOfProducts: null,
                    },
                    PersonalizedProducts: {
                        items: [],
                        loading: true,
                        ProductsToShow: null,
                        NumberOfProducts: null,
                    },
                    SuggestedProducts: {
                        items: [],
                        loading: true,
                        ProductsToShow: null,
                        NumberOfProducts: null,
                    },
                    HomePageNews: {
                        items: [],
                        loading: true,
                    },
                    HomePageBlog: {
                        items: [],
                        loading: true,
                    }
                }
            ]
        }
    },
    methods: {
        loadMore(event, NumberProducts, Products) {
            var SectionName = event.target.closest(".section");
            var LoadedProducts = SectionName.querySelectorAll(".product-box").length;
            if (LoadedProducts >= NumberProducts) {
                event.target.classList.add('disabled');
                event.target.innerText = event.target.getAttribute("data-nomore");
            } else {
                if ((NumberProducts - LoadedProducts) >= 4) {
                    Products.ProductsToShow = 4
                } else {
                    Products.ProductsToShow = Products.ProductsToShow + (NumberProducts - LoadedProducts);
                    event.target.classList.add('disabled');
                    event.target.innerText = event.target.getAttribute("data-nomore");
                }
            }
        },
    }
})
var hpi = new HomePageItems().$mount('#home-page');

function LoadStandard(section) {
    axios({
        baseURL: '/Component/Form?Name=' + section + '',
        method: 'get',
        data: null,
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json',
            'X-Response-View': 'Json'
        }
    }).then(response => {
        if (response.data != "") {
            hpi.HomePageItems[0][section].items = response.data;
        }
    }).then(function () {
        hpi.HomePageItems[0][section].loading = false;
    })   
}
function LoadMore(section) {
    axios({
        baseURL: '/Component/Form?Name=' + section + '',
        method: 'get',
        data: null,
        headers: {
            'Accept': 'asplication/json',
            'Content-Type': 'asplication/json',
            'X-Response-View': 'Json'
        }
    }).then(response => {
        if (response.data != "") {
            hpi.HomePageItems[0][section].items = response.data;
            hpi.HomePageItems[0][section].NumberOfProducts = response.data.length;
            if (response.data.length < 4) {
                hpi.HomePageItems[0][section].ProductsToShow = hpi.HomePageItems[0][section].NumberOfProducts;
            } else {
                hpi.HomePageItems[0][section].ProductsToShow = 4;
            }
        }
    }).then(function () {
        hpi.HomePageItems[0][section].loading = false;
    })
}

function LazyLoad() {
    var hp_section = document.querySelectorAll('.home-page-section');
    var viewLine = window.scrollY + window.innerHeight;
    for (var i = 0; i < hp_section.length; i++) {
        var section_loaded = hp_section[i].dataset.isloaded;
        if (hp_section[i].offsetTop - viewLine < 0 && section_loaded !== 'true') {
            var hpsectionid = hp_section[i].dataset.id;
            if (hp_section[i].dataset.load === 'standard') {
                LoadStandard(hpsectionid)
            } else {
                LoadMore(hpsectionid)
            }
            hp_section[i].dataset.isloaded = 'true';
        }
    }
}

window.onload = function () {
    LazyLoad();
};
document.addEventListener("DOMContentLoaded", function () {
    window.addEventListener('scroll', LazyLoad);
});
function deletecartitem(e) {
    return (
        axios({ method: "post", baseURL: "/deletecartitem/" + e })
            .then(function (e) {
                var t = e.data.flyoutshoppingcart,
                    n = JSON.parse(t);
                (this.flycart = n), (this.flycartitems = n.Items), (this.flycartindicator = n.TotalProducts), (vm.flycart = n), (vm.flycartitems = n.Items), (vm.flycartindicator = n.TotalProducts);
            })
            .catch(function (e) {
                alert(e);
            }),
        !1
    );
}
function displayPopupPrivacyPreference(e) {
    new Vue({
        el: "#ModalPrivacyPreference",
        data: { template: null },
        render: function (e) {
            return this.template ? this.template() : e("b-overlay", { attrs: { show: "true" } });
        },
        methods: {
            showModal() {
                this.$refs.ModalPrivacyPreference.show();
            },
        },
        mounted() {
            this.template = Vue.compile(e).render;
        },
        updated: function () {
            this.showModal();
        },
    });
}
function displayPopupAddToCart(e) {
    (document.querySelector(".modal-place").innerHTML = e),
        new Vue({
            el: "#ModalAddToCart",
            data: { template: null },
            render: function (e) {
                return this.template ? this.template() : e("b-overlay", { attrs: { show: "true" } });
            },
            methods: {
                showModal() {
                    this.$refs.ModalAddToCart.show();
                },
                onShown() {
                    runScripts(document.querySelector(".script-tag"));
                },
            },
            mounted() {
                this.template = Vue.compile(e).render;
            },
            updated: function () {
                this.showModal();
            },
        });
}
function displayPopupQuickView(html) {
    (document.querySelector(".modal-place").innerHTML = html),
        new Vue({
            el: "#ModalQuickView",
            data: { template: null, hover: !1, active: !1 },
            render: function (e) {
                return this.template ? this.template() : e("b-overlay", { attrs: { show: "true" } });
            },
            methods: {
                showModal() {
                    this.$refs.ModalQuickView.show();
                },
                onShown() {
                    runScripts(document.querySelector(".script-tag"));
                },
                productImage: function (e) {
                    var t = e.target.parentElement.getAttribute("data-href");
                    e.target.parentElement.parentElement.parentElement.parentElement.parentElement.parentElement.parentElement.parentElement.parentElement.querySelectorAll(".img-second")[0].setAttribute("src", t);
                },
                showModalBackInStock() {
                    this.$refs["back-in-stock"].show();
                },
                validateBeforeClick(event) {
                    this.$validator.validateAll().then((result) => {
                        if (result) {
                            var callFunction = event.srcElement.getAttribute("data-click");
                            eval(callFunction);
                        } else;
                    });
                },
                changeImg(e) {
                    var t = e.srcElement.getAttribute("data-src");
                    document.querySelector("#ModalQuickView .gallery .main-image").setAttribute("src", t);
                },
                isMobile: () => void 0 !== window.orientation || -1 !== navigator.userAgent.indexOf("IEMobile"),
            },
            mounted() {
                (this.template = Vue.compile(html).render),
                    this.$root.$on("bv::modal::show", (e, t) => {
                        "ModalQuickView" == t && null !== document.querySelector("#ModalQuickView___BV_modal_outer_") && document.querySelector("#ModalQuickView___BV_modal_outer_").remove();
                    });
            },
            updated: function () {
                this.showModal();
            },
        });
}
function displayBarNotification(e, t, n) {
    (toastHTML =
        "error" == t ? '<b-toast id="grandToast" auto-hide-delay=' + n + ' variant="danger" title=' + t + ">" + e + "</b-toast>" : '<b-toast id="grandToast" auto-hide-delay=' + n + ' variant="info" title=' + t + ">" + e + "</b-toast>"),
        (document.querySelector(".modal-place").innerHTML = toastHTML),
        new Vue({
            el: ".modal-place",
            methods: {
                toast() {
                    this.$bvToast.show("grandToast");
                },
            },
            mounted: function () {
                this.toast();
            },
        });
}
function addAntiForgeryToken(e) {
    e || (e = {});
    var t = document.querySelector("input[name=__RequestVerificationToken]");
    return t && (e.__RequestVerificationToken = t.value), e;
}
function newsletter_subscribe(e) {
    var t = document.getElementById("subscribe-loading-progress");
    t.style.display = "block";
    var n = { subscribe: e, email: document.getElementById("newsletter-email").value },
        o = document.getElementById("newsletterbox").getAttribute("data-href");
    axios({ url: o, params: n, method: "post" })
        .then(function (e) {
            (t.style.display = "none"),
                (document.querySelector("#newsletter-result-block .alert").innerHTML = e.data.Result),
                e.data.Success
                    ? ((document.querySelector(".newsletter-inputs .input-group").style.display = "none"),
                      document.querySelector(".newsletter-inputs .newsletter-subscribe-unsubscribe") && (document.querySelector(".newsletter-inputs .newsletter-subscribe-unsubscribe").style.display = "none"),
                      (document.querySelector("#newsletter-result-block").style.display = "block"),
                      (document.getElementById("newsletter-result-block").classList.add("success").style.bottom = "unset"),
                      data.response.Showcategories && ((document.getElementById("nc_modal_form").innerHTML = e.data.ResultCategory), window.setTimeout(function () {}, 100)))
                    : ((document.querySelector("#newsletter-result-block").style.display = "block"),
                      window.setTimeout(function () {
                          document.getElementById("newsletter-result-block").style.display = "none";
                      }, 2e3));
        })
        .catch(function (e) {
            t.style.display = "none";
        });
}
function newsletterBox() {
    document.getElementById("newsletter-subscribe-button") &&
        ((document.getElementById("newsletter-subscribe-button").onclick = function () {
            "true" == document.getElementById("newsletterbox").getAttribute("data-allowtounsubscribe").toLowerCase()
                ? document.getElementById("newsletter_subscribe").checked
                    ? newsletter_subscribe("true")
                    : newsletter_subscribe("false")
                : newsletter_subscribe("true");
        }),
        document.getElementById("newsletter-email").addEventListener("keyup", function (e) {
            13 == e.keyCode && document.getElementById("newsletter-subscribe-button").click();
        }));
}
function seq(e, t, n) {
    void 0 === n && (n = 0),
        e.length > 0 &&
            e[n](function () {
                ++n === e.length ? t() : seq(e, t, n);
            });
}
function scriptsDone() {
    var e = document.createEvent("Event");
    e.initEvent("DOMContentLoaded", !0, !0), document.dispatchEvent(e);
}
function insertScript(e, t) {
    var n = document.createElement("script");
    (n.type = "text/javascript"), e.src ? ((n.onload = t), (n.onerror = t), (n.src = e.src)) : (n.textContent = e.innerText), document.body.appendChild(n), e.parentNode.removeChild(e), e.src || t();
}
var runScriptTypes = [
    "application/javascript",
    "application/ecmascript",
    "application/x-ecmascript",
    "application/x-javascript",
    "text/ecmascript",
    "text/javascript",
    "text/javascript1.0",
    "text/javascript1.1",
    "text/javascript1.2",
    "text/javascript1.3",
    "text/javascript1.4",
    "text/javascript1.5",
    "text/jscript",
    "text/livescript",
    "text/x-ecmascript",
    "text/x-javascript",
];
function runScripts(e) {
    var t,
        n = e.querySelectorAll("script"),
        o = [];
    [].forEach.call(n, function (e) {
        ((t = e.getAttribute("type")) && -1 === runScriptTypes.indexOf(t)) ||
            o.push(function (t) {
                insertScript(e, t);
            });
    }),
        seq(o, scriptsDone);
}
function sendcontactusform(e) {
    var t;
    null == document.querySelector("#ModalQuickView")
        ? document.querySelector(".product-standard #product-details-form").checkValidity() &&
          ((t = new FormData()).append("AskQuestionEmail", document.querySelector(".product-standard #AskQuestionEmail").value),
          t.append("AskQuestionFullName", document.querySelector(".product-standard #AskQuestionFullName").value),
          t.append("AskQuestionPhone", document.querySelector(".product-standard #AskQuestionPhone").value),
          t.append("AskQuestionMessage", document.querySelector(".product-standard #AskQuestionMessage").value),
          t.append("Id", document.querySelector(".product-standard #AskQuestionProductId").value),
          t.append("__RequestVerificationToken", document.querySelector(".product-standard input[name=__RequestVerificationToken]").value),
          document.querySelector(".product-standard textarea[id^='g-recaptcha-response']") && t.append("g-recaptcha-response-value", document.querySelector(".product-standard textarea[id^='g-recaptcha-response']").value),
          axios({ url: e, data: t, method: "post", headers: { "Content-Type": "multipart/form-data" } })
              .then(function (e) {
                  e.data.success
                      ? ((document.querySelector(".product-standard #contact-us-product").style.display = "none"),
                        (document.querySelector(".product-standard .product-contact-error").style.display = "none"),
                        (document.querySelector(".product-standard .product-contact-send").innerHTML = e.data.message),
                        (document.querySelector(".product-standard .product-contact-send").style.display = "block"))
                      : ((document.querySelector(".product-standard .product-contact-error").innerHTML = e.data.message), (document.querySelector(".product-standard .product-contact-error").style.display = "block"));
              })
              .catch(function (e) {
                  alert(e);
              }))
        : document.querySelector("#ModalQuickView #product-details-form").checkValidity() &&
          ((t = new FormData()).append("AskQuestionEmail", document.querySelector("#ModalQuickView #AskQuestionEmail").value),
          t.append("AskQuestionFullName", document.querySelector("#ModalQuickView #AskQuestionFullName").value),
          t.append("AskQuestionPhone", document.querySelector("#ModalQuickView #AskQuestionPhone").value),
          t.append("AskQuestionMessage", document.querySelector("#ModalQuickView #AskQuestionMessage").value),
          t.append("Id", document.querySelector("#ModalQuickView #AskQuestionProductId").value),
          t.append("__RequestVerificationToken", document.querySelector("#ModalQuickView input[name=__RequestVerificationToken]").value),
          document.querySelector("#ModalQuickView textarea[id^='g-recaptcha-response']") && t.append("g-recaptcha-response-value", document.querySelector("#ModalQuickView textarea[id^='g-recaptcha-response']").value),
          axios({ url: e, data: t, method: "post", headers: { "Content-Type": "multipart/form-data" } })
              .then(function (e) {
                  e.data.success
                      ? ((document.querySelector("#ModalQuickView #contact-us-product").style.display = "none"),
                        (document.querySelector("#ModalQuickView .product-contact-error").style.display = "none"),
                        (document.querySelector("#ModalQuickView .product-contact-send").innerHTML = e.data.message),
                        (document.querySelector("#ModalQuickView .product-contact-send").style.display = "block"))
                      : ((document.querySelector("#ModalQuickView .product-contact-error").innerHTML = e.data.message), (document.querySelector("#ModalQuickView .product-contact-error").style.display = "block"));
              })
              .catch(function (e) {
                  alert(e);
              }));
}
function GetPrivacyPreference(e) {
    axios({ url: e, method: "get" })
        .then(function (e) {
            displayPopupPrivacyPreference(e.data.html);
        })
        .catch(function (e) {
            alert(e);
        });
}
function SavePrivacyPreference(e) {
    var t = document.querySelector("#frmPrivacyPreference"),
        n = new FormData(t);
    axios({ url: e, method: "post", data: n })
        .then(function (e) {})
        .catch(function (e) {
            alert(e);
        });
}
function newAddress(e) {
    e ? (this.resetSelectedAddress(), (document.getElementById("pickup-new-address-form").style.display = "block")) : (document.getElementById("pickup-new-address-form").style.display = "none");
}
function resetSelectedAddress() {
    var e = document.getElementById("pickup-address-select");
    e && (e.value = "");
}
function displayPopupNotification(e, t) {
    var n = "";
    "string" == typeof e
        ? ((n = '<b-modal ref="grandModal" id="grandModal" centered hide-footer hide-header><b-alert class="mb-0" show>' + e + "</b-alert></b-modal>"),
          (document.querySelector(".modal-place").innerHTML = n),
          new Vue({
              el: "#grandModal",
              data: { template: null, hover: !1 },
              render: function (e) {
                  return this.template ? this.template() : e("b-overlay", { attrs: { show: "true" } });
              },
              methods: {
                  showModal() {
                      this.$refs.grandModal.show();
                  },
              },
              mounted() {
                  this.template = Vue.compile(n).render;
              },
              updated: function () {
                  this.showModal();
              },
          }))
        : new Vue({
              el: "#app",
              methods: {
                  toast() {
                      for (var n = 0; n < e.length; n++) "error" == t ? this.$bvToast.toast(e[n], { title: t, variant: "danger", autoHideDelay: 5e3 }) : this.$bvToast.toast(e[n], { title: t, variant: "info", autoHideDelay: 5e3 });
                  },
              },
              mounted: function () {
                  this.toast();
              },
          });
}
function validation() {
    var e = document.querySelectorAll(".form-control[aria-invalid]");
    [].forEach.call(e, function (e) {
        new MutationObserver(function (t) {
            t.forEach(function (t) {
                if ("attributes" == t.type && "true" == e.getAttribute(t.attributeName))
                    if (e.classList.contains("is-invalid")) {
                        var n = e.getAttribute("data-val-required");
                        e.nextElementSibling.innerText = n;
                    } else if (((e.nextElementSibling.innerText = ""), e.getAttribute("data-val-length"))) {
                        var o = e.getAttribute("data-val-length");
                        e.nextElementSibling.innerText = o;
                    } else e.nextElementSibling.innerText = "";
            });
        }).observe(e, { attributes: !0 });
    });
}
function CloseSearchBox() {
    window.addEventListener("click", function () {
        document.getElementById("adv_search") && (document.getElementById("adv_search").style.display = "none");
    });
}
function StopPropagation(e) {
    e.stopPropagation();
}
function backToTop() {
    const e = document.createElement("div"),
        t = document.createElement("div");
    e.classList.add("up-btn", "up-btn__hide"),
        document.body.append(e),
        e.append(t),
        window.addEventListener("scroll", () => {
            !(function (t) {
                document.documentElement.scrollTop >= t ? e.classList.remove("up-btn__hide") : e.classList.add("up-btn__hide");
            })(400);
        }),
        e.addEventListener("click", () => {
            window.scrollTo({ top: 0, behavior: "smooth" });
        });
}
function headerscroll() {
    var e = document.getElementById("header-nav"),
        t = 3 * e.clientHeight,
        n = document.documentElement || document.body.parentNode || document.body,
        o = void 0 !== window.pageYOffset;
    window.pageYOffset && e.classList.add("sticky"),
        (window.onscroll = function (a) {
            (o ? window.pageYOffset : n.scrollTop) >= t ? e.classList.add("sticky", "animate__slideInDown") : e.classList.remove("sticky", "animate__slideInDown");
        });
}
function MenuOrientation() {
    var e = document.getElementById("mainMenu"),
        t = document.querySelector("#sidebar-menu .b-sidebar-body"),
        n = document.getElementById("header-container");
    991 > window.innerWidth ? t.appendChild(e) : n.appendChild(e);
}
window.addEventListener(
    "resize",
    function () {
        MenuOrientation();
    },
    !1
);
    document.addEventListener("DOMContentLoaded", function () {
        MenuOrientation(), headerscroll(), validation(), CloseSearchBox(), newsletterBox(), backToTop();
    });
/*
** single one page checkout
*/
var Checkout = {
    loadWaiting: false,
    failureUrl: false,
    init: function (failureUrl) {
        this.loadWaiting = false;
        this.failureUrl = failureUrl;
    },
    ajaxFailure: function () {
        alert('error');
        //location.href = Checkout.failureUrl;
    },

    setStepResponse: function (response) {
        if (response.data.update_section) {
            if (response.data.update_section.name == "billing") {
                vmorder.BillingAddress = true;
                vmorder.BillingExistingAddresses = response.data.update_section.model.ExistingAddresses;
                vmorder.BillingNewAddress = response.data.update_section.model.NewAddress;
                vmorder.BillingNewAddressPreselected = response.data.update_section.model.NewAddressPreselected;
                vmorder.BillingShipToSameAddress = response.data.update_section.model.ShipToSameAddress;
                vmorder.BillingShipToSameAddressAllowed = response.data.update_section.model.ShipToSameAddressAllowed;
                //document.querySelector('#checkout-' + response.data.update_section.name + '-load').innerHTML = response.data.update_section.html;
            }
            if (response.data.update_section.name == "shipping") {
                vmorder.ShippingAddress = true;
                vmorder.ShippingAllowPickUpInStore = response.data.update_section.model.AllowPickUpInStore;
                vmorder.ShippingExistingAddresses = response.data.update_section.model.ExistingAddresses;
                vmorder.ShippingNewAddress = response.data.update_section.model.NewAddress;
                vmorder.ShippingNewAddressPreselected = response.data.update_section.model.NewAddressPreselected;
                vmorder.ShippingPickUpInStore = response.data.update_section.model.PickUpInStore;
                vmorder.ShippingPickUpInStoreOnly = response.data.update_section.model.PickUpInStoreOnly;
                vmorder.ShippingPickupPoints = response.data.update_section.model.PickupPoints;
                vmorder.ShippingWarnings = response.data.update_section.model.Warnings;
            }
            if (response.data.update_section.name == "shipping-method") {
                vmorder.ShippingMethod = true;
                vmorder.NotifyCustomerAboutShippingFromMultipleLocations = response.data.update_section.model.NotifyCustomerAboutShippingFromMultipleLocations;
                vmorder.ShippingMethods = response.data.update_section.model.ShippingMethods;
                vmorder.ShippingMethodWarnings = response.data.update_section.model.Warnings;
                if (response.data.update_section.model.ShippingMethods.length > 0) {
                    var elem = response.data.update_section.model.ShippingMethods[0].Name + '___' + response.data.update_section.model.ShippingMethods[0].ShippingRateComputationMethodSystemName;
                    loadPartialView(elem);
                }
            }
            if (response.data.update_section.name == "payment-method") {
                vmorder.PaymentMethod = true
                vmorder.DisplayRewardPoints = response.data.update_section.model.DisplayRewardPoints;
                vmorder.PaymentMethods = response.data.update_section.model.PaymentMethods;
                vmorder.RewardPointsAmount = response.data.update_section.model.RewardPointsAmount;
                vmorder.RewardPointsBalance = response.data.update_section.model.RewardPointsBalance;
                vmorder.RewardPointsEnoughToPayForOrder = response.data.update_section.model.RewardPointsEnoughToPayForOrder;
                vmorder.UseRewardPoints = response.data.update_section.model.UseRewardPoints;

            }
            if (response.data.update_section.name == "payment-info") {
                vmorder.PaymentInfo = true;
                vmorder.DisplayOrderTotals = response.data.update_section.model.DisplayOrderTotals;
                vmorder.PaymentViewComponentName = response.data.update_section.model.PaymentViewComponentName;
                axios({
                    baseURL: '/Component/Index?Name=' + response.data.update_section.model.PaymentViewComponentName,
                    method: 'get',
                    data: null,
                    headers: {
                        'Accept': 'application/json',
                        'Content-Type': 'application/json',
                    }
                }).then(response => {
                    var html = response.data;
                    document.querySelector('.payment-info .info').innerHTML = html;
                }).then(function () {
                    if (document.querySelector('.script-tag-info')) {
                        runScripts(document.querySelector('.script-tag-info'))
                    }
                })
            }
            if (response.data.update_section.name == "confirm-order") {
                vmorder.Confirm = true;
                vmorder.MinOrderTotalWarning = response.data.update_section.model.MinOrderTotalWarning;
                vmorder.TermsOfServiceOnOrderConfirmPage = response.data.update_section.model.TermsOfServiceOnOrderConfirmPage;
                vmorder.ConfirmWarnings = response.data.update_section.model.Warnings;
                //document.querySelector('#checkout-' + response.data.update_section.name + '-load').innerHTML = response.data.update_section.html;
            }


        }
        if (response.data.goto_section == 'shipping_method') {
            ShippingMethod.selectShippingMethod();
        }
        if (response.data.goto_section == 'payment_method') {
            PaymentMethod.selectPaymentMethod();
            PaymentMethod.selectRewardPoints();
        }
        if (response.data.goto_section == 'confirm_order') {
            vmcheckout.checkout.ordersummary = "test";
            document.querySelector('#ordersummarypagecart .card').style.display = "d-none";
        }
        if (response.data.goto_section == 'shipping') {
            if (document.querySelector('#ShipToSameAddress').length == 0) {
                //
            }
        }
        this.updateOrderTotal();
        return false;
    },
    updateOrderTotal: function () {
        axios({
            baseURL: '/Component/Index?Name=OrderTotals',
            method: 'get',
            data: null,
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'X-Response-View': 'Json'
            }
        }).then(response => {
            vmorder.totals = response.data;
        });
    }
};

var Billing = {
    form: false,
    saveUrl: false,
    loadUrl: false,
    firstLoad: false,
    disableBillingAddressCheckoutStep: false,

    init: function (form, loadUrl, saveUrl, disableBillingAddressCheckoutStep) {
        this.form = form;
        this.loadUrl = loadUrl;
        this.saveUrl = saveUrl;
        this.disableBillingAddressCheckoutStep = disableBillingAddressCheckoutStep;
    },
    load: function (first) {
        this.firstLoad = first;
        axios({
            baseURL: this.loadUrl,
            method: 'get',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'X-Response-View': 'Json'
            }
        }).then(function (response) {
            Billing.reload(response);
        }).catch(function (error) {
            Checkout.ajaxFailure
        }).then(function () {
            setTimeout(function () {
                Billing.afterReload();
            }, 300)
            Billing.resetLoadWaiting
        });
    },
    newAddress: function (isNew) {
        if (isNew) {
            this.resetSelectedAddress();
            document.querySelector('#billing-new-address-form').style.display = "block";
            document.querySelector('#billing-buttons-container').style.display = "block";
        } else {
            this.saveSelectedAddress()
            document.querySelector('#billing-new-address-form').style.display = "none";
            document.querySelector('#billing-buttons-container').style.display = "none";
        }
    },
    resetSelectedAddress: function () {
        var selectElement = document.querySelector('#billing-address-select');
        if (selectElement) {
            selectElement.value = '';
        }
    },
    saveSelectedAddress: function () {
        var bodyFormData = new FormData(document.querySelector(this.form));
        axios({
            baseURL: this.saveUrl,
            method: 'post',
            data: bodyFormData,
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'X-Response-View': 'Json'
            }
        }).then(function (response) {
            Billing.selectnextStep(response)
        }).catch(function (error) {
            Checkout.ajaxFailure
        }).then(function () {
            Billing.resetLoadWaiting
        });
    },
    selectnextStep: function (response) {
        if (response.data.goto_section) {
            if (response.data.goto_section == 'shipping_method' ||
                response.data.goto_section == 'payment_method') {
                Checkout.setStepResponse(response);
            }
            if (response.data.goto_section == 'shipping') {
                //Checkout.setStepResponse(response);
            }
        }
    },
    save: function () {
        if (Checkout.loadWaiting != false) return;
        var bodyFormData = new FormData(document.querySelector(this.form));
        axios({
            baseURL: this.saveUrl,
            method: 'post',
            data: bodyFormData,
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'X-Response-View': 'Json'
            }
        }).then(function (response) {
            Billing.nextStep(response)
        }).catch(function (error) {
            Checkout.ajaxFailure
        }).then(function () {
            Billing.resetLoadWaiting
        });
    },

    shiptoSameAddress: function (element) {
        Shipping.setasbillingaddress();
        if (document.getElementById(element)) {
            if (document.getElementById(element).checked) {
                document.querySelector('#opc-shipping').style.display = "none";
            }
            else {
                document.querySelector('#opc-shipping').style.display = "block";
            }
        }
        else {
            document.querySelector('#opc-shipping').style.display = "block";
        }
    },
    resetLoadWaiting: function () {
    },

    reload: function (response) {
        Checkout.setStepResponse(response);
    },
    afterReload: function () {
        if (document.querySelector("#billing-address-select").length > 0) {
            document.querySelector('#billing-new-address-form').style.display = "none";
            if (Billing.firstLoad == false) {
                var size = document.querySelector("#billing-address-select").length;
                if (size > 2) {
                    document.querySelector("#billing-address-select").selectedIndex = size - 2;
                }
            }
        }
        else {
            document.querySelector('#billing-buttons-container').style.display = "block";
        }
        if (vmorder.BillingExistingAddresses.length < 1) {
            document.getElementById("billing-new-address-form").style.display = "block";
            document.getElementById("billing-buttons-container").style.display = "block";
            document.getElementById("billing-address-select").style.display = "none";
        } else {
            document.getElementById("billing-new-address-form").style.display = "none";
            document.getElementById("billing-address-select").style.display = "block";
        }
        var shipaddress = 'ShipToSameAddress';
        if (document.querySelector('#' + shipaddress)) {
            Billing.shiptoSameAddress(shipaddress);
            document.querySelector('#' + shipaddress).addEventListener('change', function () {
                Billing.shiptoSameAddress(shipaddress);
            });
        }
    },
    nextStep: function (response) {
        //ensure that response.wrong_billing_address is set
        //if not set, "true" is the default value
        if (typeof response.data.wrong_billing_address == 'undefined') {
            response.data.wrong_billing_address = false;
        }
        if (response.data.error) {

            if ((typeof response.data.message) == 'string') {
                alert(response.data.message);
            } else {
                alert(response.data.message.join("\n"));
            }

            return false;
        }
        if (response.data.goto_section) {
            if (response.data.goto_section == 'shipping' || response.data.goto_section == 'shipping_method' ||
                response.data.goto_section == 'payment_method') {
                Billing.load(false);
                Shipping.load(false);
                document.querySelector('#billing-buttons-container').style.display = "none";
            }
        }
        Checkout.setStepResponse(response);
        if (response.data.wrong_billing_address == false) {
            Shipping.resetShipping();
            Billing.load(false);
            document.querySelector('#billing-buttons-container').style.display = "none";
        }
    }
};

var Shipping = {
    form: false,
    saveUrl: false,
    loadUrl: false,
    firstLoad: false,

    init: function (form, loadUrl, saveUrl) {
        this.form = form;
        this.saveUrl = saveUrl;
        this.loadUrl = loadUrl;
    },

    load: function (first) {
        this.firstLoad = first;
        axios({
            baseURL: this.loadUrl,
            method: 'get',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'X-Response-View': 'Json'
            }
        }).then(function (response) {
            Shipping.reload(response);
        }).catch(function (error) {
            Checkout.ajaxFailure
        }).then(function () {
            Shipping.afterReload();
            Shipping.resetLoadWaiting
        });
    },
    reload: function (response) {
        Checkout.setStepResponse(response);
    },
    afterReload: function () {
        if (document.querySelector("#shipping-address-select").length > 0) {
            document.querySelector('#shipping-new-address-form').style.display = "none";
            if (Shipping.firstLoad == false) {
                var size = document.querySelector("#billing-address-select").length;
                document.querySelector("#shipping-address-select").selectedIndex = size - 2;
            }
        }
        else {
            document.querySelector('#shipping-buttons-container').style.display = "block";
        }
    },
    newAddress: function (isNew) {
        if (isNew) {
            this.resetSelectedAddress();
            document.querySelector('#shipping-new-address-form').style.display = "block";
            document.querySelector('#shipping-buttons-container').style.display = "block";
        } else {
            this.saveSelectedAddress()
            document.querySelector('#shipping-new-address-form').style.display = "none";
            document.querySelector('#shipping-buttons-container').style.display = "none";
        }
    },

    togglePickUpInStore: function (pickupInStoreInput) {
        this.saveSelectedAddress();
        if (pickupInStoreInput.checked) {
            document.querySelector('#shipping-addresses-form').style.display = "none";
            document.querySelector('#opc-shipping-method').style.display = "none";
            document.querySelector('#shipping-buttons-container').style.display = "none";
            document.querySelector('#pickup-points-form').style.display = "block";
            if (document.querySelector('#pickup-points-select')) {
                document.querySelector('#pickup-points-select').addEventListener('change', function () {
                    Shipping.saveSelectedAddress();
                });
            }
        }
        else {
            document.querySelector('#opc-shipping-method').style.display = "block";
            document.querySelector('#shipping-addresses-form').style.display = "block";
            document.querySelector('#pickup-points-form').style.display = "none";
            if (document.querySelector('.new-shipping-address').style.display == 'none' || document.querySelector('.new-shipping-address').style.visibility == "hidden") {
                document.querySelector('#shipping-buttons-container').style.display = "none";
            }
            else {
                document.querySelector('#shipping-addresses-form').style.display = "block";
            }
        }

    },

    resetSelectedAddress: function () {
        var selectElement = document.querySelector('#shipping-address-select');
        if (selectElement) {
            selectElement.value = "";
        }
    },
    setasbillingaddress: function () {
        if (document.querySelector("#shipping-address-select")) {
            var selectedIndex = document.querySelector("#billing-address-select").selectedIndex;
            document.querySelector("#shipping-address-select").selectedIndex = selectedIndex;
            Shipping.saveSelectedAddress();
        }
    },
    saveSelectedAddress: function () {
        var bodyFormData = new FormData(document.querySelector(this.form));
        axios({
            baseURL: this.saveUrl,
            method: 'post',
            data: bodyFormData,
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'X-Response-View': 'Json'
            }
        }).then(function (response) {
            Shipping.selectnextStep(response);
        }).catch(function (error) {
            Checkout.ajaxFailure
        }).then(function () {
            Shipping.resetLoadWaiting
        });
    },
    selectnextStep: function (response) {
        if (response.data.goto_section) {
            if (response.data.goto_section == 'shipping_method' ||
                response.data.goto_section == 'payment_method') {

                var rewardPointsIsChecked = false;
                if (document.querySelector('#UseRewardPoints')) {
                    if (document.getElementById('UseRewardPoints').checked == true) {
                        rewardPointsIsChecked = true;
                    }
                }
                Checkout.setStepResponse(response);

                if (document.querySelector('#UseRewardPoints') && rewardPointsIsChecked) {
                    document.querySelector('#UseRewardPoints').checked = true;
                }
                if (response.data.goto_section == 'payment_method') {
                    ConfirmOrder.load();
                }
            }
        }

    },
    save: function () {
        var bodyFormData = new FormData(document.querySelector(this.form));
        axios({
            baseURL: this.saveUrl,
            method: 'post',
            data: bodyFormData,
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'X-Response-View': 'Json'
            }
        }).then(function (response) {
            Shipping.nextStep(response)
        }).catch(function (error) {
            Checkout.ajaxFailure
        }).then(function () {
            Shipping.resetLoadWaiting
        });
    },

    resetLoadWaiting: function () {
    },

    resetShipping: function () {
        if (document.querySelector("#shipping-address-select")) {
            Shipping.newAddress(!document.querySelector('#shipping-address-select').value);
        }
    },

    nextStep: function (response) {
        if (response.data.error) {
            if ((typeof response.data.message) == 'string') {
                alert(response.data.message);
            } else {
                alert(response.data.message.join("\n"));
            }

            return false;
        }
        if (response.data.goto_section) {
            if (response.data.goto_section == 'shipping_method') {
                Shipping.load(false);
                document.querySelector('#shipping-buttons-container').style.display = "none";
            }
        }
        Checkout.setStepResponse(response);
        //Shipping.resetShipping();
    },

};

var ShippingMethod = {
    form: false,
    saveUrl: false,
    loadUrl: false,
    firstLoad: false,

    init: function (form, saveUrl, loadUrl) {
        this.form = form;
        this.saveUrl = saveUrl;
        this.loadUrl = loadUrl;
    },

    validate: function () {
        var methods = document.getElementsByName('shippingoption');
        if (methods.length == 0) {
            alert('Your order cannot be completed at this time as there is no shipping methods available for it. Please make necessary changes in your shipping address.');
            return false;
        }

        for (var i = 0; i < methods.length; i++) {
            if (methods[i].checked) {
                return true;
            }
        }
        alert('Please specify shipping method.');
        return false;
    },

    load: function (first) {
        this.firstLoad = first;
        document.querySelector('#opc-shipping-method').style.display = "block";
        axios({
            baseURL: this.loadUrl,
            method: 'get',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'X-Response-View': 'Json'
            }
        }).then(function (response) {
            ShippingMethod.reload(response);
        }).catch(function (error) {
            Checkout.ajaxFailure
        }).then(function () {
            ShippingMethod.resetLoadWaiting
        });
    },
    reload: function (response) {
        Checkout.setStepResponse(response);
    },

    selectShippingMethod: function () {

        this.saveSelected();
        document.querySelector('input[type=radio][name=shippingoption]').addEventListener('click',
            function () {
                ShippingMethod.saveSelected();
            }
        );
    },

    saveSelected: function () {
        var bodyFormData = new FormData(document.querySelector(this.form));
        axios({
            baseURL: this.saveUrl,
            method: 'post',
            data: bodyFormData,
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'X-Response-View': 'Json'
            }
        }).then(function (response) {
            ShippingMethod.selectnextStep(response);
        }).catch(function (error) {
            Checkout.ajaxFailure
        }).then(function () {
            Checkout.updateOrderTotal();
            ShippingMethod.resetLoadWaiting
        });
    },
    selectnextStep: function (response) {
        if (response.data.goto_section) {
            if (response.data.goto_section == 'payment_method') {
                var rewardPointsIsChecked = false;
                if (document.querySelector('#UseRewardPoints')) {
                    if (document.getElementById('UseRewardPoints').checked) {
                        rewardPointsIsChecked = true;
                    }
                }
                Checkout.setStepResponse(response);
                if (document.querySelector('#UseRewardPoints') && rewardPointsIsChecked) {
                    document.querySelector('#UseRewardPoints').checked = true;
                }
                ConfirmOrder.load();
            }
        }
    },
    save: function () {
        if (this.validate()) {
            var bodyFormData = new FormData(document.querySelector(this.form));
            axios({
                baseURL: this.saveUrl,
                method: 'post',
                data: bodyFormData,
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'X-Response-View': 'Json'
                }
            }).then(function (response) {
                ShippingMethod.nextStep(response)
            }).catch(function (error) {
                Checkout.ajaxFailure
            }).then(function () {
                ShippingMethod.resetLoadWaiting
            });
        }
    },

    resetLoadWaiting: function () {
    },

    nextStep: function (response) {
        if (response.data.error) {
            if ((typeof response.data.message) == 'string') {
                alert(response.data.message);
            } else {
                alert(response.data.message.join("\n"));
            }

            return false;
        }

        Checkout.setStepResponse(response);
    }
};

var PaymentMethod = {
    form: false,
    saveUrl: false,
    loadUrl: false,
    firstLoad: false,
    fullRewardPoints: false,

    init: function (form, saveUrl, loadUrl) {
        this.form = form;
        this.saveUrl = saveUrl;
        this.loadUrl = loadUrl;
    },
    toggleUseRewardPoints: function (useRewardPointsInput) {
        this.fullRewardPoints = true;
        PaymentMethod.save();
    },
    toggleUseRewardPointsChange: function (useRewardPointsInput) {
        this.saveRewardPoints();
    },
    saveRewardPoints: function () {
        var bodyFormData = new FormData(document.querySelector(this.form));
        axios({
            baseURL: this.saveUrl,
            method: 'get',
            data: bodyFormData,
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'X-Response-View': 'Json'
            }
        }).then(function (response) {
            ConfirmOrder.load(response)
        }).catch(function (error) {
            Checkout.ajaxFailure
        }).then(function () {
            PaymentMethod.selectPaymentMethod();
            PaymentMethod.resetLoadWaiting
        });
    },

    load: function (first) {
        this.firstLoad = first;
        document.querySelector('#opc-payment-method').style.display = "block";
        axios({
            baseURL: this.loadUrl,
            method: 'get',
            data: null,
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'X-Response-View': 'Json'
            }
        }).then(function (response) {
            PaymentMethod.reload(response);
        }).catch(function (error) {
            Checkout.ajaxFailure
        }).then(function () {
            PaymentMethod.resetLoadWaiting
            PaymentInfo.load(true);
        });

    },
    reload: function (response) {
        Checkout.setStepResponse(response);
        PaymentInfo.load(false);
    },
    selectPaymentMethod: function () {
        document.querySelectorAll('input[type=radio][name=paymentmethod]').forEach(function (element) {
            element.addEventListener('change', function (event) {
                PaymentInfo.load(false);
            })
        })
    },
    selectRewardPoints: function () {
        var rewardpoints = 'UseRewardPoints';
        if (document.querySelector('#' + rewardpoints)) {
            document.querySelector('#' + rewardpoints).addEventListener('change', function () {
                PaymentMethod.toggleUseRewardPointsChange(document.querySelector('#' + rewardpoints));
            });
        }
    },
    validate: function () {
        var methods = document.getElementsByName('paymentmethod');
        if (methods.length == 0) {
            alert('Your order cannot be completed at this time as there is no payment methods available for it.');
            return false;
        }

        for (var i = 0; i < methods.length; i++) {
            if (methods[i].checked) {
                return true;
            }
        }
        alert('Please specify payment method.');
        return false;
    },

    save: function () {
        if (this.validate()) {
            var bodyFormData = new FormData(document.querySelector(this.form));
            axios({
                baseURL: this.saveUrl,
                method: 'post',
                data: bodyFormData,
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'X-Response-View': 'Json'
                }
            }).then(function (response) {
                PaymentMethod.nextStep(response);
            }).catch(function (error) {
                Checkout.ajaxFailure
            }).then(function () {
                Checkout.updateOrderTotal();
                PaymentMethod.resetLoadWaiting
            });
        }
    },

    resetLoadWaiting: function () {
    },

    nextStep: function (response) {
        if (response.data.error) {
            if ((typeof response.data.message) == 'string') {
                alert(response.data.message);
            } else {
                alert(response.data.message.join("\n"));
            }

            return false;
        }
        Checkout.setStepResponse(response);
    }
};

var PaymentInfo = {
    form: false,
    saveUrl: false,
    firstLoad: false,

    init: function (form, saveUrl) {
        this.form = form;
        this.saveUrl = saveUrl;
    },

    load: function (first) {
        this.firstLoad = first;
        document.querySelector('#opc-payment-info').style.display = "block";
        var bodyFormData = new FormData(document.querySelector(PaymentMethod.form));
        axios({
            baseURL: PaymentMethod.saveUrl,
            method: 'post',
            data: bodyFormData,
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'X-Response-View': 'Json'
            }
        }).then(function (response) {
            PaymentInfo.reload(response);
        }).catch(function (error) {
            Checkout.ajaxFailure
        }).then(function () {
            PaymentInfo.resetLoadWaiting
        });
    },
    reload: function (response) {
        Checkout.setStepResponse(response);
        ConfirmOrder.load();
    },

    save: function () {
        if (document.querySelector('#billing-buttons-container').style.display == "block") {
            alert('Please save billing address');
            return false;
        }
        if (document.querySelector('#shipping-buttons-container').style.display == "block") {
            alert('Please save shipping address');
            return false;
        }
        if (PaymentMethod.fullRewardPoints == false || PaymentMethod.fullRewardPoints == undefined) {
            var bodyFormData = new FormData(document.querySelector(ShippingMethod.form));
            axios({
                baseURL: ShippingMethod.saveUrl,
                method: 'post',
                data: bodyFormData,
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'X-Response-View': 'Json'
                }
            }).then(function (response) {
                PaymentInfo.selectnextStepPaymentInfo(response)
            }).catch(function (error) {
                Checkout.ajaxFailure
            }).then(function () {
                PaymentInfo.resetLoadWaiting
            });
        }
        else {
            PaymentInfo.savePaymentInfo();
        }
    },
    selectnextStepPaymentInfo: function (response) {
        if (response.data.error) {
            alert(response.data.message);
        }
        else {
            var bodyFormData = new FormData(document.querySelector(PaymentInfo.form));
            axios({
                baseURL: PaymentInfo.saveUrl,
                method: 'post',
                data: bodyFormData,
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'X-Response-View': 'Json'
                }
            }).then(function (response) {
                PaymentInfo.nextStep(response);
            }).catch(function (error) {
                Checkout.ajaxFailure
            }).then(function () {
                PaymentInfo.resetLoadWaiting
            });
        }
    },
    savePaymentInfo: function () {
        var bodyFormData = new FormData(document.querySelector(PaymentInfo.form));
        axios({
            baseURL: PaymentInfo.saveUrl,
            method: 'post',
            data: bodyFormData,
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'X-Response-View': 'Json'
            }
        }).then(function () {
            PaymentInfo.gotoCheckout()
        }).catch(function (error) {
            Checkout.ajaxFailure
        }).then(function () {
            PaymentInfo.resetLoadWaiting
        });
    },
    gotoCheckout: function () {
        if (vmorder.MinOrderTotalWarning == null && vmorder.TermsOfServiceOnOrderConfirmPage) {
            document.getElementById("hidden_confirm").click();
        } else {
            ConfirmOrder.save();
        }
    },
    nextStep: function (response) {

        if (response.data.error) {
            if ((typeof response.data.message) == 'string') {
                alert(response.data.message);
            } else {
                alert(response.data.message.join("\n"));
            }

            return false;
        }
        if (response.data.goto_section == 'confirm_order') {
            PaymentInfo.savePaymentInfo();
        }
        else
            Checkout.setStepResponse(response);
    }
};

var ConfirmOrder = {
    form: false,
    saveUrl: false,
    isSuccess: false,
    loadUrl: false,
    init: function (saveUrl, successUrl, loadUrl) {
        this.saveUrl = saveUrl;
        this.successUrl = successUrl;
        this.loadUrl = loadUrl;
    },
    load: function () {
        axios({
            baseURL: this.loadUrl,
            method: 'get',
            data: null,
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'X-Response-View': 'Json'
            }
        }).then(function (response) {
            ConfirmOrder.reload(response)
        }).catch(function (error) {
            Checkout.ajaxFailure
        }).then(function () {
            ConfirmOrder.resetLoadWaiting
        });
    },

    reload: function (response) {
        Checkout.setStepResponse(response);
    },

    save: function () {
        //terms of service
        var termOfServiceOk = true;

        if (termOfServiceOk) {
            axios({
                baseURL: this.saveUrl,
                method: 'post',
                data: null,
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'X-Response-View': 'Json'
                }
            }).then(function (response) {
                ConfirmOrder.nextStep(response);
            }).catch(function (error) {
                Checkout.ajaxFailure
            }).then(function () {
                ConfirmOrder.resetLoadWaiting
            });
        } else {
            return false;
        }
    },

    resetLoadWaiting: function (transport) {
        //Checkout.setLoadWaiting(false, ConfirmOrder.isSuccess);
    },

    nextStep: function (response) {
        if (response.data.error) {
            if ((typeof response.data.message) == 'string') {
                alert(response.data.message);
            } else {
                alert(response.data.message.join("\n"));
            }

            return false;
        }

        if (response.data.redirect) {
            ConfirmOrder.isSuccess = true;
            location.href = response.data.redirect;
            return;
        }
        if (response.data.success) {
            ConfirmOrder.isSuccess = true;
            window.location = ConfirmOrder.successUrl;
        }

        Checkout.setStepResponse(response);
    }
};
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
        if (response.update_section) {
            $('#checkout-' + response.update_section.name + '-load').html(response.update_section.html);
        }
        if (response.goto_section == 'shipping_method') {
            ShippingMethod.selectShippingMethod();
        }
        if (response.goto_section == 'payment_method') {
            PaymentMethod.selectPaymentMethod();
            PaymentMethod.selectRewardPoints();
        }
        if (response.goto_section == 'confirm_order') {
            $('#ordersummarypagecart .card').hide();
        }
        if (response.goto_section == 'shipping') {
            if ($('#ShipToSameAddress').length == 0) {
                //
            }
        }

        return false;
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
        $.ajax({
            cache: false,
            url: this.loadUrl,
            type: 'get',
            success: this.reload,
            complete: this.resetLoadWaiting,
            error: Checkout.ajaxFailure,
        });
    },
    newAddress: function (isNew) {
        if (isNew) {
            this.resetSelectedAddress();
            $('#billing-new-address-form').show();
            $('#billing-buttons-container').show();
        } else {
            this.saveSelectedAddress()
            $('#billing-new-address-form').hide();
            $('#billing-buttons-container').hide();
        }
    },
    resetSelectedAddress: function () {
        var selectElement = $('#billing-address-select');
        if (selectElement) {
            selectElement.val('');
        }
    },
    saveSelectedAddress: function () {
        $.ajax({
            cache: false,
            url: this.saveUrl,
            success: this.selectnextStep,
            data: $(this.form).serialize(),
            type: 'post',
            error: Checkout.ajaxFailure
        });
    },
    selectnextStep: function (response) {
        if (response.goto_section) {
            if (response.goto_section == 'shipping_method' ||
                response.goto_section == 'payment_method') {
                Checkout.setStepResponse(response);
            }
            if (response.goto_section == 'shipping') {
                //Checkout.setStepResponse(response);
            }
        }
    },
    save: function () {
        if (Checkout.loadWaiting != false) return;

        $.ajax({
            cache: false,
            url: this.saveUrl,
            data: $(this.form).serialize(),
            type: 'post',
            success: this.nextStep,
            complete: this.resetLoadWaiting,
            error: Checkout.ajaxFailure
        });
    },

    shiptoSameAddress: function (element) {
        Shipping.setasbillingaddress();
        if (document.getElementById(element).checked) {
            $('#opc-shipping').hide();
        }
        else {
            $('#opc-shipping').show();
        }
    },
    resetLoadWaiting: function () {
    },

    reload: function (response) {
        Checkout.setStepResponse(response);
        if ($("#billing-address-select").length > 0) {
            $('#billing-new-address-form').hide();
            if (Billing.firstLoad == false) {
                var size = $("#billing-address-select option").size();
                if (size > 2) {
                    $("#billing-address-select").prop('selectedIndex', size - 2);
                }
            }
        }
        else {
            $('#billing-buttons-container').show();
        }
        var shipaddress = 'ShipToSameAddress';
        if ($('#' + shipaddress).length > 0) {
            Billing.shiptoSameAddress(shipaddress);
            $('#' + shipaddress).change(function () {
                Billing.shiptoSameAddress(shipaddress);
            });
        }
    },
    nextStep: function (response) {
        //ensure that response.wrong_billing_address is set
        //if not set, "true" is the default value
        if (typeof response.wrong_billing_address == 'undefined') {
            response.wrong_billing_address = false;
        }
        if (response.error) {

            if ((typeof response.message) == 'string') {
                alert(response.message);
            } else {
                alert(response.message.join("\n"));
            }

            return false;
        }
        if (response.goto_section) {
            if (response.goto_section == 'shipping' || response.goto_section == 'shipping_method' ||
                response.goto_section == 'payment_method') {
                Billing.load(false);
                Shipping.load(false);
                $('#billing-buttons-container').hide();
            }
        }
        Checkout.setStepResponse(response);
        if (response.wrong_billing_address == false) {
            Shipping.resetShipping();
            Billing.load(false);
            $('#billing-buttons-container').hide();
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
        $.ajax({
            cache: false,
            url: this.loadUrl,
            type: 'get',
            success: this.reload,
            complete: this.resetLoadWaiting,
            error: Checkout.ajaxFailure,
        });
    },
    reload: function (response) {
        Checkout.setStepResponse(response);
        if ($("#shipping-address-select").length > 0) {
            $('#shipping-new-address-form').hide();
            if (Shipping.firstLoad == false) {
                var size = $("#shipping-address-select option").size();
                if (size > 2) {
                    $("#shipping-address-select").prop('selectedIndex', size - 2);
                }
            }
        }
        else {
            $('#shipping-buttons-container').show();
        }
    },
    newAddress: function (isNew) {
        if (isNew) {
            this.resetSelectedAddress();
            $('#shipping-new-address-form').show();
            $('#shipping-buttons-container').show();
        } else {
            this.saveSelectedAddress()
            $('#shipping-new-address-form').hide();
            $('#shipping-buttons-container').hide();
        }
    },

    togglePickUpInStore: function (pickupInStoreInput) {
        this.saveSelectedAddress();
        if (pickupInStoreInput.checked) {
            $('#shipping-addresses-form').hide();
            $('#opc-shipping-method').hide();
            $('#shipping-buttons-container').hide();
            $('#pickup-points-form').show();
            if ($('#pickup-points-select').length > 0) {
                $('#pickup-points-select').on('change', function () {
                    Shipping.saveSelectedAddress();
                });
            }
        }
        else {
            $('#opc-shipping-method').show();
            $('#shipping-addresses-form').show();
            $('#pickup-points-form').hide();
            if ($('.new-shipping-address').css('display') == 'none' || $('.new-shipping-address').css("visibility") == "hidden") {
                $('#shipping-buttons-container').hide();
            }
            else {
                $('#shipping-buttons-container').show();
            }
        }

    },

    resetSelectedAddress: function () {
        var selectElement = $('#shipping-address-select');
        if (selectElement) {
            selectElement.val('');
        }
    },
    setasbillingaddress: function () {
        if ($("#shipping-address-select").length > 0) {
            var selectedIndex = $("#billing-address-select").prop('selectedIndex')
            $("#shipping-address-select").prop('selectedIndex', selectedIndex);
            Shipping.saveSelectedAddress();
        }
    },
    saveSelectedAddress: function () {
        $.ajax({
            cache: false,
            url: this.saveUrl,
            data: $(this.form).serialize(),
            success: this.selectnextStep,
            type: 'post',
            error: Checkout.ajaxFailure
        });
    },
    selectnextStep: function (response) {
        if (response.goto_section) {
            if (response.goto_section == 'shipping_method' ||
                response.goto_section == 'payment_method') {

                var rewardPointsIsChecked = false;
                if ($('#UseRewardPoints').length > 0) {
                    if (document.getElementById('UseRewardPoints').checked) {
                        rewardPointsIsChecked = true;
                    }
                }
                Checkout.setStepResponse(response);

                if ($('#UseRewardPoints').length > 0 && rewardPointsIsChecked) {
                    $('#UseRewardPoints').prop('checked', true);
                }
                if (response.goto_section == 'payment_method') {
                    ConfirmOrder.load();
                }
            }
        }

    },
    save: function () {
        $.ajax({
            cache: false,
            url: this.saveUrl,
            data: $(this.form).serialize(),
            type: 'post',
            success: this.nextStep,
            complete: this.resetLoadWaiting,
            error: Checkout.ajaxFailure
        });
    },

    resetLoadWaiting: function () {
    },

    resetShipping: function () {
        if ($("#shipping-address-select").length > 0) {
            Shipping.newAddress(!$('#shipping-address-select').val());
        }
    },

    nextStep: function (response) {
        if (response.error) {
            if ((typeof response.message) == 'string') {
                alert(response.message);
            } else {
                alert(response.message.join("\n"));
            }

            return false;
        }
        if (response.goto_section) {
            if (response.goto_section == 'shipping_method') {
                Shipping.load(false);
                $('#shipping-buttons-container').hide();
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
        $('#opc-shipping-method').show();
        $.ajax({
            cache: false,
            url: this.loadUrl,
            type: 'get',
            success: this.reload,
            complete: this.resetLoadWaiting,
            error: Checkout.ajaxFailure,
        });
    },
    reload: function (response) {
        Checkout.setStepResponse(response);
    },

    selectShippingMethod: function () {

        this.saveSelected();
        $('input[type=radio][name=shippingoption]').on('click',
            function () {
                ShippingMethod.saveSelected();
            }
        );
    },

    saveSelected: function () {
        $.ajax({
            cache: false,
            url: this.saveUrl,
            data: $(this.form).serialize(),
            success: this.selectnextStep,
            type: 'post',
            error: Checkout.ajaxFailure
        });
    },
    selectnextStep: function (response) {
        if (response.goto_section) {
            if (response.goto_section == 'payment_method') {
                var rewardPointsIsChecked = false;
                if ($('#UseRewardPoints').length > 0) {
                    if (document.getElementById('UseRewardPoints').checked) {
                        rewardPointsIsChecked = true;
                    }
                }
                Checkout.setStepResponse(response);
                if ($('#UseRewardPoints').length > 0 && rewardPointsIsChecked) {
                    $('#UseRewardPoints').prop('checked', true);
                }
                ConfirmOrder.load();
            }
        }
    },
    save: function () {
        if (this.validate()) {
            $.ajax({
                cache: false,
                url: this.saveUrl,
                data: $(this.form).serialize(),
                type: 'post',
                success: this.nextStep,
                complete: this.resetLoadWaiting,
                error: Checkout.ajaxFailure
            });
        }
    },

    resetLoadWaiting: function () {
    },

    nextStep: function (response) {
        if (response.error) {
            if ((typeof response.message) == 'string') {
                alert(response.message);
            } else {
                alert(response.message.join("\n"));
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
    },
    toggleUseRewardPointsChange: function (useRewardPointsInput) {
        this.saveRewardPoints();
    },
    saveRewardPoints: function () {
        $.ajax({
            cache: false,
            url: this.saveUrl,
            data: $(this.form).serialize(),
            type: 'post',
            success: ConfirmOrder.load(),
            complete: this.resetLoadWaiting,
            error: Checkout.ajaxFailure
        });
    },

    load: function (first) {
        this.firstLoad = first;
        $('#opc-payment-method').show();
        $.ajax({
            cache: false,
            url: this.loadUrl,
            type: 'get',
            success: this.reload,
            complete: this.resetLoadWaiting,
            error: Checkout.ajaxFailure,
        });
    },
    reload: function (response) {
        Checkout.setStepResponse(response);
        PaymentInfo.load(true);

    },
    selectPaymentMethod: function () {
        $('input[type=radio][name=paymentmethod]').on('click',
            function () {
                $('.confirm-checkout-step-button').unbind('click');
                $('.confirm-checkout-step-button').attr('onclick', 'PaymentInfo.save()');
                PaymentInfo.load(false);
            }
        );
    },
    selectRewardPoints: function () {
        var rewardpoints = 'UseRewardPoints';
        if ($('#' + rewardpoints).length > 0) {
            $('#' + rewardpoints).change(function () {
                PaymentMethod.toggleUseRewardPointsChange($('#' + rewardpoints));
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
            $.ajax({
                cache: false,
                url: this.saveUrl,
                data: $(this.form).serialize(),
                type: 'post',
                success: this.nextStep,
                complete: this.resetLoadWaiting,
                error: Checkout.ajaxFailure
            });
        }
    },

    resetLoadWaiting: function () {
    },

    nextStep: function (response) {
        if (response.error) {
            if ((typeof response.message) == 'string') {
                alert(response.message);
            } else {
                alert(response.message.join("\n"));
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
        $('#opc-payment-info').show();
        $.ajax({
            cache: false,
            url: PaymentMethod.saveUrl,
            data: $(PaymentMethod.form).serialize(),
            type: 'post',
            success: this.reload,
            error: Checkout.ajaxFailure
        });
    },
    reload: function (response) {
        Checkout.setStepResponse(response);
        ConfirmOrder.load();
    },

    save: function () {
        if ($('#billing-buttons-container').is(":visible")) {
            alert('Please save billing address');
            return false;
        }
        if ($('#shipping-buttons-container').is(":visible")) {
            alert('Please save shipping address');
            return false;
        }
        if (PaymentMethod.fullRewardPoints == false || PaymentMethod.fullRewardPoints == undefined) {
            $.ajax({
                cache: false,
                url: ShippingMethod.saveUrl,
                data: $(ShippingMethod.form).serialize(),
                success: this.selectnextStepPaymentInfo,
                type: 'post',
                error: Checkout.ajaxFailure
            });
        }
        else {
            PaymentInfo.savePaymentInfo();
        }
    },
    selectnextStepPaymentInfo: function (response) {
        if (response.error) {
            alert(response.message);
        }
        else {
            $.ajax({
                cache: false,
                url: PaymentInfo.saveUrl,
                data: $(PaymentInfo.form).serialize(),
                type: 'post',
                success: PaymentInfo.nextStep,
                error: Checkout.ajaxFailure
            });
        }
    },
    savePaymentInfo: function () {
        $.ajax({
            cache: false,
            url: PaymentInfo.saveUrl,
            data: $(PaymentInfo.form).serialize(),
            type: 'post',
            success: PaymentInfo.gotoCheckout,
            error: Checkout.ajaxFailure
        });
    },
    gotoCheckout: function () {
        ConfirmOrder.save();
    },
    nextStep: function (response) {

        if (response.error) {
            if ((typeof response.message) == 'string') {
                alert(response.message);
            } else {
                alert(response.message.join("\n"));
            }

            return false;
        }
        console.log(response);
        if (response.goto_section == 'confirm_order') {
            ConfirmOrder.save();
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
        $.ajax({
            cache: false,
            url: this.loadUrl,
            type: 'get',
            success: this.reload,
            complete: this.resetLoadWaiting,
            error: Checkout.ajaxFailure,
        });
    },

    reload: function (response) {
        Checkout.setStepResponse(response);
    },

    save: function () {

        //terms of service
        var termOfServiceOk = true;
        if ($('#termsofservice').length > 0) {
            //terms of service element exists
            if (!$('#termsofservice').is(':checked')) {
                $("#terms-of-service-warning-box").modal('show');
                termOfServiceOk = false;
            } else {
                termOfServiceOk = true;
            }
        }
        if (termOfServiceOk) {
            $.ajax({
                cache: false,
                url: this.saveUrl,
                type: 'post',
                success: this.nextStep,
                complete: this.resetLoadWaiting,
                error: Checkout.ajaxFailure
            });
        } else {
            return false;
        }
    },

    resetLoadWaiting: function (transport) {
        //Checkout.setLoadWaiting(false, ConfirmOrder.isSuccess);
    },

    nextStep: function (response) {
        if (response.error) {
            if ((typeof response.message) == 'string') {
                alert(response.message);
            } else {
                alert(response.message.join("\n"));
            }

            return false;
        }

        if (response.redirect) {
            ConfirmOrder.isSuccess = true;
            location.href = response.redirect;
            return;
        }
        if (response.success) {
            ConfirmOrder.isSuccess = true;
            window.location = ConfirmOrder.successUrl;
        }

        Checkout.setStepResponse(response);
    }
};
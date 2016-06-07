function OpenInNewTab(url) {
    var win = window.open(url, '_blank');
    win.focus();
}

function isEmail(email) {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

function cover_marterial_format(state) {
    if (!state.id) return state.text; // optgroup
    return "<div class='marterial_wrapper'><img class='marterial' src='/" + jQuery(state.element).attr("img") + "'/>" + state.text + "</div>";
}

jQuery(document).ready(function () {
    if (jQuery("#breakingnews1").length > 0) {
        $("#breakingnews1").BreakingNews({
            background: 'none',
            title: 'NEWS',
            titlecolor: '#000',
            titlebgcolor: '#FFF',
            linkcolor: '#FFF',
            linkhovercolor: '#FFF',
            fonttextsize: 16,
            isbold: false,
            border: '0px',
            width: '100%',
            timer: 2000,
            autoplay: true,
            effect: 'slide'

        });
    }

    if (jQuery(".select2").length > 0) {
        $(".select2").select2();
    }


    // for the cover marterial
    if (jQuery("#cover_marterial_select2").length > 0) {
        $("#cover_marterial_select2").select2({
            formatResult: cover_marterial_format,
            formatSelection: cover_marterial_format,
            escapeMarkup: function (m) { return m; }
        }).on("change", function (item) {
            var path = jQuery(item.currentTarget).find("option[value=" + $("#cover_marterial_select2").val() + "]").attr("img");
            var img = "<img  src='/" + path + "' data-zoom-image='/" + path + "'>";
            jQuery("#marterial_preview").html(img);
            jQuery("#marterial_preview img").elevateZoom({
                zoomWindowFadeIn: 500,
                zoomWindowFadeOut: 500,
                lensFadeIn: 500,
                lensFadeOut: 500,
                zoomWindowWidth: 150,
                zoomWindowHeight: 150,
                easing: true,
                tint: true,
                tintColour: '#F90',
                tintOpacity: 0.5,
                responsive: true
            });
        });
        setTimeout(function () {
            //$("#cover_marterial_select2").val($("#cover_marterial_select2 option:first").attr("value"));
            $("#cover_marterial_select2").trigger("change");
        }, 250);
    }

    // For New Order Form
    if (jQuery("#NewOrderForm").length > 0) {
        // detect the case site go back 
        if (jQuery("#SecCheck").val() == "1") {
            jQuery("#SecCheck").val("0");
        }
        else if (jQuery("#SecCheck").val() == "2") {
            alert("Your order has been submitted. Click OK to see all of your orders");
            window.location.href = "/Order";
            is_submitting = true;
            jQuery("#NewOrderForm").remove();
            return;
        }
        else {
            window.location.href = window.location.href;
            is_submitting = true;
            jQuery("#NewOrderForm").remove();
            return;
        }

//        // handle product changes
//        $("#Product_Id").on("change", function (e) {
//            orderpage_ProductChange();
//        });

        // handle country change
        jQuery("#Billing_Country, #Shipping_Country, #Shipping_IsSamewithBillingAddress").on("change", function (e) {
            return;
            show_loading();
            // when checked, get the shipping
            if (jQuery("#Shipping_IsSamewithBillingAddress").is(":checked")) {
                countrycode = jQuery("#Shipping_Country").val();
            }
            else {
                countrycode = jQuery("#Billing_Country").val();
            }
            orderpage_UpdateTotal();
            hide_loading();
        });

        $("#validateCoupon").on("click", function (e) {
            orderpage_ValidateCoupon();
        });

        jQuery("#remove_coupon_handler").on("click", function () {
            jQuery("#coupon_code_result").hide();
            orderpage_CouponResetForm();
            jQuery("#coupon_form").show();
            order_discount = 0;
            DiscountType = 0;
        });

        jQuery("#Shipping_IsSamewithBillingAddress").change(function () {
            jQuery(".shipping_address_block").slideToggle();
        });

        jQuery("#payment input").change(function () {
            // hide all of them first
            jQuery("#payment .payment_box:visible").slideToggle();
            var id = jQuery(this).attr("id");

            jQuery("#payment div." + id).slideToggle();
        });

        // handle form submit
        jQuery("#NewOrderForm").submit(function () {
            return orderpage_FormSubmit();
        });

        setTimeout(function () {
            // after everything, trigger onchange events to let the page load the content
            if (jQuery("#Preset_Options").val() != "") {
                order_firsttime_dontloadoptions = true;
            }

            // load the states 
            orderpage_loadState();
            // we will not update the price here, but after the Country / State has been loaded
            //orderpage_ProductChange();

        }, 500);

        // for IE 7 in NewOrder page, we alert customer
        if (navigator.appVersion.indexOf("MSIE 7.") != -1) {
            alert("Sorry but you are using Internet Explorer version 7 which is out of date. Please download Chrome or Firefox to use for better experience.");
        }
    }

    jQuery("div.ab-menu div.adminControl").on("click", function (e) {

        ToggleMenuLeftUser();
    });
});

/* For New Order page */
var neworder_Product = null;
var neworder_ProductOption = null;
var order_grandtotal = 0;
var order_gst = 0;
var order_total = 0;
var order_discount = 0;
// =0: fix amount; =1: percentage
var DiscountType = 0;
// discount in formated with currency sign
var order_discount_display = "";
var order_shippingfee = 0;
/// at the first time, dont load product option
var order_firsttime_dontloadoptions = false;
var is_submitting = false;

function orderpage_CouponResetForm() {
    jQuery("#CouponCode").val("");
    jQuery("#CouponSecrect").val("");
    jQuery("#discount_amount").html("");
}

// Load the States by country to determine we will apply extra fee or not
function orderpage_loadState() {
    show_loading();

    jQuery.post('/Home/GetCountryStates', {
        country: jQuery("#Billing_Country").val()
    }, function (states) {
        var st = "";
        if (states != undefined && states.HasVal != undefined && states.HasVal) {
            // we have the custom list of the states for this country
            // we build the list for select
            var defaultState = jQuery("#country_states_default").val();
            st = "<select name='Billing_State' id='Billing_State' class = 'requiredField form-control' >";
            for (var i = 0; i < states.Data.length; i++) {
                st += "<option value='" + states.Data[i].State + "' amount_value='" + states.Data[i].Amount + "'";
                if (states.Data[i].State == defaultState) {
                    st += " selected='selected'";
                }
                st += ">";
                st += states.Data[i].State;
                if (states.Data[i].Amount > 0) {
                    st += " (+ " + currency_sign + " " + states.Data[i].Amount + ")";
                }
                st += "</option>";
            }
            st += "</select>";
        }
        else {
            // we dont have the custom list of states for this country, we go with the text box
            st = "<input type=text name='Billing_State' id='Billing_State' class = 'requiredField form-control' value='" + jQuery("#country_states_default").val() + "' />";
        }

        jQuery("#states_container").html("").append(st);

        // define the change handle for states
        if (jQuery("#Billing_State").is("select")) {
            jQuery("#Billing_State").change(function () {
                orderpage_UpdateTotal();
            });
        }

        // update the product profile info
        orderpage_ProductChange();
        hide_loading();
    }, "json");
}

// Handle product change on new order page
function orderpage_ProductChange() {
    var pid = jQuery("#Product_Id").val();

    // hide products and options
    jQuery("tr.product_option").remove();
    //jQuery("#option_wrapper").fadeOut();
    neworder_ProductOption = null;
    neworder_Product = null;
    order_discount = 0;
    order_grandtotal = 0;
    DiscountType = 0;
    order_gst = 0;
    order_total = 0;

    show_loading();
    // get the product information
    jQuery.post('/product/newOrder_getProductDetail', {
        id: pid
    }, function (product) {
        neworder_Product = product;
        jQuery("#product_quantity").html(product.Pages + " Pages");
        jQuery("#product_price").html(currency_sign + " " + product.Price.format(2));
        //product_price = product.Price;

        // shipping
        orderpage_UpdateShipping();

        // now we load options
        orderpage_LoadProductOptions(product.Id);
        hide_loading();
    }, "json");

}

/// process the result of the loading product option
function orderpage_LoadProductOptions_Processing(options) {

    neworder_ProductOption = Enumerable.From(options);

    // build the options panel
    jQuery("#option_wrapper").hide();
    var count_opts = 0;
    var optional_option = Enumerable.From(neworder_ProductOption).Where(function (x) { return x.isRequire == false; }).ToArray()
    // render
    for (var i = 0; i < optional_option.length; i++) {
        orderpage_OptionRender(optional_option[i]);
        count_opts++;
    }
    var require_option = Enumerable.From(neworder_ProductOption).Where(function (x) { return x.isRequire == true; }).ToArray()
    // render
    for (var i = 0; i < require_option.length; i++) {
        orderpage_OptionRender(require_option[i]);
        count_opts++;
    }
    //
    if (count_opts > 0) {
        jQuery("#option_wrapper").show();
    }

    // now add options plus and minus handler
    jQuery("input.option_handler").click(function () {
        // plus or minus?
        var is_plus = true;
        if (jQuery(this).hasClass("option_minus")) {
            is_plus = false;
        }
        var tr = jQuery(this).closest("tr");
        var option_id = tr.attr("option_id");

        // update on our data obj
        var opt = neworder_ProductOption.Where(function (x) { return x.Id == option_id; }).FirstOrDefault();
        if (is_plus) {
            opt.DefaultQuantity += 1;
        }
        else {
            opt.DefaultQuantity += -1;
        }

        if (opt.DefaultQuantity < 0) {
            opt.DefaultQuantity = 0;
        }

        if (opt.MaxQuantity != 0) {
            if (opt.DefaultQuantity > opt.MaxQuantity) {
                opt.DefaultQuantity = opt.MaxQuantity;
            }

            if (opt.DefaultQuantity < opt.MinQuantity) {
                opt.DefaultQuantity = opt.MinQuantity;
            }
        }

        if (opt.isRequire && opt.DefaultQuantity == 0) {
            opt.DefaultQuantity = 1; // can not remove
        }

        // now we update the td field
        var unit = "";
        if (opt.UnitName != undefined) {
            unit = " " + opt.UnitName;
        }
        jQuery(this).closest("td").find("span").html(opt.DefaultQuantity + " " + unit);
        // and update the price
        orderpage_UpdateTotal();
    });

    // now add options plus and minus handler
    jQuery("input.quantity_handler").click(function () {
        // plus or minus?
        var is_plus = true;
        if (jQuery(this).hasClass("option_minus")) {
            is_plus = false;
        }
        var tr = jQuery(this).closest("tr");

        var quantity = parseInt(jQuery("input[name='Quantity']").val());

        if (is_plus) {
            quantity += 1;
        }
        else {
            quantity += -1;
        }

        if (quantity <= 0) {
            quantity = 1;
        }
        jQuery("input[name='Quantity']").val(quantity);
        jQuery(this).closest("td").find("span").html(quantity);
        // and update the price
        orderpage_UpdateTotal();
    });

    // Now add options view thumbnail handler
    jQuery(".product_option a.view-thumbnail-opt").fancybox({
        "width": "100%",
        "height": "100%",
        "apactity": true,
        "autoScale": true,
        "overlayShow": true,
        "transitionIn": "elastic",
        "transitionOut": "elastic"
    });

    // update the total price
    orderpage_UpdateTotal();
}

/// to load all options relative to this product
function orderpage_LoadProductOptions() {
    if (order_firsttime_dontloadoptions) {
        order_firsttime_dontloadoptions = false;
        orderpage_LoadProductOptions_Processing(jQuery.parseJSON(jQuery("#Preset_Options").val()));
        return;
    }

    show_loading();
    jQuery.post('/product/newOrder_getProductOptions', {
        id: neworder_Product.Id
    }, function (options) {
        orderpage_LoadProductOptions_Processing(options);
        hide_loading();
    }, "json");
}

// Update the total amount in table
function orderpage_UpdateTotal(ignore_coupon_calculation) {

    if (ignore_coupon_calculation == undefined) {
        ignore_coupon_calculation = false;
    }

    if (!$('#coupon_form').is(':visible')) {
        // user entered the code here
        if (ignore_coupon_calculation == false) {
            orderpage_ValidateCoupon();
            return;
        }
    }

    var quantity = parseInt(jQuery("input[name='Quantity']").val());
    if (quantity <= 0) {
        quantity = 1;
    }
    // update the quantity
    jQuery("input[name='Quantity']").val(quantity);
    if (quantity == 1) {
        jQuery("#quantity_display").html(quantity + " pc");
    }
    else {
        jQuery("#quantity_display").html(quantity + " pcs");
    }

    var t = neworder_Product.Price;
    jQuery("#product_price").html(currency_sign + " " + t.format(2));
    jQuery("#product_price2").html(currency_sign + " " + t.format(2));

    orderpage_UpdateShipping();
    t += order_shippingfee;

    //
    var opts = neworder_ProductOption.ToArray();
    for (var i = 0; i < opts.length; i++) {
        var x = opts[i];
        var price = orderpage_OptionGetPrice(x);
        var total = price.Value * x.DefaultQuantity;
        t += total;
        // update
        //jQuery("tr[option_id=" + x.Id + "]").find("td:last").html(currency_sign + " " + total.toFixed(2));
        jQuery("tr[option_id=" + x.Id + "]").find("td:last").html(price.CurrencyCode + " " + total.format(2));
        // update the display price
        jQuery("tr[option_id=" + x.Id + "]").find("td:eq(1)").html(price.CurrencyCode + " " + price.Value.format(2));
    }

    // update the sub total
    jQuery("#subtotal").html(currency_sign + " " + t.format(2));

    // grand total = subtotal * quantity
    t = t * quantity;

    // grand total
    jQuery("#grandtotal").html(currency_sign + " " + t.format(2));
    order_grandtotal = t;

    // coupon discount
    if (!$('#coupon_form').is(':visible')) {
        if (DiscountType == 0) {
            // fix amount
            t -= order_discount;
            jQuery("#discount_amount").html(currency_sign + " " + order_discount.format(2));
        }
        else {
            // percentage
            t -= order_discount * quantity;
            var x = order_discount * quantity;
            jQuery("#discount_amount").html(currency_sign + " " + x.format(2));
        }
    }
    else {
        jQuery("#discount_amount").html("");
    }

    // gst
    if (jQuery("#gst").length > 0) {
        var gst = 6 * t / 100;
        order_gst = gst;
        jQuery("#gst").html(currency_sign + " " + gst.format(2));
    }
    else {
        order_gst = 0;
    }
    // total
    t += order_gst;
    jQuery("#bill_total").html(currency_sign + " " + t.format(2));
    order_total = t;
}

// Render an option
function orderpage_OptionRender(opt) {
    var price = orderpage_OptionGetPrice(opt);
    var unit = "";
    if (opt.UnitName != undefined) {
        unit = " " + opt.UnitName;
    }

    var opt_name = opt.Name;
    if (opt.isRequire) {
        opt_name = "<div class='option_require'>" + opt_name + "</div>";
    }
    var is_additional_page = opt_name.indexOf("Additional Page") > -1;

    var thumbnail = "";
    if (opt.Thumbnail != null && opt.Thumbnail != "") {
        thumbnail = "&nbsp;-<a style='text-decoration: none;' rel='' href='".concat(urlTheme, opt.Thumbnail, "' class='view-thumbnail-opt'><span class='icon-eye-open'></span>&nbsp;View Sample</a>");
    }
    var html = '<tr valign="top" class="product_option" option_id="' + opt.Id + '"><td valign="top">' + opt_name + thumbnail + '</td><td valign="top">' + price.CurrencyCode + " " + price.Value.format(2) + '</td><td valign="top" class="option_quantity_field">';
    if (!is_additional_page) {
        html += '<input type=button  class="option_handler option_minus">';
    }
    html += '<span>' + opt.DefaultQuantity + " " + unit + '</span>';
    if (!is_additional_page) {
        html += '<input type=button class="option_handler option_plus">';
    }
    html += '</td><td valign="top">' + price.CurrencyCode + " " + price.Value.format(2) + '</td></tr>';
    jQuery(html).insertAfter(jQuery("#option_wrapper"));
}

// to update error to server and redirect to homepage
function orderpage_Error(st) {
    if (!is_submitting) {
        jQuery.post("/Product/ErrorReport", {
            error: st + "; Options Detail: " + orderpage_PrepareOptionsToSubmit() + " ; Raw Option Id = " + jQuery("#Preset_Options").val()
        }, function () {
            alert("There is an internal error while processing your order.\r\nWe can not process your order at this time.");
            window.location.href = "/";
        });
    }
}

function orderpage_UpdateShipping() {
    // shipping
    order_shippingfee = 0;
    if (!neworder_Product.isFreeShip) {
        var ship_price_list = toLinq(neworder_Product.ShippingPrice);
        var s_price = ship_price_list.Where(function (x) { return x.CountryCode == countrycode; });
        if (s_price.Count() == 0) {
            // can not find, then search the default country MY
            // s_price = ship_price_list.Where(function (x) { return x.CountryCode == "MY"; });
            orderpage_Error("There is no Shipping Price for product id " + neworder_Product.Id + ", Name = " + neworder_Product.Name + ". Country = " + countrycode);
            return;
        }
        if (s_price.Count() > 0) {
            var k = s_price.FirstOrDefault();
            order_shippingfee = k.Value;

            // we adjust the extra shipping for some states
            if (jQuery("#Billing_State").is("select") && parseFloat(jQuery("#Billing_State option:selected").attr("amount_value")) > 0) {
                order_shippingfee += parseFloat(jQuery("#Billing_State option:selected").attr("amount_value"));
            }

            jQuery("#shipping_price").html(k.CurrencyCode + " " + order_shippingfee.format(2));
            jQuery("#shipping_total").html(k.CurrencyCode + " " + order_shippingfee.format(2));
        }
    }
    else {
        jQuery("#shipping_price").html(currency_sign + " 0<br />Free");
        jQuery("#shipping_total").html(currency_sign + " 0<br />Free");
    }
}

function toLinq(obj) {
    return Enumerable.From(obj);
}

/// Render display price of the option
function orderpage_OptionGetPrice(opt) {
    var price = toLinq(opt.Price);
    var s_price = price.Where(function (x) { return x.CountryCode == countrycode });
    if (s_price.Count() == 0) {
        // can not find, then search the default country MY
        // s_price = price.Where(function (x) { return x.CurrencyCode == currency_sign; });
        orderpage_Error("There is no price for product option id " + opt.Id + " , Code = " + opt.Code + " , Name = " + opt.InternalName + ", Country = " + countrycode);
        return;
    }
    return s_price.FirstOrDefault();
}

/// return json string of options encoded, before we submit to server
function orderpage_PrepareOptionsToSubmit() {
    var ret = new Array();

    var opts = neworder_ProductOption.ToArray();
    for (var i = 0; i < opts.length; i++) {
        ret.push({
            Quantity: opts[i].DefaultQuantity,
            Option_Id: opts[i].Id
        });
    }
    // instead of using JSON.stringify, we use jquery plugin to make sure it works with IE
    // https://github.com/Krinkle/jquery-json
    //return JSON.stringify(ret);
    return jQuery.toJSON(ret);
}

// validate a coupon
function orderpage_ValidateCoupon() {
    if (jQuery("#CouponCode").val() == "") {
        alert("Please enter Coupon Code");
        jQuery("#CouponCode").focus();
        return false;
    }

    show_loading();
    jQuery.post('/product/newOrder_CouponValidate', {
        CouponCode: jQuery("#CouponCode").val(),
        Product_Id: neworder_Product.Id,
        Options: orderpage_PrepareOptionsToSubmit(),
        CountryCode: countrycode
    }, function (result) {
        //  result = eval('(' + result + ')');
        if (result.Valid != undefined) {
            // we get correct result
            if (result.Valid) {
                if (result.RequiredSecurityCode) {
                    if (jQuery("#CouponSecrect").val() == "" || jQuery("#CouponSecrect").val() == undefined) {
                        // ask for the security code
                        var securityCode = prompt("Please Enter Coupon Security Code", "");
                        if (securityCode != null && securityCode.length > 0) {
                            // keep the security code
                            jQuery("#CouponSecrect").val(securityCode);
                        }
                        else {
                            ret.Valid = false;
                        }
                    }
                }

                if (result.Valid) {
                    order_discount = result.Discount;
                    order_discount_display = result.Discount_In_Other_Currency;
                    DiscountType = result.DiscountType;
                    // update the price table
                    jQuery("#coupon_form").hide();
                    jQuery("#coupon_code_result>div").html(jQuery("#CouponCode").val())
                    jQuery("#coupon_code_result").show();
                }
            }

            if (!result.Valid) {
                alert("Sorry, your entered coupon is invalid.");
                order_discount = 0;
                DiscountType = 0;
                order_discount_display = "";
                jQuery("#coupon_form").show();
                jQuery("#coupon_code_result").hide();
            }
        }
        else {
            alert("Sorry, we can not validate your entered coupon at this time");
            order_discount = 0;
            DiscountType = 0;
            order_discount_display = "";
            jQuery("#coupon_form").show();
            jQuery("#coupon_code_result").hide();
        }

        orderpage_UpdateTotal(true);

        hide_loading();
    }, "json");
}

// Handle form submit
function orderpage_FormSubmit() {
    if (is_submitting) {
        return false;
    }
    is_submitting = true;
    // validation
    if (jQuery("#PhotobookCode").val() == "") {
        alert("Please enter PhotobookCode");
        jQuery("#PhotobookCode").focus();
        is_submitting = false;
        return false;
    }

    if (jQuery("#Billing_FirstName").val() == "") {
        alert("Please enter your billing first name");
        jQuery("#Billing_FirstName").focus();
        is_submitting = false;
        return false;
    }

    if (jQuery("#Billing_LastName").val() == "") {
        alert("Please enter your billing last name");
        jQuery("#Billing_LastName").focus();
        is_submitting = false;
        return false;
    }

    if (jQuery("#Billing_Address").val() == "") {
        alert("Please enter your billing address");
        jQuery("#Billing_Address").focus();
        is_submitting = false;
        return false;
    }

    if (jQuery("#Billing_City").val() == "") {
        alert("Please enter billing city");
        jQuery("#Billing_City").focus();
        is_submitting = false;
        return false;
    }
    if (jQuery("#Billing_ZipCode").val() == "") {
        alert("Please enter billing zip code");
        jQuery("#Billing_ZipCode").focus();
        is_submitting = false;
        return false;
    }
    if (jQuery("#Billing_Email").val() == "") {
        alert("Please enter billing email");
        jQuery("#Billing_Email").focus();
        is_submitting = false;
        return false;
    }
    if (jQuery("#Billing_Phone").val() == "") {
        alert("Please enter your billing phone");
        jQuery("#Billing_Phone").focus();
        is_submitting = false;
        return false;
    }

    // for shipping
    if (jQuery("#Shipping_IsSamewithBillingAddress").is(":checked")) {
        if (jQuery("#Shipping_FirstName").val() == "") {
            alert("Please enter your shipping first name");
            jQuery("#Shipping_FirstName").focus();
            is_submitting = false;
            return false;
        }

        if (jQuery("#Shipping_LastName").val() == "") {
            alert("Please enter your shipping last name");
            jQuery("#Shipping_LastName").focus();
            is_submitting = false;
            return false;
        }

        if (jQuery("#Shipping_Address").val() == "") {
            alert("Please enter your shipping address");
            jQuery("#Shipping_Address").focus();
            is_submitting = false;
            return false;
        }

        if (jQuery("#Shipping_City").val() == "") {
            alert("Please enter shipping city");
            jQuery("#Shipping_City").focus();
            is_submitting = false;
            return false;
        }
        if (jQuery("#Shipping_ZipCode").val() == "") {
            alert("Please enter shipping zip code");
            jQuery("#Shipping_ZipCode").focus();
            is_submitting = false;
            return false;
        }
        if (jQuery("#Shipping_Email").val() == "") {
            alert("Please enter shipping email");
            jQuery("#Shipping_Email").focus();
            is_submitting = false;
            return false;
        }
        if (jQuery("#Shipping_Phone").val() == "") {
            alert("Please enter your shipping phone");
            jQuery("#Shipping_Phone").focus();
            is_submitting = false;
            return false;
        }
    }

    if (!jQuery("#AcceptTermCondition").is(":checked")) {
        alert("Please accept our Terms and Conditions before place your order.");
        is_submitting = false;
        return false;
    }

    // if everything is ok then let's go
    jQuery("#Options").val(orderpage_PrepareOptionsToSubmit());
    // set SecCheck to 2 so say that order has been submitted
    jQuery("#SecCheck").val("2");
    show_loading();
    jQuery("input[name='sendMessage']").hide();
    setTimeout(function () {
        jQuery("#NewOrderForm").remove();
        window.location.href = "/Order";
    }, 200);
    return true;
}

function ToggleMenuLeftUser() {

    var toggle = jQuery("div.ab-menu div.adminControl");

    var target = jQuery("div.ab-menu div.admin");

    toggle.toggleClass("active");

    if (toggle.hasClass("active")) {

        target.removeClass("display-none")

    } else {

        target.addClass("display-none");
    }
}

if (typeof String.prototype.endsWith === 'undefined') {
    String.prototype.endsWith = function (suffix) {
        return this.indexOf(suffix, this.length - suffix.length) !== -1;
    };
}

if (typeof Number.prototype.format === 'undefined') {
    Number.prototype.format = function (precision) {
        if (!isFinite(this)) {
            return this.toString();
        }

        var a = this.toFixed(precision).split('.');
        a[0] = a[0].replace(/\d(?=(\d{3})+$)/g, '$&,');
        var ret = a.join('.');
        if (ret.endsWith(".00")) {
            ret = ret.substring(0, ret.length - 3);
        }
        return ret;
    }
}

// Sta: For Views/Common/_Intro.cshtml
$(function () {
    $('#accordion > li').hover(
        function () {
            var $this = $(this);
            $this.stop().animate({ 'width': '200px' }, 500);
            $('.heading', $this).stop(true, true).fadeOut();
            $('.bgDescription', $this).stop(true, true).slideDown(500);
            $('.description', $this).stop(true, true).fadeIn();
        },
        function () {
            var $this = $(this);
            $this.stop().animate({ 'width': '115px' }, 1000);
            $('.heading', $this).stop(true, true).fadeIn();
            $('.description', $this).stop(true, true).fadeOut(500);
            $('.bgDescription', $this).stop(true, true).slideUp(700);
        }
    );
});
// End: For Views/Common/_Intro.cshtml

// Sta: For Views/Home/SignIn.cshtml
jQuery(document).ready(function ($) {

    if ($("#frmSignIn").length != 0) {

        $("#frmSignIn").on("submit", function (e) {

            if ($(this).find("input[name='UserName']").val() == "") {

                alert("Please enter your username");
                $(this).find("input[name='UserName']").focus();
                return false;
            }

            if ($(this).find("input[name='Pass']").val() == "") {

                alert("Please enter your password");
                $(this).find("input[name='Pass']").focus();
                return false;
            }
        });
    }
});
// End: For Views/Home/SignIn.cshtml

/*
For Order Detail Panel
Front End Panel
Project Photobookmart
Author: Trung Dang (trungdt@absoft.vn)
*/

var order_id = 0;


function OrderDetail_History_Reload(page) {
    jQuery.post(jQuery("#panel_history").attr("data-url"), {
        page: page
    }, function (data) {
        if (data != "") {
            // update new page
            jQuery("#panel_history").attr("page", page);
            jQuery("#panel_history .chat-list").append(data);
            jQuery("#panel_history .mws-panel-body > div").attr('loading', "no");
        }
        else {
            if (page != 1) {
                alert("There is no more history log in this order");
            }
        }
        hide_loading();
    });
}

jQuery(document).ready(function () {
    // update the common value
    order_id = jQuery("#OrderId").val();
    // ------------------------------------------- History Detail -----------------------------------//

    setInterval(function () {
        jQuery("#panel_history .chat-list").html("");
        OrderDetail_History_Reload(1);
    }, 1000 * 60);

    // handle order history infinite load
    jQuery("#panel_history .mws-panel-body > div").bind('scroll', function () {
        if (jQuery(this).scrollTop() + jQuery(this).innerHeight() >= this.scrollHeight - 30) {
            // check in loading
            var loading = jQuery(this).attr('loading');

            // For some browsers, `attr` is undefined; for others,
            // `attr` is false.  Check for both.
            if (typeof loading !== typeof undefined && loading !== false) {
                if (loading == "yes") {
                    return;
                }
            }
            jQuery(this).attr('loading', "yes");
            var page = parseInt(jQuery("#panel_history").attr("page"));
            page++;

            OrderDetail_History_Reload(page);
            show_loading();
        }
    }).trigger("scroll");

    // handle message form submit
    jQuery("#OrderHistory_Form_InsertMessage").submit(function () {
        // validate
        if (jQuery("#OrderHistory_Message").val() == "") {
            alert("Please enter your message");
            jQuery("#OrderHistory_Message").focus();
            return false;
        }
        show_loading();

        // and post to server
        jQuery.post(jQuery(this).attr("action"), jQuery(this).serialize(), function (returnObj) {
            jQuery("#OrderHistory_Message").val("");
            if (returnObj.Status == "success") {
                if (returnObj.Message != null) {
                    //notify_success("Success", returnObj.Message);
                    // reload
                    jQuery("#panel_history .chat-list").html("");
                    OrderDetail_History_Reload(1);
                }
            } else {
                alert(returnObj.Message);
                //                $.pnotify({
                //                    title: 'Error',
                //                    text: returnObj.Message,
                //                    type: 'error',
                //                    opacity: .8
                //                });
            }

            hide_loading();
        });
        return false;
    });

    // ------------------------------------------- End of History Detail -----------------------------------//

    // -- STA: Update Shipping Address

    jQuery(".btn-shipping-addr, #shipping_update_form_cancel").on("click", function (e) {

        jQuery(".container-shipping-addr").toggleClass("display-none");
        jQuery("#shipping_old_detail").toggleClass("display-none");
        //$(this).text(jQuery(".container-shipping-addr").hasClass("display-none") ? "Update Address" : "Cancel Update");
    });

    if (jQuery("#frmShippingAddr").length != 0) {

        jQuery("#frmShippingAddr").on("submit", function (e) {

            var $frm = jQuery(this);

            var $email = $frm.find("[name='Email']");

            if ($email.val() != "" && !isEmail($email.val())) {
                alert("Email format is not valid.");
                return false;
            }

            if ($frm.find("[name='FirstName']").val() == "") {
                alert("Please enter First Name.");
                return false;
            }

            if ($frm.find("[name='LastName']").val() == "") {
                alert("Please enter Last Name.");
                return false;
            }

            if ($frm.find("[name='PhoneNumber']").val() == "") {
                alert("Please enter Phone Number.");
                return false;
            }

            if ($frm.find("[name='Address']").val() == "") {
                alert("Please enter Address.");
                return false;
            }

            if ($frm.find("[name='State']").val() == "") {
                alert("Please enter State.");
                return false;
            }

            show_loading();
            jQuery.post($frm.attr("action"), $frm.serialize(), function (data) {
                if (data.Message != null && data.Message != "") {
                    alert(data.Message);
                }

                if (data.Status == "success") {
                    window.location.href = window.location.href;
                }
                hide_loading();
            });

            return false;
        });
    }

    // -- END: Update Shipping Address
});
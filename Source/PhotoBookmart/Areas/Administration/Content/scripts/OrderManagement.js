/*
For Order Detail Panel
Administration Panel
Project Photobookmart
Author: Trung Dang (trungdt@absoft.vn)
*/

/// Add handler event for assign button
function Handler_Assign_Reset_Button() {
    jQuery("a.btn_assign_order, a.btn_reset_order").click(function () {
        show_loading();
        jQuery.post(jQuery(this).attr("href"), function (returnObj) {
            if (returnObj.Status == "success") {
                if (returnObj.Message != null) {
                    notify_success("Success", returnObj.Message);
                }
                if (returnObj.RedirectUrl != null && returnObj.RedirectUrl != "") {
                    setTimeout(function () {
                        window.location.href = returnObj.RedirectUrl;
                    }, 800);
                }
            } else {
                $.pnotify({
                    title: 'Error',
                    text: returnObj.Message,
                    type: 'error',
                    opacity: .8
                });
            }
            hide_loading();
        });

        return false;
    });
}

// Reload this page
function OrderMan_Reload() {
    var base_url = jQuery("#OrderMan_Wrapper").attr("data-url");

    $.get(base_url, function (data) {
        $(".listuser").html("");
        $(".listuser").html(data);
        hide_loading();
    });
}


jQuery(document).ready(function () {
    setTimeout(function () {
        jQuery("#PanelOrderSearch").show();
    }, 500);

    // -------------------- Quick Navigate --------------------
    jQuery('#DialogQuickSearch_OrderId').keyup(function () {
        this.value = this.value.replace(/[^0-9\.]/g, '');
        if (this.value.length > 6) {
            this.value = this.value.substring(0, 6);
        }
    });

    jQuery("#Dialog_QuickSearch_Form").submit(function () {
        $("#Dialog_QuickSearch").parent().find('button:first').trigger("click");
        return false;
    });

    $("#Dialog_QuickSearch").dialog({
        autoOpen: false,
        modal: true,
        width: 500,
        buttons: {
            "Ok": function () {
                if (jQuery("#DialogQuickSearch_OrderId").val() == "") {
                    alert("Please enter » Order number");
                    jQuery("#DialogQuickSearch_OrderId").focus();
                    return false;
                }

                if (jQuery("#DialogQuickSearch_OrderId").val().length < 6) {
                    alert("» Order number should be in 6 digits format");
                    jQuery("#DialogQuickSearch_OrderId").focus();
                    return false;
                }


                // ajax submit
                show_loading();

                // and post
                jQuery.post(jQuery("#Dialog_QuickSearch_Form").attr("action"), jQuery("#Dialog_QuickSearch_Form").serialize(), function (returnObj) {
                    if (returnObj.Status == "success") {
                        if (returnObj.Message != null) {
                            notify_success("Success", returnObj.Message);
                        }
                        if (returnObj.RedirectUrl != null && returnObj.RedirectUrl != "") {
                            setTimeout(function () {
                                window.location.href = returnObj.RedirectUrl;
                            }, 800);
                        }
                    } else {
                        $.pnotify({
                            title: 'Error',
                            text: returnObj.Message,
                            type: 'error',
                            opacity: .8
                        });
                    }
                    hide_loading();
                });

                $(this).dialog("close");
            },
            Cancel: function () {
                $(this).dialog("close");
            }
        }
    });
    // handle quick search
    jQuery("#orderpage_quicksearch").click(function () {
        jQuery("#DialogQuickSearch_OrderId").val("");
        $("#Dialog_QuickSearch").dialog('open')
    });

    // -------------------- ernd of Quick Navigate --------------------

    // -------------------- Order Search --------------------
    jQuery("#Order_Search").submit(function () {
        if (jQuery("#ResultType").val() > 0) {
            return true;
        }
        else {
            show_loading();

            jQuery.post(jQuery(this).attr("action"), jQuery(this).serialize(), function (returnObj) {
                $.pnotify({
                    title: 'Success',
                    text: returnObj.Message,
                    type: 'info',
                    opacity: .8
                });
                //var x = jQuery(returnObj);
                $(".listuser").html("");
                $(".listuser").html(returnObj);

                jQuery("#order_page_title").html("Search orders result");

                // handle the pagination


                hide_loading();
            }, "html");

            return false;
        }
    });
    // -------------------- End Order Search --------------------
});


// -------------------- Automated Shipping --------------------
var isAutoApprove = false;

    function Init_OASF() {

        HideSMSSSTAAR_OASF();
        
        var frm = jQuery("#Order_Automated_Shipping_Form");

        frm.find("[name='Order_Number']").focus().val("");
        $("#Order_Automated_Shipping_Form [name='Auto_Approve']").attr("checked",false);
    };

    function Click_OK() {

        var frm = jQuery("#Order_Automated_Shipping_Form");

        var txtOrderNum = frm.find("[name='Order_Number']");

        var ddlShippingStatus = frm.find("[name='Shipping_Status']");

        var txtShippingTrackingNumber = frm.find("[name='Shipping_TrackingNumber']");

        if (!txtOrderNum.prop("disabled")) {

            if (!IsValidOrderNumClient(txtOrderNum.val())) {

                notify_error("Alert", "Enter order number in 6 digits format");

                txtOrderNum.focus();

                return false;
            }

            show_loading();

            $.post("/Administration/Order/Automated_Shipping_Exists", { id: txtOrderNum.val() }, function (data) {

                hide_loading();

                if (data.Status == "success") {
                    var obj = eval('(' + data.Message + ')');
                    // update this order info into the form
                    jQuery("#Order_Automated_Shipping_Form").find("[name='Shipping_Status']").select2("val",obj.Shipping_Status);
                    jQuery("#Order_Automated_Shipping_Form").find("[name='Shipping_TrackingNumber']").val(obj.Shipping_TrackingNumber);
                    jQuery("#Order_Automated_Shipping_Form").find("[name='Shipping_Method']").select2("val",obj.Shipping_Method);
                    // then show the form
                    ShowSMSSSTAAR_OASF();

                } else if (data.Status == "error" &&
                    data.Message != null &&
                    data.Message != "") {

                    notify_error("Alert", data.Message);

                    txtOrderNum.focus();
                }
            });

            return false;
        }

        if (ddlShippingStatus.val() == "Shipped" &&
            (txtShippingTrackingNumber.val() == null || txtShippingTrackingNumber.val() == "")) {

            notify_error("Alert", "Please enter for » Tracking Number");

            txtShippingTrackingNumber.focus();

            return false;
        }

        if (confirm("Do you want to update the shipping of this order?")) {
            
            $("#Order_Automated_Shipping_Form").trigger("submit");
        }

        return false;
    };

    function Click_Cancel() {
        
        $("#Order_Automated_Shipping_Dialog").dialog("close");

        if (isAutoApprove) location.reload();
    };

    function HideSMSSSTAAR_OASF() {

        var frm = jQuery("#Order_Automated_Shipping_Form");

        frm.find("[name='Order_Number']").prop("disabled", false);

        jQuery("#Con_Shipping_Method").hide();

        jQuery("#Con_Shipping_Status").hide();

        jQuery("#Con_Shipping_TrackingNumber").hide();

        jQuery("#Con_Auto_Approve").hide();

        var btns = jQuery("#Order_Automated_Shipping_Dialog").parent().find("button");

        for (var i = 0; i < btns.length; i++) {

            if (btns.eq(i).children("span").text().toLowerCase() == "reset") {

                btns.eq(i).hide();
            };
        }
    };

    function ShowSMSSSTAAR_OASF() {

        var frm = jQuery("#Order_Automated_Shipping_Form");

        frm.find("[name='Order_Number']").prop("disabled", true);

        jQuery("#Con_Shipping_Method").show();

        jQuery("#Con_Shipping_Status").show();

        frm.find("[name='Shipping_Status']").trigger("change");
        
        setTimeout(function(){
           jQuery("#Con_Auto_Approve").show(); 
           jQuery("#Order_Automated_Shipping_Dialog").parent().find("button").show();
           jQuery("#Order_Automated_Shipping_Form").find("[name='Shipping_Method']").select2("open");
        },350);
    };

    function IsValidOrderNumClient(orderNum) {

        if (orderNum == null ||
            orderNum == "" ||
            orderNum.length > 6 ||
            /[^0-9]/g.test(orderNum)) {

            return false;
        }

        return true;
    };

    jQuery(document).ready(function ($) {
        var frm = jQuery("#Order_Automated_Shipping_Form");

        frm.find("[name='Order_Number']").keyup(function (e) {

            this.value = this.value.replace(/[^0-9\.]/g, '');

            if (this.value.length > 6) {

                this.value = this.value.substring(0, 6);
            }
        });
        
        frm.find("[name='Order_Number']").keypress(function (e) {

            if (e.which == 13) {

                Click_OK();
            }
        });

        frm.find("[name='Auto_Approve']").keypress(function (e) {

            if (e.which == 13) {

                Click_OK();
            }
        });

        frm.find("[name='Shipping_TrackingNumber']").keypress(function (e) {

            if (e.which == 13) {

                Click_OK();
            }
        });

       // handle shipping method change event
       frm.find("[name='Shipping_Method']").select2({selectOnBlur : true}).on("change", function (e) {
            jQuery("#Order_Automated_Shipping_Form").find("[name='Shipping_Status']").select2("open");
       }).on("close", function (e) {
            jQuery("#Order_Automated_Shipping_Form").find("[name='Shipping_Status']").select2("open");
       });

        frm.find("[name='Shipping_Status']").on("change", function (e) {

            if ($(this).val() == "Shipped") {

                $("#Con_Shipping_TrackingNumber").show();

                setTimeout(function(){
                    jQuery("#Order_Automated_Shipping_Form").find("[name='Shipping_TrackingNumber']").focus().select();
                },200);

            } else {

                $("#Con_Shipping_TrackingNumber").hide();

                jQuery("#Order_Automated_Shipping_Form").find("[name='Auto_Approve']").focus();
            }
        }).on("close", function (e) {
            if ($(this).val() == "Shipped") {

                $("#Con_Shipping_TrackingNumber").show();

                setTimeout(function(){
                    jQuery("#Order_Automated_Shipping_Form").find("[name='Shipping_TrackingNumber']").focus().select();
                },200);

            } else {

                $("#Con_Shipping_TrackingNumber").hide();

                jQuery("#Order_Automated_Shipping_Form").find("[name='Auto_Approve']").focus();
            }
       });

        $("#order_automated_shipping").on("click", function (e) {

            Init_OASF();

            $("#Order_Automated_Shipping_Dialog").dialog("option", "title", "Order Automated Shipping");

            $("#Order_Automated_Shipping_Dialog").dialog("open");

            $("#Order_Automated_Shipping_Dialog").css("overflow","hidden");
        });

        $("#Order_Automated_Shipping_Dialog").dialog({

            autoOpen: false,
            closeOnEscape: false,
            modal: false,
            width: 550,
            buttons: {

                "OK": function () {

                    Click_OK();
                },

                "Reset": function () {

                    Init_OASF();
                },

                Cancel: function () {

                    Click_Cancel();
                }
            }
        });

        $("#Order_Automated_Shipping_Form").submit(function () {

            var objDisabled = $(this).find(":disabled");

            show_loading();
            
            objDisabled.prop("disabled", false);

            $.post(jQuery(this).attr("action"), $(this).serialize(), function (data) {
                
                objDisabled.prop("disabled", true);
                
                hide_loading();

                if (data.Status == "success") {
                    if ($("#Order_Automated_Shipping_Form [name='Auto_Approve']").prop("checked")) {
                        isAutoApprove = true;

                        // we try to approve this order
                        var item_id = jQuery("#Order_Automated_Shipping_Form").find("[name='Order_Number']").val();
                        $.post("/Administration/Order/Order_Approve" + "?id=" + item_id, function (returnObj) {
                            if (returnObj.Status == "success") {
                                if (returnObj.Message != null) {
                                    notify_success("Success", returnObj.Message);
                                }
                            }
                            else {
                                $.pnotify({
                                    title: 'Error',
                                    text: returnObj.Message,
                                    type: 'error',
                                    opacity: .8
                                });
                            }
                            Init_OASF();
                        }, "json");
            
                        return false;
                    }
                    else{
                        Init_OASF();
                    }

                } else if (data.Status == "error" &&
                    data.Message != null &&
                    data.Message != "") {

                    notify_error("Alert", data.Message);
                }
            });

            return false;
        });
    });
// -------------------- Automated Shipping --------------------
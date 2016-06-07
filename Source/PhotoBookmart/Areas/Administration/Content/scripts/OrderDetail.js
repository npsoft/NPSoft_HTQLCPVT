/*
For Order Detail Panel
Administration Panel
Project Photobookmart
Author: Trung Dang (trungdt@absoft.vn)
*/

var order_id = 0;

function UpdateOrderHistoryMaxHeight() {
    var h = jQuery("#orderdetail_leftcolumn").height() - jQuery("#panel_history > .mws-panel-header").height() - 30;
    jQuery("#panel_history").css("height", h + "px");
    jQuery("#panel_history .mws-panel-body > div").css("height", (h - 30) + "px");
}

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
                notify_error("There is no more history log in this order");
            }
        }
        hide_loading();
    });
}

jQuery(document).ready(function () {
    // update the common value
    order_id = jQuery("#OrderId").val();

    $(".float-number").spinner({
        min: 0.00,
        step: 0.01,
        numberFormat: "n"
    });

    // ------------------------------------------- History Detail -----------------------------------//
    // set the max height of the chat history panel
    setTimeout(function () {
        UpdateOrderHistoryMaxHeight();
    }, 500);

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
                    notify_success("Success", returnObj.Message);
                    // reload
                    jQuery("#panel_history .chat-list").html("");
                    OrderDetail_History_Reload(1);
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

    // ------------------------------------------- End of History Detail -----------------------------------//

    // ------------------------------------------- Common Buttons Handler -----------------------------------//
    // handle all buttons with basic ajax
    jQuery(".btn-group a.basic_ajax").click(function () {
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
    // ------------------------------------------- End of common buttons handler-----------------------------------//
});

// STA: File Upload Request

function ReqUploadFileAgain(id) {

    if (confirm("Do you want to request customer to upload file again?[Yes/No]")) {

        show_loading();

        jQuery.post("/Administration/Order/ReqUploadFileAgain?Id=" + id, function (data) {

            hide_loading();

            if (data.Message != null) {

                if (data.Status == "success") {

                    notify_success("Success", data.Message);

                } else if (data.Status == "error") {

                    notify_error("Error", data.Message);
                }
            }

            if (data.Status == "success") {

                location.reload();
            }
        });
    }
};

function DialogUploadFileRequest() {

    $("#Dialog_UploadFilesTicket").dialog("option", "title", "File Upload Request");

    $("#Dialog_UploadFilesTicket").dialog("open");

    $("#Dialog_UploadFilesTicket").css("overflow", "hidden");
};

function ApproveUploadFileRequest() {
    
    var dialog = $("#Dialog_UploadFilesTicket");

    var id = dialog.find("[name='OrderId']").val();

    show_loading();

    jQuery.post("/Administration/Order/ApproveUploadFileRequest", { Id: id }, function (data) {

        hide_loading();

        if (data.Message != null) {

            if (data.Status == "success") {

                notify_success("Success", data.Message);

            } else if (data.Status == "error") {

                notify_error("Error", data.Message);
            }
        }

        if (data.Status == "success") {
            
            location.reload();
        }
    });
};

function CancelUploadFileRequest() {

    var dialog = $("#Dialog_UploadFilesTicket");

    var id = dialog.find("[name='OrderId']").val();

    var reason = dialog.find("[name='Reason']").val();

    show_loading();

    jQuery.post("/Administration/Order/CancelUploadFileRequest", { Id: id, Reason: reason }, function (data) {

        hide_loading();

        if (data.Message != null) {

            if (data.Status == "success") {

                notify_success("Success", data.Message);

            } else if (data.Status == "error") {

                notify_error("Error", data.Message);
            }
        }

        if (data.Status == "success") {

            location.reload();
        }
    });
};

jQuery(document).ready(function ($) {

    var dialog = $("#Dialog_UploadFilesTicket");

    if (dialog.length > 0) {

        dialog.dialog({
            autoOpen: false,
            closeOnEscape: false,
            modal: false,
            width: 550,
            buttons: { }
        });

        $("#FileUploadRequest").on("click", function (e) {

            DialogUploadFileRequest();
        });

        $(dialog.find("[name='Approve']")).on("click", function (e) {

            ApproveUploadFileRequest();

            return false;
        });

        $(dialog.find("[name='Cancel']")).on("click", function (e) {

            CancelUploadFileRequest();
        });
    }
});

// END: File Upload Request
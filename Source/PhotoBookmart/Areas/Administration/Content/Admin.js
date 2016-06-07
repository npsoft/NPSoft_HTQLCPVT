/// Administration Panel Javascript
// Travel To Go project
// Copyright: Trung Dang (trungdt@absoft.vn)

var mainList_Website = [];
var SVC_KEEP_ALIVE = "/KeepAlive";
var SVC_GETSETTINGSBYSCOPE = "/Administration/WebAdmin/Svc_GetSettingsByScope";
var SVC_GETALLPROVINCES = "/Administration/WebAdmin/Svc_GetAllProvinces";
var SVC_GETDISTRICTSBYPROVINCE = "/Administration/WebAdmin/Svc_GetDistrictsByProvince";
var SVC_GETVILLAGESBYDISTRICT = "/Administration/WebAdmin/Svc_GetVillagesByDistrict";
var SVC_GETHAMLETSBYVILLAGE = "/Administration/WebAdmin/Svc_GetHamletsByVillage";
var SVC_GETALLTYPESOBJ = "/Administration/WebAdmin/Svc_GetAllTypesObj";
var SVC_GETALLTYPESDISABILITY = "/Administration/WebAdmin/Svc_GetAllTypesDisability";
var SVC_GETALLMARITALSTATUSES = "/Administration/WebAdmin/Svc_GetAllMaritalStatuses";
var SVC_GETALLSELFSERVINGS = "/Administration/WebAdmin/Svc_GetAllSelfServings";
var SVC_GETTINHTRANGDTSBYPARAMS = "/Administration/WebAdmin/Svc_GetTinhTrangDTsByParams";
var SVC_GETDOITUONGBYPARAMS = "/Administration/WebAdmin/Svc_GetDoiTuongByParams";
var SVC_GETMUCTROCAPCOBANBYPARAMS = "/Administration/WebAdmin/Svc_GetMucTroCapCoBanByParams";
var SVC_GETSBD = "/Administration/WebAdmin/Svc_GetSBD";

jQuery(document).ready(function ($) {
    $("input[type='text'].date-of-birth-year").spinner({
        min: 1000,
        max: 9999,
        step: 1,
        numberFormat: "n"
    });

    $("input[type='text'].date-of-birth-month").spinner({
        min: 1,
        max: 12,
        step: 1,
        numberFormat: "n"
    });

    $("input[type='text'].date-of-birth-date").spinner({
        min: 1,
        max: 31,
        step: 1,
        numberFormat: "n"
    });

    $("input[type='text'].currency").spinner({
        min: 0,
        step: 100000.00,
        numberFormat: "n"
    });

    $("body").on("reset", "form", function (e) {
        var $frm = $(this);
        setTimeout(function (e) {
            $frm.find("select.mws-select2[data-init]").each(function (index, element) {
                $element = $(element);
                $element.select2().select2("val", $element.attr("data-init"));
            });
        }, 100);
    });

    $("body").on("keyup", ".select2-with-searchbox > .select2-search > input.select2-input", function (e) {
        $ddl_genders = $("[name='GioiTinh'], [name='MaLDT_Type3_Gender']");
        $ddl_genders.each(function (index, element) {
            var $ddl = $(element);
            if ($ddl.data("select2").opened()) {
                if (e.keyCode === 49) {
                    $ddl.select2().select2("val", "Male");
                } else if (e.keyCode === 50) {
                    $ddl.select2().select2("val", "Female");
                }
            }
        });
    });

    setInterval(function () {
        $.ajax({
            url: SVC_KEEP_ALIVE,
            global: false,
            success: function (result, textStatus, jqXHR) {
                console.log("Keep alive: " + result);
            },
            error: function (jqXHR, textStatus, errorThrown) {
                console.warn("Keep alive: " + "textStatus: " + textStatus + " | errorThrown: " + errorThrown);
            }
        });
    }, 25 * 60 * 1000);
});

jQuery(document).ajaxStart(function () {
    show_loading();
});

jQuery(document).ajaxStop(function () {
    hide_loading();
});

/// This function will reload the Main List Website on the Sidebar
function mainList_ReloadWebsite(dis_id, callback) {
    // reload 
    var html = "";
    html += "<optgroup label='Active'>";
    for (var i = 0; i < mainList_Website.length; i++) {
        var item = mainList_Website[i];
        if (item.DisId != dis_id || item.Status != 1) {
            continue;
        }
        html += "<option value=" + item.Id + ">" + item.Name + "</option>";
    }
    html += "</optgroup>";

    html += "<optgroup label='Non-active'>";
    for (var i = 0; i < mainList_Website.length; i++) {
        var item = mainList_Website[i];
        if (item.DisId != dis_id || item.Status == 1) {
            continue;
        }
        html += "<option value=" + item.Id + ">" + item.Name + "</option>";
    }
    html += "</optgroup>";

    jQuery("#MainList_Website").select2("destroy").html(html).select2();

    mainList_WebsiteChange();

    if (callback != null) {
        callback();
    }
}

function site_reload(wait) {
    if (wait == null) {
        window.location.href = window.location.href;
    }
    else {
        setTimeout(function () {
            window.location.href = window.location.href;
        }, wait);
    }
}

// Parse working time from int to hh:mm
function WorkingTimeParse(t) {
    var h = Math.floor(t / 60);
    var m = t % 60;
    var pmam = "AM";
    if (h > 11) {
        h = h - 12;
        pmam = "PM";
    }

    if (h < 10) {
        h = "0" + h;
    }
    if (m < 10) {
        m = "0" + m;
    }

    return ret = h + ":" + m + " " + pmam;
}

function getUrlParameter(_param, _url) {
    var sPageURL = window.location.search.substring(1);
    if (typeof (_url) !== "undefined") {
        var index = _url.indexOf("?");
        sPageURL = index != -1 ? _url.substring(index + 1) : "";
    }
    var sURLVariables = sPageURL.split("&");
    for (var i = 0; i < sURLVariables.length; i++) {
        var sParameterName = sURLVariables[i].split("=");
        if (sParameterName[0] == _param) {
            return sParameterName[1];
        }
    }
};

$.fn.serializeObject = function()
{
    var o = {};
    var a = this.serializeArray();
    $.each(a, function() {
        if (o[this.name] !== undefined) {
            if (!o[this.name].push) {
                o[this.name] = [o[this.name]];
            }
            o[this.name].push(this.value || '');
        } else {
            o[this.name] = this.value || '';
        }
    });
    return o;
};

if (!String.prototype.format) {
    String.prototype.format = function () {
        var args = arguments;
        return this.replace(/{(\d+)}/g, function (match, number) {
            return typeof args[number] != 'undefined'
              ? args[number]
              : match
            ;
        });
    };
}

function NewGuid() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
};

function IsNullOrEmpty(_data) {
    return _data == undefined || _data == null || _data == "";
};

function ParseTime(_data) {
    if (typeof _data !== "undefined") {
        // Template: Date object
        if (typeof _data == "object") {
            return _data;
        }
        // Template: Number object
        if (typeof _data == "number") {
            return new Date(_data);
        }
        // Template: String object {
        if (typeof _data == "string") {
            // Template: "\/Date(INTEGER)\/"
            if (/^\/Date\(\d+\)\/$/g.test(_data)) {
                var str = _data.replace(/\/Date\((-?\d+)\)\//, '$1');
                return new Date(parseInt(str));
            }
            // Template: "\/Date(INTEGER[+|-]INTEGER)\/"
            if (/^\/Date\(\d+[\+\-]\d+\)\/$/g.test(_data)) {
                var isPlus = true;
                var idx = _data.indexOf("+");
                if (/^\/Date\(\d+\-\d+\)\/$/g.test(_data)) {
                    isPlus = false;
                    idx = _data.indexOf("-");
                }
                var offset = parseInt(_data.substr(idx + 1, 2)) * 60 + parseInt(_data.substr(idx + 3, 2));
                var time = parseInt(_data.replace(/[\+\-]\d{4}/, "").replace(/\/Date\((-?\d+)\)\//, '$1'));
                return new Date(new Date(time).getTime() + ((new Date()).getTimezoneOffset() - (isPlus ? -offset : offset)) * 60 * 1000);
            }
            // Template: "yyyy-MM-ddTHH:mm:ss"
            if (/^([1-9]\d{3})-(0[1-9]|1[012])-(0[1-9]|[12][0-9]|3[01])T([01][0-9]|2[0-3]):([0-5][0-9]):([0-5][0-9])$/g.test(_data)) {
                var time = new Date(_data).getTime();
                return new Date(time + (new Date()).getTimezoneOffset() * 60 * 1000);
            }
            // Template: "MM/dd/yyyy HH:mm:ss [AM|PM]"
            if (/^([1-9]|1[012])\/([1-9]|[12][0-9]|3[01])\/([1-9]\d{3}) ([01][0-9]|2[0-3]):([0-5][0-9]):([0-5][0-9]) (AM|PM)$/.test(_data)) {
                return new Date(_data);
            }
            // Template: "MM/dd/yyyy HH:mm:ss"
            if (/^(0[1-9]|1[012])\/(0[1-9]|[12][0-9]|3[01])\/([1-9]\d{3}) ([01][0-9]|2[0-3]):([0-5][0-9]):([0-5][0-9])$/g.test(_data)) {
                var y = parseInt(_data.substr(6, 4));
                var M = parseInt(_data.substr(0, 2));
                var d = parseInt(_data.substr(3, 2));
                var H = parseInt(_data.substr(11, 2));
                var m = parseInt(_data.substr(14, 2));
                var s = parseInt(_data.substr(17, 2));
                return new Date(y, M - 1, d, H, m, s, 0);
            }
            // Template: "dd/MM/yyyy"
            if (/^(0?[1-9]|[12][0-9]|3[01])[\/](0?[1-9]|1[012])[\/]([1-9]\d{3})$/.test(_data)) {
                var arr = _data.split("/");
                var y = parseInt(arr[2]);
                var m = parseInt(arr[1]);
                var d = parseInt(arr[0]);
                var dt = new Date(y, m, 1, 0, 0, -1);
                return d > dt.getDate() ? null : new Date(y, m - 1, d, 0, 0, 0, 0);
            }
        }
    }
    return null;
};

function ConvertTime(_data) {
    if (!(typeof _data.tzOffsetOrg !== "undefined")) {
        _data["tzOffsetOrg"] = 0;
    }
    if (!(typeof _data.tzOffsetNew !== "undefined")) {
        _data["tzOffsetNew"] = 0 - (new Date()).getTimezoneOffset();
    }
    _data.dtTime = ParseTime(_data.dtTime);

    return new Date(_data.dtTime.getTime() + (_data.tzOffsetNew - _data.tzOffsetOrg) * 60 * 1000);
};

function TimeForReq(_data) {
    if (typeof _data !== "object" || _data == null) {
        return null;
    }
    var y = _data.getFullYear();
    var M = _data.getMonth() + 1;
    var d = _data.getDate();
    var H = _data.getHours();
    var m = _data.getMinutes();
    var s = _data.getSeconds();
    return (M > 9 ? M.toString() : "0" + M.toString()) +
           "/" +
           (d > 9 ? d.toString() : "0" + d.toString()) +
           "/" +
           y +
           " " +
           (H > 9 ? H.toString() : "0" + H.toString()) +
           ":" +
           (m > 9 ? m.toString() : "0" + m.toString()) +
           ":" +
           (s > 9 ? s.toString() : "0" + s.toString());
};

function SubmitWithoutAjax(_data) {
    var frm_id = NewGuid({});
    var frm_name = frm_id;
    var frm_action = typeof _data.action !== "undefined" ? _data.action : "/";
    var frm_method = typeof _data.method !== "undefined" ? _data.method : "get";
    var frm_target = typeof _data.target !== "undefined" ? _data.target : "_self";
    var frm_html = '<form id="' + frm_id + '" name="' + frm_name + '" action="' + frm_action + '" method="' + frm_method + '" target="' + frm_target + '" enctype="multipart/form-data"></form>';
    var frm_data = _data.data;

    $("body").append(frm_html);
    $frm = jQuery("form#" + frm_id);
    for (var attr in frm_data) {
        if (frm_data.hasOwnProperty(attr)) {
            if (typeof frm_data[attr] === "object" && typeof frm_data[attr].length !== "undefined") {
                frm_data[attr].forEach(function (element, index, array) {
                    var $input = $('<input type="hidden" name="' + attr + '" value="" />');
                    $input.val(element);
                    $frm.append($input);
                });
            } else {
                var $input = $('<input type="hidden" name="' + attr + '" value="" />');
                $input.val(frm_data[attr]);
                $frm.append($input);
            }
        }
    }
    $frm.submit();
    $frm.remove();
};

function ChangeProvince(_data) {
    return _data.new.length >= 2 && _data.old.substr(0, 2) != _data.new.substr(0, 2);
};

function ChangeDistrict(_data) {
    return _data.new.length >= 5 && _data.old.substr(0, 5) != _data.new.substr(0, 5);
};

function ChangeVillage(_data) {
    return _data.new.length >= 10 && _data.old.substr(0, 10) != _data.new.substr(0, 10);
};

function CheckDateOfBirth(_data) {
    var $txt_year = _data.$txt_year;
    var $txt_month = _data.$txt_month;
    var $txt_date = _data.$txt_date;

    var year = $.trim($txt_year.val());
    var month = $.trim($txt_month.val());
    var date = $.trim($txt_date.val());

    if (IsNullOrEmpty(year)) {
        notify_error("Lỗi", "Vui lòng nhập ngày sinh » Năm.");
        $txt_year.focus();
        return false;
    } else {
        // TODO: Need check range of year in MS SQL Server
        var min_year = 1000, max_year = 9999; // Are you sure?
        if (!/^([1-9]\d{3})$/.test(year) || parseInt(year) < min_year || parseInt(year) > max_year) {
            notify_error("Lỗi", "Ngày sinh » Năm không đúng định dạng.");
            $txt_year.focus();
            return false;
        }
    }

    if (!IsNullOrEmpty(date) && IsNullOrEmpty(month)) {
        notify_error("Lỗi", "Vui lòng nhập ngày sinh » Tháng.")
        $txt_month.focus();
        return false;
    }
    if (!IsNullOrEmpty(month) && !/^(0?[1-9]|1[012])$/.test(month)) {
        notify_error("Lỗi", "Ngày sinh » Tháng không đúng định dạng.")
        $txt_month.focus();
        return false;
    }

    if (!IsNullOrEmpty(date)) {
        var dt = new Date(parseInt(year), parseInt(month), 1, 0, 0, -1);
        if (!/^(0?[1-9]|[12][0-9]|3[01])$/.test(date) || parseInt(date) > dt.getDate()) {
            notify_error("Lỗi", "Ngày sinh » Ngày không đúng định dạng.")
            $txt_date.focus();
            return false;
        }
    }

    month = !IsNullOrEmpty(month) && month.length < 2 ? "0" + month : month;
    date = !IsNullOrEmpty(date) && date.length < 2 ? "0" + date : date
    $txt_year.val(year);
    $txt_month.val(month);
    $txt_date.val(date);
    return true;
};

function CheckDateOfBirth_MaLDT(_data) {
    var now = new Date();

    var y1 = parseInt(_data.year);
    var m1 = IsNullOrEmpty(_data.month) ? 1 : parseInt(_data.month);
    var d1 = IsNullOrEmpty(_data.date) ? 1 : parseInt(_data.date);
    var y2 = now.getFullYear();
    var m2 = now.getMonth() + 1;
    var d2 = now.getDate();

    var dis = y2 - y1;
    if (m2 > m1 || m2 == m1 && d2 > d1) { dis -= 1; }
    switch (_data.type) {
        case "0101":
            return dis < 4;
        case "0102":
            return dis >= 4 && dis < 16;
        case "0103":
            return dis >= 16 && dis <= 22;
        case "0201":
            return dis < 4;
        case "0202":
            return dis >= 4 && dis < 16;
        case "0203":
            return dis >= 16;
        case "0401":
            return dis >= 60 && dis < 80;
        case "0402":
            return dis >= 80;
        case "0403":
            return dis >= 80;
        case "0601":
            return dis < 16;
        default:
            return true;
    }
};

function RefillDataForSelect2(_data) {
    var $ddl = _data.$ddl, val = _data.val, data = _data.data, id = _data.id, name = _data.name;
    var frst_child = typeof _data.frst_child !== "undefined" && !_data.frst_child ? false : true;
    var opts = "";
    data.forEach(function (element, index, array) {
        opts += "<option value='" + element[id] + "'>" + element[name] + "</option>";
    });
    $ddl.children(frst_child ? ":not(:first-child)" : "").remove();
    $ddl.append(opts);
    $ddl.select2().select2("val", _data.val);
};

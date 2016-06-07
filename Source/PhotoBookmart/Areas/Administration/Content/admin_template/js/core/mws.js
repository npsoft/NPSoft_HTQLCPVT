/*
 * MWS Admin v2.1 - Core JS
 * This file is part of MWS Admin, an Admin template build for sale at ThemeForest.
 * All copyright to this file is hold by Mairel Theafila <maimairel@yahoo.com> a.k.a nagaemas on ThemeForest.
 * Last Updated:
 * December 08, 2012
 *
 */
 
(function($) {
	$(document).ready(function() {	

		// Collapsible Panels
		$( '.mws-panel.mws-collapsible' ).each(function(i, element) {
			var p = $( element ),	
				header = p.find( '.mws-panel-header' );

			if( header && header.length) {
				var btn = $('<div class="mws-collapse-button mws-inset"><span></span></div>').appendTo(header);
				$('span', btn).on( 'click', function(e) {
					var p = $( this ).parents( '.mws-panel' );
					if( p.hasClass('mws-collapsed') ) {
						p.removeClass( 'mws-collapsed' )
							.children( '.mws-panel-inner-wrap' ).hide().slideDown( 250 );
					} else {
						p.children( '.mws-panel-inner-wrap' ).slideUp( 250, function() {
							p.addClass( 'mws-collapsed' );
						});
					}
					e.preventDefault();
				});
			}

			if( !p.children( '.mws-panel-inner-wrap' ).length ) {
				p.children( ':not(.mws-panel-header)' )
					.wrapAll( $('<div></div>').addClass( 'mws-panel-inner-wrap' ) );
			}
		})
	
		/* Side dropdown menu */
		$("div#mws-navigation ul li a, div#mws-navigation ul li span")
			.on('click', function(event) {
				if(!!$(this).next('ul').length) {
					$(this).next('ul').slideToggle('fast', function() {
						$(this).toggleClass('closed');
					});
					event.preventDefault();
				}
			});
		
		/* Responsive Layout Script */
		$("#mws-nav-collapse").on('click', function(e) {
			$( '#mws-navigation > ul' ).slideToggle( 'normal', function() {
				$(this).css('display', '').parent().toggleClass('toggled');
			});
			e.preventDefault();
		});
		
		/* Form Messages */
		$(".mws-form-message").on("click", function() {
			$(this).animate({ opacity:0 }, function() {
				$(this).slideUp("normal", function() {
					$(this).css("opacity", '');
				});
			});
		});

		// Checkable Tables
		$( 'table thead th.checkbox-column :checkbox' ).on('change', function() {
			var checked = $( this ).prop( 'checked' );
			$( this ).parents('table').children('tbody').each(function(i, tbody) {
				$(tbody).find('.checkbox-column').each(function(j, cb) {
					$( ':checkbox', $(cb) ).prop( "checked", checked ).trigger('change');
				});
			});
		});

		// Bootstrap Dropdown Workaround
		$(document).on('touchstart.dropdown.data-api', '.dropdown-menu', function (e) { e.stopPropagation() });
		
		/* File Input Styling */
		$.fn.fileInput && $("input[type='file']").fileInput();

		// Placeholders
		$.fn.placeholder && $('[placeholder]').placeholder();

		// Tooltips
		$.fn.tooltip && $('[rel="tooltip"]').tooltip();

		// Popovers
		$.fn.popover && $('[rel="popover"]').popover();

		/* Chosen Select Box Plugin */
		if ($.fn.select2) {
		    $("select.mws-select2").select2();
		}

		$.fn.iButton && $('.ibutton').iButton();

		// AutoSize
		$.fn.autosize && $('.autosize').autosize();

		/* Chosen Select Box Plugin */
		if ($.fn.select2) {
		    $("select.mws-select2").select2();
		}

		// jQuery-UI Accordion
		$.fn.accordion && $(".mws-accordion").accordion();

		// jQuery-UI Tabs
		$.fn.tabs && $(".mws-tabs").tabs();


		if ($.fn.datepicker) {
		    $(".mws-datepicker").datepicker({
		        showOtherMonths: true
		    });

		    $(".mws-datepicker-wk").datepicker({
		        showOtherMonths: true,
		        showWeek: true
		    });

		    $(".mws-datepicker-mm").datepicker({
		        showOtherMonths: true,
		        numberOfMonths: 3
		    });

		    $("#mws-datepicker-from").datepicker({
		        defaultDate: "+1w",
		        changeMonth: true,
		        numberOfMonths: 3,
		        showOtherMonths: true,
		        onSelect: function (selectedDate) {
		            $("#mws-datepicker-to").datepicker("option", "minDate", selectedDate);
		        }
		    });
		    $("#mws-datepicker-to").datepicker({
		        defaultDate: "+1w",
		        changeMonth: true,
		        numberOfMonths: 3,
		        showOtherMonths: true,
		        onSelect: function (selectedDate) {
		            $("#mws-datepicker-from").datepicker("option", "maxDate", selectedDate);
		        }
		    });

		    if ($.fn.timepicker) {
		        $(".mws-dtpicker").datetimepicker();

		        $(".mws-tpicker").timepicker({});
		    }
		}

		if ($.fn.timepicker) {
		    $(".mws-dtpicker").datetimepicker();

		    $(".mws-tpicker").timepicker({});
		}
	});
})(jQuery);

function show_loading() {
    jQuery(".loading-box").fadeIn();
}

function hide_loading() {
    jQuery(".loading-box").fadeOut();
}
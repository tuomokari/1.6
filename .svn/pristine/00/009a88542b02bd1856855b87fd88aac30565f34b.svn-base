/**
 * horizontalworkview widget
 */
$.widget("horizontalworkview.horizontalworkview", $.core.base, {
    options: {
    },

    /**
     * Fired by jQuery when the widget is created.
     */
    _initWidget: function () {
    	this._uiState = new Object;
    	this._model = new Object();
    	this._uiState.firstRender = true;
    	this._model.workEntries = new Array();

    	this._dataSource = widgets[this.options.datasource];

    	this._setupEvents();
    },
    _setupEvents: function() {
        var base = this;

        $(this._dataSource.element).on("datarefreshed", function () {
            base.render();
        });

        $(this._dataSource.element).on("selecteddatechanged", function () {
            base.render({ selectionOnly: true });
        });

        $(this._dataSource.element).on("scrollrequest", function () {
            base.scroll.scrollToElement($(base.element).find(".active")[0], null, -50);
        });
    },
    /**
     * Refresh all data in the model from the server.
     * 
     * @param (bool) renderData True if data should be rendered after refreshing it.
     * 
     * @note: You should not do any dom manipulation here. All DOM manipulation needs
     *        to go to the render method.
     */
    refreshData: function (renderData) {

    },

    /**
    * Render data in the model and settings into view.
    * 
    * @note: should not make any changes into model or settings
    */
    render: function (options) {

    	if (!options)
    		options = {};

    	if (this._uiState.firstRender) {

    		this._renderDays(options);
    		this.element.removeClass("__hidden");

    		var base = this;
    		base.scroll = new IScroll(base.element.parent().get()[0], {
    		    scrollX: true,
    		    scrollY: false,
    		    deceleration: 0.005
    		});
            
    		base.scroll.on('scrollEnd', function () {
    		    base.element.removeClass("__disabled");
    		});

    		base.scroll.on('scrollStart', function () {
    			base.element.addClass("__disabled");
    		});

    	    // initially set scrollposition todo: revise
    		base.scroll.scrollToElement($(base.element).find(".active")[0], null, -50);

    		this._uiState.firstRender = false;
    	}
    	else
    	{
    		this._renderDays(options);
    	}
		
    },
    _renderDays: function (options) {
        var base = this;
    	var days = this._dataSource._getDaysArray();

    	if (this._uiState.firstRender) {
    		for (var i = 0; i < days.length; i++) {
    			var day = days[i];

    			var isFutureDay = (moment(day.date) > moment().add(1, "days").startOf("day")) ? true : false;

    			var card = this._createWorkCard()
    			card.setHours(day.totalAllHours);
    			card.setType(this._determineDayType(day));
    			card.setOvertime(day.overtime50Hours, day.overtime100Hours, day.overtime150Hours, day.overtime200Hours);
    			card.setExtras(day.extras);
    			card.setDate(day.date);
    			card.setExpensesIcon(day.dayEntries.length > 0);
    			card.setAbsencesIcon(day.absenceEntries.length > 0);
    			card.setAssetsIcon(day.assetEntries.length > 0);
    			card.setArticlesIcon(day.articleEntries.length > 0);
    			card.setAllocationsIcon(day.allocationEntries.length > 0 && isFutureDay);
    			this.element.append(card.element);
    			this._model.workEntries.push(card);

    		    // add weekstart indicator
    			if (day.date.getDay() == 1)
    			    $(card.element).before("<div class='weekstart'></div>");
    		}
    	} else if (!options.selectionOnly)
    	{
			// Update card values
    		for (var i = 0; i < days.length; i++) {
    			var day = days[i];

    			var card = this._model.workEntries[i];
    			card.setHours(day.totalAllHours);
    			card.setType(this._determineDayType(day));
    			card.setOvertime(day.overtime50Hours, day.overtime100Hours, day.overtime150Hours, day.overtime200Hours);
    			card.setExtras(day.extras);
    			card.setDate(day.date);
    			card.setExpensesIcon(day.dayEntries.length > 0);
    			card.setAbsencesIcon(day.absenceEntries.length > 0);
    			card.setAssetsIcon(day.assetEntries.length > 0);
    			card.setArticlesIcon(day.articleEntries.length > 0);
    			card.setAllocationsIcon(day.allocationEntries.length > 0);
			}
    	}

    	// Set selected entry
    	for (var i = 0; i < this._model.workEntries.length; i++) {
    		var card = this._model.workEntries[i];

    		if (card.dateKey == this._dataSource._model.selectedDateKey) {
    		    $(card.element).addClass("active");
    		}
    		else {
    		    $(card.element).removeClass("active");
    		}
    	}
    },
    _determineDayType: function(day) {

    	var type = "noentries";

    	if (day.hasUnapprovedEntries) {
			type = "unapproved"
    	}
    	else if (day.hasApprovedEntries) {
    		type = "approved";
    	}

    	if (day.isToday)
    		type += " today";
		
    	return type;
    },
    _createWorkCard: function () {
		var base = this;

		var workCardTemplate = $(".horizontalworkview_templates .horizontalworkview_entry_template");
		var workCardElement = workCardTemplate.clone().removeClass("__widgettemplate");

		var workCard = {
			element: workCardElement,
			setDate: function (date) {
			    $(this.element).attr("data-date", txt("day_abbreviated_" + date.getDay()) + " " + formatDateShort(date));
				workCard.dateKey = base._dataSource.getKeyFromDate(date);
			},
			setHours: function (hours) {
			    if (hours != "0") {
			    	$(this.element).find(".time").removeClass("__hidden");
			    	var hoursString = hours.toString();
			        if (hoursString.indexOf(".") != -1) {
			            var decimal = hoursString.substr(hoursString.indexOf("."));
			            $(this.element).find(".decimal").text(decimal);
			            hours = hoursString.substr(0, hoursString.indexOf("."));
			        }
			        $(this.element).find(".hours").text(hours);
			        $(this.element).find(".unit").text(txt("hours_unit", "horizontalworkview"));
			    }
			    else {
			        $(this.element).find(".time").addClass("__hidden");
			    }
			},
			setOvertime: function (overtime50, overtime100, overtime150, overtime200) {
				var hoursUnit = txt("hours_unit", "horizontalworkview");

				var overtimeText = "";
				if (overtime200 > 0)
					overtimeText = "200%: " + overtime200 + hoursUnit + " ";

				if (overtime150 > 0)
					overtimeText += "150%: " + overtime150 + hoursUnit + " ";

				if (overtime100 > 0)
					overtimeText += "100%: " + overtime100 + hoursUnit + " ";

				if (overtime50 > 0)
					overtimeText += "50%: " + overtime50 + hoursUnit + " ";

				$(this.element).find(".overtime").text(overtimeText.trim());
			},
			setExpensesIcon: function(enabled) {
				if (enabled)
					$(this.element).find(".expensesicon").show();
				else
					$(this.element).find(".expensesicon").hide();
			},
			setAbsencesIcon: function (enabled) {
				if (enabled)
					$(this.element).find(".absencesicon").show();
				else
					$(this.element).find(".absencesicon").hide();
			},
			setArticlesIcon: function (enabled) {
				if (enabled)
					$(this.element).find(".articlesicon").show();
				else
					$(this.element).find(".articlesicon").hide();
			},
			setAllocationsIcon: function (enabled) {
				if (enabled)
					$(this.element).find(".allocationsicon").show();
				else
					$(this.element).find(".allocationsicon").hide();
			},
			setAssetsIcon: function (enabled) {
				if (enabled)
					$(this.element).find(".assetsicon").show();
				else
					$(this.element).find(".assetsicon").hide();
			},
			setExtras: function (extras) {
				if (!extras) {
					return;
				}

				var extrasHtml = "";

				for (var i = 0; i < extras.length; i++) {
					var extra = extras[i];

					if (i > 0)
						extrasHtml += " ";

					if (extra.substr(0, "[icon]".length) == "[icon]") {
						var icon = extra.substr("[icon]".length);
						extrasHtml += "<i class='material-icons'>" + icon + "</i>";
					} else {
						extrasHtml += extra;
					}
				}

				$(this.element).find(".extras").html(extrasHtml);
			},
			setType: function (type) {
				if (typeof type === "undefined")
					type = "noentries";

				$(this.element).removeClass("approved");
				$(this.element).removeClass("unapproved");
				$(this.element).removeClass("noentries");
				$(this.element).removeClass("today");

				$(this.element).addClass(type);
			}
		};

		workCardElement.on("click tap", function (e) {
			e.stopPropagation();
        	base._dataSource.setSelectedDateKey(workCard.dateKey);
        });

        return workCard;
    }
});
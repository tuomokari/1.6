/**
 * Single-page logic for timesheetentry, absence and expense(dayentry) types
 */
$.widget("timesheetentrypaytypewidget.timesheetentrypaytypewidget", $.core.base, {
    options: {
    },

    _create: function () {
        this._uiState = new Object;
        this._model = new Object();

        this._uiState.firstRender = true;
    },
    _initWidget: function () {
        // Base pay is timesheetentry without a parent. Determine from the form or view whether selected document has parent or not.
        if (!$("[name='parent']").val() && $("[data-propertyschemaname='parent']").children(0).text() == "")
            this._model.isBasePay = true;

        if (this.options.viewtype == "input") {
        	this._setupWidgetEvents();
        	$(".form").first().hide();
        }
        else if (this.options.viewtype == "display") {
        	this.render();
        }

		// Duration value to store when duration selector is hidden
        this._uiState.lastSelectedDuration = "0";
    },
    _hideProjectSelection: function () {
        $("div[data-name='project'].__searchfilter-wrapper").addClass("__hidden");
    },
    refreshData: function (renderData) {

        if (this.options.viewtype != "input")
            return;

        var promises = [];
        var base = this;

        var restApi = new RestApi();

        var timesheetEntryDisabled = true;
        var dayEntryTypedDsabled = true;
        var absenceDisabled = true;

        if ($("select[name='timesheetentrydetailpaytype']").length > 0)
            timesheetEntryDisabled = false;
        else if ($("select[name='dayentrytype']").length > 0)
            dayEntryTypedDsabled = false;
        else if ($("select[name='absenceentrytype']").length > 0)
            absenceDisabled = false;

        var projectId = $("input[name='project']").val();
		
		// Use placehodler projectid if no project is available.
        if (!projectId)
			projectId = "000000000000000000000000";

        // Base data
        var baseDataPromise = restApi.find("timesheetentrypaytypewidget", "clacontractsandpaytypes",
		{
		    initialproject: new ObjectId(projectId),
		    timesheetentrydisabled: timesheetEntryDisabled,
		    projectcategorydisabled: timesheetEntryDisabled && dayEntryTypedDsabled,
		    dayentrytypedisabled: dayEntryTypedDsabled,
		    absencedisabled: absenceDisabled
		});

        promises.push(baseDataPromise);

        baseDataPromise.done(function (data) {
            base._model.clacontracts = data.clacontract;

            if ($("select[name='timesheetentrydetailpaytype']").length > 0)
                base._model.paytypes = data.timesheetentrydetailpaytype;
            else if ($("select[name='dayentrytype']").length > 0)
                base._model.paytypes = data.dayentrytype;
            else if ($("select[name='absenceentrytype']").length > 0)
            	base._model.paytypes = data.absenceentrytype;

            base._model.payTypesById = {};
            for (var i = 0; i < base._model.paytypes.length; i++) {
            	var payType = base._model.paytypes[i];
            	base._model.payTypesById[payType._id] = payType;
            }

            if ($("select[name='projectcategory']").length > 0)
                base._model.projectcategories = data.projectcategory;

            if (features.enablesocialproject && data.initialproject && data.initialproject.length > 0 && data.initialproject[0].projecttype == "__socialproject") {
                base._model.isSocialProjectEntry = true;

                // Hide project selection for social projects. Social projects should never be changed to any other type.
                base._hideProjectSelection();

                // Show different title for social projects
                $(".__titlepanel-title").first().text(txt("addsocialentry", "timesheetentrypaytypewidget"));
            }
        })

        // Initial user
        var userId = $("input[name='user']").val();

        // In case user selection is not available use the current user
        if (isEmpty(userId)) {
            userId = $("body").data("user");
        }

    	// Initial project
    	// Currently project data is only required by project category default.
        if (features.useprojectscategoryasdefault && features.usedirectprojectcategory)
        	promises.push(this._refreshProjectData());


        var initialUserPromise = restApi.getDocument("user", userId).done(function (userData) {
            base._model.selectedUser = userData;
        });

        promises.push(initialUserPromise);

        $.when.apply($, promises).done(function () {
            base._model.datareceived = true;

            if (renderData) {
                base.render();
            }
        });
    },
    _refreshProjectData: function () {
        var base = this;

        var projectSearchFilter = $("[data-name='project']");
        var projectId = $("#" + projectSearchFilter.attr("targetid")).val();

        var restApi = new RestApi();
        if (isEmpty(projectId)) {
            return $.Deferred(function (def) { def.resolve(); }).promise();
        } else {
            return restApi.getDocument("project", projectId).done(function (project) {
                base._model.selectedProject = project;
            });
        }
    },
    _setupWidgetEvents: function () {
        var userSearchFilter = $("[data-name='user']");

        var userId = $("#" + $(this).attr("targetid")).val();

        var base = this;
        // Dropdown populated
        $("select[name='timesheetentrydetailpaytype']").on("dropdownupdated", function () {
        	base.render()
        });

        // Timesheet entries
        $("select[name='timesheetentrydetailpaytype']").change(function () {
            if (base._model.datareceived)
                base.render();
        });

        // Absence entries
        $("select[name='absenceentrytype']").change(function () {
            if (base._model.datareceived)
                base.render();
        });

        // Daily entries
        $("select[name='dayentrytype']").change(function () {
            if (base._model.datareceived)
                base.render();
        });

        // Project
        var projectSearchFilter = $("[data-name='project']");
        projectSearchFilter.on("resolved", function () {

			// Save selected project as default project in session storage. We can then default to the same project later.
        	sessionStorage.setItem($("body").attr("data-user") + "_defaultproject", $("[name='project']").val());

        	// Currently project data is only required by project category default.
        	if (features.useprojectscategoryasdefault && features.usedirectprojectcategory) {
        		base._refreshProjectData().done(function () {
        			base.render();
        		});
        	}

        });

        $("input[name='differentdurationforbilling']").change(function () {
            if (base._model.datareceived) {
                base.render();
            }
        });

        $("input[name='hasprice']").change(function () {
            if (base._model.datareceived) {
                base.render();
            }
        });

        userSearchFilter.on("resolved", function () {

            var userId = $("#" + $(this).attr("targetid")).val();

            var restApi = new RestApi();
            restApi.getDocument("user", userId).done(function (userData) {
                base._model.selectedUser = userData;

                if (base._model.datareceived)
                    base.render();
            });
        });
        
        // validation
        $(this.element).parents("form").first().on("submit", function (e) {
            var formElement = this;
            // paytype required
            var valueFound = false;
            var controlElement = null;

            $("select[name='absenceentrytype'], select[name='dayentrytype'], select[name='timesheetentrydetailpaytype']").each(function () {
                if ($(this).is(":hidden"))
                    return;
                controlElement = $(this).parent();
                if ($(this).val()) {
                    valueFound = true;
                }
                if (valueFound || !controlElement) {
                    $(formElement).off("submit");
                    formElement.submit();
                    e.preventDefault();
                    return false;
                }
                else {
                    e.preventDefault();
                    if (!controlElement.next().hasClass("__form_validatemessage"))
                        controlElement.after("<div class='__form_validatemessage __clientgenerated'>" + txt("validate_required") + "</div>");
                    return false;
                }
            });

            var payTypeSelect = base._getPayTypeSelect();

        	// Update paytype favourites
            var favouritesData = getRelationDropdownFavourites(payTypeSelect[0]);

            var favouriteName = $(payTypeSelect).val();
            var displayName = $(payTypeSelect).find("option:selected").text();
            favouritesData.addFavourite(favouriteName, displayName);
        });
    },
    _getPayTypeSelect: function() {
    	var payTypeSelect = $("select[name='timesheetentrydetailpaytype']");
    	if (payTypeSelect.length == 0)
    		payTypeSelect = $("select[name='dayentrytype']");
    	if (payTypeSelect.length == 0)
    		payTypeSelect = $("select[name='absenceentrytype']");

    	return payTypeSelect;
    },

    /**
    * Render data in the model and settings into view.
    * 
    * @note: should not make any changes into model or settings
    */
    render: function () {
        this.element.removeClass("__hidden");

        if (this._uiState.firstRender)
            this._initialOperations();

        this._filterPayTypes();
        this._setDefaultPayType();
        this._refreshProjectCategoryVisibility();
        this._filterProjectCategories();
        this._refreshDurationVisibility();
        this._refreshBillingDurationVisibility(this._uiState.firstRender);
        this._refreshCustomPriceVisibility();
        this._refreshAmountVisibility();
        this._refreshStartAndEndTimeVisibility();
        this._refreshNoteMandatory();
        this._refreshUserVisibility();
        this._uiState.firstRender = false;

    },
    _setDefaultPayType: function() {
    	var select = this._getPayTypeSelect();

		// Only set default value if selection is empty.
    	if (!select.val()) {
    		$(select).val(select.find("option:first").val());
    	}
    },
    /**
	 * Operations that only need to be set up once at the start
	 */
    _initialOperations: function () {
    	$("form").first().show();

        if (this.options.viewtype == "input") {
            // Base pay relation describes relation to base timesheetentry for timetracked entries. Always hidden but
            // data is needed.
            $("[data-name='parent']").hide()

            // If workers can only use projects allocated to them, no project selection should be shown.
            if (!features.workerscanuseunallocatedprojects && (parseInt($("body").data("userlevel")) == 1 || parseInt($("body").data("userlevel")) == 6))
                this._hideProjectSelection();

            if (features.timetracking) {
            	// Check whether this is a child entry. Either there is a parent (when creating a detail) or we
				// are showing the parent as a relation (when editing a detail)
            	if ($("[name='parent']").val()) {
                    // This is a child entry and timestamps are hidden.
                    $("[name='starttimestamp']").parent().hide();
                    $("[name='endtimestamp']").parent().hide();
                    $("[name='istraveltime']").parent().hide();
                    $("[name='istraveltime']").parent().hide();
					// Hide traveltime tooltip
                    $("[name='istraveltime']").parent().next().hide();

                    // Project is hidden for child entry. It's taken from parent.
                    $("[data-name='project']").hide();

                    // Add placeholder value to clear validation. Will be replaced by parent's project.
                    $("[name='project']").val("000000000000000000000000");
                    $("[data-propertyschemaname='parent']").parent().hide();

                } else {
                    // This is a base item and duration is not shown.
                    $("[name='duration']").parent().hide();
                }
            }
        }
        if (this.options.viewtype == "display") {

            // Hide child entries from children. Only one level is allowed
            if (!this._model.isBasePay) {
            	$(".__externalrelation_timesheetentry").hide();
            }

            $("[data-propertyschemaname='parent']").parent().hide();

        	// Hide price information if no price is set
            if ($("[data-propertyschemaname='hasprice']").find("span").attr("data-boolvalue") != "true") {
            	$("[data-propertyschemaname='hasprice']").parents("tr").first().hide();
            	$("[data-propertyschemaname='price']").parents("tr").first().hide();
            }
        }
    },
	/*
	 *  Show or hide duration according to selected paytype
	 */
    _refreshDurationVisibility: function () {
    	if ($("input[name='duration']").length == 0)
    		return;

    	var selectedPayTypeId = $("select[name='timesheetentrydetailpaytype']").val();

    	if (!selectedPayTypeId)
    		return;

    	var selectedPayType = this._model.payTypesById[selectedPayTypeId];

    	var duration = $("input[name='duration']");
    	if (selectedPayType.noduration) {
    		this._uiState.lastSelectedDuration = duration.val();
    		duration.val("0");
    		createTimespan(duration.parent().get()[0]);
    		duration.parent().hide();
    	}else
    	{
    		if (this._uiState.lastSelectedDuration != "0") {
    			duration.val(this._uiState.lastSelectedDuration);
    			createTimespan(duration.parent().get()[0]);
    		}

    		duration.parent().show();
		}
    },
	/*
	 *  Show or hide billing duration according to pay type and user selection.
	 */
    _refreshBillingDurationVisibility: function (initial) {
        if ($("input[name='billingduration']").length == 0)
            return;

        // No billing duration for social project.
        if (this._model.isSocialProjectEntry) {
            if (initial) {
                $("input[name='differentdurationforbilling']").parent().addClass("__hidden");
                $("input[name='billingduration']").parent().addClass("__hidden");
            }

            return;
        }

        var base = this;
        if ($("input[name='differentdurationforbilling']").prop("checked")) {
            $("input[name='billingduration']").parent().show();

            // Start with equal values.
            if (initial !== true) {
                $("input[name='billingduration']").val(
                    $("input[name='duration']").val()
                    );

                createTimespan($("input[name='billingduration']").parent().get()[0]);
            }

        } else {
            $("input[name='billingduration']").parent().hide();
            $("input[name='billingduration']").val("");

            createTimespan($("input[name='billingduration']").parent().get()[0]);
        }
    },
    _refreshCustomPriceVisibility: function (initial) {
        if ($("input[name='price']").length == 0 || !this._model.paytypes)
            return;

        var userLevel = parseInt($("body").data("userlevel"));

        // Set the whole item's visiblity based on selected pay type. Try getting timesheetentrydetailpaytype
        // first and dayentrytype if that is not found.
        var selectedPayTypeId = $("select[name='timesheetentrydetailpaytype']").val();

        if (isEmpty(selectedPayTypeId))
            selectedPayTypeId = $("select[name='dayentrytype']").val();

        var controlHidden = true;
        var priceOptional = false;

        for (var i = 0; i < this._model.paytypes.length; i++) {
            var payType = this._model.paytypes[i];

            if (payType._id === selectedPayTypeId) {
                if (payType.hasprice && (!payType.priceonlyformanagers || userLevel > 2)) {
                    controlHidden = false;
                }
                if (payType.priceoptional)
                    priceOptional = true;
            }
        }

        if (!controlHidden) {
            $("input[name='price']").parents(".__control").removeClass("__hidden");

            if (priceOptional) {
                // Set price's visibility based on checkbox
                var base = this;
                $("input[name='hasprice']").parents(".__control").removeClass("__hidden");
                if ($("input[name='hasprice']").prop("checked")) {
                    $("input[name='price']").parents(".__control").removeClass("__hidden");
                } else {
                    $("input[name='price']").parents(".__control").addClass("__hidden");
                    $("input[name='price']").val(0);
                    $("input[name='__displayvalue_price']").val(0);
                }
            }
            else
                $("input[name='hasprice']").prop('checked', true).parents(".__control");
        } else {
            $("input[name='price']").parents(".__control").addClass("__hidden");
            $("input[name='hasprice']").parents(".__control").addClass("__hidden");
            $("input[name='hasprice']").prop('checked', false);
            $("input[name='price']").val(0);
            $("input[name='__displayvalue_price']").val(0);
        }
    },
    /**
	 * Show "amount" field for dayentries where value is not fiexed to default.
	 */
    _refreshAmountVisibility: function (initial) {
        if ($("input[name='amount']").length == 0 || !this._model.paytypes)
            return;

        // Set the whole item's visiblity based on selected pay type. Try getting timesheetentrydetailpaytype
        // first and dayentrytype if that is not found.
        var selectedPayTypeId = $("select[name='timesheetentrydetailpaytype']").val();

        if (isEmpty(selectedPayTypeId))
            selectedPayTypeId = $("select[name='dayentrytype']").val();

        var controlHidden = false;

        for (var i = 0; i < this._model.paytypes.length; i++) {
            var payType = this._model.paytypes[i];

            if (payType._id === selectedPayTypeId) {
                if (!isEmpty(payType.noamountselection) && payType.noamountselection) {
                    controlHidden = true;
                }
            }
        }

        if (!controlHidden) {
            $("input[name='amount']").parents(".__control").removeClass("__hidden");
        } else {
            $("input[name='amount']").parents(".__control").addClass("__hidden");
            $("input[name='amount']").val(1);
        }
    },
    /**
     * Show payment types according to selected user's CLA contract
     */
    _filterPayTypes: function () {
        if (features.usedefaultbasepaytype && this._model.isBasePay) {
            $("[name='timesheetentrydetailpaytype']").parent().addClass("__hidden");
        }

        if (!this._model.paytypes)
            return;

        // Use [none] to prevent matches with empty strings
        var personClaContract = "[none]";
        if (!isEmpty(this._model.selectedUser.clacontract))
            personClaContract = this._model.selectedUser.clacontract._id;

        // Clear and repopulate paytype selection
        var payTypeSelect = this._getPayTypeSelect();

        var originalValue = payTypeSelect.val();

        if (originalValue === null) {
            var extraInfoName = payTypeSelect.attr("name") + "_hiddenfield";
            var extraInfo = $("#" + extraInfoName);
            var collection = extraInfo.attr("data-collection");
            originalValue = extraInfo.attr("data-value");
        }

        payTypeSelect.html("");

    	// Add favourites
        var favouritesData = getRelationDropdownFavourites(payTypeSelect[0]);
        if (favouritesData) {

        	var optionGroup = $("<optgroup label='" + txt("recent") + "'></optgroup>");

        	var favouritesFound = false;

        	for (var i = 0; favouritesData.favourites && i < favouritesData.favourites.length; i++) {
        		var favourite = favouritesData.favourites[i];

        		if (!this._model.payTypesById[favourite.name])
        			continue;

        		var item = this._model.payTypesById[favourite.name];

        		var option =
					$("<option></option>")
						.attr("value", item._id)
						.text(item.name);

        		optionGroup.append(option);
        		favouritesFound = true;
        	}

        	if (favouritesFound)
        		$(payTypeSelect).append(optionGroup);
        }


        for (var i = 0; i < this._model.paytypes.length; i++) {

            var payType = this._model.paytypes[i];

            var claContractFound = false;

            if (payType.enabledclacontracts !== undefined) {
                for (var j = 0; j < payType.enabledclacontracts.length; j++) {
                    if (payType.enabledclacontracts[j]._id === personClaContract) {
                        claContractFound = true;
                        break;
                    }
                }
            }

            var socialProjectStatusOk = true;

            if (features.enablesocialproject) {
                // Filter only social bookings for social projects and no social projects for other bookings

                if (this._model.isSocialProjectEntry) {
                    if (payType.issocialpaytype !== true) {
                        socialProjectStatusOk = false;
                    }
                } else {
                    if (payType.issocialpaytype === true) {
                        socialProjectStatusOk = false;
                    }
                }
            }

            if (claContractFound && socialProjectStatusOk)
                payTypeSelect.append($("<option value='" + payType._id + "'>" + payType.name + "</option>")
                    .data("countsasregularhours", payType.countsasregularhours)
                    .data("isovertime50", payType.isovertime50)
                    .data("isovertime100", payType.isovertime100)
                    .data("isovertime150", payType.isovertime150)
                    .data("isovertime200", payType.isovertime200)
                    .data("issocialpaytype", payType.issocialpaytype)
                );
        }

        payTypeSelect.val(originalValue);

        // make sure there are no favorites that are not found in the available options
        var favourites = payTypeSelect.children("optgroup");
        favourites.children().each(function () {
            var favouriteElement = $(this);
            var favouriteElementValue = favouriteElement.val();
            var existsInResults = payTypeSelect.children("[value='" + favouriteElementValue + "']").length > 0;

            if (!existsInResults)
                favouriteElement.remove();
        });
        if (favourites.children().length == 0)
            favourites.remove();

        // show social paytypes for base entry if they exist
        if (this._model.isSocialProjectEntry && payTypeSelect.children().length > 0)
            $("[name='timesheetentrydetailpaytype']").parent().removeClass("__hidden");

    },
    /**
     * Show project categories according to selected pay type
     */
    _filterProjectCategories: function () {

        if (!this._model.projectcategories)
            return;

        // Get the selected PayType
        var payTypeSelect = this._getPayTypeSelect();

        var selectedPayTypeElement = payTypeSelect.children("[value='" + payTypeSelect.val() + "']");

        // Clear and repopulate project category selection
        var projectCategorySelect = $("select[name='projectcategory']");

        var originalValue = projectCategorySelect.val();

        if (originalValue === null) {
            var extraInfoName = projectCategorySelect.attr("name") + "_hiddenfield";
            var extraInfo = $("#" + extraInfoName);
            var collection = extraInfo.attr("data-collection");
            originalValue = extraInfo.attr("data-value");
        }

        projectCategorySelect.html("");

        var projectCategorySelectHtml = "";

        for (var i = 0; i < this._model.projectcategories.length; i++) {
            // compare the projectCategory against selected PayType
            if (selectedPayTypeElement.length != -1) {
                if (!compareProjectCategoryToPayType(selectedPayTypeElement, this._model.projectcategories[i])) {
                    continue;
                }
            }

            var projectCategory = this._model.projectcategories[i];

            projectCategorySelectHtml += "<option value='" + projectCategory._id +
				"' data-isovertime50='" + !!projectCategory.isovertime50 +
				"' data-isovertime100='" + !!projectCategory.isovertime100 +
				"' data-isovertime150='" + !!projectCategory.isovertime150 +
				"' data-isovertime200='" + !!projectCategory.isovertime200 +
				"'>" + projectCategory.identifier + " " + projectCategory.name + "</option>";
        }

        projectCategorySelect.html(projectCategorySelectHtml);

        projectCategorySelect.val(originalValue);

        function compareProjectCategoryToPayType(payTypeElement, projectCategory) {
            var returnValue = false;
            if ((!payTypeElement.data("isovertime50") && !payTypeElement.data("isovertime100") && !payTypeElement.data("isovertime150") && !payTypeElement.data("isovertime200")) &&
                (!projectCategory.isovertime50 && !projectCategory.isovertime100 && !projectCategory.isovertime150 && !projectCategory.isovertime200)) {
                return true;
            }
            else {
                if (payTypeElement.data("isovertime50") === true && projectCategory.isovertime50 === true)
                    returnValue = true;
                else if (payTypeElement.data("isovertime100") === true && projectCategory.isovertime100 === true)
                    returnValue = true;
                else if (payTypeElement.data("isovertime150") === true && projectCategory.isovertime150 === true)
                    returnValue = true;
                else if (payTypeElement.data("isovertime200") === true && projectCategory.isovertime200 === true)
                    returnValue = true;
            }
            return returnValue;
        }
    },
    /*
	 * Refresh visibility of the time (not the timestamp) of booking. This is relevant for daily entries that have time selection enabled.
	 */
    _refreshStartAndEndTimeVisibility: function () {
        var payTypeSelect = $("select[name='dayentrytype']");

        if (payTypeSelect.length == 0 || !this._model.paytypes)
            return;

        selectedPayTypeId = payTypeSelect.val();

        var selectedPayType = null;
        for (var i = 0; i < this._model.paytypes.length; i++) {
            var payType = this._model.paytypes[i];

            if (payType._id === selectedPayTypeId) {
                selectedPayType = payType;
                break;
            }
        }

        if (selectedPayType == null || !selectedPayType.hastime) {
            $("input[name='starttime']").parent().addClass("__hidden");
            $("input[name='endtime']").parent().addClass("__hidden");
            $("input[name='requiretime']").val("");
            $("input[name='starttime']").val("");
            $("input[name='endtime']").val("");
        } else {
            $("input[name='starttime']").parent().removeClass("__hidden");
            $("input[name='endtime']").parent().removeClass("__hidden");
            $("input[name='requiretime']").val("true");
        }

        if (this._uiState.firstRender == true) {
            var label = $("input[name='starttime']").parents(".__control").find(".__required-asterisk").removeClass("__hidden");
            var label = $("input[name='endtime']").parents(".__control").find(".__required-asterisk").removeClass("__hidden");
        }
    },

    /*
	 * Checks if paytype needs a note for an entry
	 */
    _refreshNoteMandatory: function () {
        var selectedPayTypeId = $("select[name='timesheetentrydetailpaytype']").val();

        if (isEmpty(selectedPayTypeId)) {
            selectedPayTypeId = $("select[name='dayentrytype']").val();
            if (isEmpty(selectedPayTypeId))
                return;
        }

        var mandatoryNote = false;

        for (var i = 0; i < this._model.paytypes.length; i++) {
            var payType = this._model.paytypes[i];

            if (payType._id === selectedPayTypeId) {
                if (payType.mandatorynote)
                    mandatoryNote = true;
                break;
            }
        }
        $("input[name='note']").removeAttr('required');

        var mylabel = $("input[name='note']").parent().find('label');
        mylabel.children(1).remove()
        mylabel.children(0).remove()
        if (mandatoryNote)
        {
            $("input[name='note']").parent().find('label').first().append('<span class="__required-asterisk ">*</span>');
            $("input[name='note']").attr('required', '');
        }

    },
    _refreshUserVisibility: function () {
		// Hide user selection for details.
    	if (!this._model.isBasePay) {
    		$("input[name='__searchfilterinput-user']").parents(".__control").first().hide()
    	}
    },
    /*
	 * Displays project cateogry selection if selected paytype doesn't contain project category.
	 * Sets default value for project category if required.
	 */
    _refreshProjectCategoryVisibility: function () {
        var payTypeSelect = $("select[name='timesheetentrydetailpaytype']");
        if (!this._model.paytypes)
            return;

        if (payTypeSelect.length == 0)
            payTypeSelect = $("select[name='dayentrytype']");

        var projectCategorySelect = $("select[name='projectcategory']");
        var selectedPayTypeId = payTypeSelect.val();

        if (this._uiState.firstRender == true)
            var label = projectCategorySelect.parents(".__control").find(".__required-asterisk").removeClass("__hidden");

        var projectCategoryHidden = true;
        var projectCategory;

        for (var i = 0; i < this._model.paytypes.length; i++) {
            var payType = this._model.paytypes[i];

            if (payType._id === selectedPayTypeId) {

                if (isEmpty(payType.projectcategory)) {
                    if (payType.exporttoax || payType.exporttoerp) {
                        projectCategoryHidden = false;
                    }
                    else {
                        // Project category doesn't exist for the selected paytype and it's not exported to
                        // ERP. No selection for projectCategory should be shown and default set to empty.
                        $("select[name='projectcategory']").val("");
                    }
                }
                else {
                    projectCategory = payType.projectcategory._id;
                }

                break;
            }
        }

        // If no default project category was found, use project's project category as a default
        if (features.useprojectscategoryasdefault && features.usedirectprojectcategory &&
			!projectCategoryHidden && !isEmpty(this._model.selectedProject) &&
			!isEmpty(this._model.selectedProject.projectcategory)) {

            projectCategory = this._model.selectedProject.projectcategory._id;
        }

        // If no project category is selected or no choice is possible, use the default value if it exists
        if ((isEmpty(projectCategorySelect.val()) || projectCategoryHidden)
			&& !isEmpty(projectCategory)) {
            projectCategorySelect.val(projectCategory);
        }

        if (projectCategoryHidden || !features.usedirectprojectcategory) {
            projectCategorySelect.parents(".__control").addClass("__hidden");

            $("input[name='requireprojectcategory']").val("");
        } else {
            projectCategorySelect.parents(".__control").removeClass("__hidden");
            $("input[name='requireprojectcategory']").val("true");
        }
    }
});
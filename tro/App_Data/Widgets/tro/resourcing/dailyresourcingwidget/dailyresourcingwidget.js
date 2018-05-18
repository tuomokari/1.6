/**
 * dailyresourcingwidget widget
 */
$.widget("dailyresourcingwidget.dailyresourcingwidget", $.core.base, {
    options: {
    },

    /**
     * Fired by jQuery when the widget is created.
     */
    _create: function () {

        this._uiState = new Object;
        this._model = new Object();

        this._model.currentDate = newDate();
        this._model.projects = new Array();
        this._model.projectsHashtable = {};
        this._model.users = new Array();
        this._model.assets = new Array();

        this._uiState.firstRender = true;
        this._uiState.showProjects = {};
        this._uiState.renderedProjects = {};
        this._uiState.renderedAllocations = {};

        this._setupWidgetEvents();
        this._autoCollapse();
    },
    _setupWidgetEvents: function () {

        var base = this;

        // settings
        base.element.find(".settings").on("click", function () {
            $(this).parents(".dailyresourcingsetup").toggleClass("expanded");
        });

        // show or hide users and assets
        base.element.find("#show-users").on("change", function () {
            if (this.checked)
                document.querySelector(".resources-users").style.display = "block";
            else
                document.querySelector(".resources-users").style.display = "none";
        });
        base.element.find("#show-assets").on("change", function () {
            if (this.checked)
                document.querySelector(".resources-assets").style.display = "block";
            else
                document.querySelector(".resources-assets").style.display = "none";
        });

        base.element.find("select[name='dailyresourcingwidget_profitcenter']").on("dropdownupdated", function () {
            $(this).val(base._model.selectedProfitCenter);

            // First data refresh when profit center is available.
            base.refreshData(true);
        });

        base.element.find("select[name='dailyresourcingwidget_profitcenter_2']").on("dropdownupdated", function () {
            $(this).val(base._model.selectedProfitCenter2);

            // First data refresh when profit center is available.
            base.refreshData(true);
        });

        base.element.find("select[name='dailyresourcingwidget_profitcenter']").on("change", function (e) {
            base._model.selectedProfitCenter = $(this).val();
            // update the second dropdown, if it is hidden todo: revise how dropdown selected
            if (base.element[0].querySelector("#show-secondary-profitcenter+label+div").offsetParent === null) {
                base.element.find("select[name='dailyresourcingwidget_profitcenter_2']").val($(this).val());
                base._model.selectedProfitCenter2 = $(this).val();
            }
            base._clearSelection();
        });

        base.element.find("select[name='dailyresourcingwidget_profitcenter_2']").on("change", function (e) {
            base._model.selectedProfitCenter2 = $(this).val();
            base._clearSelection();
        });

        var refreshDelay = 300;

        base.element.find(".dailyresourcingwidget_nextbutton").on("click", function () {
            base._performResourcingAction("next", refreshDelay);
        });

        base.element.find(".dailyresourcingwidget_previousbutton").on("click", function () {
            base._performResourcingAction("prev", refreshDelay);
        });

        base.element.find(".dailyresourcingwidget_todaybutton").on("click", function () {
            base._performResourcingAction("today", refreshDelay);
        });

        // date selector
        base.element.find(".datecontrols .calendar").on("click", function (e) {
            e.stopPropagation();
            base.element.find(".datepicker").datepicker("show");
        });

        base.element.find(".datepicker").datepicker({
            weekHeader: txt("week_short"),
            dayNamesMin: [txt("sunday_short"), txt("monday_short"), txt("tuesday_short"), txt("wednesday_short"), txt("thursday_short"), txt("friday_short"), txt("saturday_short")],
            monthNames: [txt("january"), txt("february"), txt("march"), txt("april"), txt("may"), txt("june"), txt("july"), txt("august"), txt("september"), txt("october"), txt("november"), txt("december")],
            showWeek: true,
            firstDay: 1, // todo: get from locale
            onSelect: function (date) {
                base._performResourcingAction(new Date(date), refreshDelay);
                base.render();
            }
        });

        // minimize panels on mobile
        //$(window).on("resize", function () {
        //    clearTimeout(window.autoCollapseTimeout);
        //    window.autoCollapseTimeout = setTimeout(function () {
        //        base._autoCollapse();
        //    }, 500);
        //});
    },
    /**
	 * Collapses flexpanels.
	 */
    _autoCollapse: function () {
        var base = this;

        if (mobile()) {
            base.element.find(".__flexpanel-column").addClass("collapsed");
        }
    },
    /**
	 * Clears selection of projects.
	 */
    _clearSelection: function () {
        var base = this;

        // Clean all projects when changing profit center.
        base._uiState.projects = {};

        // Clean all selections when changing profit center.
        base._uiState.showProjects = {};

        // Nothing is rendered after changing profit center.
        base._uiState.renderedProjects.length = 0;
        base._uiState.renderedAllocations.length = 0;

        base.refreshData(true);
    },
    /**
	 * Provides delayed action to prevent abusing the UI.
	 */
    _performResourcingAction: function (action, refreshDelay) {
        var base = this;

        if (base._uiState.lastAction != action)
            clearTimeout(base._model.uiTimeoutToken);
        else
            return;

        base._uiState.lastAction = action;

        base._uiState.uiTimeoutToken = setTimeout(function () {
            if (base._uiState.lastAction == "next") {
                base._model.currentDate.setDate(base._model.currentDate.getDate() + 1);
            } else if (base._uiState.lastAction == "prev") {
                base._model.currentDate.setDate(base._model.currentDate.getDate() - 1);
            } else if (base._uiState.lastAction == "today") {
                base._model.currentDate = newDate();
            } else if (base._uiState.lastAction instanceof Date) { // todo: Hannu will hate for doing this, but it works and is very handy
                base._model.currentDate = base._uiState.lastAction;
            }

            base.refreshData(true);
            base._uiState.lastAction = null;
        }, refreshDelay);
    },
    _initWidget: function () {
        this._model.selectedProfitCenter = this.options.userprofitcenter;
        this._model.selectedProfitCenter2 = this.options.userprofitcenter;
    },
    /**
     * Refresh all data in the model from the server.
     * 
     * @param (bool) renderData True if data should be rendered after refreshing it.
     * 
     * @note: You should not do any dom manipulation here. All DOM manipulation needs
     *        to go to the render method.
     */
    refreshData: function (renderData, options) {
        if (!options) options = {};

        console.log("Refresing data (dailyresourcing): " + options);

        // We don't immediately have any valid selection and don't need to get any data.
        if (this._uiState.firstRender) {
            this.render()
            return;
        }

        if (!this._model.selectedProfitCenter || !this._model.selectedProfitCenter2) {
            // No valid selection is possible for empty profitcenter. clear everything and skip sending request.
            this._model.projects = new Array();
            this._model.projectsHashtable = {};
            this.render();
            return;
        }

        var dayStart = moment(this._model.currentDate).startOf("day");
        var dayEnd = moment(this._model.currentDate).endOf("day");

        // Create an array of promises to wait before calling base and potentially rendering.
        var promises = [];

        var base = this;

        var restApi = new RestApi();
        var projectsPromise = restApi.find("dailyresourcingwidget", "projectsandallocationsbyprofitcenter",
		{
		    start: dayStart.toDate(),
		    end: dayEnd.toDate(),
		    profitcenter: base._model.selectedProfitCenter,
		});

        projectsPromise.done(function (data) {
            if (data.project) {
                base._model.projects = data.project;
                base._model.projectsHashtable = {};

                base._model.projects.sort(function (a, b) {
                    if (!a.name && !b.name)
                        return 0;

                    if (!a.name)
                        return -1;

                    if (!b.name)
                        return 1;

                    if (a.name == b.name)
                        return 0;

                    return (a.name < b.name) ? -1 : 1;
                });

                // Save projects inside object for quicked access with identifier.
                for (var i = 0; i < base._model.projects.length; i++) {
                    var project = base._model.projects[i];
                    base._model.projectsHashtable[project._id] = project;
                }
            }
            else {
                base._model.projects.length = 0;
                base._model.projectsHashtable = {};
            }
        });

        promises.push(projectsPromise);

        var usersPromise = restApi.find("dailyresourcingwidget", "usersforprofitcenter",
		{
		    start: dayStart.toDate(),
		    end: dayEnd.toDate(),
		    profitcenter: base._model.selectedProfitCenter2
		});

        usersPromise.done(function (data) {
            if (data.user) {
                base._model.users = data.user;
            }

            base._model.users.sort(function (a, b) {
                if (!a.lastname && !b.lastname)
                    return 0;

                if (!a.lastname)
                    return -1;

                if (!b.lastname)
                    return 1;

                if (a.lastname == b.lastname)
                    return 0;

                return (a.lastname < b.lastname) ? -1 : 1;
            });
        });

        promises.push(usersPromise);

        var assetsPromise = restApi.find("dailyresourcingwidget", "usersforprofitcenter",
		{
		    start: dayStart.toDate(),
		    end: dayEnd.toDate(),
		    profitcenter: base._model.selectedProfitCenter2
		});

        assetsPromise.done(function (data) {
            if (data.asset) {
                base._model.assets = data.asset;
            }

            base._model.assets.sort(function (a, b) {
                if (!a.name && !b.name)
                    return 0;

                if (!a.name)
                    return 1;

                if (!b.name)
                    return -1;

                if (a.name == b.name)
                    return 0;

                return (a.name < b.name) ? 1 : -1;
            });
        });

        promises.push(assetsPromise);

        $.when.apply($, promises).done(function () {
            base.render();
        });
    },

    /**
    * Render data in the model and settings into view.
    * 
    * @note: should not make any changes into model or settings
    */
    render: function (options) {

        if (!options)
            options = {};

        if (this._uiState.firstRender)
            this._uiState.firstRender = false;

        this.element.removeClass("__hidden");

        this.element.find(".selecteddate .long").text(txt("day_" + this._model.currentDate.getDay()) + " " + formatDateShort(this._model.currentDate));
        this.element.find(".selecteddate .short").text(txt("day_abbreviated_" + this._model.currentDate.getDay()) + " " + formatDateShort(this._model.currentDate));

        // set datepicker
        this.element.find(".datepicker").datepicker("setDate", this._model.currentDate);

        var base = this;

        // Draw selected projects
        for (var i = 0; i < this._model.projects.length; i++) {
            var projectEntry = this._model.projects[i];

            // Determine whether project should be shown and whether it's already been rendered
            var projectShown = base._uiState.showProjects[projectEntry._id];
            var projectRendered = base._uiState.renderedProjects[projectEntry._id];

            if (projectShown != projectRendered) {
                // Project needs to be drawn or removed
                if (projectShown) {
                    base._addProjectCard(projectEntry);
                    base._uiState.renderedProjects[projectEntry._id] = true;
                } else {
                    base._removeProjectCard(projectEntry);
                    base._uiState.renderedProjects[projectEntry._id] = false;
                }
            }

            if (projectShown) {
                // If the project is shown, set it's allocations' display states
                if (projectEntry.allocationentry) {
                    for (var j = 0; j < projectEntry.allocationentry.length; j++) {

                        var allocation = projectEntry.allocationentry[j];

                        var allocationRendered = base._uiState.renderedAllocations[allocation._id];

                        if (!allocationRendered) {
                            base._addAllocationCard(allocation, base._getProjectCard(projectEntry).find(".allocations"));
                            base._uiState.renderedAllocations[allocation._id] = true;
                        }
                    }
                }

                // Make sure all allocations not in rendered list are cleared. This is needed for example
                // when profit center changes.
                $(base._getProjectCard(projectEntry)).find(".allocations").children().each(function () {
                    id = $(this).attr("data-allocationid");

                    var allocationFound = false;
                    if (projectEntry.allocationentry) {
                        for (var j = 0; j < projectEntry.allocationentry.length; j++) {
                            var allocationEntry = projectEntry.allocationentry[j];
                            if (allocationEntry._id == id) {
                                allocationFound = true;
                                base._setAllocationCardDates(this, getDateFromIsoString(
									allocationEntry.starttimestamp),
									getDateFromIsoString(allocationEntry.endtimestamp));
                                // todo: this is now checked three times -- optimize
                                //var displayName = "";
                                //if (typeof allocation.user !== "undefined")
                                //    displayName = allocation.user.__displayname;
                                //else if (typeof allocation.asset !== "undefined")
                                //    displayName = allocation.asset.__displayname;
                                //base._setAllocationCardTitle(this, displayName);
                            }
                        }
                    }

                    if (!allocationFound) {
                        $(this).remove();
                        base._uiState.renderedAllocations[id] = false;
                    }
                });

            }

            // remove spinners
            $(".__blockscreen").each(function () {
                $(this).data("blockscreen").release();
            });
        }

        // Make sure all projects not in rendered list are cleared. This is needed for example
        // when profitcenter changes.
        base.element.find(".dailyresourcingitems").children().each(function () {
            id = $(this).attr("data-projectid");

            if (id && !base._model.projectsHashtable[id]) {
                $(this).remove();
                base._uiState.renderedProjects[id] = false;
            }
        });

        // Draw users
        var userElement = base.element.find(".resources .__flexpanel-column-content .resources-users");

        userElement.html("");
        for (var i = 0; i < this._model.users.length; i++) {
            var userEntry = this._model.users[i];

            var userCard = base._addUserCard(userEntry, userElement);
            var draggableUserCard = $(userCard).draggabilly({
                // options...
            });

            userCard.on('mousedown touchstart pointerdown', function () {
                $(this).parents(".__flexpanel-column").each(function () {
                    if (typeof ($(this)).data("scrollbar") !== "undefined") {
                        $(this).addClass("scrolling-locked").data("scrollbar").disable();
                    }
                });
            });

            userCard.on('mouseup touchend pointerup', function () {
                $(this).parents(".__flexpanel-column").each(function () {
                    if (typeof ($(this)).data("scrollbar") !== "undefined") {
                        $(this).removeClass("scrolling-locked").data("scrollbar").enable();
                    }
                });
            });

            draggableUserCard.on('dragEnd', function (event, pointer) {
                var draggedCard = this;
                draggedCard.style.display = "none"; // a hack to be able to get the element under this one
                var elem = document.elementFromPoint(pointer.pageX - window.pageXOffset, pointer.pageY - window.pageYOffset);
                draggedCard.style.display = "flex";
                window.setTimeout(function () { // to make sure the item is visible before animating it back to original location
                    draggedCard.style.left = 0;
                    draggedCard.style.top = 0;
                }, 0);

                // Determine user to be allocated and allocation target
                if (elem == null)
                    return;

                var projectCard;

                if (elem.classList.contains("dailyresourcingwidget_projectcard"))
                    projectCard = $(elem);
                else
                    projectCard = $(elem).parents(".dailyresourcingwidget_projectcard");


                var userId = $(this).attr("data-userid");
                var projectId = projectCard.attr("data-projectid");

                if (projectId) {
                    console.log(projectId);
                    // spinner
                    projectCard.blockscreen({
                        spinner: true
                    });

                    var start = new Date(base._model.currentDate.getTime());
                    var end = new Date(base._model.currentDate.getTime());

                    start.setHours(base.options.allocationstarthour, base.options.allocationstartminute, 0, 0);
                    end.setHours(base.options.allocationendhour, base.options.allocationendminute, 0, 0);

                    // Send allocation request
                    var url = getAjaxUrl("dailyresourcingwidget", "allocateuser");

                    url = addParameterToUrl(url, "user", userId);
                    url = addParameterToUrl(url, "project", projectId);
                    url = addParameterToUrl(url, "start", UTCISOString(start));
                    url = addParameterToUrl(url, "end", UTCISOString(end));
                    url = addParameterToUrl(url, "status", "In progress"); // todo: this is added for demo purposes, because otherwise the allocation would not show

                    $.ajax({
                        url: url,
                        async: true,
                        success: function (data, status, xhr) {
                            if (data == "conflict") {
                                // In case of conflict ask user whether to resource anyway and send allocation with ignore flag.
                                messageBoxConfirm(txt("confirmconflict", "dailyresourcingwidget"), true).then(
									function (input) {
									    if (input === false) {
									        projectCard.data("blockscreen").release();
									        return;
									    } else {
									        url = addParameterToUrl(url, "ignoreconflicts", true);
									        $.ajax({
									            url: url,
									            async: true,
									            success: function (data, status, xhr) {
									                base.refreshData(true);
									            }
									        });
									    }
									});

                            } else {
                                base.refreshData(true);
                            }
                        }
                    });
                }
            });
        }

        // Draw assets
        var assetElement = base.element.find(".resources .__flexpanel-column-content .resources-assets");

        assetElement.html("");
        for (var i = 0; i < this._model.assets.length; i++) {
            var assetEntry = this._model.assets[i];

            var assetCard = base._addAssetCard(assetEntry, assetElement);
            var draggableAssetCard = $(assetCard).draggabilly({
                // options...
            });

            assetCard.on('mousedown touchstart pointerdown', function () {
                $(this).parents(".__flexpanel-column").each(function () {
                    if (typeof ($(this)).data("scrollbar") !== "undefined") {
                        $(this).addClass("scrolling-locked").data("scrollbar").disable();
                    }
                });
            });

            assetCard.on('mouseup touchend pointerup', function () {
                $(this).parents(".__flexpanel-column").each(function () {
                    if (typeof ($(this)).data("scrollbar") !== "undefined") {
                        $(this).removeClass("scrolling-locked").data("scrollbar").enable();
                    }
                });
            });

            draggableAssetCard.on('dragEnd', function (event, pointer) {
                var draggedCard = this;
                draggedCard.style.display = "none"; // a hack to be able to get the element under this one
                var elem = document.elementFromPoint(pointer.pageX - window.pageXOffset, pointer.pageY - window.pageYOffset);
                draggedCard.style.display = "flex";
                window.setTimeout(function () { // to make sure the item is visible before animating it back to original location
                    draggedCard.style.left = 0;
                    draggedCard.style.top = 0;
                }, 0);

                // Determine asset to be allocated and allocation target
                if (elem == null)
                    return;

                var projectCard = $(elem).parents(".dailyresourcingwidget_projectcard");


                var assetId = $(this).attr("data-assetid");
                var projectId = projectCard.attr("data-projectid");

                if (projectId) {
                    console.log(projectId);
                    // spinner
                    projectCard.blockscreen({
                        spinner: true
                    });

                    var start = new Date(base._model.currentDate.getTime());
                    var end = new Date(base._model.currentDate.getTime());

                    start.setHours(base.options.allocationstarthour, base.options.allocationstartminute, 0, 0);
                    end.setHours(base.options.allocationendhour, base.options.allocationendminute, 0, 0);

                    // Send allocation request
                    var url = getAjaxUrl("dailyresourcingwidget", "allocateasset");

                    url = addParameterToUrl(url, "asset", assetId);
                    url = addParameterToUrl(url, "project", projectId);
                    url = addParameterToUrl(url, "start", UTCISOString(start));
                    url = addParameterToUrl(url, "end", UTCISOString(end));
                    url = addParameterToUrl(url, "status", "In progress"); // todo: this is added for demo purposes, because otherwise the allocation would not show

                    $.ajax({
                        url: url,
                        async: true,
                        success: function (data, status, xhr) {
                            if (data == "conflict") {
                                // In case of conflict ask user whether to resource anyway and send allocation with ignore flag.
                                messageBoxConfirm(txt("confirmconflict", "dailyresourcingwidget"), true).then(
									function (input) {
									    if (input === false) {
									        projectCard.data("blockscreen").release();
									        return;
									    } else {
									        url = addParameterToUrl(url, "ignoreconflicts", true);
									        $.ajax({
									            url: url,
									            async: true,
									            success: function (data, status, xhr) {
									                base.refreshData(true);
									            }
									        });
									    }
									});

                            } else {
                                base.refreshData(true);
                            }
                        }
                    });
                }
            });
        }


        var scheduledProjectFound = false;
        var unscheduledProjectFound = false;

        // Add projects as a checkbox list
        if (!options.noProjectListUpdate) {
            var scheduledProjectsList = this.element.find(".scheduledprojectslist");
            var unscheduledProjectList = this.element.find(".unscheduledprojectslist");
            unscheduledProjectList.html("");
            scheduledProjectsList.html("");

            for (var i = 0; i < this._model.projects.length; i++) {
                var projectEntry = this._model.projects[i];
                var id = base.id + "_projects_" + i;

                var checked = (base._uiState.showProjects[projectEntry._id]) ? " checked " : "";

                // Todo: replace with class
                var active = false;
                if (projectEntry.allocationentry) {
                    active = true;
                }

                var classString = "__control __checkbox";
                if (active)
                    classString += " __active";

                if (!projectEntry.projectstart && !projectEntry.projectend) {
                    unscheduledProjectList.append("<div class='" + classString + "'><input type='checkbox' id='" + id + "' class='__checkbox __switch' data-identifier='" + projectEntry._id + "'" + checked + "></input><label for='" + id + "'>" + projectEntry.name + "</label></div>")
                    unscheduledProjectFound = true;
                }
                else {
                    scheduledProjectsList.append("<div class='" + classString + "'><input type='checkbox' id='" + id + "' class='__checkbox __switch' data-identifier='" + projectEntry._id + "'" + checked + "></input><label for='" + id + "'>" + projectEntry.name + "</label></div>")
                    scheduledProjectFound = true;
                }
                this.element.find("#" + id).on("click", function () {
                    base._uiState.showProjects[$(this).data("identifier")] = $(this).is(':checked');

                    // Delay render after modifying checkboxes to allow choosing multiple and only render once.
                    window.clearTimeout(base._uiState.checkboxRender);
                    base._uiState.checkboxRender = window.setTimeout(function () {
                        base.render({ noProjectListUpdate: true });
                    }, 650);
                });
            }

            // Only shown project lists if they have some content
            if (unscheduledProjectFound)
                base.element.find(".unscheduledprojects").show();
            else
                base.element.find(".unscheduledprojects").hide();

            if (scheduledProjectFound)
                base.element.find(".scheduledprojects").show();
            else
                base.element.find(".scheduledprojects").hide();

        }
        // add projects with allocations to render list automatically todo: revise
        this.element.find(".__checkbox.__active input:not(:checked)").each(function () {
            $(this).click();
        });

        updateScrollBars(base.element);
    },
    _createProjectCard: function (identifier) {
        var base = this;

        var projectCardTemplate = $(".dailyresourcingwidget_templates .dailyresourcingwidget_projectcard.widgettemplate");
        var projectCard = projectCardTemplate.clone().removeClass("widgettemplate");

        projectCard.attr("data-projectid", identifier);

        projectCard.find(".dailyresourcingwidget_projectcard_details").click(function (e) {
            e.stopPropagation();
            e.preventDefault();
            projectCard.showProjectDetails();
        });


        projectCard.setProjectName = function (projectName) {
            $(this).find(".projectname").text(projectName);
        }

        projectCard.setCustomerName = function (customerName) {
            $(this).find(".customer").removeClass("__hidden").children("span").text(customerName);
        }

        projectCard.showProjectDetails = function () {
            var dialogUrl = getAjaxUrl("dailyresourcingwidget", "projectdetails");
            dialogUrl = addParameterToUrl(dialogUrl, "projectid", this.attr("data-projectid"));

            $(window).dialog({
                mc2url: dialogUrl,
                maxHeight: "750px",
                maxWidth: "750px",
                width: "90%",
                height: "90%",
                enablePrint: true,
                enableDefaultClick: true // enables users to navigate away from links
            });
        }

        return projectCard;
    },
    _createUserCard: function (identifier) {
        var base = this;

        var userCardTemplate = $(".dailyresourcingwidget_templates .dailyresourcingwidget_usercard.widgettemplate");
        var userCard = userCardTemplate.clone().removeClass("widgettemplate");

        userCard.attr("data-userid", identifier);

        userCard.setUserName = function (userName) {
            $(this).find(".dailyresourcingwidget_allocationcard_username").text(userName);
        }

        userCard.setAllocatedHours = function (hours) {
            $(this).find(".dailyresourcingwidget_allocationcard_userallocatedhours").text(hours);
        }

        userCard.setPicture = function (pictureURL, initials) {
            $(this).find(".dailyresourcingwidget_allocationcard_userpicture_wrapper").prepend(document.createTextNode(initials));
            if (!pictureURL)
                return;
            // base url hard coded
            pictureURL = "/CachedImages/" + pictureURL;
            $(this).find(".dailyresourcingwidget_allocationcard_userpicture").attr("src", pictureURL);
        }

        return userCard;

    },
    _createAssetCard: function (identifier) {
        var base = this;

        var assetCardTemplate = $(".dailyresourcingwidget_templates .dailyresourcingwidget_assetcard.widgettemplate");
        var assetCard = assetCardTemplate.clone().removeClass("widgettemplate");

        assetCard.attr("data-assetid", identifier);

        assetCard.setAssetName = function (assetName) {
            $(this).find(".dailyresourcingwidget_allocationcard_assetname").text(assetName);
        }

        assetCard.setAllocatedHours = function (hours) {
            $(this).find(".dailyresourcingwidget_allocationcard_assetallocatedhours").text(hours);
        }

        return assetCard;

    },
    _setAllocationCardTitle: function (allocationCard, displayName) {
        $(allocationCard).find(".username").text(displayName);
    },
    _setAllocationCardDates: function (allocationCard, startDate, endDate) {
        try {
            var differentDays = true;
            if (!startDate || !endDate || (
                startDate.getDate() == endDate.getDate() && startDate.getMonth() == endDate.getMonth() && startDate.getYear() == endDate.getYear()
                ))
                differentDays = false;

            var startDateStr = "";
            if (!isEmpty(startDate) && startDate) {
                startDateStr = formatDateShort(startDate) + " " + formatTime(startDate);
            }
            var endDateStr = "";

            if (!!endDate) {
                if (!!startDate) {
                    if (differentDays)
                        endDateStr = formatDateShort(endDate) + " " + formatTime(endDate);
                    else
                        endDateStr = formatTime(endDate);
                }
                else
                    endDateStr = formatDateShort(endDate) + " " + formatTime(endDate);
            }

            $(allocationCard).find(".dailyresourcingwidget_allocationcard_starttime").text(startDateStr);
            $(allocationCard).find(".dailyresourcingwidget_allocationcard_card_endtime").text(endDateStr);

            if (!startDate && !endDate) {
                $(allocationCard).find(".dailyresourcingwidget_allocationcard_starttime").text("");
                $(allocationCard).find(".dailyresourcingwidget_allocationcard_card_endtime").text("");
                $(allocationCard).find(".time").hide();
            }
            else {
                $(allocationCard).find(".time").css("display", "");
            }
        }
        catch (ex) {
            console.log("Error when displaying project details: " + ex);
            $(this).find(".time").hide();
        }
    },
    _createAllocationCard: function (allocation) {
        var base = this;

        var allocationCardTemplate = $(".dailyresourcingwidget_templates .dailyresourcingwidget_allocationcard.widgettemplate");
        var allocationCard = allocationCardTemplate.clone().removeClass("widgettemplate");

        // figure out displayname for the card
        var displayName = "";
        var type = "";
        if (typeof allocation.user !== "undefined") {
            displayName = allocation.user.__displayname;
            type = "user";
        }
        else if (typeof allocation.asset !== "undefined") {
            displayName = allocation.asset.__displayname;
            type = "asset";
        }

        allocationCard.attr("data-type", type);

        allocationCard.attr("data-allocationid", allocation._id);

        allocationCard.find(".dailyresourcingwidget_allocationcard_removebutton").on("click", function () {
            messageBoxConfirm(txt("confirmunallocate", "dailyresourcingwidget").replace("{0}", displayName), true).then(
				function (input) {
				    if (input === false) {
				        return;
				    } else {
				        var url = getAjaxUrl("dailyresourcingwidget", "unallocate");
				        url = addParameterToUrl(url, "allocationentry", allocation._id);

				        $.ajax({
				            url: url,
				            async: true,
				            success: function (data, status, xhr) {
				                base.refreshData(true);
				            }
				        });
				    }
				});
        });

        allocationCard.find(".dailyresourcingwidget_allocationcard_modifybutton").on("click", function (event) {
            event.preventDefault();
            event.stopPropagation();

            var url = getAjaxUrl("tro", "allocationentry");

            url = addParameterToUrl(url, "isdialog", true);
            url = addParameterToUrl(url, "actiontype", "modify");
            url = addParameterToUrl(url, "id", allocation._id);

            $(document.body).dialog({
                url: url,
                maxHeight: "750px",
                height: "90%",
                width: "90%",
                maxWidth: "750px",
                identifier: "entryedit",
                onClose: function () { base.refreshData(true); }
            })
        });

        return allocationCard;
    },
    _addProjectCard: function (project) {

        var targetContainer = this.element.find(".dailyresourcingitems")
        var projectCard = this._createProjectCard(project._id);
        projectCard.setProjectName(project.name);

        if (features.showallocationcustomername && !isEmpty(project.customer)) {
            projectCard.setCustomerName(project.customer.__displayname);
        }

        targetContainer.prepend(projectCard);
        window.setTimeout(function () { projectCard.addClass("show"); }, 0);

        return projectCard;
    },
    _removeProjectCard: function (project, targetContainer) {

        var projectCard = this._getProjectCard(project);
        projectCard.remove();

        // Remove allocation cards for project if project is removed
        if (project.allocationentry) {
            for (var i = 0; i < project.allocationentry.length; i++) {
                var allocation = project.allocationentry[i];
                this._removeAllocationCard(allocation, projectCard);
            }
        }
    },
    _addUserCard: function (user, targetContainer) {

        var userCard = this._createUserCard(user._id);
        userCard.setUserName(user.firstname + " " + user.lastname);

        var userHours = 0;

        if (user.allocationentry) {
            for (var i = 0; i < user.allocationentry.length; i++) {
                var allocation = user.allocationentry[i];

                if (!allocation.starttimestamp || !allocation.endtimestamp)
                    continue;

                var start = getDateFromIsoString(allocation.starttimestamp).getTime();
                var end = getDateFromIsoString(allocation.endtimestamp).getTime();
                var difference = (end - start) / 10 / 60 / 60; // Difference in hundreth parts of hours
                difference = Math.round(difference); // Round to hundreth part of an hour.
                userHours += difference / 100; // Get amount in hours.
            }
        }

        userCard.setAllocatedHours((userHours == 0) ? "" : userHours);

        if (features.useprofilepictures) {

            var initials = user.firstname.substring(0, 1) + user.lastname.substring(0, 1);
            var pictureURL = false;
            if (user.picture)
                pictureURL = user.picture.filename;
            userCard.setPicture(pictureURL, initials);
        }

        targetContainer.prepend(userCard);

        // update calendarview. todo: revise
        // Extremely inefficient. Do not enable this before finding a better way.
        //$(".resourcingcalendarwidget").data("resourcingcalendarwidget-resourcingcalendarwidget").refreshData(true);

        return userCard;
    },
    _addAssetCard: function (asset, targetContainer) {

        var assetCard = this._createAssetCard(asset._id);
        assetCard.setAssetName(asset.licenseplate + " " + asset.name);

        var assetHours = 0;

        if (asset.allocationentry) {
            for (var i = 0; i < asset.allocationentry.length; i++) {
                var allocation = asset.allocationentry[i];

                if (!allocation.starttimestamp || !allocation.endtimestamp)
                    continue;

                var start = getDateFromIsoString(allocation.starttimestamp).getTime();
                var end = getDateFromIsoString(allocation.endtimestamp).getTime();
                var difference = (end - start) / 10 / 60 / 60; // Difference in hundreth parts of hours
                difference = Math.round(difference); // Round to hundreth part of an hour.
                assetHours += difference / 100; // Get amount in hours.
            }
        }

        assetCard.setAllocatedHours((assetHours == 0) ? "" : assetHours);

        targetContainer.prepend(assetCard);

        // update calendarview. todo: revise
        // Extremely inefficient. Do not enable this before finding a better way.
        //$(".resourcingcalendarwidget").data("resourcingcalendarwidget-resourcingcalendarwidget").refreshData(true);

        return assetCard;
    },
    _addAllocationCard: function (allocation, target) {

        var allocationCard = this._createAllocationCard(allocation);

        var displayName = "";
        if (typeof allocation.user !== "undefined")
            displayName = allocation.user.__displayname;
        else if (typeof allocation.asset !== "undefined")
            displayName = allocation.asset.__displayname;

        this._setAllocationCardTitle(allocationCard, displayName);
        this._setAllocationCardDates(allocationCard, getDateFromIsoString(allocation.starttimestamp), getDateFromIsoString(allocation.endtimestamp));
        var base = this;

        $(target).prepend(allocationCard);

        return allocationCard;
    },
    _removeAllocationCard: function (allocation, targetProjectCard) {
        var allocationCard = targetProjectCard.find("[data-allocationid='" + allocation._id + "']");
        allocationCard.remove();
        this._uiState.renderedAllocations[allocation._id] = false;
    },
    _getProjectCard: function (project) {
        var targetContainer = this.element.find(".dailyresourcingitems")
        return targetContainer.find("[data-projectid='" + project._id + "']");
    },
});
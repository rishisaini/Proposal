
(function ($) {
    $(document).ready(function () {

        var div = $("#divSchoolEndorsement");
        var radioBtns = div.find("input[type='radio'].rbClass");
        //$('#sfcMessag').hide();
        $('#roomValidation').hide();
        $('#proposalstatusmsg').hide();
        $("#programValidation").hide();
        $("#spMessage").hide();
        $("#pocMessage").hide();
        var scChecked = false;
        var sfcDisabled = false;
        var rdSchool0 = $("#approvedSchool0").is(':checked');
        var rdSchool1 = $("#approvedSchool1").is(':checked');
        var rdSchool2 = $("#approvedSchool2").is(':checked');
        var schoolSelfDelivery = $('#SchoolSelfDeliveryHidden').val();
        initializeDocumetsTable();
        var reactiveCategory = $('#workNominatedSelectList :selected').text();
        var proposalStatus = $('#callLogTypeSelectList :selected').text();
        if ($("#ProposalId").val() == 0) {
            setDefaultBuilding();
            if ($("#userType").val() == 'SM' || $("#userType").val() == 'F') {
                $("#schoolFunded").prop("checked", true);
                $('#SchoolFundedHidden').val(true);
                //$("#schoolSelfDelivery").prop("checked", true);
                //$('#SchoolSelfDeliveryHidden').val(true);
                setDefaultFundingSource("True");

            }
            else {
                $("#amuFunded").prop("checked", true);
                //if ($('#GrantApplication').is(':checked')) {
                $("#schoolFunded").prop("checked", false);
                //$("#schoolSelfDelivery").prop("checked", false);
                //$('#SchoolSelfDeliveryHidden').val(false);
                //$('#schoolSelfDelivery').removeAttr('disabled');
                $("#AMUFundedHidden").val(true);
                setDefaultFundingSource("True");
                $('#txtFundsAvailibility').attr('disabled', 'disabled');

            }

        }
        $("#schoolFundCode").prop('disabled', true);
        if (reactiveCategory == "School Funded Project" || reactiveCategory == "School Principal") {
            if (proposalStatus != "Submitted") {// && ($("#userType").val() == 'SM' || $("#userType").val() == 'F' || $("#userType").val() == 'SD' || $("#userType").val() == 'ED')) {
                /*$("#schoolFundCode").prop('disabled', false);*/
                $("#FundsAvailability").prop('disabled', false);
                $("#divSchoolFunded").prop('disabled', false);
                $("#txtConceptEstimate").prop('disabled', false);
            }
        } else {
            //$("#schoolFundCode").prop('disabled', true);
            //$("#FundsAvailability").prop('disabled', true);
            //$("#divSchoolFunded").prop('disabled', true);
        }
        //if ($("#SchoolFunded").val() == "False") {
        //    $('#txtFundsAvailibility').attr('disabled', 'disabled');
        //    $("#txtFundsAvailibility").val('');
        //} else {
        //    $('#txtFundsAvailibility').removeAttr('disabled');
        //}

        calculateEstimateGST();
        DisableSubmitted();
        defaultProgramForLRLV();
        defaultDocumentType(0);

        if (proposalStatus != "Submitted" && ($("#userType").val() == 'SM' || $("#userType").val() == 'F' || $("#userType").val() == 'SD' || $("#userType").val() == 'ED')) {
            var schoolFunded = $("input[name='SchoolFunded']:checked").val();
            var communityFunded = $("input[name='CommunityFunded']:checked").val();

            if (schoolFunded || communityFunded) {
                $('#txtFundsAvailibility').removeAttr('disabled');
            } else {
                $('#txtFundsAvailibility').attr('disabled', 'disabled');
                $("#txtFundsAvailibility").val('');
            }

        }

        $("#txtFundsAvailibility").keyup(function () {
            $("#fundsAvailableValidation").hide();
        });
        if ($('#amuFunded').is(':checked')) {
            $("#SchoolSelfDelivery").prop("disabled", true);
        }
        if ($('#GrantApplication').is(':checked'))
            $("#documentTypeSelectList").attr('disabled', 'disabled');


        $("#schoolFunded").change(function () {
            var previousFund = $("#FundsAvailabilityDisabled").val() == "" ? $('#txtFundsAvailibility').val() : $("#FundsAvailabilityDisabled").val();
            $('#amuFunded').prop('checked', false);
            $('#communityFunded').prop('checked', false);
            //$("#schoolSelfDelivery").prop("checked", true);
            setDefaultFundingSource('True');
            $('#SchoolFundedHidden').val(true);
            $('#CommunityFundedHidden').val(false);
            $('#AMUFundedHidden').val(false);
            $("#SchoolSelfDelivery").prop("disabled", false);
            $('#txtFundsAvailibility').val(previousFund);
            $('#plannedMaintenance').prop('checked', false);
            //checkSchoolSelfDelivery(true);
            if ($("#txtConceptEstimate").val() > 0)
                selectProposalProgram();
        });
        $("#communityFunded").change(function () {
            var previousFund = $("#FundsAvailabilityDisabled").val() == "" ? $('#txtFundsAvailibility').val() : $("#FundsAvailabilityDisabled").val();
            $('#amuFunded').prop('checked', false);
            $('#schoolFunded').prop('checked', false);
            $("#SchoolSelfDelivery").prop("disabled", false);
            setDefaultFundingSource('True');
            $('#CommunityFundedHidden').val(true);
            $('#SchoolFundedHidden').val(false);
            $('#AMUFundedHidden').val(false);
            //$('#SchoolSelfDeliveryHidden').val(true);
            $('#txtFundsAvailibility').val(previousFund);
            $('#plannedMaintenance').prop('checked', false);
            //checkSchoolSelfDelivery(true);
            if ($("#txtConceptEstimate").val() > 0)
                selectProposalProgram();
        });
        $("#amuFunded").change(function () {
            amuFunded();
        });

        $("#plannedMaintenance").change(function () {
            $('#amuFunded').prop('checked', false);
            $('#communityFunded').prop('checked', false);
            $('#schoolFunded').prop('checked', false);

            $("#SchoolSelfDelivery").prop("disabled", true);
            checkSchoolSelfDelivery(false);
            

            $('#AMUFundedHidden').val(false);
            $('#CommunityFundedHidden').val(false);
            $('#SchoolFundedHidden').val(false);

            setDefaultFundingSource("True");
            $('#txtFundsAvailibility').attr('disabled', 'disabled');
            $('#txtFundsAvailibility').val('');

            
            if ($("#txtConceptEstimate").val() > 0)
                selectProposalProgram();
        });


        function amuFunded() {
            $("#SchoolSelfDelivery").prop("disabled", true);
            $('#communityFunded').prop('checked', false);
            $('#schoolFunded').prop('checked', false);
            setDefaultFundingSource("True");
            $('#AMUFundedHidden').val(true);
            $('#txtFundsAvailibility').attr('disabled', 'disabled');
            $('#txtFundsAvailibility').val('');
            $('#CommunityFundedHidden').val(false);
            $('#SchoolFundedHidden').val(false);
            $('#plannedMaintenance').prop('checked', false);
            //$('#SchoolSelfDeliveryHidden').val(false);
            checkSchoolSelfDelivery(false);
            if ($("#txtConceptEstimate").val() > 0)
                selectProposalProgram();
        }

        function schoolFunded() {
            var previousFund = $("#FundsAvailabilityDisabled").val() == "" ? $('#txtFundsAvailibility').val() : $("#FundsAvailabilityDisabled").val();
            $('#amuFunded').prop('checked', false);
            $('#communityFunded').prop('checked', false);
            //$("#schoolSelfDelivery").prop("checked", true);
            setDefaultFundingSource('True');
            $('#SchoolFundedHidden').val(true);
            $('#CommunityFundedHidden').val(false);
            $('#AMUFundedHidden').val(false);
            $("#SchoolSelfDelivery").prop("disabled", false);
            $('#txtFundsAvailibility').val(previousFund);
            //checkSchoolSelfDelivery(true);
            if ($("#txtConceptEstimate").val() > 0)
                selectProposalProgram();
        }


        $("#SchoolSelfDelivery").change(function () {
            if ($('#SchoolSelfDelivery').is(':checked'))
                $('#SchoolSelfDeliveryHidden').val(true);
            else
                $('#SchoolSelfDeliveryHidden').val(false);
        });

        $("#GrantApplication").change(function () {
            if ($('#GrantApplication').is(':checked')) {
                amuFunded();
                $("#schoolFunded").prop("checked", false);
                $("#schoolFunded").attr('disabled', 'disabled');
                $('#GrantApplicationHidden').val(true);
                $("#amuFunded").prop("checked", true);
                defaultDocumentType(0);
                $("#documentTypeSelectList").attr('disabled', 'disabled');
            }
            else {
                //schoolFunded();
                $("#schoolFunded").removeAttr('disabled');
                $("#documentTypeSelectList").removeAttr('disabled');
                $('#documentTypeSelectList').val(null);
                $('#GrantApplicationHidden').val(false);
                ////school funded
                if ($("#RoleID").val() == 'F' || $("#RoleID").val() == 'SM') {
                    var previousFund = $("#FundsAvailabilityDisabled").val() == "" ? $('#txtFundsAvailibility').val() : $("#FundsAvailabilityDisabled").val();
                    $('#amuFunded').prop('checked', false);
                    $('#communityFunded').prop('checked', false);
                    $('#schoolFunded').prop('checked', true);
                    setDefaultFundingSource('True');
                    $('#SchoolFundedHidden').val(true);
                    $('#CommunityFundedHidden').val(false);
                    $('#AMUFundedHidden').val(false);
                    $("#SchoolSelfDelivery").prop("disabled", false);
                    $('#txtFundsAvailibility').val(previousFund);
                    //checkSchoolSelfDelivery(true);
                    if ($("#txtConceptEstimate").val() > 0)
                        selectProposalProgram();
                }
            }
        });

        function defaultDocumentType(uploadReturn) {
            var grantApplication = $('#GrantApplicationHidden').val() == "True" ? true : false;
            if ($('#GrantApplication').is(':checked')) {
                var defaultText = "Premier's Office Confirmation";
                $("#documentTypeSelectList").removeAttr("disabled");
                $("#documentTypeSelectList option").each(function () {
                    if ($(this).text() == defaultText) {
                        $(this).attr('selected', 'selected');
                        $('#documentTypeSelectList').val($(this).val());

                    }
                });
                $("#schoolFunded").prop("checked", false);
                $("#schoolFunded").attr('disabled', 'disabled');
                if (uploadReturn == 1)
                    $("#documentTypeSelectList").attr("disabled", "disabled");
            }
        }



        function checkSchoolSelfDelivery(selectedValue) {
            if (!$('#Hazmat').is(':checked')) {
                $("#SchoolSelfDelivery").prop("checked", selectedValue);
                $('#SchoolSelfDeliveryHidden').val(selectedValue);
            } else {
                $("#SchoolSelfDelivery").prop("checked", false);
                $('#SchoolSelfDeliveryHidden').val(false);
            }
        }

        if (reactiveCategory == "School Funded Project" || reactiveCategory == "School Principal") {
            if (proposalStatus != "Submitted" && ($("#userType").val() == 'SM' || $("#userType").val() == 'F' || $("#userType").val() == 'SD' || $("#userType").val() == 'ED')) {
                //$("#schoolFundCode").prop('disabled', false);
                $("#FundsAvailability").prop('disabled', false);
                $("#divSchoolFunded").prop('disabled', false);
                $("#txtConceptEstimate").prop('disabled', false);
            }
        } else {
            //$("#schoolFundCode").prop('disabled', true);
            //$("#FundsAvailability").prop('disabled', true);
            //$("#divSchoolFunded").prop('disabled', true);
        }

        function fundsAvailableValid() {
            var inputErrorClass = 'input-validation-error';
            var schoolFunded = $("input[name='SchoolFunded']:checked").val();
            var communityFunded = $("input[name='CommunityFunded']:checked").val();
            var txtAvailability = $("#txtFundsAvailibility").val();
            if ((schoolFunded || communityFunded) && txtAvailability.length == 0) {
                if (!$('#schoolFunded').hasClass(inputErrorClass)) {
                    $('#schoolFunded').addClass(inputErrorClass);
                }
                if (!$('#communityFunded').hasClass(inputErrorClass)) {
                    $('#communityFunded').addClass(inputErrorClass);
                }
                $("#fundsAvailableValidation").show();
                $("#txtFundsAvailibility").addClass('input-validation-error');
                $("#txtFundsAvailibility").prop('required', true);
                return false;
            } else {
                if ($('#schoolFunded').hasClass(inputErrorClass)) {
                    $('#schoolFunded').removeClass(inputErrorClass);
                }
                if ($('#communityFunded').hasClass(inputErrorClass)) {
                    $('#communityFunded').removeClass(inputErrorClass);
                }
                $("#fundsAvailableValidation").hide();
                $("#txtFundsAvailibility").removeClass('input-validation-error');
                $("#txtFundsAvailibility").removeAttr('required');
                $("#txtFundsAvailibility").prop('required', false);
                $('#txtFundsAvailibility').valid();
                return true;
            }

        }

        function setDefaultFundingSource(defaultValue) {
            var schoolFunded = $("input[name='SchoolFunded']:checked").val() == "True" ? true : false;
            if (defaultValue == "True") {
                // Disable your txtFundsAvailibility element here
                /*   if(!(source=="lrlv"))*/
                $('#txtFundsAvailibility').removeAttr('disabled');
                var defaultText = '';
                if (schoolFunded)
                    defaultText = '6100 - Consolidated Fund';
                else
                    defaultText = '6300 - School and community sources';
                $("#schoolFundCode option").each(function () {
                    if ($(this).text() == defaultText) {
                        $(this).attr('selected', 'selected');
                        $('#schoolFundCodeHidden').val($(this).val());
                        $('#schoolFundCode').val($(this).val());
                        $('#ProposalSFCID').val($(this).val());
                    }
                });
            } else {
                // Re-enable here 
                $('#txtFundsAvailibility').attr('disabled', 'disabled');
                $("#txtFundsAvailibility").val('');
                $('#schoolFundCode').val(0);
                //if (source == "lrlv")
                //    $('#ProposalSFCID').val(0);

            }
        }
        function setDefaultBuilding() {
            //Default value is 3 - '6300 - School and community sources';
            var defaultText = '(Grounds) Grounds';
            $("#JobBuildingIDs option").each(function () {
                if ($(this).text() == defaultText) {
                    $(this).attr('selected', 'selected');
                    $('#JobBuildingIDs').val($(this).val());
                    $('#DefaultBuildingId').val($(this).val());

                }

            });
        }




        cancelButtonClicked = function (e, url) {
            e.preventDefault();
            window.location.href = url;
        }

        //setCurrentDate();
        //callLogTimeChanged();
        if ($("#FundsAvailabilityOld").val() > 0) {
            $('#divFundsAvailabilityOld').show();
        } else {
            $('#divFundsAvailabilityOld').hide();
        }
        if (rdSchool0 == true || rdSchool1 == true || rdSchool2 == true) {
            sfcDisabled = true;
            $("#schoolFundCode").prop('disabled', true);
            $("#FundsAvailability").prop('disabled', true);
            $("#divSchoolFunded").prop('disabled', true);
            $('#txtFundsAvailibility').attr('disabled', 'disabled');
        }
        $("input[type='radio'].rbClass").change(function () {
            if ($(this).is(':checked')) {
                if (($("#userType").val() != 'AO' || $("#userType").val() != 'D') && sfcDisabled == false) {
                    scChecked = true;
                    //if ($("#schoolFundCode").val().length == 0) {
                    //    $('#sfcMessag').show();
                    //    $("#schoolFundCode").prop('required', true);
                    //    $("#schoolFundCode").prop('disabled', false);
                    //}

                }
            }
        });
        $("#approvedSchool0").change(function () {
            if ($(this).is(':checked')) {
                $("#spMessage").show();
                documentTypeCheck();
            }
        });

        $("#notapprovedSchool0").change(function () {
            if ($(this).is(':checked')) {
                $("#pocMessage").hide();
            }
        });


        $("#txtConceptEstimate").keyup(function () {
            calculateEstimateGST();
        });
        $("#programSelectList").change(function () {
            updateProgramList();

            //Funding source logic
            var optionsText = this.options[this.selectedIndex].text;
            updateFundingSource(optionsText);

        });
        function defaultProgramForLRLV() {
            var defaultText = 'Community Funded Works - Recurrent';
            $("#programSelectList option").each(function () {
                if ($(this).text() == defaultText) {
                    $('#DefaultProgramForLRLV').val($(this).val());
                }
            });
        }

        function updateFundingSource(optionsText) {
            if (optionsText == 'Community Funded Works - Capital' || optionsText == 'Community Funded Works - Recurrent') {
                $("#schoolFundCode").prop('disabled', true);
                var defaultText = '6300 - School and community sources';
                $("#schoolFundCode option").each(function () {
                    if ($(this).text() == defaultText) {
                        $(this).attr('selected', 'selected');
                        $('#schoolFundCodeHidden').val($(this).val());
                        $('#schoolFundCode').val($(this).val());
                    }
                });

            } else {
                // Re-enable here 
                //$("#schoolFundCode").prop('disabled', false);
                //$('#schoolFundCode').val(0);
            }
        }

        function updateProgramList() {
            var txtConceptEstimate = $("#txtConceptEstimate").val();
            var programSelectedList = $('#programSelectList :selected').text();
            var programSelectedListValue = $('#programSelectList').val();
            $("#programValidation").hide();
            $("#programSelectList").removeClass('input-validation-error');
            $("#ProposalProgramID").removeAttr('required');
            if (txtConceptEstimate >= 10000 && programSelectedList == "School Recurrent Projects") {
                $("#programValidation").show();
                $("#programSelectList").addClass('input-validation-error');
                $("#ProposalProgramID").prop('required', true);
                $("#programSelectList").prop('required', true);
                return false;
            }
            else {
                $("#programValidation").hide();
                $("#programSelectList").removeClass('input-validation-error');
                $("#ProposalProgramID").removeAttr('required');
                return true;
            }
        }

        function calculateEstimateGST() {
            var txtConceptEstimate = $("#txtConceptEstimate").val();
            var programSelectedList = $('#programSelectList option:selected').text();
            if (txtConceptEstimate < 10000 && programSelectedList == "School Recurrent Projects") {
                $("#programSelectList").prop("disabled", true);
            }
            if (txtConceptEstimate.length <= 0) {
                $("#txtConceptEstimateGST").val('');

            } else {
                var ceText = txtConceptEstimate.replace(/,/g, '');
                var conceptEstimateGST = parseFloat(ceText) + parseFloat(ceText * 10 / 100);
                $("#txtConceptEstimate").val(ceText);
                $("#txtConceptEstimateGST").val(conceptEstimateGST.toFixed(2));
            }

        }
        jQuery("#txtConceptEstimate").blur(function () {
            selectProposalProgram();
        });

        function selectDeliveryTypeProposalProgram(deliveryType) {
            var txtConceptEstimate = $("#txtConceptEstimate").val();
            var schoolFunded = $("input[name='SchoolFunded']:checked").val() == "True" ? true : false;
            var communityFunded = $("input[name='CommunityFunded']:checked").val() == "True" ? true : false;
            var amuFunded = $("input[name='AMUFunded']:checked").val() == "True" ? true : false;

            //School Recurrent radio button
            if (deliveryType == "lrlv") {
                var defaultText = 'Community Funded Works - Recurrent';
                $("#programSelectList option").each(function () {
                    if ($(this).text() == defaultText) {
                        $('#ProgramSelectListHidden').val($(this).val());
                    }
                });
            }
            else if (txtConceptEstimate.length > 0) {

                if (txtConceptEstimate < 10000) {
                    var defaultText = '';
                    if (amuFunded || communityFunded) {
                        defaultText = 'Community Funded Works - Recurrent';
                    } else
                        defaultText = 'School Recurrent Projects';
                    $("#programSelectList option").each(function () {
                        if ($(this).text() == defaultText) {
                            $('#ProgramSelectListHidden').val($(this).val());
                        }
                    });

                } else {

                    if (amuFunded || communityFunded) {
                        defaultText = 'Community Funded Works - Capital';
                    } else
                        defaultText = 'School Capital Projects';
                    $("#programSelectList option").each(function () {
                        if ($(this).text() == defaultText) {
                            $('#ProgramSelectListHidden').val($(this).val());
                        }
                    });
                }
            }
        }

        function selectProposalProgram() {
            var txtConceptEstimate = $("#txtConceptEstimate").val() == "" ? "0" : $("#txtConceptEstimate").val();
            var schoolFunded = $("input[name='SchoolFunded']:checked").val() == "True" ? true : false;
            var communityFunded = $("input[name='CommunityFunded']:checked").val() == "True" ? true : false;
            var amuFunded = $("input[name='AMUFunded']:checked").val() == "True" ? true : false;
            //School Recurrent radio button
            if (txtConceptEstimate.length > 0) {

                if (txtConceptEstimate < 10000) {
                    var div = $("div#divSchoolRecurrent");
                    div.find($("input[type='radio'][id^='recurrentYes']")).prop("checked", true)
                    var defaultText = '';
                    if (amuFunded || communityFunded) {
                        defaultText = 'Community Funded Works - Recurrent';
                    } else
                        defaultText = 'School Recurrent Projects';
                    $("#programSelectList option").each(function () {
                        if ($(this).text() == defaultText) {
                            $(this).attr('selected', 'selected');
                            $('#ProgramSelectListHidden').val($(this).val());
                            $('#programSelectList').val($(this).val()).change();
                            $("#SchoolRecurrent").val(true);
                        }
                    });
                    $("#programSelectList").prop("disabled", true);
                } else {
                    $("#programSelectList").prop("disabled", false);
                    var div = $("div#divSchoolRecurrent");
                    div.find($("input[type='radio'][id^='recurrentNo']")).prop("checked", true)
                    if (amuFunded || communityFunded) {
                        defaultText = 'Community Funded Works - Capital';
                    } else
                        defaultText = 'School Capital Projects';
                    $("#programSelectList option").each(function () {
                        if ($(this).text() == defaultText) {
                            $(this).attr('selected', 'selected');
                            $('#ProgramSelectListHidden').val($(this).val());
                            $('#programSelectList').val($(this).val()).change();
                            $("#SchoolRecurrent").val(false);
                        }
                    });

                }
                $("#programSelectList-error").hide();
                $("#programSelectList").removeClass("input-validation-error");
            }
        }

        $("input[type='radio'].rbNAClass").change(function () {
            scChecked = false;
            if ($(this).is(':checked')) {
                //$('#sfcMessag').hide();
                $("#schoolFundCode").removeAttr('required');
                $("#spMessage").hide();
            }
        });

        $("#Submitted").change(function () {
            if (this.checked) {
                var programSelectedList = $('#programSelectList :selected').text();
                var programSelectedListValue = $('#programSelectList').val();
                $('#ProgramSelectListHidden').val(programSelectedListValue);
                $('#ProposalProgramID').val(programSelectedListValue);

                $('#Entitlements_ValidateEntitlements').val(true);
                $('#callLogTypeSelectList').val(11);
                var proposalStatus = $('#callLogTypeSelectList :selected').text();
                if (proposalStatus != "Submitted") {
                    $('#proposalstatusmsg').show();
                }
                var proposalId = $("#ProposalId").val();
                var txtConceptEstimate = parseFloat($("#txtConceptEstimateGST").val());
                var fundsavailable = parseFloat($("#txtFundsAvailibility").val());
                if (proposalId > 0 && txtConceptEstimate > fundsavailable) {
                    //$('#divFundsAvailabilityOld').show();
                    //$("#FundsAvailabilityOld").val(fundsavailable);
                    /*  $("#txtFundsAvailibilityOld").val(fundsavailable);*/
                    $("#FundsAvailabilityOldHidden").val(fundsavailable);
                    //$("#txtFundsAvailibility").val(txtConceptEstimate);
                }
            } else {

                $('#Entitlements_ValidateEntitlements').val(false);
                $('#proposalstatusmsg').hide();
                $('#callLogTypeSelectList').val(null);
                $('#divFundsAvailabilityOld').hide();
                $("#txtFundsAvailibilityOld").val('');
                $("#FundsAvailabilityOld").val('');
            }
            updateProgramList();
        });
        // proposal status validation message hide....
        $("#callLogTypeSelectList").change(function () {
            $('#proposalstatusmsg').text('');
            $("#Submitted").prop('disabled', false);

            var proposalStatusVal = $('#callLogTypeSelectList').val();
            if (CurrentUserRoleID == "A" || CurrentUserRoleID == "AO" || CurrentUserRoleID == "D") {
                if (proposalStatusVal != "15" && proposalStatusVal != "2") {

                    $('#proposalstatusmsg').text("Only Cancelled or Archived is allowed.").show();
                    $("#callLogTypeSelectList").val(ProposalStatusId);
                    return;
                }
            }


            var proposalStatus = $('#callLogTypeSelectList :selected').text();
            if (proposalStatus == "Submitted") {
                $('#proposalstatusmsg').hide();
            } else if (proposalStatus != "Submitted" && proposalStatus != "Archived") {
                if ($("#Submitted").is(":checked")) {
                    $('#proposalstatusmsg').show();
                }
                DisableSubmitted();
            }
            
        });

        function DisableSubmitted() {
            $("#Submitted").prop('disabled', false);
            var proposalStatus = $('#callLogTypeSelectList :selected').text();
            if (proposalStatus == "Cancelled" || proposalStatus == "Query Only") {
                $("#Submitted").prop('disabled', true);
                if ($("#SubmitDisabled").val() != "True")
                    $("#Submitted").prop('checked', false);
            }
            if ($("#Submitted").is(":checked")) {
                $("#Submitted").prop('disabled', true);
            }
        }

        showHideSchoolEndorsements();
        showHideEndorsements();
        var proposalStatus = $("#callLogTypeSelectList").val();
        if (proposalStatus == "2") {
            DisableEndorsementDivs();
            DisableSEDivs();
            $("#lblAwaiting0").hide();
            $("#lblAwaiting1").hide();
            $("#lblAwaiting2").hide();
            $('#lblEndorsementAwaiting0').hide();
            $('#lblEndorsementAwaiting1').hide();
            $('#lblEndorsementAwaiting2').hide();
            $('#lblEndorsementAwaiting3').hide();
            $('#lblEndorsementAwaiting4').hide();
        }
        deliveryType();
        checkHAZMAT();
        $("#txtConceptEstimate").blur(function () {
            showHideSchoolEndorsements();
            showHideEndorsements();
            updateStatusMessage();
        });

        $('#workNominatedSelectList').change(function () {
            var reactiveCategory = $('#workNominatedSelectList :selected').text();
            if (reactiveCategory == "School Funded Project" || reactiveCategory == "School Principal") {
                $('#divSchoolEndorsement').show();
            } else {
                $('#divSchoolEndorsement').hide();
            }
            showHideEndorsements();
            updateStatusMessage();
        });

        var deliveryTypeEditable = false;

        function deliveryType() {
            var rdSchool1 = $("#approved1").is(':checked');
            var rdSchool2 = $("#approved2").is(':checked');
            var rdSchool3 = $("#approved3").is(':checked');
            var calloutID = $("#CallLogFITSID").val().length > 0 ? $("#CallLogFITSID").val() : 0;
            var proposalStatus = $('#callLogTypeSelectList :selected').text();

            if (!(($("#userType").val() == 'GL' && rdSchool1 == false) || $("#userType").val() == 'SL' || $("#userType").val() == 'AM') || calloutID > 0 || proposalStatus == "Cancelled") {
                deliveryTypeEditable = false;
            } else {
                deliveryTypeEditable = ($("#AwaitingActionBy").val() != 'None' && $("#RoleID").val().length > 0 && (rdSchool2 == false || rdSchool3 == false));
            }

            $('#divEndorsement #rdDeliveryType').children().attr('disabled', !deliveryTypeEditable);
            applyLRLVFundRule(false);
        }

        // LRLV is only allowed when Funding source is "6100 - Consolidated Fund"
        function applyLRLVFundRule(clearIfInvalid) {
            var $lrlv = $('#divEndorsement #deliveryTypeLRLV1');
            var $lrlvLabel = $('#divEndorsement label[for="deliveryTypeLRLV1"]');
            var fundText = $.trim($('#schoolFundCode option:selected').text());
            var isConsolidatedFund = fundText.indexOf('6100') === 0;

            $lrlv.prop('disabled', !(deliveryTypeEditable && isConsolidatedFund));
            $lrlvLabel.css('opacity', isConsolidatedFund ? 1 : 0.5);

            if (!isConsolidatedFund && clearIfInvalid && $lrlv.is(':checked')) {
                $lrlv.prop('checked', false);
                $('#DeliveryTypeLRLVHidden').val(false);
            }
        }

        function updateStatusMessage() {
            //School Endorsement
            if ($("#lblAwaiting0").css('display') != 'none') {
                $("#statusMessage").val("School Principal");
            } if ($("#lblAwaiting1").css('display') != 'none') {
                $("#statusMessage").val("School Director");
            } if ($("#lblAwaiting2").css('display') != 'none') {
                $("#statusMessage").val("Executive Director School");
            }

            //Endorsemnet
            if ($("#lblEndorsementAwaiting0").css('display') != 'none') {
                $("#statusMessage").val("Asset Services Officer");
            } if ($("#lblEndorsementAwaiting1").css('display') != 'none') {
                $("#statusMessage").val("Group Leader");
            } if ($("#lblEndorsementAwaiting2").css('display') != 'none') {
                $("#statusMessage").val("Senior Group Leader");
            }
            if ($("#lblEndorsementAwaiting3").css('display') != 'none') {
                $("#statusMessage").val("Director Asset Management");
            } if ($("#lblEndorsementAwaiting4").css('display') != 'none') {
                $("#statusMessage").val("Executive Director Asset Management");
            }

        }

        function DisableEndorsementDivs() {
            $('#divEndorsement0 *').prop('disabled', true);
            $('#divEndorsement1 *').prop('disabled', true);
            $('#divEndorsement2 *').prop('disabled', true);
            $('#divEndorsement3 *').prop('disabled', true);
            $('#divEndorsement4 *').prop('disabled', true);
        }

        function showHideEndorsements() {
            var div = $("#divEndorsement");
            var reactiveCategory = $('#workNominatedSelectList :selected').text();
            if (($("#RoleID").val() == 'SM' || $("#RoleID").val() == 'F' || $("#RoleID").val() == 'SD' || $("#RoleID").val() == 'ED')) {
                if (reactiveCategory == "School Funded Project" || reactiveCategory == "School Principal") {
                    $('#divEndorsement *').prop('disabled', true); $('#lblEndorsementAwaiting0').hide();
                }
            }
            else {
                DisableEndorsementDivs();

                var txtConceptEstimate = parseFloat($("#txtConceptEstimate").val());
                var txtConceptEstimateGST = parseFloat($("#txtConceptEstimate").val() * (10 / 100));
                var txtVal = parseFloat(txtConceptEstimate) + parseFloat(txtConceptEstimateGST);
                var rdSchool0 = $("#approved0").is(':checked');
                var rdSchool1 = $("#approved1").is(':checked');
                var rdSchool2 = $("#approved2").is(':checked');
                var rdSchool3 = $("#approved3").is(':checked');
                var rdSchool4 = $("#approved4").is(':checked');
                var notApproved0 = $("#notapproved0").is(':checked');
                var notApproved1 = $("#notapproved1").is(':checked');
                var notApproved2 = $("#notapproved2").is(':checked');
                var notApproved3 = $("#notapproved3").is(':checked');
                var notApproved4 = $("#notapproved4").is(':checked');

                if (typeof (txtVal) === "undefined" || txtVal == 0) {
                    if (rdSchool0 == false) {
                        DisableEndorsementDivs();
                        if (notApproved0 == false || ($("#userType").val() == 'AO' || $("#userType").val() == 'D')) {
                            $('#divEndorsement0 *').prop('disabled', false);
                            setEndorsementValues($('#divEndorsement0').attr('id'));
                        }
                        $('#lblEndorsementAwaiting0').show();
                    } else if (rdSchool0 == true && rdSchool1 == false) {
                        if ($("#userType").val() == 'GL' || $("#userType").val() == 'SL' || $("#userType").val() == 'AM' || $("#userType").val() == 'EM' || $("#userType").val() == 'A') {
                            if (notApproved1 == false || ($("#userType").val() == 'GL' || $("#userType").val() == 'A')) {
                                $('#divEndorsement1 *').prop('disabled', false); setEndorsementValues($('#divEndorsement1').attr('id'));
                            }
                        } else {
                            $('#divEndorsement1 *').prop('disabled', true);
                        }
                        $('#lblEndorsementAwaiting1').show();
                    }
                    else if (rdSchool1 == true && rdSchool2 == false) {
                        if ($("#userType").val() == 'SL' || $("#userType").val() == 'AM' || $("#userType").val() == 'EM' || $("#userType").val() == 'A') {
                            if (notApproved2 == false || ($("#userType").val() == 'SL' || $("#userType").val() == 'A')) {
                                $('#divEndorsement2 *').prop('disabled', false); setEndorsementValues($('#divEndorsement2').attr('id'));
                            }
                        } else {
                            $('#divEndorsement2 *').prop('disabled', true);
                        }
                        $('#lblEndorsementAwaiting2').show();
                    }
                    else if (rdSchool2 == true && rdSchool3 == false) {
                        if ($("#userType").val() == 'AM' || $("#userType").val() == 'EM' || $("#userType").val() == 'A') {
                            if (notApproved3 == false || ($("#userType").val() == 'AM' || $("#userType").val() == 'A')) {
                                $('#divEndorsement3 *').prop('disabled', false); setEndorsementValues($('#divEndorsement3').attr('id'));
                            }
                        } else {
                            $('#divEndorsement3 *').prop('disabled', true);
                        }
                        $('#lblEndorsementAwaiting3').show();
                    }
                    else if (rdSchool3 == true && rdSchool4 == false) {
                        if ($("#userType").val() == 'EM' || $("#userType").val() == 'A') {
                            $('#divEndorsement4 *').prop('disabled', false); setEndorsementValues($('#divEndorsement4').attr('id'));
                        } else {
                            $('#divEndorsement4 *').prop('disabled', true);
                        }
                        $('#lblEndorsementAwaiting4').show();
                    }

                }
                else if (rdSchool0 == false) {
                    if (notApproved0 == false || ($("#userType").val() == 'AO' || $("#userType").val() == 'D')) {
                        $('#divEndorsement0 *').prop('disabled', false); setEndorsementValues($('#divEndorsement0').attr('id'));
                    }
                    $('#lblEndorsementAwaiting0').show();
                }
                else if (rdSchool0 == true && rdSchool1 == false) {
                    if ($("#userType").val() == 'GL' || $("#userType").val() == 'SL' || $("#userType").val() == 'AM' || $("#userType").val() == 'EM' || $("#userType").val() == 'A') {
                        if (notApproved1 == false || ($("#userType").val() == 'GL' || $("#userType").val() == 'A')) {
                            $('#divEndorsement1 *').prop('disabled', false); setEndorsementValues($('#divEndorsement1').attr('id'));
                        }
                    } else {
                        $('#divEndorsement1 *').prop('disabled', true);
                    }
                    $('#lblEndorsementAwaiting1').show();
                }

                else if (rdSchool0 == true && rdSchool1 == true && rdSchool2 == true && rdSchool3 == false) {
                    var calloutID = $("#CallLogFITSID").val().length > 0 ? $("#CallLogFITSID").val() : 0;
                    if (calloutID == 0) {
                        $('#lblEndorsementAwaiting3').show();
                        if ($("#userType").val() == 'AM' || $("#userType").val() == 'EM' || $("#userType").val() == 'A') {
                            if (notApproved3 == false || ($("#userType").val() == 'AM' || $("#userType").val() == 'A')) {
                                $('#divEndorsement3 *').prop('disabled', false); setEndorsementValues($('#divEndorsement3').attr('id'));
                            }
                        } else {
                            $('#divEndorsement3 *').prop('disabled', true);
                        }
                    }
                }
                else if (rdSchool0 == true && rdSchool1 == true && rdSchool3 == true && rdSchool4 == false) {
                    if ($("#AwaitingActionBy").val() != 'None' && $("#RoleID").val().length > 0) {
                        if ($("#userType").val() == 'EM' || $("#userType").val() == 'A') {
                            $('#divEndorsement4 *').prop('disabled', false); setEndorsementValues($('#divEndorsement4').attr('id'));
                        } else {
                            $('#divEndorsement4 *').prop('disabled', true);
                        }
                        $('#lblEndorsementAwaiting4').show();
                    }
                }
                else if (rdSchool0 == true && rdSchool1 == true && rdSchool2 == false) {
                    //if (txtVal <= 82500) {
                    if (rdSchool2 == false) {
                        $('#lblEndorsementAwaiting2').show();
                        if ($("#userType").val() == 'SL' || $("#userType").val() == 'AM' || $("#userType").val() == 'EM' || $("#userType").val() == 'A') {
                            $('#divEndorsement2 *').prop('disabled', false); setEndorsementValues($('#divEndorsement2').attr('id'));
                        } else {
                            $('#divEndorsement2 *').prop('disabled', true);
                        }
                        $('#divEndorsement3 *').prop('disabled', true);
                    }
                    //if (txtVal <= 165000) {
                    //    $('#lblEndorsementAwaiting3').show();
                    //    // if ($("#userType").val() == 'SL' || $("#userType").val() == 'AM' || $("#userType").val() == 'EM') {
                    //    if ($("#userType").val() == 'AM' || $("#userType").val() == 'EM' || $("#userType").val() == 'A') {
                    //        if (notApproved3 == false || ($("#userType").val() == 'AM' || $("#userType").val() == 'A')) {
                    //            $('#divEndorsement3 *').prop('disabled', false); setEndorsementValues($('#divEndorsement3').attr('id'));
                    //        }
                    //    } else {
                    //        $('#divEndorsement3 *').prop('disabled', true);
                    //    }
                    //}
                    //else if (txtVal > 82500 && txtVal <= 550000) {
                    else if (txtVal > 165000 && txtVal <= 550000) {

                        if (rdSchool3 == true && rdSchool4 == false) {
                            var calloutID = $("#CallLogFITSID").val().length > 0 ? $("#CallLogFITSID").val() : 0;
                            if (calloutID == 0) {
                                $('#lblEndorsementAwaiting4').show();
                                if ($("#userType").val() == 'EM' || $("#userType").val() == 'A') {
                                    $('#divEndorsement4 *').prop('disabled', false); setEndorsementValues($('#divEndorsement4').attr('id'));
                                } else {
                                    $('#divEndorsement4 *').prop('disabled', true);
                                }
                            }
                        }
                        else if (rdSchool3 == false) {
                            var calloutID = $("#CallLogFITSID").val().length > 0 ? $("#CallLogFITSID").val() : 0;
                            if (calloutID == 0) {
                                $('#lblEndorsementAwaiting2').show();
                                if ($("#userType").val() == 'SL' || $("#userType").val() == 'AM' || $("#userType").val() == 'EM' || $("#userType").val() == 'A') {
                                    $('#divEndorsement2 *').prop('disabled', false); setEndorsementValues($('#divEndorsement2').attr('id'));
                                } else {
                                    $('#divEndorsement2 *').prop('disabled', true);
                                }
                                $('#divEndorsement3 *').prop('disabled', true);
                            }
                        }
                    }

                    //else if (txtVal > 550000 && txtVal <= 2200000) {
                    else if (txtVal > 550000) {
                        if (rdSchool2 == false) {
                            $('#lblEndorsementAwaiting2').show();
                            if ($("#userType").val() == 'SL' || $("#userType").val() == 'AM' || $("#userType").val() == 'EM' || $("#userType").val() == 'A') {
                                $('#divEndorsement2 *').prop('disabled', false); setEndorsementValues($('#divEndorsement2').attr('id'));
                            } else {
                                $('#divEndorsement2 *').prop('disabled', true);
                            }
                            $('#divEndorsement3 *').prop('disabled', true);
                        }
                        else if (rdSchool3 == false) {
                            var calloutID = $("#CallLogFITSID").val().length > 0 ? $("#CallLogFITSID").val() : 0;
                            if (calloutID == 0) {
                                if ($("#userType").val() == 'EM' || $("#userType").val() == 'A') {
                                    $('#divEndorsement3 *').prop('disabled', false); setEndorsementValues($('#divEndorsement3').attr('id'));
                                } else {
                                    $('#divEndorsement3 *').prop('disabled', true);
                                }
                                $('#lblEndorsementAwaiting3').show();
                            }
                        }
                        else if (rdSchool4 == false) {
                            var calloutID = $("#CallLogFITSID").val().length > 0 ? $("#CallLogFITSID").val() : 0;
                            if (calloutID == 0) {
                                if ($("#userType").val() == 'EM' || $("#userType").val() == 'A') {
                                    $('#divEndorsement4 *').prop('disabled', false); setEndorsementValues($('#divEndorsement4').attr('id'));
                                } else {
                                    $('#divEndorsement4 *').prop('disabled', true);
                                }
                                $('#lblEndorsementAwaiting4').show();
                            }
                        }
                    }
                }

                if ($("#awaitingAction").val() == 'None') {
                    $('#divEndorsement5 *').prop('disabled', true);
                    $('#lblEndorsementAwaiting5').hide();
                }
                if ($("#userType").val() == 'SM' || $("#userType").val() == 'SD' || $("#userType").val() == 'ED') { //|| $("#userType").val() == 'F'
                    $('#divEndorsement *').prop('disabled', true);
                }
            }


        }

        function setEndorsementValues(divID) {
            $('#' + divID).find('.txtClass').val($("#approver").val());
            $('#' + divID).find('.clPosition').val($("#position").val());
        }


        function DisableSEDivs() {
            $('#divSchoolEndorsement0 *').prop('disabled', true);
            $('#divSchoolEndorsement1 *').prop('disabled', true);
            $('#divSchoolEndorsement2 *').prop('disabled', true);
        }


        function showHideSchoolEndorsements() {
            var div = $("#divSchoolEndorsement");
            var radioBtns = div.find("input[type='radio'].rbClass");
            var txtConceptEstimate = parseFloat($("#txtConceptEstimate").val());
            var txtConceptEstimateGST = parseFloat($("#txtConceptEstimate").val() * (10 / 100));
            var txtVal = parseFloat(txtConceptEstimate) + parseFloat(txtConceptEstimateGST);
            var rdSchool0 = $("#approvedSchool0").is(':checked');
            var rdSchool1 = $("#approvedSchool1").is(':checked');
            var rdSchool2 = $("#approvedSchool2").is(':checked');
            var naSchool0 = $("#notapprovedSchool0").is(':checked');
            var naSchool1 = $("#notapprovedSchool1").is(':checked');
            var approvedBy0 = $("#approvedBy0").val();
            var approvedBy1 = $("#approvedBy1").val();
            var approvedBy2 = $("#approvedBy2").val();
            var roleID = $("#RoleID").val();

            if ($("#userType").val() == 'AO' || $("#userType").val() == 'D' || $("#userType").val() == 'GL' || $("#userType").val() == 'SL' || $("#userType").val() == 'AM' || $("#userType").val() == 'EM') {
                //if (!($("#RoleID").val() == 'SD' || $("#RoleID").val() == 'ED' || $("#RoleID").val() == 'F' || $("#RoleID").val() == 'SM')) {
                DisableSEDivs();
                $('#lblAwaiting0').hide();
                $('#lblAwaiting1').hide();
                $('#lblAwaiting2').hide();
                if (rdSchool0 == false && rdSchool1 == false && rdSchool2 == false && roleID == 'SM') {
                    //if (txtVal <= 150000) {
                    $('#lblAwaiting0').show();
                    //}
                    //else if (txtVal > 150000 && txtVal <= 500000) {
                    //    $('#lblAwaiting1').show();
                    //} else if (txtVal > 500000) {
                    //    $('#lblAwaiting2').show();
                    //}

                } else if (rdSchool0 == true && rdSchool1 == false) {

                    //if ($("#userType").val() == 'AO' || $("#userType").val() == 'D' || $("#userType").val() == 'GL' || $("#userType").val() == 'SL' || $("#userType").val() == 'AM' || $("#userType").val() == 'EM') { }
                    //else {
                    if (txtVal > 165000)
                        $('#lblAwaiting1').show();
                    // }

                }
                else if (rdSchool0 == true && rdSchool1 == true && rdSchool2 == false) {
                    //if ($("#userType").val() == 'AO' || $("#userType").val() == 'D' || $("#userType").val() == 'GL' || $("#userType").val() == 'SL' || $("#userType").val() == 'AM' || $("#userType").val() == 'EM') { }
                    //else {
                    if (txtVal > 550000)
                        $('#lblAwaiting2').show();
                    //}
                }
                else if (rdSchool0 == false && rdSchool1 == true && rdSchool2 == false) {
                    if ($("#userType").val() == 'AO' || $("#userType").val() == 'D' || $("#userType").val() == 'GL' || $("#userType").val() == 'SL' || $("#userType").val() == 'AM' || $("#userType").val() == 'EM') { }
                    else { $('#lblAwaiting2').show(); }
                }
            }
            else {
                DisableSEDivs();
                if (typeof (txtVal) === "undefined" || txtVal > 550000) {
                    if ($("#RoleID").val() == 'SD' || $("#RoleID").val() == 'ED' || $("#RoleID").val() == 'F' || $("#RoleID").val() == 'SM' || $("#RoleID").val() == 'A') {
                        if (rdSchool0 == false) {
                            if ($("#userType").val() != 'F') {
                                if (naSchool0 == true && ($("#userType").val() == 'SD' || $("#userType").val() == 'ED')) {
                                    $('#divSchoolEndorsement0 *').prop('disabled', true);
                                } else {
                                    $('#divSchoolEndorsement0 *').prop('disabled', false); setEndorsementValues($('#divSchoolEndorsement0').attr('id'));
                                }
                            }
                            $('#lblAwaiting0').show();
                        } else if (rdSchool0 == true && rdSchool1 == false) {
                            $('#divSchoolEndorsement0 *').prop('disabled', true);
                            if ($("#userType").val() == 'SM') { //|| $("#userType").val() == 'F'
                                $('#divSchoolEndorsement1 *').prop('disabled', true);
                            } else if ($("#userType").val() == 'SD' || $("#userType").val() == 'ED' || $("#userType").val() == 'A') {
                                $('#divSchoolEndorsement1 *').prop('disabled', false); setEndorsementValues($('#divSchoolEndorsement1').attr('id'));
                            }
                            $('#divSchoolEndorsement2 *').prop('disabled', true);
                            $('#lblAwaiting1').show();
                            if (naSchool1 == true && $("#userType").val() == 'ED') {
                                $('#divSchoolEndorsement1 *').prop('disabled', true);
                            }
                        }
                        else if (rdSchool1 == true && rdSchool2 == false) {
                            if (($("#userType").val() == 'SM' || $("#userType").val() == 'SD') && $("#userType").val() != 'ED') { //|| $("#userType").val() == 'F'
                                $('#divSchoolEndorsement2 *').prop('disabled', true);
                            } else if ($("#userType").val() == 'ED' || $("#userType").val() == 'A') {
                                $('#divSchoolEndorsement2 *').prop('disabled', false); setEndorsementValues($('#divSchoolEndorsement2').attr('id'));
                            }

                            $('#lblAwaiting2').show();
                        }
                    }
                }

                else if (typeof (txtVal) === "undefined" || txtVal > 0) {
                    if ($("#RoleID").val() == 'SD' || $("#RoleID").val() == 'ED' || $("#RoleID").val() == 'SM') { //|| $("#RoleID").val() == 'F'

                        if (txtVal <= 165000) {
                            if (rdSchool0 == false && rdSchool1 == false) {
                                if ($("#userType").val() != 'F') {
                                    if (naSchool0 == true && ($("#userType").val() == 'SD' || $("#userType").val() == 'ED')) {
                                        $('#divSchoolEndorsement0 *').prop('disabled', true);
                                    } else {

                                        $('#divSchoolEndorsement0 *').prop('disabled', false); setEndorsementValues($('#divSchoolEndorsement0').attr('id'));
                                    }
                                }
                                $('#divSchoolEndorsement1 *').prop('disabled', true);
                                $('#divSchoolEndorsement2 *').prop('disabled', true);
                                $('#lblAwaiting0').show();
                            }
                        } else if (txtVal > 165000 && txtVal <= 550000) {
                            //Step 1
                            if (rdSchool0 == false && rdSchool1 == false) {
                                if ($("#userType").val() != 'F') {
                                    if (naSchool0 == true && ($("#userType").val() == 'SD' || $("#userType").val() == 'ED')) {
                                        $('#divSchoolEndorsement0 *').prop('disabled', true);
                                    } else {

                                        $('#divSchoolEndorsement0 *').prop('disabled', false); setEndorsementValues($('#divSchoolEndorsement0').attr('id'));
                                    }
                                }
                                $('#divSchoolEndorsement1 *').prop('disabled', true);
                                $('#divSchoolEndorsement2 *').prop('disabled', true);
                                $('#lblAwaiting0').show();
                            }

                            //Step 2
                            else if (rdSchool0 == true && rdSchool1 == false && (!($("#RoleID").val() == 'D' || $("#RoleID").val() == 'AO'))) {
                                $('#divSchoolEndorsement0 *').prop('disabled', true);
                                if ($("#userType").val() == 'SD' || $("#userType").val() == 'ED' || $("#userType").val() == 'A') {
                                    if (naSchool1 == true && $("#userType").val() == 'ED') {
                                        $('#divSchoolEndorsement1 *').prop('disabled', true);
                                    }
                                    else {
                                        $('#divSchoolEndorsement1 *').prop('disabled', false); setEndorsementValues($('#divSchoolEndorsement1').attr('id'));
                                    }
                                } else if ($("#userType").val() == 'SM') { //|| $("#userType").val() == 'F'
                                    $('#divSchoolEndorsement1 *').prop('disabled', true);
                                }
                                $('#divSchoolEndorsement2 *').prop('disabled', true);
                                $('#lblAwaiting1').show();
                            }

                        } else if (txtVal > 550000) {
                            //Step 1
                            if (rdSchool0 == false && rdSchool1 == false) {
                                if (naSchool0 == true && ($("#userType").val() == 'SD' || $("#userType").val() == 'ED')) {
                                    $('#divSchoolEndorsement0 *').prop('disabled', true);
                                } else {

                                    $('#divSchoolEndorsement0 *').prop('disabled', false); setEndorsementValues($('#divSchoolEndorsement0').attr('id'));
                                }
                                $('#divSchoolEndorsement1 *').prop('disabled', true);
                                $('#divSchoolEndorsement2 *').prop('disabled', true);
                                $('#lblAwaiting0').show();
                            }
                            //Step 2
                            else if (rdSchool0 == true && rdSchool1 == false && (!($("#RoleID").val() == 'D' || $("#RoleID").val() == 'AO'))) {
                                $('#divSchoolEndorsement0 *').prop('disabled', true);
                                if ($("#userType").val() == 'SD' || $("#userType").val() == 'ED') {
                                    if (naSchool1 == true && $("#userType").val() == 'ED') {
                                        $('#divSchoolEndorsement1 *').prop('disabled', true);
                                    }
                                    else {
                                        $('#divSchoolEndorsement1 *').prop('disabled', false); setEndorsementValues($('#divSchoolEndorsement1').attr('id'));
                                    }
                                } else if ($("#userType").val() == 'SM') { //|| $("#userType").val() == 'F'
                                    $('#divSchoolEndorsement1 *').prop('disabled', true);
                                }
                                $('#divSchoolEndorsement2 *').prop('disabled', true);
                                $('#lblAwaiting1').show();
                            }
                            //Step 3
                            else if (rdSchool1 == true && rdSchool2 == false && (!($("#RoleID").val() == 'D' || $("#RoleID").val() == 'AO'))) {
                                $('#divSchoolEndorsement0 *').prop('disabled', true);
                                $('#divSchoolEndorsement1 *').prop('disabled', true);
                                if ($("#userType").val() == 'ED' || $("#userType").val() == 'A') {
                                    $('#divSchoolEndorsement2 *').prop('disabled', false); setEndorsementValues($('#divSchoolEndorsement1').attr('id'));
                                } else if ($("#userType").val() == 'SM' || $("#userType").val() == 'SD') { //|| $("#userType").val() == 'F'
                                    $('#divSchoolEndorsement2 *').prop('disabled', true);
                                }
                                $('#lblAwaiting2').show();
                            }

                        }
                    }
                    if (proposalStatus == 'Archived') {//Archived{
                        DisableSEDivs(); $('#lblAwaiting0').hide(); $('#lblAwaiting1').hide(); $('#lblAwaiting2').hide();
                    }
                }


            }


        }


        updateStatusMessage();
        //$('#btnSave').click(function (e) {
        //    var isSubmitted = false;
        //    showSFC();
        //    var isProgramValid = updateProgramList();
        //    if (!isSubmitted && isProgramValid ) {
        //        preventDoubleClick('btnSave', e);
        //        if (!isSubmitted && $('#frmProposalDetails').valid()) {
        //            $('#btnSubmit').prop('disabled', true);

        //            $('#SaveType').val('Save');
        //            isSubmitted = checkBeforePost(e, 'btnSave');
        //            var $btn = $(this);
        //            var html = $btn.data('loading-text');
        //            $btn.html(html);
        //            return checkBeforePost(e, 'btnSave');
        //        }
        //    } else {
        //        e.preventDefault();
        //        return false;
        //    }
        //   // $('#btnSubmit').removeAttr('disabled');
        //});
        var validator = $('form').validate({
            ignore: '.ignore',
            errorClass: 'form__error__single',
            errorElement: 'label',
            onkeyup: function (element) {
                $(element).valid();
                customErrors();
            },
            errorPlacement: function (error, element) {
                if ($('form').attr('data-validation-list') != 'true')
                    $(element).parent('.form__group__input').append(error);
            },
            highlight: function (element) {
                $(element).parents('.form__group')
                    .removeClass('form__group--valid')
                    .addClass('form__group--error');
            },
            unhighlight: function (element) {
                $(element).parents('.form__group')
                    .removeClass('form__group--error')
                    .addClass('form__group--valid');
            },
            invalidHandler: function (form, validator) {
                customErrors();
            }
        });

        function grantApplicationDocumentUpload() {
            if ($("#approvedSchool0").is(':checked')) {
                return documentTypeCheck();
            } else
                return true;
        }

        var isSubmitted = false;
        $('#btnSave').click(function (e) {
            var isFundsAvailableValid = fundsAvailableValid();
            var isProgramValid = updateProgramList();
            var isFormValid = $('#frmProposalDetails').valid();
            var pocDocumentGA = grantApplicationDocumentUpload();
            var errors = validator.numberOfInvalids();
            if (!isSubmitted && isFormValid && isProgramValid && pocDocumentGA) {
                $('#btnSubmit').prop('disabled', true);
                $('#SaveType').val('Save');
                isSubmitted = true;
                return true;
            }
            else {
                return false
            }

        });


        //$('#btnSubmit').click(function (e) {
        //    var isSubmitted = false;
        //    //preventDoubleClick('btnSubmit', e);

        //    showSFC();
        //    if (!isSubmitted && $('#frmProposalDetails').valid()) {
        //        $('#btnSave').prop('disabled', true);
        //        preventDoubleClick('btnSubmit', e);
        //        $('#SaveType').val('SaveAndExit');
        //        isSubmitted = checkBeforePost(e, 'btnSubmit');
        //        var $btn = $(this);
        //        var html = $btn.data('loading-text');
        //        $btn.html(html);
        //        return checkBeforePost(e, 'btnSubmit');

        //    } else {
        //        e.preventDefault();
        //        return false;
        //    }

        //});
        $("*").dblclick(function (e) {
            e.preventDefault();
        });

        $('#btnSubmit').click(function (e) {
            preventDoubleClick('btnSubmit', e);
            var inputErrorClass = 'input-validation-error';
            var pocDocumentGA = grantApplicationDocumentUpload();
            var isValid = callLogTimeChanged();
            var isProgramValid = updateProgramList();
            if (isValid && isProgramValid && (!isSubmitted && $('#frmProposalDetails').valid()) && pocDocumentGA) {
                $('#btnSave').prop('disabled', true);
                isSubmitted = true;//checkBeforePost(e, 'btnSave');
                var $btn = $(this);
                var html = $btn.data('loading-text');
                $btn.html(html);
                $('#dateTimeMsg').text("");
                $('#txtCallDate').removeClass(inputErrorClass);
                $('#CallLogTime').removeClass(inputErrorClass);
                $('#programSelectList').removeClass(inputErrorClass);
                return isSubmitted;
            } else {
                //$('#btnSave').prop('disabled', true);
                //isSubmitted = true;//checkBeforePost(e, 'btnSave');
                var $btn = $(this);
                var html = $btn.data('loading-text');
                $btn.html(html);
                if (!isValid) {
                    if (!$('#txtCallDate').hasClass(inputErrorClass)) {
                        $('#txtCallDate').addClass(inputErrorClass);
                    }

                    if (!$('#CallLogTime').hasClass(inputErrorClass)) {
                        $('#CallLogTime').addClass(inputErrorClass);
                    }
                    $('#dateTimeMsg').text("Date cannot be in future");
                }
                else if (!isProgramValid) {
                    if (!$('#programSelectList').hasClass(inputErrorClass)) {
                        $('#programSelectList').addClass(inputErrorClass);
                    }
                }

                return false;
            }


        });

        $("#Hazmat").change(function () {
            var schoolFunded = $("input[name='SchoolFunded']:checked").val() == "True" ? true : false;
            var communityFunded = $("input[name='CommunityFunded']:checked").val() == "True" ? true : false;
            var amuFunded = $("input[name='AMUFunded']:checked").val() == "True" ? true : false;
            if (!$('#Hazmat').is(':checked') && (schoolFunded || communityFunded)) {
                checkSchoolSelfDelivery(true);
            } else {
                checkSchoolSelfDelivery(false);
            }

        });

        function checkHAZMAT() {
            var schoolFunded = $("#SchoolFundedHidden").val() == "True" ? true : false;
            var communityFunded = $("#CommunityFundedHidden").val() == "True" ? true : false;
            var amuFunded = $("#AMUFundedHidden").val() == "True" ? true : false;
            var dLRLV = $('#DeliveryTypeLRLVHidden').val() == "True" ? true : false;
            var dLSSD = $('#DeliveryTypeSSDHidden').val() == "True" ? true : false;
            var dLAMU = $('#DeliveryTypeAMUHidden').val() == "True" ? true : false;
            if (dLRLV || dLSSD || dLAMU) {
                $('#divEndorsement #deliveryTypeSSD1').prop('checked', dLSSD);
                $('#divEndorsement #deliveryTypeAMU1').prop('checked', dLAMU);
                $('#divEndorsement #deliveryTypeLRLV1').prop('checked', dLRLV);
                if (amuFunded) {
                    $('#divEndorsement #deliveryTypeSSD1').prop('disabled', true);
                }
            }
            else {
                var hazmat = $('#Hazmat').val() == "True" ? true : false;
                if ((schoolFunded || communityFunded) && !(hazmat)) { // $('#Hazmat').is(':checked'))
                    $('#divEndorsement #deliveryTypeSSD1').prop('checked', true);
                    var rdSchool1 = $("#approved1").is(':checked');
                    var rdSchool2 = $("#approved2").is(':checked');
                    if ((rdSchool1 == true || rdSchool2 == true) && $("#userType").val() == 'AM') {
                        $('#DeliveryTypeSSDHidden').val(true);
                    }
                } else {
                    $('#divEndorsement #deliveryTypeAMU1').prop('checked', true);
                    $('#divEndorsement #deliveryTypeSSD1').prop('disabled', true);
                    if ($("#ProposalId").val() > 0) {
                        $('#txtFundsAvailibility').attr('disabled', 'disabled');
                        //$("#txtFundsAvailibility").val('');
                    }
                    var rdSchool1 = $("#approved1").is(':checked');
                    var rdSchool2 = $("#approved2").is(':checked');
                    if ((rdSchool1 == true || rdSchool2 == true) && $("#userType").val() == 'AM') {
                        $('#DeliveryTypeAMUHidden').val(true);
                    }
                }
            }

        }

        $('#divEndorsement #deliveryTypeSSD1').on('click', function () {
            $('#divEndorsement #deliveryTypeLRLV1').prop('checked', false);
            $('#divEndorsement #deliveryTypeAMU1').prop('checked', false);
            $('#DeliveryTypeAMUHidden').val(false);
            $('#DeliveryTypeLRLVHidden').val(false);
            $('#DeliveryTypeSSDHidden').val(true);
            var schoolFunded = $("#SchoolFundedHidden").val() == "True" ? true : false;
            var communityFunded = $("#CommunityFundedHidden").val() == "True" ? true : false;
            var ssd = schoolSelfDelivery == "True" ? true : false;
            if (schoolFunded || communityFunded) {
                checkSchoolSelfDelivery(ssd);
            }
            selectDeliveryTypeProposalProgram();
        });
        $('#divEndorsement #deliveryTypeLRLV1').on('click', function () {
            $('#divEndorsement #deliveryTypeSSD1').prop('checked', false);
            $('#divEndorsement #deliveryTypeAMU1').prop('checked', false);
            $('#DeliveryTypeAMUHidden').val(false);
            $('#DeliveryTypeLRLVHidden').val(true);
            $('#DeliveryTypeSSDHidden').val(false);
            /* $('#SchoolSelfDelivery').prop("disabled", false);*/
            $('#SchoolSelfDelivery').removeAttr("checked");
            checkSchoolSelfDelivery(false);
            setDefaultFundingSource("True");
            selectDeliveryTypeProposalProgram("lrlv");
        });
        $('#divEndorsement #deliveryTypeAMU1').on('click', function () {
            $('#divEndorsement #deliveryTypeSSD1').prop('checked', false);
            $('#divEndorsement #deliveryTypeLRLV1').prop('checked', false);
            $('#DeliveryTypeAMUHidden').val(true);
            $('#DeliveryTypeLRLVHidden').val(false);
            $('#DeliveryTypeSSDHidden').val(false);
            var schoolFunded = $("#SchoolFundedHidden").val() == "True" ? true : false;
            var communityFunded = $("#CommunityFundedHidden").val() == "True" ? true : false;
            checkSchoolSelfDelivery(false);
            selectDeliveryTypeProposalProgram();
        });


        $('#btnUpload').click(function (e) {
            //Code to upload the file to the server
            e.preventDefault();
            if (validateFileUpload()) {
                return;
            }

            $('#btnUpload').prop('disabled', 'disabled');
            var url = uploadUrl;
            var agencyID = $('#AgencyId').val();
            var propertyID = $('#PropertyId').val();
            var roleID = $('#CurrentRoleID').val().length <= 0 ? $('#RoleID').val() : $('#CurrentRoleID').val();
            var supportDocumentClassID = $('#SupportDocumentClassID').val();
            var issueDate = moment(new Date()).format('DD/MM/YYYY HH:mm:ss A'); //$('#txtIssueDate').val();
            var file = $('#fileSelectedBtn').prop('files')[0];
            var documentType = $('#documentTypeSelectList option:selected').val();
            var formData = new FormData();
            formData.append('AgencyID', agencyID);
            formData.append('MasterRecordID', (isNullOrEmpty($('#CallOutId').val()) ? 0 : $('#CallOutId').val()));
            formData.append('PropertyID', propertyID);
            formData.append('SupportDocumentClassID', supportDocumentClassID);
            formData.append('IssueDate', issueDate);
            formData.append('SupportDocumentTypeID', documentType);
            formData.append('SupportDocumentTypeDesc', $('#documentTypeSelectList option:selected').text());
            formData.append('fileToUpload', file);
            formData.append('ClientDocumentName', file.name);
            formData.append('RoleID', roleID);
            //Send the proper header information along with the request
            var request = new XMLHttpRequest();
            request.open("POST", url);
            request.send(formData);
            request.onreadystatechange = function () { // Call a function when the state changes.
                if (this.readyState === XMLHttpRequest.DONE && this.status === 200) {
                    $('#btnUpload').prop('disabled', false);
                    var obj = JSON.parse(this.response);

                    updateFilesTable(obj.downloadLink, obj.filePath, obj.sequenceId, obj.documentId, file.name);
                    assetCare.initToastr('{"type":"success","content":"File successfully uploaded."}');
                    $('#fileSelectedBtn').val('');
                    if (!$("#GrantApplication").val()) {
                        $('#documentTypeSelectList').val('');
                    }
                    if (supportDocuments.length >= 10) {
                        $('#btnUpload').prop('disabled', 'disabled');
                    }
                    //  $('#txtIssueDate').val('');

                    //remove the type once successful
                    //$("#documentTypeSelectList option[value='" + documentType + "']").remove();

                }
                if (this.readyState === XMLHttpRequest.DONE && this.status === 400) {
                    $('#btnUpload').prop('disabled', false);
                    if (supportDocuments.length >= 10) {
                        $('#btnUpload').prop('disabled', 'disabled');
                    } else {
                        $('#btnUpload').prop('disabled', false);

                    }
                    assetCare.initToastr('{"type":"error","content":"File upload unsuccessfully."}');
                }
                defaultDocumentType(1);
            }
            documentTypeCheck();

        });

        function documentTypeCheck() {
            if ($("#GrantApplication").val()) {
                var pocDocumentTypeID = $('#POCDocumentTypeID').val();
                var poc = false;
                for (var i = 0; i < supportDocuments.length; i++) {
                    if (supportDocuments[i].SupportDocumentTypeID == pocDocumentTypeID) {
                        poc = true;
                        break;
                    }
                }
                var rdSchool0 = $("#approvedSchool0").is(':checked');
                var grantApplication = $("#GrantApplication").is(':checked');
                if (!poc && rdSchool0 && grantApplication) {
                    $("#pocMessage").show();
                    return false;
                }
                else {
                    $("#pocMessage").hide();
                    return true;
                }

            } else { return true; }
        }

        function validateFileUpload() {
            var hasError = false;
            var inputErrorClass = 'input-validation-error';
            var documentTypeMsg = 'Document type is required';
            var fileMsg = 'Please select a file';
            var acceptedFileExtensions = $('#AcceptedFileTypes').val();
            var invalidFileMsg = 'Only ' + $('#AcceptedFileTypes').val() + ' allowed';
            var pattern = invalidFilenameCharacters.replace("&amp;", "&");
            var format = new RegExp(pattern);
            var invalidFileNameMsg = 'A file name can not contain any of the following characters: ' + pattern.slice(1, -1).split('').join(' ');

            var documentTypeElem = $('#documentTypeSelectList');

            var file = $('#fileSelectedBtn').prop('files')[0];
            var size = file.size / 1024 / 1024;
            var documentType = $('#documentTypeSelectList option:selected').val();

            if (isNullOrEmpty(file)) {
                hasError = true;
                $('#fileErr').text(fileMsg);
                if (!$('#fileSelectedBtn').hasClass(inputErrorClass))
                    $('#fileSelectedBtn').addClass(inputErrorClass);
            } else if (size > 10) {
                hasError = true;
                $('#fileErr').text('File size should be less than 10 MB');
                if (!$('#fileSelectedBtn').hasClass(inputErrorClass))
                    $('#fileSelectedBtn').addClass(inputErrorClass);
            } else if (acceptedFileExtensions.lastIndexOf(file.name.split('.').pop().toLowerCase()) == -1) {
                hasError = true;
                $('#fileErr').text(invalidFileMsg);
                if (!$('#fileSelectedBtn').hasClass(inputErrorClass))
                    $('#fileSelectedBtn').addClass(inputErrorClass);
            } else if (format.test(file.name)) {
                hasError = true;
                $('#fileErr').text(invalidFileNameMsg);
                if (!$('#fileSelectedBtn').hasClass(inputErrorClass))
                    $('#fileSelectedBtn').addClass(inputErrorClass);
            }
            else {
                $('#fileErr').empty();
                if ($('#fileSelectedBtn').hasClass(inputErrorClass)) {
                    $('#fileSelectedBtn').removeClass(inputErrorClass);
                }
            }

            if (isNullOrEmpty(documentType)) {
                hasError = true;
                $('#documentTypeErr').text(documentTypeMsg);
                if (!documentTypeElem.hasClass(inputErrorClass))
                    documentTypeElem.addClass(inputErrorClass);
            } else {
                $('#documentTypeErr').empty();
                if (documentTypeElem.hasClass(inputErrorClass)) {
                    documentTypeElem.removeClass(inputErrorClass);
                }
            }

            return hasError;
        }
        deleteDocument = function (id) {
            var url = delUrl + id;


            $.ajax({
                type: 'POST',
                url: url,
                dataType: 'json',
                contentType: 'application/json; charset=utf-8',
                data: {},
                success: function (response) {
                    assetCare.initToastr('{"type":"success","content":"File successfully deleted."}');
                    $('table#proposalDocumentList span.seqHandle').each(function (index, spn) {
                        $(spn).text(index + 1);
                    });
                },
                error: function (ex) {
                    console.log(ex);
                }
            });
        }

        remove = function (ele, deletingId) {
            $('#proposalDocumentList').DataTable().row($(ele).parents('tr')).remove().draw();
            var deletedTypeId;
            var typeDesc;
            for (var i = 0; i < supportDocuments.length; i++) {
                if (supportDocuments[i].SupportDocumentID == deletingId) {
                    deletedTypeId = supportDocuments[i].SupportDocumentTypeID;
                    typeDesc = supportDocuments[i].SupportDocumentTypeDesc;
                    supportDocuments.splice(i, 1);
                    i--;
                }
            }

            $('#SupportDocs').val(JSON.stringify(supportDocuments));
            deleteDocument(deletingId);
            if (supportDocuments.length < 10) {
                $('#btnUpload').prop('disabled', false);
            } else {
                $('#btnUpload').prop('disabled', false);

            }
            documentTypeCheck();
            return false;
        }

        function updateFilesTable(downloadLink, filePath, seqId, documentId, clientFileName) {
            var uploadedOn = $('#txtIssueDate').val();
            var documentType = $('#documentTypeSelectList  option:selected').text();
            var uploadedFileName = filePath.split('/').pop();
            var table = $('#proposalDocumentList').DataTable();
            table.row.add([
                '<a href="#" onclick="return remove(this,' + documentId.toString() + ');"><i class="fa fa-trash" data-toggle="tooltip" data-placement="top" title="" data-original-title="Delete the callout" style="font-size:21px; margin-top:-3px; margin-left:16px;"></i></a>',
                documentType,
                '<a href=' + encodeDownloadLink(downloadLink) + ' target="_blank" >' + clientFileName + '</a>',
                moment(new Date()).format('DD/MM/YYYY HH:mm:ss A'),

            ]).draw();

            supportDocuments.push({
                SupportDocumentID: documentId,
                SupportDocumentClassID: $('#SupportDocumentClassID').val(),
                SupportDocumentTypeID: $('#documentTypeSelectList option:selected').val(),
                MasterRecordID: 0,
                DocumentSequenceID: seqId,
                AgencyID: $('#AgencyID').val(),
                PropertyID: $('#PropertyID').val(),
                IssueDate: uploadedOn,
                AttachmentLink: filePath,
                AttachmentText: uploadedFileName,
                RecordedBy: null,
                RecordedDate: null,
                LastRecordedBy: null,
                LastRecordedDate: null,
                SupportDocumentTypeDesc: $('#documentTypeSelectList option:selected').text(),
                PropertyName: $('#schoolName').val(),
            });

            $('#SupportDocs').val(JSON.stringify(supportDocuments));
            documentTypeCheck();
            defaultDocumentType(0);
        }

        function encodeDownloadLink(downloadLink) {
            downloadLink = encodeURI(downloadLink); // encodeURI does not encode all the characters.
            // encodeURI escapes all characters except: A–Z a–z 0–9 - _ . ! ~ * ' ( ) ; / ? : @ = $ , # & +

            if (downloadLink.includes("#")) {
                downloadLink = downloadLink.replace("#", "%23");
            }
            if (downloadLink.includes("&")) {
                downloadLink = downloadLink.replace("&", "%26");
            }
            if (downloadLink.includes("+")) {
                downloadLink = downloadLink.replace("+", "%2B");
            }
            return downloadLink;
        }
        function isNullOrEmpty(v) {
            return ((v === '') || (v == null));
        }

        $(".select2").select2();
        //setWorkTypeDesc();
        //$("#WorkCategoryId").change(function () {
        //    fmcWorkCategoryChanged();
        //});

        $("#JobBuildingIDs").change(function () {
            checkRooms()
        });
        $("#rooms").change(function () {
            checkRooms();
        });

        function checkRooms() {
            var buildingId = $('#JobBuildingIDs').val();
            var buildingText = $('#JobBuildingIDs :selected').text();
            if (($("#userType").val() != 'SL' || $("#userType").val() != 'GL')) {

                if (buildingText.includes('Grounds') == false && $("#rooms").val().length == 0) {

                    $('#roomValidation').show();
                    $("#rooms").prop('required', true);
                    return false;
                } else {
                    $('#roomValidation').hide();
                    $("#rooms").removeAttr('required');
                    return true;
                }
            }
        }

        $("#SelectedMaintenanceCallLogId").change(function () {
            maintenanceTaskChanged(this.value);
        });

        $('#btnSetCurrentDate').click(function () {
            setCurrentDate();
        });

        $('#txtCallDate').on('input', function () {
            callLogTimeChanged();
        });

        $('#tpIncidentFrom').datetimepicker({
            format: 'HH:mm',
            pickDate: false,
            pickSeconds: false,
            pick12HourFormat: false,
            showMeridian: false
        });

        $('#headerCallOutId').hide();
        $('#headerSchoolName').hide();
        $('#callOutHeader').collapse('show');
        $('#callOutHeader').on('hidden.bs.collapse', function () {
            $('#headerCallOutId').show();
            $('#headerSchoolName').show();
        });

        $('#callOutHeader').on('shown.bs.collapse', function () {
            $('#headerCallOutId').hide();
            $('#headerSchoolName').hide();
        });



        $('#SelectedMaintenanceCallLogId').change(function () {
            var reactiveCategory = $('#SelectedMaintenanceCallLogId').val();
            var reactiveElement = $('#SelectedMaintenanceCallLogId');
            var inputErrorClass = 'input-validation-error';
            if (reactiveCategory == 0 || reactiveCategory == null) {
                if (!reactiveElement.hasClass(inputErrorClass)) {
                    reactiveElement.addClass(inputErrorClass);
                    $('#reactiveValidationMsg').show();
                }
            } else {
                reactiveElement.removeClass(inputErrorClass);
                $('#reactiveValidationMsg').hide();
            }
        });

        $('#WorkTypeId').change(function () {
            setWorkTypeDesc();
        });




        updateErrorSummary = function () {
            var x = $('#JobBuildingIDs').val();
            if (x) {
                if ($('#JobBuildingIDs').hasClass('input-validation-error')) {
                    $('#JobBuildingIDs').removeClass('input-validation-error');
                }
            }
        }

        callLogTimeChanged = function () {
            var inputErrorClass = 'input-validation-error';
            var selectedDate = $('#txtCallDate').val();
            var selectedTime = $('#CallLogTime').val();
            var dt = null;
            if (selectedTime != "" && selectedTime.lastIndexOf("/") > 0) {
                dt = new moment(selectedTime, 'DD/MM/YYYY HH:mm A');
            } else {
                dt = new moment(selectedDate + " " + selectedTime, 'DD/MM/YYYY HH:mm A');
            }

            var now = new Date();
            var thisMoment = new moment(now.getDate() + "/" + (now.getMonth() + 1) + "/" + now.getFullYear() + " " + now.getHours() + ":" + now.getMinutes() + " " + (now.getHours() >= 12 ? 'PM' : 'AM'), 'DD/MM/YYYY HH:mm A');

            var dif = thisMoment.diff(dt, 'minutes');
            if (dif < 0) {

                return false;

            } else {
                $('#dateTimeMsg').text("");
                $('#txtCallDate').removeClass(inputErrorClass);
                $('#CallLogTime').removeClass(inputErrorClass);
                return true;

            }
        }


        function initializeDocumetsTable() {
            $('#proposalDocumentList').DataTable({
                "pageLength": 5,
                "lengthMenu": [5, 10, 15]
            });
            //remove existing document types
            if (supportDocuments.length >= 10) {
                $('#btnUpload').prop('disabled', 'disabled');
            }


        }


        function setCurrentDate() {
            var mode = $('#mode').val();
            if (mode == 'add') {
                $('#txtCallDate').val(moment(new Date()).format('DD/MM/YYYY'));
                $('#tpIncidentFrom').datetimepicker('date', moment(new Date(), 'HH:mm'));

                callLogTimeChanged();
            }
        }
        setCurrentDate();
        if ($('#ProposalWorkNominatedIDHidden').val().length == 0)
            $('#workNominatedSelectList').val(2);

        $("#getHelp").click(function (e) {
            $.ajax({
                url: helpUrl,
                method: "POST",
                success: function (quickHelp) {
                    $('#HelpModalBody').empty();
                    $('#HelpModalBody').append(quickHelp);
                    $('#HelpModal').modal('show');
                },
            });
        });

        let isIOS = (/iPad|iPhone|iPod/.test(navigator.platform) ||
            (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1)) && !window.MSStream

        // Get SitePlan from FMWeb network folder
        $("#getSitePlan").click(function (e) {
            //$.ajax({
            //    url: getSitePlanUrl,
            //    async: false,
            //    method: "GET",
            //    success: function (response) {
            //        var sitePlanPDFPath = response['PDFPath'];
            //        var isMobileBrowsers = response['IsMobileBrowsers'];
            //        if (sitePlanPDFPath !== null && sitePlanPDFPath !== '') {
            //            if (isMobileBrowsers || isIOS) {
            //                window.open(root + sitePlanPDFPath, '_blank');
            //            }
            //            else {
            //                let el = document.createElement("iframe");
            //                //el.src = root + sitePlanPDFPath +"#toolbar=0&navpanes=0"; to hide PDF tool bar
            //                el.src = root + sitePlanPDFPath; el.width = "100%"; el.height = "600";
            //                $('#sitePlanModalBody').empty();
            //                $('#sitePlanModalBody').append(el);
            //                $('#sitePlan').modal('show');
            //            }
            //        }
            //    },
            //    error: function (arg, data, value) {
            //        $.ajax({
            //            url: fileNotFoundErrorUrl,
            //            method: "POST",
            //            success: function (fileNotFoundErrorView) {
            //                $('#sitePlanModalBody').empty();
            //                $('#sitePlanModalBody').append(fileNotFoundErrorView);
            //                $('#sitePlan').modal('show');
            //            },
            //        });
            //    }
            //});
            assetCare.showBlobInModal(e, getSitePlanUrl, '#sitePlanModalBody', '#sitePlan');
        });

        // Get SitePlan from FMWeb API
        $("#getSitePlan1").click(function (e) {
            $.ajax({
                url: getSitePlanFromFMWebAPIUrl,
                method: "GET",
                xhrFields: { responseType: 'blob' },
                success: function (data) {
                    // To View
                    let el = document.createElement("iframe");
                    el.src = window.URL.createObjectURL(data); el.width = "100%"; el.height = "600";
                    $('#sitePlanModalBody').empty();
                    $('#sitePlanModalBody').append(el);
                    $('#sitePlan').modal('show');

                },
                error: function (arg, data, value) {
                    $.ajax({
                        url: fileNotFoundErrorUrl,
                        method: "POST",
                        success: function (fileNotFoundErrorView) {
                            $('#sitePlanModalBody').empty();
                            $('#sitePlanModalBody').append(fileNotFoundErrorView);
                            $('#sitePlan').modal('show');
                        },
                    });
                }
            });
        });

    });
})(jQuery);

let hecpAuthorUpdateCompleted = false;
String.isNullOrEmpty = function (string) {
    return !string;
};

function zip(a, b, c) {
    return a.map(function (_, i) {
        return [a[i], b[i], c[i]];
    });
}

/* Add event listener so that when a user uses the 'Back' button in their browser, the content
 * of the page is reloaded and thus the state of the discrete is accurately represented.
 * For example, after creating a LOTO from a Discrete, if the user hits the 'Back' browser button,
 * we need to re-navigate to the desired page so that the data isn't 'old'.
*/
window.addEventListener("pageshow", function (event) {
    var historyTraversal = event.persisted ||
        (typeof window.performance != "undefined" &&
            window.performance.navigation.type === 2);
    if (historyTraversal) { // If backwards navigation
        window.location.href = window.location.href; // "Re-navigate" to the current page so the UI components are rendered correctly
        // TODO: this probably isn't the most graceful solution, but it's MVP for now.
    }
});

function populateScopeSelectLinePartial(site, program) {
    //show loading
    $('#line-loading').show();

    $.ajax({
        url: '/Admin/GetAircraftBySite/',
        type: 'GET',
        data: { site: site },
        success: function (response) {
            aircraft = sortAircraftByProgramAndLineNumber(response, program);

            $('#discrete-line-select').append("<option value='' selected></option>");
            $.each(aircraft, function (i, v) {
                $('#discrete-line-select').append("<option id='" + v.lineNumber + "-id' value = '" + v.lineNumber + "' > " + v.lineNumber + "</option > ");
            });

            if ($('#LineNumber').val() !== '') {
                $('#discrete-line-select').val($('#LineNumber').val());
            }

            $('#line-loading').hide();
            validateScopeForm();
        },
        error: function (e) {
            console.log(e);
            validateScopeForm();
        }
    });
}

function sortAircraftByProgramAndLineNumber(aircraft, program) {
    var sortedAircraft = aircraft.filter(function (item) {
        return item.model === program;
    }).sort(function (a, b) {
        return b.lineNumber - a.lineNumber;
    });

    return sortedAircraft;
}

function validateCanCreateLotoFromDiscrete() {
    var site = $.trim($('#site__review_and_sign').text());
    var program = $.trim($('#program__review_and_sign').text());
    var lineNumber = $.trim($('#lineNumber__review_and_sign').text());
    var isDiscreteLineNumberActive = false;

    var request = $.ajax({
        url: '/Admin/GetAircraftBySite/',
        type: 'GET',
        data: { site: site },
        success: function (response) {
            aircraft = sortAircraftByProgramAndLineNumber(response, program);

            $.each(aircraft, function (i, ac) {
                if (ac.lineNumber == lineNumber) {
                    isDiscreteLineNumberActive = true;
                }
            });
        },
        error: function (e) {
            console.log(e);
        }
    }).done(function () {
        if (isDiscreteLineNumberActive) {
            $('#create-loto-from-discrete-modal').modal('show');
        } else {
            toastr['error']('Return to Scope and select a new line.', 'Line Number ' + lineNumber + ' Inactive', TOAST_OPTIONS);
        }
    });
}

function validateScopeForm() {
    var isScopeFormValid =
        //$.trim($('#discrete-line-select option:selected').text()) !== ''
        //&&
        //$.trim($('#scope-job-name').val()).match("^[a-zA-Z0-9_)(-.&\/\\\\]*$") &&
        //$.trim($('#scope-job-number').val()) !== '' &&
        $.trim($('#scope-job-name').val()) !== '' &&
        ($('.Minor-Model-CheckBoxes:checked').length > 0 || !$("#minor-model-Checkbox").is(":visible"));
    if (!isScopeFormValid) {
        $('#continueButton').attr('disabled', 'disabled');
        return false;
    } else {
        $('#continueButton').removeAttr('disabled');
        return true;
    }
}

$('.btn-discrete').click(function () {
    setIsDirtyTrue();

    if (!$(this).hasClass("read-only")) {
        toggleBtnDiscreteSelected($(this));
        validateForm();
    }
});

function toggleOtherAffectedSystem() {
    let wrapperVisibilityProp = $('#wrapper-other-affected-system').css('visibility');

    if (wrapperVisibilityProp === 'visible') {
        $('#wrapper-other-affected-system')
            .css({ opacity: 1, visibility: 'hidden' })
            .animate({ opacity: 0 }, 500);
         $('#input-discrete-affectedsys-OTHER').val('');      
    } else {
        $('#wrapper-other-affected-system')
            .css({ opacity: 0, visibility: 'visible' })
            .animate({ opacity: 1.0 }, 500);
        $('#input-discrete-affectedsys-OTHER').focus();
    }  
}

function toggleMagHazOtherInput() {
    let $wrapperOtherMagHazInput = $('#other-maghaz-input-wrapper');
    let visibility_WrapperOtherMagHazInput = $wrapperOtherMagHazInput.css(
        'visibility'
    );
    if (visibility_WrapperOtherMagHazInput === 'visible' ) {
        $wrapperOtherMagHazInput
            .css({ opacity: 1, visibility: 'hidden' })
            .animate({ opacity: 0 }, 500);
         $('#input-other-maghaz').val('');  
    } else {
        $wrapperOtherMagHazInput
            .css({ opacity: 0, visibility: 'visible' })
            .animate({ opacity: 1.0 }, 500);
        $('#input-other-maghaz').focus();
    }
}

function toggleBtnDiscreteSelected($btnDiscrete) {
    $btnDiscrete.toggleClass('btn-discrete-selected');
    $btnDiscrete.find('.discrete-title').toggleClass('white-text-important');

    var $maghazIcon = $btnDiscrete.find('.wrapper-discrete-maghaz-icon');

    if ($maghazIcon !== null) {
        $maghazIcon.toggleClass('wrapper-discrete-maghaz-icon-selected');
    }

    $otherMagHazInputWrapper = $btnDiscrete.find('#other-maghaz-input-wrapper');

    if ($otherMagHazInputWrapper !== null) {
        $inputOtherMagHaz = $otherMagHazInputWrapper.find(
            '#input-discrete-affectedsys-OTHER'
        );

        $inputOtherMagHaz.toggleClass('invert-input-to-white');
        $otherMagHazInputWrapper
            .find('#label-other-affected-system')
            .toggleClass('white-text-important');
    }
}

function validateForm() {
    var isValidForm = validationFunction();

    enableDiscreteContinueButton(isValidForm);
}

function checkForSignatures(discreteId, isDiscreteSigned) {
    if (isDiscreteSigned) {
        setIsDirtyTrue();
        var $removeSignaturesModal = $('#discrete-removesignatures-modal');

        $removeSignaturesModal.modal('show');
        $removeSignaturesModal.on('hidden.bs.modal', function (e) {
            window.location.reload();
        })
    }
}

function getIsFormValid() {
    var isValidForm = false;
    var numOfSelected = $('.form-discrete-validation').find(
        '.btn-discrete-selected'
    ).length;
    if (numOfSelected > 0) {
        isValidForm = true;
    }
    var wrapperVisibilityProp = $('.wrapper_other-discrete-validation').css('visibility');
    if (wrapperVisibilityProp === 'visible') {
        if (!isOtherSysHasValue()) {
            isValidForm = false;
        }
    }

    return isValidForm;
}

function validateMagHazForm() {
    var isValidForm = getIsFormValid();

    if (isValidForm) {
        $('#maghazEnergyContinueButton').removeAttr('title');
        enableDiscreteContinueButton(isValidForm);
    } else {
        $('#maghazEnergyContinueButton').attr('title', "Please make a selection. If 'other' also enter details.");
    }

    return isValidForm;
}

function tryNavigate(isFormValid, discreteId, targetStep, formId) {
    var navigateToURL;
    var saveURL = getSaveUrl(formId);

    if (formId === "scope-discrete-form") {
        $(".Minor-Model-CheckBoxes").removeAttr('disabled');
        $("#scope-site-select").removeAttr('disabled');
    }
    if (discreteId > 0) {
        navigateToURL = '/Hecp/ViewHecp/?hecpId=' + discreteId + '&targetStep=' + targetStep
    } else {
        // This should never happen. If it does, blame Preston
        navigateToURL = "";
    }

    if ($('#current-step').val() > 7) {
        window.location.href = navigateToURL;
        return;
    }
    var data = '';
    let steps = "";
    var currentStep = $('#current-step').val();
    if (currentStep == 5 || currentStep == 6 || currentStep == 7) {
        steps = getStepOrder(currentStep);

        if (steps !== "") {
            saveURL = saveURL + steps;
        }
    }
    if (isFormValid && isDirty) {
        if (currentStep == 5 || currentStep == 6) {
            data = new FormData($('#' + formId)[0]);
            $.ajax({
                url: encodeURI(saveURL),
                type: 'post',
                dataType: 'json',
                async: false,
                cache: false,
                data: data,
                processData: false,
                contentType: false,
                success: function (result) {
                    window.location.href = '/Hecp/ViewHecp/?hecpId=' + safeResponseFilter(result.id) + '&targetStep=' + targetStep;
                },
                error: function (msg) {
                    console.log("Error: " + msg);
                    toastr['error'](msg.responseText, 'Error', TOAST_OPTIONS);
                },
            });
        }
        else {
            $.ajax({
                url: encodeURI(saveURL),
                type: 'post',
                dataType: 'json',
                async: false,
                cache: false,
                data: $('#' + formId).serialize(),
                success: function (result) {
                    window.location.href = '/Hecp/ViewHecp/?hecpId=' + discreteId + '&targetStep=' + targetStep;
                },
                error: function (msg) {
                    console.log("Error: " + msg);
                    toastr['error'](msg.responseText, 'Error', TOAST_OPTIONS);
                },
            });
        }
    } else if (!isDirty) {
        window.location.href = navigateToURL;
    } else {
        var leaveButton = document.getElementById("discrete-navigation-modal-leave");

        leaveButton.setAttribute("onclick", "window.location.href ='" + navigateToURL + "';");

        $('#discrete-navigation-modal').modal('show');
    }
}

function getStepOrder(currentStep) {
    let stepElements;
    let steps = "";
    let stepsArray = [];
    if (currentStep == 5) {
        stepElements = $("#wrapper-deactivation-steps").children().find(".row-hecp-deactivation-step");
    }
    else if (currentStep == 6) {
        stepElements = $("#wrapper-discrete-steps").children().find(".row-hecp-step");
    }
    else if (currentStep == 7) {
        stepElements = $("#wrapper-reactivation-steps").children().find(".row-hecp-reactivation-step");
    }
    $.each(stepElements, function (i, v) {
        stepsArray.push($(v).attr("step-number"));
    })
    return stepsArray.join(",");
}

function viewPage(discreteId, targetStep, fromEdit) {
    if (discreteId > 0) {
        let navigateToURL = '/Hecp/EditInWorkHecp/?hecpId=' + discreteId + '&targetStep=' + targetStep + '&fromEdit=' + fromEdit;
        window.location.href = navigateToURL;
    }
}

function viewDetails(hecpId) {
    let isPublishedList = getIsPublished();
    if (hecpId > 0) {
        let navigateToURL;
        if (isPublishedList) {
            navigateToURL = '/Hecp/ViewPublishedHecpDetails?hecpId=' + hecpId;
        }
        else {
            navigateToURL = '/Hecp/ViewDetails?hecpId=' + hecpId + '&targetStep= 8';
        }
        
        window.location.href = navigateToURL;
    }
}

function viewHecpDetailsForLoto(hecpId) {
    if (hecpId > 0) {
        window.location.href = '/Hecp/ViewPublishedHecpDetails?hecpId=' + hecpId;
    }
}

function tryNavigateFromCreateDiscrete(isFormValid, discreteId, targetStep, formId) {

    var navigateToURL;
    var saveURL = getSaveUrl(formId);

    if (formId === "scope-discrete-form") {
        $(".Minor-Model-CheckBoxes").removeAttr('disabled');
        $("#scope-site-select").removeAttr('disabled');
    }

    if (discreteId > 0) {
        navigateToURL = '/Hecp/ViewHecp/?hecpId=' + discreteId + '&targetStep=' + targetStep
    } else {
        // This should never happen. If it does, blame Preston
        navigateToURL = "";
    }
    reNumberDescriptionImageIndices();

    if (isFormValid && isDirty) {
        let data = new FormData($('#' + formId)[0]);
        $.ajax({
            url: encodeURI(saveURL),
            type: 'post',
            dataType: 'json',
            async: false,
            cache: false,
            data: data,
            processData: false,
            contentType: false,
            success: function (result) {
                var baseUrl = '/Hecp/ViewHecp/?hecpId=' + safeResponseFilter(result.id) + '&targetStep=';

                var anyObj = { foo: "bar" };
                window.history.replaceState(anyObj, "Scope", baseUrl + 1);

                window.location.href = baseUrl + targetStep;

            },
            error: function (msg) {
                console.log("Error: " + msg);
                toastr['error'](msg.responseText, 'Error', TOAST_OPTIONS);
            },
        });
    } else if (!isDirty) {
        window.location.href = navigateToURL;
    } else {
        var leaveButton = document.getElementById("discrete-navigation-modal-leave");

        leaveButton.setAttribute("onclick", "window.location.href ='" + navigateToURL + "';");

        $('#discrete-navigation-modal').modal('show');
    }
}

function getSaveUrl(formId) {
    var controllerMethod = "";

    switch (formId) {
        case "scope-discrete-form":
            controllerMethod = "SaveNameAndDescription";
            break;
        case "systems-discrete-form":
            controllerMethod = "SaveAffectedSystems";
            break;
        case "hazardous-energy-form":
            controllerMethod = "SaveMagnitudeHazardous";
            break;
        case "hecp-deactivation-steps-form":
            controllerMethod = "SaveDeactivationSteps?stepnumber=";
            break;
        case "steps-form":
            controllerMethod = "SaveTryoutProcedure?stepnumber=";
            break;
        case "hecp-reactivation-steps-form":
            controllerMethod = "SaveReactivationSteps?stepnumber=";
            break;
        case "atachapter-form":
            controllerMethod = "SaveAtaChapter";
            break;
        case "review-and-sign-form":
            controllerMethod = "SaveSignatures";
            break;
    }

    return '/Hecp/' + controllerMethod;
}

function setState(stepnumber, isoNumber) {
    var Stateid = "#input-system_circuit_state-step-" + stepnumber + "-isolation-" + isoNumber
    var state = $(Stateid + " option:selected").text();
    $('#' + circuitDeatilTextBoxId + 'step-' + stepnumber + '-isolation-' + isoNumber).val(-1);
    $('#' + circuitIdTextBoxID + 'step-' + stepnumber + '-isolation-' + isoNumber).val(-1);
    $('#' + circuitNameTextBoxID + 'step-' + stepnumber + '-isolation-' + isoNumber).val($.trim($('#' + circuitNameTextBoxID + 'step-' + stepnumber + '-isolation-' + isoNumber).val()));
    $('#' + circuitDescTextBoxID + 'step-' + stepnumber + '-isolation-' + isoNumber).val($.trim($('#' + circuitDescTextBoxID + 'step-' + stepnumber + '-isolation-' + isoNumber).val()));
    $('#' + circuitPanelTextBoxID + 'step-' + stepnumber + '-isolation-' + isoNumber).val($.trim($('#' + circuitPanelTextBoxID + 'step-' + stepnumber + '-isolation-' + isoNumber).val()));
    $('#' + circuitLocationTextBoxID + 'step-' + stepnumber + '-isolation-' + isoNumber).val($.trim($('#' + circuitLocationTextBoxID + 'step-' + stepnumber + '-isolation-' + isoNumber).val()));
    $(Stateid).val(state);

    validateDeactivationStepsForm();
    setIsDirtyTrue();
    $('.input-circuit_state').removeClass("hecpHideCursor");
}

function setProgram(stepNumber, isoNumber) {
    let modelId = "#input-system-minor-model-step-" + stepNumber + "-isolation-" + isoNumber
    let state = $(modelId + " option:selected").val();
    $(modelId).val(state);
    validateDeactivationStepsForm();
    setIsDirtyTrue();
}

function setTryoutProgram(stepNumber, isoNumber) {
    let modelId = "#input-system-minor-model-step-" + stepNumber + "-isolation-" + isoNumber
    let state = $(modelId + " option:selected").val();
    $(modelId).val(state);
    setIsDirtyTrue();
}

function setTryoutState(stepnumber, isoNumber) {
    var Stateid = "#input-system_circuit_state-step-" + stepnumber + "-isolation-" + isoNumber
    var state = $(Stateid + " option:selected").text();
    $('#' + circuitDeatilTextBoxId + 'step-' + stepnumber + '-isolation-' + isoNumber).val(-1);
    $('#' + circuitIdTextBoxID + 'step-' + stepnumber + '-isolation-' + isoNumber).val(-1);
    $('#' + circuitNameTextBoxID + 'step-' + stepnumber + '-isolation-' + isoNumber).val($.trim($('#' + circuitNameTextBoxID + 'step-' + stepnumber + '-isolation-' + isoNumber).val()));
    $('#' + circuitDescTextBoxID + 'step-' + stepnumber + '-isolation-' + isoNumber).val($.trim($('#' + circuitDescTextBoxID + 'step-' + stepnumber + '-isolation-' + isoNumber).val()));
    $('#' + circuitPanelTextBoxID + 'step-' + stepnumber + '-isolation-' + isoNumber).val($.trim($('#' + circuitPanelTextBoxID + 'step-' + stepnumber + '-isolation-' + isoNumber).val()));
    $('#' + circuitLocationTextBoxID + 'step-' + stepnumber + '-isolation-' + isoNumber).val($.trim($('#' + circuitLocationTextBoxID + 'step-' + stepnumber + '-isolation-' + isoNumber).val()));
    $(Stateid).val(state);

    validateTryoutProcedure();
    setIsDirtyTrue();
    $('.input-circuit_state').removeClass("hecpHideCursor");
}

function enableDiscreteContinueButton(enable) {
    var $btnContinue = $('.btn_continue-discrete-validation');

    if (enable) {
        $btnContinue.removeAttr('disabled');
        updateModelDiscreteItemsList();
        return;
    }
    $btnContinue.attr('disabled', 'disabled');
}

$('.input_other-discrete-validation').on('input', function () {
    var validForm = true;
    var wrapperVisibilityProp = $('.wrapper_other-discrete-validation').css('visibility');

    if (wrapperVisibilityProp === 'visible') {
        if (!isOtherSysHasValue()) {
            validForm = false;
        }
    }

    enableDiscreteContinueButton(validForm);
});

function isOtherSysHasValue() {
    return (
        $('.input_other-discrete-validation')
            .val()
            .trim() !== ''
    );
}

function updateModelDiscreteItemsList() {
    var selectedSystems = [];
    $('.btn-discrete-selected:not(#btn-discrete-maghaz-other)').each(function () {
        if (!!$(this).attr('value') && $(this).attr('value').trim() !== '') {
            selectedSystems.push($(this).attr('value'));
        }
    });
    var otherSys = $('.input_other-discrete-validation')
        .val()
        .trim();

    if (otherSys !== '') {
        selectedSystems.push(otherSys);
    }
    var serializedSelectedSystemsList = JSON.stringify(selectedSystems);
    $('#selectedDiscreteItems').val(serializedSelectedSystemsList);
}

let addNewUIDeactivationStep = (function () {
    
    let isGettingDeactivationStep = false;

    return function (currentStepNumber) {

        setIsDirtyTrue();
        // If the next step does not already exist
        let nextStepNumber = currentStepNumber + 1;
        let $nextStep = $(
            '.row-hecp-deactivation-step[step-number=' +
            nextStepNumber +
            ']'
        );
        // Show the "Add Isolations For Step # button"
        let $isolationsWrapper = getIsolationsWrapperForStepNumber(currentStepNumber);
        $isolationsWrapper.find('.btn-add-isolations-to-step').fadeIn();

        if (!isGettingDeactivationStep && $nextStep.length === 0) {
            isGettingDeactivationStep = true;
            // Add the next step
            $.ajax({
                url: '/Hecp/GetDeactivationStep?stepNum=' + nextStepNumber,
                type: 'get',
                dataType: 'html',
                cache: false,
                success: function (newDeactivationStep) {
                    let safeDeactivationStep = safeResponseFilter(newDeactivationStep);
                    // this is the id for the Li
                    let id = "deactLi-" + nextStepNumber;
                    //creating the LI only with the up/down anchors
                    let litoappend = "<li class='list-unstyled' id=" + id + ">" + "</li>";
                    //appending the LI to the deactivation wrapper(does not contain the actual empty deactivation step)
                    $('#wrapper-deactivation-steps').append(litoappend);
                    //getting the Li which was appended to the wrpper
                    let x = $('#' + id);

                    //appending the empty deactivationstep to the Li
                    $(safeDeactivationStep)
                        .appendTo(x);

                    //Appendin the Li with the deactivation step to the wrapper
                    $(x)
                        .css('visibility', 'hidden')
                        //.appendTo($('#wrapper-deactivation-steps'))
                        .css({ opacity: 0, visibility: 'visible' })
                        .animate({ opacity: 1.0 }, 250);
                    isGettingDeactivationStep = false;
                },
                error: function (msg) {
                    console.log(msg);
                    isGettingDeactivationStep = false;
                },
                complete: function () {
                    disableMoveStepButtons(
                        'wrapper-deactivation-steps',
                        'row-hecp-deactivation-step',
                        'deact-step-up-btn',
                        'deact-step-down-btn',
                        'deact-move-step'
                    );
                }
            });
        }
    };
})();

function UploadStepImage(stepnumber) {
    //var node = $(this).parent()[0].firstElementChild.files[0];
    isDirty = true;
    var stepNode = $('#row-hecp-deactivation-step-' + stepnumber);
    var imageList = stepNode.find('#HecpImagesList');
    var uploadedFiles = imageList.find("#input-HecpDeactivationSteps-" + stepnumber + "-UploadedFiles")[0].files;

    var imageListGrid = imageList.find("#ImageListGrid");
    imageListGrid.html('');
    for (var i = 0; i < uploadedFiles.length; i++) {
        var filename = uploadedFiles[i].name;
        var format = uploadedFiles[i].name.split('.')[1];
        var imageListRow = "<li><label name='HecpDeactivationSteps[" + stepnumber + "].HecpDeactivationImages[" + i + "].name'>" + filename + "<label/>" +
            "<a class='btn btn-secondary-gray btn-rounded btn-trash' onclick='removeDeactivationImage(" + stepnumber + ", " + i + "); '><span class='fa fa-trash' style='padding: 0.4rem;'></span></a>" +
            "<input name='HecpDeactivationSteps[" + stepnumber + "].HecpDeactivationImages[" + i + "].name' hidden='hidden' value='" + filename + "'/>" +
            "<input name='HecpDeactivationSteps[" + stepnumber + "].HecpDeactivationImages[" + i + "].Extension' hidden='hidden' value='" + format + "' />" +
            "<input name='HecpDeactivationSteps[" + stepnumber + "].HecpDeactivationImages[" + i + "].DataString' hidden='hidden' value='" + "tempstring" + "' /></li>";
        imageListGrid.append(imageListRow);
    }
};

function UploadDescriptionImage() {
    let uploadedFiles = $("#description-image-container").find("#description-uploadedfiles")[0].files;
    let imageListGrid = $("#description-image-container").find("#description-imagelist-grid");
    if (uploadedFiles.length > 0) {
        isDirty = true;
        // Loop through the uploaded files and append them to the existing list
        for (let i = 0; i < uploadedFiles.length; i++) {
            let filename = uploadedFiles[i].name;
            let format = uploadedFiles[i].name.split('.').pop();
            let reader = new FileReader();

            // Read the file as a data URL
            reader.onload = (function (fileName, fileFormat) {
                return function (e) {
                    let base64Image = e.target.result;
                    let base64Data = base64Image.split(',')[1];
                    let imageListRow = "<li><label name='HecpDescriptionImages[" + (imageListGrid.children().length) + "].Images.Name'>" + fileName + "</label>" +
                        "<div class='description-image-container'><img src='" + base64Image + "'/></div>" +
                        "<a class='btn btn-secondary-gray btn-rounded btn-trash description-image-remove'onclick='removeDescriptionImage(this);'><span class='fa fa-trash' style='padding: 0.4rem;'></span></a>" +
                        "<input name='HecpDescriptionImages[" + (imageListGrid.children().length) + "].Images.Name' hidden='hidden' value='" + fileName + "'/>" +
                        "<input name='HecpDescriptionImages[" + (imageListGrid.children().length) + "].Images.Extension' hidden='hidden' value='" + fileFormat + "' />" +
                        "<input name='HecpDescriptionImages[" + (imageListGrid.children().length) + "].DataString' hidden='hidden' value='" + base64Data + "' /></li>";
                    imageListGrid.append(imageListRow);
                };
            })(filename, format);

            // Read the file as a data URL
            reader.readAsDataURL(uploadedFiles[i]);
        }
        // Making it empty to reupload the same image again if needed
        $("#description-uploadedfiles").val('');
    }
}

function removeDescriptionImage(element) {
    const $item = $(element).closest('li');
    const filename = $item.find('input[name*="Images.Name"]').val();

    $item.remove();

    const $fileInput = $("#description-uploadedfiles");
    const uploadedFiles = $fileInput[0].files;

    for (let i = 0; i < uploadedFiles.length; i++) {
        if (uploadedFiles[i].name === filename) {

            $fileInput.val('');
            break;
        }
    }
    isDirty = true;
}

//This method is to renumber indices to avoid any issue post deleting an image
function reNumberDescriptionImageIndices() {
    let $imageListItems = $('#description-imagelist-grid li');

    $imageListItems.each((index, item) => {
        let $item = $(item);
        let $label = $item.find('label');
        let $nameInput = $item.find('input[name*="Images.Name"]');
        let $extensionInput = $item.find('input[name*="Images.Extension"]');
        let $dataStringInput = $item.find('input[name*="DataString"]');

        $label.attr('name', `HecpDescriptionImages[${index}].Images.Name`);
        $nameInput.attr('name', `HecpDescriptionImages[${index}].Images.Name`);
        $extensionInput.attr('name', `HecpDescriptionImages[${index}].Images.Extension`);
        $dataStringInput.attr('name', `HecpDescriptionImages[${index}].DataString`);
    });
}
function UploadTryoutStepImage(stepnumber) {
    isDirty = true;
    var stepNode = $('#row-hecp-tryout-step-' + stepnumber);
    var imageList = stepNode.find('#HecpTryOutImagesList');
    var uploadedFiles = imageList.find("#input-TryoutProcedure-" + stepnumber + "-UploadedFiles")[0].files;

    var imageListGrid = imageList.find("#TryoutImageListGrid");
    imageListGrid.html('');
    for (var i = 0; i < uploadedFiles.length; i++) {
        var filename = uploadedFiles[i].name;
        var format = uploadedFiles[i].name.split('.')[1];
        var imageListRow = "<li><label name='HecpTryoutProcedure[" + stepnumber + "].HecpTryoutImages[" + i + "].name'>" + filename + "<label/>" +
            "<a class='btn btn-secondary-gray btn-rounded btn-trash' onclick='removeTryoutImage(" + stepnumber + ", " + i + "); '><span class='fa fa-trash' style='padding: 0.4rem;'></span></a>" +
            "<input name='HecpTryoutProcedure[" + stepnumber + "].HecpTryoutImages[" + i + "].name' hidden='hidden' value='" + filename + "'/></li>" +
            "<input name='HecpTryoutProcedure[" + stepnumber + "].HecpTryoutImages[" + i + "].Extension' hidden='hidden' value='" + format + "' />" +
            "<input name='HecpTryoutProcedure[" + stepnumber + "].HecpTryoutImages[" + i + "].DataString' hidden='hidden' value='" + "tempstring" + "' /></li>";
        imageListGrid.append(imageListRow);
    }
};
// Shows the isolation table inside the discrete deactivation step
function showIsolationTableForStep(stepNumber) {
    setIsDirtyTrue();

    let $isolationsWrapper = getIsolationsWrapperForStepNumber(stepNumber);

    $isolationsWrapper.html(
        '<div style="border:1px solid #BDBDBD;margin-bottom:0.5rem;"><div class="row wrapper-discrete-isolation-header"><div class="col-xs-3 col-md-3" style="font-size:1.25rem;">Isolations</div></div><div class="row wrapper-discrete-isolation-header" style="justify-content: space-between;margin-right: 0;margin-left: 0;background-color:#1E88E5;color:white;">' +
        '<div class="col-xs-1 col-md-1 text-center">ID</div> <div class="col-xs-2 col-md-2">Name</div>' +
        '<div class="col-xs-2 col-md-2 text-center">Panel/Tool</div> <div class="col-location">Location</div>' +
        '<div class="col-minor-model-header text-center">Model</div>' +
        '<div class="col-set-to-state text-center">Set to</div>' +
        '<div class="col-action-menu-header text-center" style="margin-left:-30px;">Action</div>' +
        '</div></div>' +
        '<div class="wrapper-discrete-isolation-body padding"></div>'
    );
    addNewUIIsolationToStep(stepNumber, 0);
}

function createEmptyisolationStepTemplate(stepNumber) {
    setIsDirtyTrue();

    let $isolationsWrapper = getIsolationsWrapperForStepNumber(stepNumber);

    $isolationsWrapper.html(
        '<div style="border:1px solid #BDBDBD;margin-bottom:0.5rem;"><div class="row wrapper-discrete-isolation-header"><div class="col-xs-3 col-md-3" style="font-size:1.25rem;">Isolations</div></div><div class="row wrapper-discrete-isolation-header" style="justify-content: space-between;margin-right:0;margin-left:0;background-color:#1E88E5;color:white;">' +
        '<div class="col-xs-1 col-md-1 text-center">ID</div> <div class="col-xs-2 col-md-2 text-center">Name</div>' +
        '<div class="col-xs-2 col-md-2 text-center">Panel/Tool</div> <div class="col-location text-center">Location</div>' +
        '<div class="col-minor-model-header text-center">Model</div>' +
        '<div class="col-set-to-state text-center">Set to</div>' +
        '<div class="col-action-menu-header text-center" style="margin-left:-30px;">Action</div>' +
        '</div></div>' +
        '<div class="wrapper-discrete-isolation-body padding"></div>'
    );
}

function createEmptyisolationStepTryoutTemplate(stepNumber) {
    setIsDirtyTrue();

    let $isolationsWrapper = getIsolationsWrapperForTryoutStepNumber(stepNumber);

    $isolationsWrapper.html(
        '<div style="border:1px solid #BDBDBD;margin-bottom:0.5rem;"><div class="row wrapper-discrete-isolation-header tryout-isolation-row"><div class="col-xs-3 col-md-3" style="font-size:1.25rem;">Isolations</div></div><div class="row wrapper-discrete-isolation-header tryout-isolation-row" style="justify-content: space-between;margin-right: 0;margin-left: 0;background-color:#1E88E5;color:white;">' +
        '<div class="col-xs-2 col-md-2 text-center">ID</div> <div class="col-xs-2 col-md-2 text-center">Name</div>' +
        '<div class="col-xs-2 col-md-2 text-center">Panel/Tool</div> <div class="col-location text-center">Location</div>' +
        '<div class="col-minor-model-header text-center">Model</div>' +
        '<div class="col-action-menu-header text-center" style="margin-left:-15px;">Action</div>' +
        '</div></div>' +
        '<div class="wrapper-discrete-isolation-body padding"></div>'
    );
}

let addNewUIIsolationToStep = (function () {
    let isGettingNewIsolationStep = false;

    return function (stepNumber, isolationNumber) {
        let $isolationsWrapper = getIsolationsWrapperForStepNumber(stepNumber);
        let $isolationsBodyWrapper = $isolationsWrapper.find('.wrapper-discrete-isolation-body');
        let nextIsolationNumber = isolationNumber + 1;
        let allAssociatedMinorModels = [];
        let allAssociatedMinorModelIds = [];

        $(".DeactivationAssociatedMinorModels").each(function (i, v) {
            allAssociatedMinorModels.push(v.value.trim());

        });
        $(".DeactivationAssociatedMinorModelIds").each(function (i, v) {
            allAssociatedMinorModelIds.push(v.value.trim());
        });

        // If the next isolation does NOT already exist
        if (
            !isGettingNewIsolationStep &&
            $isolationsBodyWrapper.find(
                '.wrapper-discrete-isolation-row[isolation-number=' +
                isolationNumber +
                ']'
            ).length === 0
        ) {
            // Then add a new isolation row
            isGettingNewIsolationStep = true;

            $.ajax({
                url:
                    '/Hecp/GetIsolationRow?stepNum=' +
                    stepNumber +
                    '&isolationNum=' +
                    isolationNumber +
                    '&associatedMinorModels=' +
                    allAssociatedMinorModels.join(",") +
                    '&associatedMinorModelIds=' +
                    allAssociatedMinorModelIds.join(",") +
                    '&program=' + program.trim(),
                type: 'get',
                dataType: 'html',
                cache: false,
                async: false,
                //contentType: 'application/json; charset=utf-8',
                success: function (newIsolationRow) {
                    if ($isolationsBodyWrapper.length == 0) {
                        createEmptyisolationStepTemplate(stepNumber);
                        $isolationsWrapper = getIsolationsWrapperForStepNumber(stepNumber);
                        $isolationsBodyWrapper = $isolationsWrapper.find('.wrapper-discrete-isolation-body');
                    }
                    let safeIsolationRow = safeResponseFilter(newIsolationRow);
                    $(safeIsolationRow)
                        .css('visibility', 'hidden')
                        .appendTo($isolationsBodyWrapper)
                        .css({ opacity: 0, visibility: 'visible' })
                        .animate({ opacity: 1.0 }, 250);
                    isGettingNewIsolationStep = false;
                    //append border element to the isolation row given it doesn't have one already
                    $('.wrapper-discrete-isolation-row').each(function (index, item) {
                        if ($('.wrapper-discrete-isolation-body').find('.border-isolation').length <= index) {
                            $(item).append('<div class="col-xs-12 col-md-12 border-isolation"></div>');
                        }
                    });
                },
                error: function (msg) {
                    console.log(msg);
                    isGettingNewIsolationStep = false;
                },
            });
        }
    };
})();

let addNewTryoutUIIsolationToStep = (function () {
    let isGettingNewIsolationStep = false;

    return function (stepNumber, isolationNumber) {
        let $isolationsWrapper = getIsolationsWrapperForTryoutStepNumber(stepNumber + 1);
        let $isolationsBodyWrapper = $isolationsWrapper.find('.wrapper-discrete-isolation-body');
        let nextIsolationNumber = isolationNumber + 1;
        let allAssociatedMinorModels = [];
        let allAssociatedMinorModelIds = [];

        $(".TryoutAssociatedMinorModels").each(function (i, v) {
            allAssociatedMinorModels.push(v.value.trim());
        })

        $(".TryoutAssociatedMinorModelIds").each(function (i, v) {
            allAssociatedMinorModelIds.push(v.value.trim());
        });
        // If the next isolation does NOT already exist
        if (
            !isGettingNewIsolationStep &&
            $isolationsBodyWrapper.find(
                '.wrapper-discrete-isolation-row[isolation-number=' +
                isolationNumber +
                ']'
            ).length === 0
        ) {
            // Then add a new isolation row
            isGettingNewIsolationStep = true;

            $.ajax({
                url:
                    '/Hecp/GetTryoutIsolationRow?stepNum=' +
                    stepNumber +
                    '&isolationNum=' +
                    isolationNumber +
                    '&associatedMinorModels=' +
                    allAssociatedMinorModels.join(",") +
                    '&associatedMinorModelIds=' +
                    allAssociatedMinorModelIds.join(",") +
                    '&program=' + program.trim(),
                type: 'get',
                dataType: 'html',
                cache: false,
                async: false,
                success: function (newIsolationRow) {
                    if ($isolationsBodyWrapper.length == 0) {
                        createEmptyisolationStepTryoutTemplate(stepNumber + 1);
                        $isolationsWrapper = getIsolationsWrapperForTryoutStepNumber(stepNumber + 1);
                        $isolationsBodyWrapper = $isolationsWrapper.find('.wrapper-discrete-isolation-body');
                    }
                    let safeIsolationRow = safeResponseFilter(newIsolationRow);
                    $(safeIsolationRow)
                        .css('visibility', 'hidden')
                        .appendTo($isolationsBodyWrapper)
                        .css({ opacity: 0, visibility: 'visible' })
                        .animate({ opacity: 1.0 }, 250);
                    isGettingNewIsolationStep = false;
                    //append border element to the isolation row given it doesn't have one already
                    $('.wrapper-discrete-isolation-row').each(function (index, item) {
                        if ($('.wrapper-discrete-isolation-body').find('.border-isolation').length <= index) {
                            $(item).append('<div class="col-xs-12 col-md-12 border-isolation"></div>');
                        }
                    });
                },
                error: function (msg) {
                    console.log(msg);
                    isGettingNewIsolationStep = false;
                },
            });
        }
    };
})();

function getIsolationsWrapperForStepNumber(stepNumber) {
    return $(
        '.row-hecp-deactivation-step[step-number=' + stepNumber + ']'
    ).find('.wrapper-discrete-deactivation-step-isolations');
}

function getIsolationsWrapperForTryoutStepNumber(stepNumber) {
    return $(
        '.row-hecp-step[step-number=' + stepNumber + ']'
    ).find('.wrapper-discrete-tryout-step-isolations');
}

$('.character-check').on('keyup', function () {
    checkCharacterIsAcceptable(this.id);
});

function getIsolationsWrapperForStepNumberReactivation(stepNumber) {
    return $(
        '.row-hecp-reactivation-step[step-number=' + stepNumber + ']'
    ).find('.wrapper-discrete-reactivation-step-isolations');
}

$('.non-blank-input').on('blur', function () {
    checkIsBlank(this.id);
});

$('.character-check').on('blur', function () {
    checkCharacterIsAcceptable(this.id);
});

$('.non-blank-input').on('input', function () {
    checkIsBlank(this.id);
});

function checkIsBlank(inputFieldId) {
    var inputField = document.getElementById(inputFieldId);
    var val = inputField.value.trim();
    var warningSpan = document.getElementById(inputFieldId + "-span");

    // Add the warning
    if (val === '' && warningSpan === null) {
        warningSpan = document.createElement("SPAN");
        warningSpan.classList.add('blank-warning');
        warningSpan.id = inputFieldId + '-span';
        warningSpan.innerHTML = "Cannot leave blank";

        inputField.parentNode.appendChild(warningSpan);

        $('#' + inputFieldId).addClass("blank-warning-input");
    }
    // Take away the warning    
    else if (val != '' && warningSpan != null) {
        warningSpan.parentNode.removeChild(warningSpan);
        $('#' + inputFieldId).removeClass("blank-warning-input");
    }
}

function checkCharacterIsAcceptable(inputFieldId) {
    var element = inputFieldId;
    var inputField = document.getElementById(inputFieldId);
    var val = inputField.value.trim();
    var warningSpan = document.getElementById(inputFieldId + "-span");

    // Add the warning
    if (!val.match("^[a-zA-Z0-9_)(-.&\/\\\\ ]*$") && warningSpan === null) {
        warningSpan = document.createElement("SPAN");
        warningSpan.clientWidth = '15%'
        warningSpan.classList.add('blank-warning');
        warningSpan.id = inputFieldId + '-span';
        warningSpan.innerHTML = "Invalid name format - Valid name can contain Alphanumeric characters, '&', '-' , '_' , '()' , '/' , '\\' ";
        inputField.parentNode.appendChild(warningSpan);

        $('#' + inputFieldId).addClass("blank-warning-input");
    }
    // Take away the warning
    else if (val.match("^[a-zA-Z0-9_)(-.&\/\\\\ ]*$") && warningSpan != null) {

        warningSpan.parentNode.removeChild(warningSpan);
        $('#' + inputFieldId).removeClass("blank-warning-input");
        if (val === '') {
            checkIsBlank(element);
        }
    }
}

function checkIfFormatIsSupport(inputFieldId) {
    var inputField = document.getElementById(inputFieldId);
    var val = inputField.value.trim();
    var warningSpan = document.getElementById(inputFieldId + "-span");

    // Add the warning
    if (val !== 'PNG' && val !== 'JPEG' && warningSpan === null) {
        warningSpan = document.createElement("SPAN");
        warningSpan.classList.add('blank-warning');
        warningSpan.id = inputFieldId + '-span';
        warningSpan.innerHTML = "Format is not supported";

        inputField.parentNode.appendChild(warningSpan);

        $('#' + inputFieldId).addClass("blank-warning-input");
    }
    // Take away the warning    
    else if (val != '' && warningSpan != null) {
        warningSpan.parentNode.removeChild(warningSpan);
        $('#' + inputFieldId).removeClass("blank-warning-input");
    }
}

function checkCharacterIsAcceptable(inputFieldId) {
    var element = inputFieldId;
    var inputField = document.getElementById(inputFieldId);
    var val = inputField.value.trim();
    var warningSpan = document.getElementById(inputFieldId + "-span");

    // Add the warning
    if (!val.match("^[a-zA-Z0-9_)(-.&\/\\\\ ]*$") && warningSpan === null) {
        warningSpan = document.createElement("SPAN");
        warningSpan.clientWidth = '15%'
        warningSpan.classList.add('blank-warning');
        warningSpan.id = inputFieldId + '-span';
        warningSpan.innerHTML = "Invalid name format - Valid name can contain Alphanumeric characters, '&', '-' , '_' , '()' , '/' , '\\' ";
        inputField.parentNode.appendChild(warningSpan);

        $('#' + inputFieldId).addClass("blank-warning-input");
    }
    // Take away the warning
    else if (val.match("^[a-zA-Z0-9_)(-.&\/\\\\ ]*$") && warningSpan != null) {

        warningSpan.parentNode.removeChild(warningSpan);
        $('#' + inputFieldId).removeClass("blank-warning-input");
        if (val === '') {
            checkIsBlank(element);
        }
    }
}

function checkIfFormatIsSupport(inputFieldId) {
    var inputField = document.getElementById(inputFieldId);
    var val = inputField.value.trim();
    var warningSpan = document.getElementById(inputFieldId + "-span");

    // Add the warning
    if (val !== 'PNG' && val !== 'JPEG' && warningSpan === null) {
        warningSpan = document.createElement("SPAN");
        warningSpan.classList.add('blank-warning');
        warningSpan.id = inputFieldId + '-span';
        warningSpan.innerHTML = "Format is not supported";

        inputField.parentNode.appendChild(warningSpan);

        $('#' + inputFieldId).addClass("blank-warning-input");
    }
    // Take away the warning
    else if (val != '' && warningSpan != null) {
        warningSpan.parentNode.removeChild(warningSpan);
        $('#' + inputFieldId).removeClass("blank-warning-input");
    }
}

function checkIsIsolationRowBlank(curruentInputFieldId, otherInputFieldId) {

    var currentInputField = document.getElementById(curruentInputFieldId);
    var currentVal = currentInputField.value.trim();

    var otherInputField = document.getElementById(otherInputFieldId);
    var otherVal = otherInputField.value.trim();

    if (currentVal != '' || otherVal != '') {
        checkIsBlank(otherInputFieldId);
        checkIsBlank(curruentInputFieldId);
    }
}

function removeDeactivationStep(stepNumber) {
    var stepOrder = getStepOrder(5);
    $stepRow = $('#row-hecp-deactivation-step-' + stepNumber);
    var data = new FormData($('#hecp-deactivation-steps-form')[0]);
    tinymce.remove();
    $.ajax({
        url: '/Hecp/RemoveStepFromHecpViewModel/' + stepNumber + '/' + stepOrder,
        type: 'post',
        data: data,
        processData: false,
        contentType: false,
        success: function (response) {
            $stepRow.hide('slow', function () {
                var safeResponse = safeResponseFilter(response)
                BuildContent(safeResponse);                
                validateDeactivationStepsForm();
                setIsDirtyTrue();
                initTinyMceForDeactivation();
            });
        },
        error: function () {
            initTinyMceForDeactivation();
            console.log;
        }
    });
}

function removeTryoutStep(stepNumber) {
    var stepOrder = getStepOrder(6);
    stepNumber = stepNumber - 1;
    var data = new FormData($('#steps-form')[0]);
    $stepRow = $('#row-hecp-tryout-step-' + stepNumber);
    tinymce.remove();
    $.ajax({
        url: '/Hecp/RemoveStepFromHecpTryoutModel/' + stepNumber + '/' + stepOrder,
        type: 'post',
        data: data,
        processData: false,
        contentType: false,
        success: function (response) {
            $stepRow.hide('slow', function () {
                var safeResponse = safeResponseFilter(response)
                BuildContent(safeResponse);                
                validateTryoutProcedure();
                setIsDirtyTrue();
                initTinyMceForTryout();
            });
        },
        error: function () {
            initTinyMceForTryout();
            console.log;
        }
    });
}

function BuildContent(response) {
    $('#hecp-content').html('');
    $('#hecp-content').append(response);
}


function removeTitle(stepNumber) {

    $stepRow = $('#row-hecp-atachapter-step-' + stepNumber);
    let program = $("#Program").val();
    let hecpATAId = $("#Id").val();
    var data = new FormData($('#atachapter-form')[0]);
    $.ajax({
        url: '/Hecp/RemoveStepFromAtaChapter/' + stepNumber,
        type: 'post',
        data: data,
        processData: false,
        contentType: false,
        success: function (response) {
            $stepRow.hide('slow', function () {
                var safeResponse = safeResponseFilter(response);
                $('#hecp-content').html(safeResponse);
                var ataCount = $('.row-hecp-atachapter-step').length;
                if (ataCount > 2) {
                    $('.row-hecp-atachapter-step').last().remove();
                    var stepNum = parseInt($('.row-hecp-atachapter-step').last().attr('step-number'));
                    addNewAtaChapter(stepNum, program, hecpATAId);
                }
            });
        },
        error: function () {
            toastr.error("Failed", "Failed to delete ATA chapter", TOAST_OPTIONS);
        }
    });

}

function removeLocationText(stepNumber) {

    $stepRow = $('#row-hecp-atalocation-step-' + stepNumber);
    var data = new FormData($('#atachapter-form')[0]);
    $.ajax({
        url: '/Hecp/RemoveStepFromLocations/' + stepNumber,
        type: 'post',
        processData: false,
        contentType: false,
        data: data,
        success: function (response) {
            var safeResponse = safeResponseFilter(response);
            $('#wrapper-location-steps').html($(safeResponse).find('#wrapper-location-steps').html());
            $stepRow.hide('slow');
        },
        error: console.log,
    });

}

function editIsolationRow(editIconId, saveElemId, editIsos) {
    var editIcon = $('#' + editIconId);
    var saveElem = $('#' + saveElemId);
    var isoElems = $('.' + editIsos);
    if ($(editIcon).hasClass("fa-pencil")) {
        $(editIcon).data('isEditOpen', true);
        $(editIcon).removeClass("fa-pencil");
        $(editIcon).addClass("fa-close");
        $(editIcon).css({ color: "red" });
        $(saveElem).css({ color: "blue" });
        $(isoElems).each(function () {
            $(this).attr("readonly", false);
            $(this).data('initialValue', $(this).val());
        })
    }
    else {
        $(editIcon).data('isEditOpen', false)
        $(editIcon).removeClass("fa-close");
        $(editIcon).addClass("fa-pencil");
        $(editIcon).css({ color: "#0000FF" });
        $(saveElem).css({ color: "darkslategrey" });
        $(isoElems).each(function () {
            $(this).attr("readonly", true)
            $(this).val($(this).data('initialValue'));
        })
    }
}

function saveIsolationRow(editIconId, saveIconId, editIsos, Program) {
    var editIcon = $('#' + editIconId);
    var saveIcon = $('#' + saveIconId);
    if ($(editIcon).data("isEditOpen") != true) {
        return false;
    }

    var checkForChanges = parseInt('0', 10);

    if (checkForChanges == false)
        var isoElems = $('.' + editIsos);
    $(isoElems).each(function () {
        if ($(this).val().toString() != $(this).data('initialValue').toString()) {
            checkForChanges++;
        }
    });
    if (checkForChanges == 0) {
        toastr["error"]("Please edit before saving the isolation.")
        $(editIcon).data("isEditOpen", false);
        $(editIcon).removeClass("fa-close");
        $(editIcon).addClass("fa-pencil");
        $(editIcon).css({ color: "#0000FF" });
        $(saveIcon).css({ color: "darkslategrey" });
        $(isoElems).each(function () {
            $(this).attr("readonly", true)
            $(this).val($(this).data('initialValue'));
        })
        return false;
    }
    var isolationComponent = {
        Id: $.trim($(isoElems)[0].value),
        CircuitId: $.trim($(isoElems)[1].value),
        CircuitNomenclature: $.trim($(isoElems)[2].value),
        Panel: $.trim($(isoElems)[3].value),
        Location: $.trim($(isoElems)[4].value),
        Program: $.trim(Program)
    };
    $(editIcon).data("isEditOpen", false);
    $(editIcon).removeClass("fa-close");
    $(editIcon).addClass("fa-pencil");
    $(editIcon).css({ color: "#0000FF" });
    $(saveIcon).css({ color: "darkslategrey" });

    $.ajax({
        url: "/HECP/UpdateIsolationComponents",
        type: 'POST',
        data: JSON.stringify(isolationComponent),
        dataType: 'json',
        contentType: 'application/json; charset=utf-8',
        error: function (xhr) {
            var errorMsg = xhr.Status == 400 ? xhr.responseText : "";
            toastr.error(errorMsg, "Failed to Update Isolation Components", TOAST_OPTIONS);
        },
        success: function (response) {
            if (response.message == "Duplicate Component.") {
                if (!String.isNullOrEmpty(response.data.circuitId)) {
                    toastr["error"]("Isolation Component with ID " + response.data.circuitId + " already exists.", "Error", TOAST_OPTIONS);
                    $(isoElems).each(function () {
                        $(this).attr("readonly", true)
                        $(this).val($(this).data('initialValue'));
                    })
                }
                else {
                    toastr["error"]("Isolation Component with Name " + response.data.circuitNomenclature + " already exists.", "Error", TOAST_OPTIONS);
                    $(isoElems).each(function () {
                        $(this).attr("readonly", true)
                        $(this).val($(this).data('initialValue'));
                    })
                }
            }
            else {
                toastr.success("Updated Isolation Components", "Success");
                $(isoElems).each(function () {
                    $(this).attr("readonly", true);
                    $(this).data('initialValue', $(this).val);
                })
            }
        },
        complete: function () {
            $(isoElems).each(function () {
                $(this).attr("readonly", true)
            })
        }
    });
}

function removeIsolationRow(stepNumber, isolationNumber, hasAnySignatures) {
    var stepOrder = getStepOrder(5);
    var data = new FormData($('#hecp-deactivation-steps-form')[0]);
    if (!hasAnySignatures) {
        var $isolationsWrapper = getIsolationsWrapperForStepNumber(stepNumber);
        tinymce.remove();
        $.ajax({
            url:
                '/Hecp/RemoveIsolationFromDeactivationStep/' +
                stepNumber +
                '/' +
                isolationNumber +
                '/' +
                stepOrder,
            type: 'post',
            data: data,
            processData: false,
            contentType: false,
            success: function (response) {
                var $isolationRow = $isolationsWrapper.find(
                    '.wrapper-discrete-isolation-row[isolation-number=' +
                    isolationNumber +
                    ']'
                );
                $isolationRow.hide('slow', function () {
                    var $hecp_Content = $('#hecp-content');
                    var safeResponse = safeResponseFilter(response);
                    $hecp_Content.html(safeResponse);
                    validateDeactivationStepsForm();
                    setIsDirtyTrue();
                    initTinyMceForDeactivation();
                });
            },
            error: function () {
                initTinyMceForDeactivation();
                console.log;
            }
        });
    }
}

function removeTryoutIsolationRow(stepNumber, isolationNumber, hasAnySignatures) {
    var stepOrder = getStepOrder(6);
    var data = new FormData($('#steps-form')[0]);
    if (!hasAnySignatures) {
        tinymce.remove();
        var $isolationsWrapper = getIsolationsWrapperForTryoutStepNumber(stepNumber + 1);
        $.ajax({
            url:
                '/Hecp/RemoveIsolationFromTryoutStep/' +
                stepNumber +
                '/' +
                isolationNumber +
                '/' +
                stepOrder,
            type: 'post',
            data: data,
            processData: false,
            contentType: false,
            success: function (response) {
                var $isolationRow = $isolationsWrapper.find(
                    '.wrapper-discrete-isolation-row[isolation-number=' +
                    isolationNumber +
                    ']'
                );
                $isolationRow.hide('slow', function () {
                    var $hecp_Content = $('#hecp-content');
                    var safeResponse = safeResponseFilter(response);
                    $hecp_Content.html(safeResponse);
                    validateTryoutProcedure();
                    setIsDirtyTrue();  
                    initTinyMceForTryout();
                });
            },
            error: function () {
                initTinyMceForTryout();
                console.log;
            }
        });
    }
}


function removeReactivationStep(stepNumber) {
    var stepOrder = getStepOrder(7);
    $stepRow = $('#row-hecp-reactivation-step-' + stepNumber);
    tinymce.remove();
    $.ajax({
        url: '/Hecp/RemoveStepFromHecpViewModelForReactivation/' + stepNumber + '/' + stepOrder,
        type: 'post',
        data: $('#hecp-reactivation-steps-form').serialize(),
        success: function (response) {
            $stepRow.hide('slow', function () {
                var $hecp_Content = $('#hecp-content');
                var safeResponse = safeResponseFilter(response);
                $hecp_Content.html(safeResponse);                
                validateReactivationStepsForm();
                setIsDirtyTrue();
                initTinymceForReactivation();
            });
        },
        error: function () {
            initTinymceForReactivation();
            console.log;
        }
    });
}

function removeIsolationRowForReactivation(stepNumber, isolationNumber, hasAnySignatures) {
    if (!hasAnySignatures) {
        var $isolationsWrapper = getIsolationsWrapperForStepNumber(stepNumber);

        $.ajax({
            url:
                '/Hecp/RemoveIsolationFromReactivationStep/' +
                stepNumber +
                '/' +
                isolationNumber,
            type: 'post',
            data: $('#hecp-reactivation-steps-form').serialize(),
            success: function (response) {
                var $isolationRow = $isolationsWrapper.find(
                    '.wrapper-discrete-isolation-row[isolation-number=' +
                    isolationNumber +
                    ']'
                );

                $isolationRow.hide('slow', function () {
                    var $hecp_Content = $('#hecp-content');
                    var safeResponse = safeResponseFilter(response);
                    $hecp_Content.html(safeResponse);
                    validateDeactivationStepsForm();
                    setIsDirtyTrue();
                });
            },
            error: console.log
        });
    }
}

function isolationIsValid(isolation) {
    return (!String.isNullOrEmpty(isolation.circuitdesc)
    );
}

function isolationIsValidOrBlank(isolation) {
    return (
        isolationIsValid(isolation) ||
        String.isNullOrEmpty(isolation.circuitdesc)
    );
}

function validateDeactivationSteps(result, step) {
    return (
        result &&
        // either isolations are empty (which is valid)
        (!step.isolations.length ||
            // all isolations[] are either fully filled-in or blank
            ((!String.isNullOrEmpty(step.description) &&
                step.isolations.every(isolationIsValidOrBlank))))
    );
}

function deactivationStepsAreValid(deactivationSteps) {
    // return false if there aren't any deactivation steps 
    if (deactivationSteps.length == 0) { return false };
    return (
        !!deactivationSteps.length &&
        !!deactivationSteps.filter(function (step) {
            return step.description.length > 0;
        }).length &&
        deactivationSteps.reduce(validateDeactivationSteps, true)
    );

}


function validateDeactivationStepsForNewStepAddForm() {
    if (isDeactivationStepsFormValid()) {
        $('#btn-deactivationsteps-continue').prop('disabled', '');
        $('#btn-deactivationsteps-continue').removeAttr('title');

    } else {
        $('#btn-deactivationsteps-continue').prop('disabled', 'disabled');
        $('#btn-deactivationsteps-continue').attr('title', 'Please ensure that at least 1 step and 1 isolation have been fully completed');
    }
}

function validateDeactivationStepsForm() {
    let nextStepButton = $('#btn-deactivationsteps-continue');
    if (isDeactivationStepsFormValid()) {
        $(nextStepButton).prop('disabled', '');
        $(nextStepButton).removeAttr('title');

    } else {
        $(nextStepButton).prop('disabled', 'disabled');
        $(nextStepButton).attr('title', 'Please ensure that at least 1 step and 1 isolation have been fully completed');
    }
    validateMinorModelSelection($(nextStepButton));
}

function validateMinorModelSelection(navigationButton) {
    let isolationCount = $('.isolation-minor-model-list').length;
    let minorModelCount = $('.isolation-minor-model-list').find('.input-system-minormodel').length;
    let noMinorModelSelected = false;
    //check that isolation row is present and each isolation has minor models
    if (isolationCount > 0 && minorModelCount > 0) {
        $('.isolation-minor-model-list').each(function (index, item) {
            noMinorModelSelected = $(item).children().find('.input-system-minormodel:checked').length === 0;
            if (noMinorModelSelected) {
                $(navigationButton).prop('disabled', 'disabled');
                $(navigationButton).attr('title', 'Please ensure that at least 1 step and 1 isolation have been fully completed');
                return false;
            }
        });
        if (!noMinorModelSelected) {
            $(navigationButton).prop('disabled', '');
            $(navigationButton).removeAttr('title');
        }
    }
}

function isDeactivationStepsFormValid() {
    let deactivationSteps =
        $('#wrapper-deactivation-steps')
            .find('.row-hecp-deactivation-step')
            .get()
            .map(function (step) {
                return new DeactivationStep(
                    $(step)
                        .find('.discrete-textarea')
                        .val()
                        .trim(),
                    zip(
                        $(step)
                            .find('.input-system_circuit')
                            .get()
                            .map(function (circuitname) {
                                return $(circuitname)
                                    .val()
                                    .trim();
                            }),
                        $(step)
                            .find('.input-circuit_nomenclature')
                            .get()
                            .map(function (circuitdesc) {
                                return $(circuitdesc)
                                    .val()
                                    .trim();
                            }),
                        $(step)
                            .find('.input-system_circuitdetail_id')
                            .get()
                            .map(function (circuitid) {
                                return $(circuitid)
                                    .val()
                                    .trim();
                            })
                    )
                );
            }) || [];

    return deactivationStepsAreValid(deactivationSteps);
}

function _valueIsEmpty($obj) {
    return $obj.val().trim() === '';
}

let addNewUIReactivationStep = (function () {

    let isGettingReactivationStep = false;

    return function (currentStepNumber, hecpId) {

        setIsDirtyTrue();

        // If the next step does not already exist
        let nextStepNumber = currentStepNumber + 1;
        let $nextStep = $(
            '.row-hecp-reactivation-step[step-number=' +
            nextStepNumber +
            ']'
        );

        // Show the "Add Isolations For Step # button"
        let $isolationsWrapper = getIsolationsWrapperForStepNumberReactivation(currentStepNumber);
        $isolationsWrapper.find('.btn-add-isolations-to-step').fadeIn();

        if (!isGettingReactivationStep && $nextStep.length === 0) {
            isGettingReactivationStep = true;
            // Add the next step
            $.ajax({
                url: '/Hecp/GetReactivationStep?stepNum=' + nextStepNumber + '&hecpId=' + hecpId,
                type: 'get',
                dataType: 'html',
                cache: false,
                success: function (newReactivationStep) {
                    let safeReactivationStep = safeResponseFilter(newReactivationStep);
                    // this is the id for the Li
                    let id = "reactLi-" + nextStepNumber;
                    //creating the LI only with the up/down anchors
                    let litoappend = "<li class='list-unstyled' id=" + id + ">" + "</li>";
                    //appending the LI to the reactivation wrapper(does not contain the actual empty reactivation step)
                    $('#wrapper-reactivation-steps').append(litoappend);
                    //getting the Li which was appended to the wrpper
                    let x = $('#' + id);
                    $(safeReactivationStep)
                        .appendTo(x);

                    //Appending the Li with the reactivation step to the wrapper
                    $(x)
                        .css('visibility', 'hidden')
                        .appendTo($('#wrapper-reactivation-steps'))
                        .css({ opacity: 0, visibility: 'visible' })
                        .animate({ opacity: 1.0 }, 250);
                    isGettingReactivationStep = false;
                },
                error: function (msg) {
                    console.log(msg);
                    isGettingReactivationStep = false;
                },
                complete: function () {
                    disableMoveStepButtons(
                        'wrapper-reactivation-steps',
                        'row-hecp-reactivation-step',
                        'react-step-up-btn',
                        'react-step-down-btn',
                        'react-move-step'
                    );
                }
            });
        }
    };
})();

function validateReactivationStepsForm() {

    if (isReactivationStepsFormValid()) {
        $('#btn-reactivationsteps-continue').prop('disabled', '');
        $('#btn-reactivationsteps-continue').removeAttr('title');
        setIsDirtyTrue();
    } else {
        $('#btn-reactivationsteps-continue').prop('disabled', 'disabled');
        $('#btn-reactivationsteps-continue').attr('title', 'Please ensure that at least 1 step and 1 isolation have been fully completed');
    }
}

function isReactivationStepsFormValid() {

    var isValid = false;
    $('.selectpicker').selectpicker('refresh');
    $("#wrapper-reactivation-steps .row-hecp-reactivation-step").each(function () {

        var stepDescription = $(this).find(".discrete-textarea");
        var selectIsolations = $(this).find(".select-hecp-reactivation-step select");
        isDescriptionValid = stepDescription.val().trim().length > 0;
        isSelectInputValid = selectIsolations.val().length > 0;
        if (isDescriptionValid && isSelectInputValid) {
            isValid = true;
        }
    });
    return isValid;
}

let addNewTryoutProcedureStep = (function () {
    let isGettingNewTryoutProcedureStep = false;

    return function (newStepNumber) {

        let newStep = $('.row-hecp-step[step-number=' + newStepNumber + ']');
        if (!isGettingNewTryoutProcedureStep && newStep.length === 0) {
            isGettingNewTryoutProcedureStep = true;
            $.ajax({
                url: '/Hecp/GetTryoutProcedureStep?stepNum=' + newStepNumber,
                type: 'get',
                dataType: 'html',
                cache: false,
                success: function (newTryoutStep) {
                    let safeTryoutStep = safeResponseFilter(newTryoutStep);
                    // this is the id for the Li
                    let id = "tryOutLi-" + newStepNumber;
                    //creating the LI only with the up/down anchors
                    let litoappend = "<li class ='list-unstyled' id=" + id + ">" + "</li>";
                    //appending the LI to the deactivation wrapper(does not contain the actual empty deactivation step)
                    $('#wrapper-discrete-steps').append(litoappend);
                    //getting the Li which was appended to the wrpper
                    let x = $('#' + id);
                    $(safeTryoutStep)
                        .appendTo(x);
                    $(x)
                        .css('visibility', 'hidden')
                        .appendTo('#wrapper-discrete-steps')
                        .css({ opacity: 0, visibility: 'visible' })
                        .animate({ opacity: 1.0 }, 500);
                    isGettingNewTryoutProcedureStep = false;
                },
                error: function (msg) {
                    console.log(msg);
                    isGettingNewTryoutProcedureStep = false;
                },
                complete: function () {
                    disableMoveStepButtons(
                        'wrapper-discrete-steps',
                        'row-hecp-step',
                        'tryout-step-up-btn',
                        'tryout-step-down-btn',
                        'tryout-move-step'
                    );
                }
            });
        }
    };
})();

var addNewAtaChapter = (function () {

    let isNewAtaChapter = false;

    return function (currentStepNumber, HecpProgram, HecpId) {

        setIsDirtyTrue();

        // If the next step does not already exist
        let nextStepNumber = currentStepNumber + 1;
        let $nextStep = $(
            '.row-hecp-atachapter-step[step-number=' +
            nextStepNumber +
            ']'
        );

        if (!isNewAtaChapter && $nextStep.length === 0) {
            isNewAtaChapter = true;
            // Add the next step
            $.ajax({
                url: '/Hecp/GetAtaChapter?stepNum=' + nextStepNumber + '&&program=' + HecpProgram + '&&hecpId=' + HecpId,
                type: 'get',
                dataType: 'html',
                cache: false,
                success: function (newAtaStep) {
                    let safeStep = safeResponseFilter(newAtaStep)
                    $(safeStep)
                        .css('visibility', 'hidden')
                        .appendTo($('#wrapper-atachapter-steps'))
                        .css({ opacity: 0, visibility: 'visible' })
                        .animate({ opacity: 1.0 }, 250);
                    isNewAtaChapter = false;
                },
                error: function (msg) {
                    console.log(msg);
                    isNewAtaChapter = false;
                },
                complete: function () {
                    showSearchButton(nextStepNumber);
                    $('#btn-HecpAtaChapter-Search-' + nextStepNumber).prop('hidden', false);
                }
            });
        }
    };
})();

function showSearchButton(stepNumber) {
    let stepCount = $('.hecp-ata-row').length;
    if (stepCount === 1) {
        $('#btn-HecpAtaChapter-Search-' + stepNumber).prop('hidden', false);
    }
    else {
        for (let i = 0; i < stepCount-1; i++) {
            $('#btn-HecpAtaChapter-Search-' + i).prop('hidden', true);
        }
    }
}

function lastStepSearchButton(){
    let stepCount = $('.hecp-ata-row').length - 1;
    $('#btn-HecpAtaChapter-Search-' + stepCount).prop('hidden', false);
}

var addNewLocation = (function () {
    var isNewLocation = false;
    return function (newStepNumber) {
        var nxtStepNumber = newStepNumber + 1;
        var newStep = $('.row-hecp-atalocation-step[step-number=' + nxtStepNumber + ']');
        if (!isNewLocation && newStep.length === 0) {
            isNewLocation = true;
            // Add the next step
            $.ajax({
                url: '/Hecp/GetHecpLocation?stepNum=' + nxtStepNumber,
                type: 'get',
                dataType: 'html',
                cache: false,
                success: function (newLocationStep) {
                    var safeLocationStep = safeResponseFilter(newLocationStep);
                    $(safeLocationStep)
                        .css('visibility', 'hidden')
                        .appendTo($('#wrapper-location-steps'))
                        .css({ opacity: 0, visibility: 'visible' })
                        .animate({ opacity: 1.0 }, 250);
                    isNewLocation = false;
                },
                error: function (msg) {
                    console.log(msg);
                    isNewLocation = false;
                },
            });
        }
    };
})();

function atLeastOneTryoutStepIsValid() {
    var tryoutSteps =
        $('.discrete-textarea')
            .map(function () {
                return $(this)
                    .val()
                    .trim();
            })
            .get() || [];

    return tryoutSteps.some(function (value) {
        return !String.isNullOrEmpty(value);
    });
}

function validateTryoutProcedure() {
    let isValid = atLeastOneTryoutStepIsValid();
    let nextStepButton = $('#btn-discretetryoutprocedure-continue');
    if (isValid) {
        $(nextStepButton).removeAttr('disabled');
        $(nextStepButton).removeAttr('title');
    } else {
        $(nextStepButton).attr('disabled', 'disabled');
        $(nextStepButton).attr('title', 'Please make sure step 1 has been completed');
    }
    validateMinorModelSelection(nextStepButton);
}

function Isolation(values) {
    this.circuitname = values[0] || null;
    this.circuitdesc = values[1] || null;
    this.circuitid = values[2] || null;
    return this;
}

function DeactivationStep(description, isolations) {
    this.description = description;
    this.isolations = isolations.map(function (value) {
        return new Isolation(value);
    });
    return this;
}

function ReactivationStep(description) {
    this.description = description;

    return this;
}

function DisableAddIsolations(stepNumber, stepDescription) {
    var addIsolationLink = document.getElementById('searchIcon-' + stepNumber);

    if (stepDescription.value == "") {
        addIsolationLink.style.visibility = "hidden";
    } else {
        if (addIsolationLink != null) {
            addIsolationLink.style.visibility = "visible";
        }
    }
}

function DisableAddIsolationsForReactivation(stepNumber) {
    var stepDescription = document.getElementById('input-HecpReactivationSteps-' + stepNumber + '-StepDescription');
    var addIsolationLink = document.getElementById('searchIcon-' + stepNumber);

    if (stepDescription.value == "") {
        addIsolationLink.style.visibility = "hidden";
    } else {
        if (addIsolationLink != null) {
            addIsolationLink.style.visibility = "visible";
        }
    }
}

//function deleteSignature(elementId) {
//    $(elementId).hide();
//    $(elementId + ' .is-deleted').attr('value', 'True');
//    //saveSignature();
//}

function setConfirmingAuthorModalVisibility(modalAction) {
    // reset confirming author modal bemsid input
    $('#confirming-author-modal-bemsid-input').val('');
    // 'hide' or 'show'
    $('#confirming-author-modal').modal(modalAction);

    $('#confirming-author-modal').on('shown.bs.modal', function () {
        $('#confirming-author-modal-bemsid-input').trigger('focus');
    });
}

var isSavingSignature = false;
//function saveSignature(discreteId, bemsId, signatureType) {
//    if (!isSavingSignature) {
//        isSavingSignature = true;
//        $.ajax({ // Save signature
//            url: '/Discrete/' + discreteId + '/Signature/' + signatureType + '/' + bemsId,
//            type: 'post',
//            dataType: 'json',
//            cache: false,
//            success: function (response) {
//                if (response.status === HTTP.STATUS.SUCCESS) { // If save success
//                    $.ajax({ // ajax out the current page to refresh DOM
//                        url: window.location.href,
//                        dataType: 'html',
//                        cache: false,
//                        success: function (html) {
//                            $('body').html(html);
//                            toastr.success(response.message, "Signature Saved", TOAST_OPTIONS_SHORT_TIMEOUT);
//                        },
//                        error: function (xhr) {
//                            toastr.error('An error occurred when attempting to view Discrete.', 'Failed', TOAST_OPTIONS);
//                        },
//                        complete: function () {
//                            isSavingSignature = false;
//                        }
//                    });
//                } else { // If save fail
//                    toastr.error(response.message, 'Signature Not Saved', TOAST_OPTIONS);
//                    isSavingSignature = false;
//                }
//            },
//            error: function (msg) { // Unknown error
//                console.log("Error: " + msg);
//                toastr.error('An error occurred when attempting to save signature', 'Error', TOAST_OPTIONS);
//            },
//            complete: function () {
//                isSavingSignature = false;
//            }
//        });
//    }
//};

var isDeletingSignature = false;
//function deleteSignature(signatureId) {
//    if (!isDeletingSignature) {
//        isDeletingSignature = true;
//        $.ajax({ // Delete signature
//            url: '/Discrete/Signature/' + signatureId,
//            type: 'DELETE',
//            dataType: 'json',
//            cache: false,
//            success: function (response) {
//                if (response.status === HTTP.STATUS.SUCCESS) { // If Delete success
//                    $.ajax({ // ajax out the current page to refresh DOM
//                        url: window.location.href,
//                        dataType: 'html',
//                        cache: false,
//                        success: function (html) {
//                            $('body').html(html);
//                            toastr.success(response.message, "Signature Deleted", TOAST_OPTIONS_SHORT_TIMEOUT);
//                        },
//                        error: function (xhr) {
//                            toastr.error('An error occurred when attempting to view Discrete.', 'Failed', TOAST_OPTIONS);
//                        },
//                        complete: function () {
//                            isDeletingSignature = false;
//                        }
//                    });
//                } else { // If delete fail
//                    toastr.error(response.message, 'Signature Not Deleted', TOAST_OPTIONS);
//                    isDeletingSignature = false;
//                }
//            },
//            error: function (msg) { // Unknown error
//                console.log("Error: " + msg.responseText);
//                toastr.error('An error occurred when attempting to delete signature', 'Error', TOAST_OPTIONS);
//            },
//            complete: function () {
//                isDeletingSignature = false;
//            }
//        });
//    }
//};

function UploadImage() {
    var obj = $(this);

    alert(obj);
};

/*Search isolations*/
var program = 0;
var hecpATAId = 0;
var stepID = 0;
var isolationID = 0;
var circuitDeatilTextBoxId = 'input-system_circuit_circuitdetailid-';
var circuitIdTextBoxID = 'input-system_circuitdetail_id-';
var circuitNameTextBoxID = 'input-system_circuit_id-';
var circuitDescTextBoxID = 'input-system_circuit_nomenclature-';
var circuitPanelTextBoxID = 'input-system_circuit_panel-';
var circuitLocationTextBoxID = 'input-system_circuit_location-';


var stepTryoutID = 0;
var isolationTryoutID = 0;
function moveItems(origin, dest) {
    $(origin).find(':selected').appendTo(dest);
}

$(document).ready(function () {
    clearModalValues();

    $('#removeItar').click(function () {
        $('#updateItarProgramModal').modal('show');
        //moveItems('#itarList', '#nonItarList');
    });

    $('#addItar').on('click', function () {
        moveItems('#nonItarList', '#itarList');
    });
});

function printHecpWithoutAspose() {
    let tagid = "hecp-content";
    let hashid = "#" + tagid;
    let tagname = $(hashid).prop("tagName").toLowerCase();
    let attributes = "";
    let attrs = document.getElementById(tagid).attributes;
    $.each(attrs, function (i, elem) {
        attributes += " " + elem.name + " ='" + elem.value + "' ";
    })
    let divToPrint = $(hashid).html();
    let head = "<html><head>" + $("head").html() + "</head>";
    let allcontent = head + "<body  onload='window.print()' >" + "<" + tagname + attributes + ">" + divToPrint + "</" + tagname + ">" + "</body></html>";

    let newWindow = window.open('', 'Print-Window');
    newWindow.document.open();
    newWindow.document.write(allcontent);
    newWindow.document.close();
}

function printHecp(id, status) {
    disableFilterButton("#print-hecp-link");

    toastr.info("Please wait while we prepare the document. This may take a few moments. Do not refresh or navigate away from the page.", "PDF Generation Started!", TOAST_OPTIONS_NO_TIMEOUT);

    var xhr = $.ajax({
        url: '/print/Print?id=' + id + '&&status=' + status,
        type: 'get',
        cache: false,
        xhrFields: {
            responseType: 'arraybuffer' // to avoid binary data being mangled on charset conversion
        },
        success: function (result) {
            let fileName="";
            var disposition = xhr.getResponseHeader('Content-Disposition');
            if (disposition && disposition.indexOf('attachment') !== -1) {
                var filenameRegex = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/;
                var matches = filenameRegex.exec(disposition);
                if (matches != null && matches[1]) {
                    fileName = matches[1].replace(/['"]/g, '');
                }
            }
            var blob = new Blob([result], { type: 'application/pdf;base64' });
            let downloadUrl = URL.createObjectURL(blob);
            let a = document.createElement("a");
            a.href = downloadUrl;            
            a.download = fileName;
            document.body.appendChild(a);
            a.click();
            toastr.remove();
            toastr.success("PDF Generation Succeeded!", "Success");           
        },
        error: function (error) {
            toastr.remove();
            toastr.error("Failed to Download PDF. Please use browser's print function instead(Shortcut: Ctrl+P).", "Error");
            console.log(error);
        },
        complete: function () {
            enableFilterButton("#print-hecp-link");
        }
    });
}

$(document).on('change', 'input[type=radio][name=filterCriteria]', function () {
    if (this.value == 'Panel') {
        $('#manage-isolation-panel').prop('disabled', false);
        $('#manage-isolation-id').prop('disabled', true).val('');
        $('#manage-isolation-name').prop('disabled', true).val('');
    }
    else if (this.value == 'ComponentID') {
        $('#manage-isolation-panel').prop('disabled', true).val('');
        $('#manage-isolation-id').prop('disabled', false);
        $('#manage-isolation-name').prop('disabled', true).val('');
    }
    else if (this.value == 'ComponentName') {
        $('#manage-isolation-panel').prop('disabled', true).val('');
        $('#manage-isolation-id').prop('disabled', true).val('');
        $('#manage-isolation-name').prop('disabled', false);
    }
});

$(document).on('change', 'input[type=radio][name=searchCriteria]', function () {
    if (this.value == 'Panel') {
        $('#txtPanel').prop('disabled', false);
        $('#txtComponentID').prop('disabled', true).val('');
        $('#txtComponentName').prop('disabled', true).val('');
    }
    else if (this.value == 'ComponentID') {
        $('#txtPanel').prop('disabled', true).val('');
        $('#txtComponentID').prop('disabled', false);
        $('#txtComponentName').prop('disabled', true).val('');
    }
    else if (this.value == 'ComponentName') {
        $('#txtPanel').prop('disabled', true).val('');
        $('#txtComponentID').prop('disabled', true).val('');
        $('#txtComponentName').prop('disabled', false);
    }
    else {

    }
});

function showSiteAdmin(site, name, bemsid) {
    $('#inputDeleteUserSiteBemsId').val("");
    $('#inputDeleteUserSiteSite').val("");
    $('#lblRemoveSiteAdminConfirmation').html("");

    $('#inputDeleteUserSiteBemsId').val(bemsid);
    $('#inputDeleteUserSiteSite').val(site);
    $('#lblRemoveSiteAdminConfirmation').html("Do you want to remove Site Admin <b>'" + name + " (" + bemsid + ") " + "</b>' from Site <b>'" + site + "</b>'");

    $('#showDeleteSiteAdminModal').modal('show');
}

function getSearchIsolationsModal(stepNumber, isolationNumber) {
    /* Get the current no of isolations already there in this Step - select multiple isolations feature */

    isolationNumber = $('#row-hecp-deactivation-step-' + stepNumber + ' .row.wrapper-discrete-isolation-row').length;

    /* Get the current no of isolations already there in this Step - select multiple isolations feature */

    program = $('#Hecp-deactivation-program').val();
    stepID = stepNumber;
    isolationID = isolationNumber;
    $('#searchIsolationsModal').modal('show');
    clearModalValues();
};

function getTryoutSearchIsolationsModal(stepNumber, isolationNumber) {
    /* Get the current no of isolations already there in this Step - select multiple isolations feature */

    stepNumber = stepNumber - 1;
    isolationNumber = $('#row-hecp-tryout-step-' + stepNumber + ' .row.wrapper-discrete-isolation-row').length;

    /* Get the current no of isolations already there in this Step - select multiple isolations feature */

    program = $('#Hecp-tryout-program').val();
    stepTryoutID = stepNumber;
    isolationTryoutID = isolationNumber;
    $('#searchIsolationsModal').modal('show');
    clearModalValues();
};

function getSearchAtaModal(stepNumber, HecpProgram, hecpId) {
    program = HecpProgram;
    hecpATAId = hecpId;
    stepID = stepNumber;
    $('#searchAtaModal').modal('show');
    clearATAModalValues();
};

function clearATAModalValues() {
    $('#ataTitle').val('');
    $('#ataNumber').val('');
    $('#searchAtaComponent').prop('disabled', true);
    $('#dtSearchResult tbody').html('');
    $('#lblSearchResult').hide();
    $('#dtSearchResult').hide();
    $('#lblNoData').hide();
    $('#otherCompoenentNumber').val('');
    $('#otherCompoenentTitle').val('');
    $('#addComponent').prop('disabled', true);
};

function approveAndPublishHecp(hecpId, bemsId, nextStatus, currStatus, comments, type, revision, role) {
    if (checkIsCommentsBlank(comments)) {
        if (nextStatus == "published") {
            $('#errReviewComments').text("Enter the comment highlighting changes done in this revision here...");
            return;
        }
        else {
            document.getElementById("errReviewComments").style.visibility = "hidden";
        }
    }
    $("#sign-as-approve-btn").attr("disabled", true);
    $("#sign-as-publish-btn").attr("disabled", true);

    // Comments need to be encoded since it mat have special characters like #,&,/
    var encodedComments = encodeURIComponent(comments);
    let selectedIds = $('#hiddenSelectedApproverIds').val();
    var submitHecpRequest = {
        HecpId: hecpId,
        BemsId: bemsId,
        SelectedApproverIds: selectedIds,
        Status: nextStatus,
        Comments: encodedComments
    };

    $.ajax({ // Approve and Publish Hecp
        url: '/Hecp/ApproveAndPublishHecp',
        type: 'POST',
        data: JSON.stringify(submitHecpRequest),
        dataType: 'json',
        contentType: 'application/json; charset=utf-8',
        cache: false,
        success: function () {
            //Update reviewcomments in revisionhistory table
            if (String(nextStatus).toLowerCase() == "published" && String(currStatus).toLowerCase() == "inreview") {
                getApproveAndPublishModal();
            }
            else if (String(nextStatus).toLowerCase() == "published" && String(currStatus).toLowerCase() == "approved") {
                getPublishModal();
            }
            else if (String(nextStatus).toLowerCase() == "approved") {
                getApproveModal();
            }
        },
        error: function (error) {
            console.log(error);
            toastr.error("Error updating Hecp.", "Error");
        }
    });
}

function updateSelectedHecpApprovers() {
    let selectedHecpApprovers = [];
    let selectedIds = [];
    $('.approverCheckBox:checked').each(function () {
        selectedHecpApprovers.push({ bemsId: $(this).data('bems'), name: $(this).data('name') });
        selectedIds.push($(this).data('id'));
    });
    if (selectedHecpApprovers.length > 0) {
        $('#hiddenSelectedApproverIds').val(selectedIds.join(','));

        var userSpans = selectedHecpApprovers.map(user =>
            `<span class="insite-hovercard approver-hover" data-bemsid="${user.bemsId}">${user.name}</span>`
        ).join(',&nbsp;');

        $('#selectedHecpApprovers').html(userSpans);        
        $(".hecp-accept-button").attr("disabled", false);
        $("#hecp-submit-btn").attr("disabled", false);
        $("#sign-as-approve-btn").attr("disabled", false);
        hovercards = new INSITE.Hovercard();
    }
    else {
        $('#selectedHecpApprovers').html('');
        $(".hecp-accept-button").attr("disabled", true);
        $("#hecp-submit-btn").attr("disabled", true);
        $("#sign-as-approve-btn").attr("disabled", true);
    }
}

function submitHecp(hecpId, bemsId, roleId, comments, revision, roleName, type, status) {
    if (status == "draft" && revision.toLowerCase() != "new") {
        if (comments == undefined || comments == '') {
            $('#hecp-revision-comments').trigger('focus');
            document.getElementById("errRevisionComments").style.visibility = "visible";
            return;
        }
    }
    let selectedIds = $('#hiddenSelectedApproverIds').val();
    var submitHecpRequest = {
        HecpId: hecpId,
        BemsId: bemsId,
        RoleId: roleId,
        SelectedApproverIds: selectedIds
    };

    $("#hecp-submit-btn").attr("disabled", true);
    $.ajax({
        url: '/Hecp/SubmitHecp',
        type: 'POST',
        data: JSON.stringify(submitHecpRequest),
        dataType: 'json',
        contentType: 'application/json; charset=utf-8',
        cache: false,
        success: function (response) {
            //Update reviewcomments in revisionhistory table

            if (comments == undefined) comments = '';
            if (type == 3 && comments != '') requestReview(hecpId, comments, type, revision, bemsId, roleName);
            if (type == 4) requestReview(hecpId, comments, type, revision, bemsId, roleName);

            if (response) {
                $('#HecpSubmitNonEngineer').modal({
                    backdrop: 'static',
                    keyboard: false,
                    show: true
                });
            }
            else {
                toastr.error("An error occured. Please try again after some time.", "Error");
            }
        },
        error: function (error) {
            console.log(error);
        }
    });
}

function getApproveAndPublishModal() {
    $('#ApproveAndPublishModal').modal({
        backdrop: 'static',
        keyboard: false,
        show: true
    });
}

function getApproveModal() {
    $('#ApproveModal').modal({
        backdrop: 'static',
        keyboard: false,
        show: true
    });
}

function getPublishModal() {
    $('#PublishModal').modal({
        backdrop: 'static',
        keyboard: false,
        show: true
    });
}

function clearModalValues() {
    $('input[name="searchCriteria"]').prop('checked', false);
    $('#txtPanel').prop('disabled', true).val('');
    $('#txtComponentID').prop('disabled', true).val('');
    $('#txtComponentName').prop('disabled', true).val('');
    $('#searchComponent').prop('disabled', true);
    $('#dtSearchResult tbody').html('');
    $('#lblSearchResult').hide();
    $('#dtSearchResult').hide();
    $('#btnSelectIsolations').hide();
    $('#lblNoData').hide();
    $('#otherCompoenentId').val('');
    $('#otherCompoenentName').val('');
    $('#otherCompoenentPanel').val('');
    $('#otherCompoenentLocation').val('');
    $('#addComponent').prop('disabled', true);
}

function getSearchComponentResultData() {

    var panelText = (($('#txtPanel').val() == undefined) || ($('#txtPanel').val() == "")) ? "" : $('#txtPanel').val();
    var idText = (($('#txtComponentID').val() == undefined) || ($('#txtComponentID').val() == "")) ? "" : $('#txtComponentID').val();
    var nameText = (($('#txtComponentName').val() == undefined) || ($('#txtComponentName').val() == "")) ? "" : $('#txtComponentName').val();

    if ($.trim(idText) === "" && $.trim(nameText) === "" && $.trim(panelText) === "") {
        return false;
    }

    $.ajax({
        url: '/HECP/GetCircuitDetails?program=' + $.trim(program) + '&id=' + $.trim(idText) + '&name=' + $.trim(nameText) + '&panel=' + $.trim(panelText),
        type: 'GET',
        cache: false,
        success: function (response) {
            console.log(response);
            if (response.length > 0) {
                $('#lblSearchResult').show();
                $('#dtSearchResult').show();
                $('#btnSelectIsolations').show();
                $('#lblNoData').hide();
                buildSearchResultHtml(response);
            }
            else {
                $('#lblSearchResult').show();
                $('#dtSearchResult').hide();
                $('#btnSelectIsolations').hide();
                $('#lblNoData').show();
            }
        },
        error: function (e) {
            console.log(e);
            toastr.error("Error fetching search result.", "Error");
        }
    });
};
function filterIsolationComponentData(isReset, pageNumber) {
    collapseAddComponents();
    if (isReset) {
        clearfilterIsolationComponentData();
        hideEditComponent();
    }
    var program = $('#manage-isolation-program-select').val();
    if ($.trim(program) == '') {
        toastr.warning("Please select the program.", "Warning");
        return false;
    }

    var panelText = (($('#manage-isolation-panel').val() == undefined) || ($('#manage-isolation-panel').val() == "")) ? "" : $('#manage-isolation-panel').val();
    var idText = (($('#manage-isolation-id').val() == undefined) || ($('#manage-isolation-id').val() == "")) ? "" : $('#manage-isolation-id').val();
    var nameText = (($('#manage-isolation-name').val() == undefined) || ($('#manage-isolation-name').val() == "")) ? "" : $('#manage-isolation-name').val();


    $.ajax({
        url: '/HECP/GetCircuitDetailsByPageNumber?program=' + program + '&id=' + idText + '&name=' + nameText + '&panel=' + panelText + '&pageNumber=1',
        type: 'GET',
        cache: false,
        success: function (response) {
            console.log(response);
            if (response != "undefined" && response.data.length > 0) {
                $('#lblSearchResult').show();
                $('#dtIsolationResult_paginate').hide();
                $('#dtIsolationResult').hide();
                $('#lblNoData').hide();
                buildManageIsolationGrid(response.data, pageNumber);
            }
            else {
                $('#lblSearchResult').show();
                $('#dtIsolationResult_paginate').hide();
                $('#dtIsolationResult').hide();
                $('#lblNoData').show();
                //destroying the data table
                if ($.fn.dataTable.isDataTable('#dtIsolationResult')) {
                    table = $('#dtIsolationResult').DataTable();
                    table.destroy();
                }
            }
        },
        error: function (e) {
            console.log(e);
            toastr.error("Error fetching search result.", "Error");
        }
    });
};

function getSearchComponentAtaResultData() {
    var ataTitle = $('#ataTitle').val()?.trim() || "";
    var ataNumber = $('#ataNumber').val()?.trim() || "";

    if (ataTitle === "" && ataNumber === "") {
        return false;
    }

    $.ajax({
        url: '/HECP/GetAtaChapterDetails?program=' + $.trim(program) + '&title=' + ataTitle + '&ataNumber=' + ataNumber,
        type: 'GET',
        cache: false,
        success: function (response) {
            console.log(response);
            if (response.length > 0) {
                $('#lblSearchResult').show();
                $('#dtSearchResult').show();
                $('#lblNoData').hide();
                buildAtaSearchResultHtml(response);
            }
            else {
                $('#lblSearchResult').hide();
                $('#dtSearchResult').hide();
                $('#lblNoData').show();
                $('#lblAtaNoData').show();
            }
        },
        error: function (e) {
            console.log(e);
            toastr.error("Error fetching search result.", "Error");
        }
    });
};

function buildManageIsolationGrid(jsonResponse, pageNumber) {

    if ($.fn.dataTable.isDataTable('#dtIsolationResult')) {
        table = $('#dtIsolationResult').DataTable();
        table.destroy();
    }
    var trHTML = '';
    $('#dtIsolationResult tbody').html('');
    $.each(jsonResponse, function (i, item) {
        trHTML += '<tr><td>' + item.circuitId + '</td><td>' + item.circuitNomenclature +
            '</td><td>' + ((item.panel == null) ? '' : item.panel) +
            '</td><td>' + ((item.row == null) ? '' : item.row) + ((item.row != null && item.column != null) ? '-' : '') + ((item.column == null) ? '' : item.column) + '</td><td><a style="color: #0000FF" onclick="showEditComponent(' + item.id + ',\'' + item.circuitId + '\',\'' + item.circuitNomenclature + '\',\'' + ((item.panel == null) ? '' : item.panel) + '\',\'' + ((item.row == null) ? '' : item.row) + ((item.row != null && item.column != null) ? '-' : '') + ((item.column == null) ? '' : item.column) + '\')"><i class="fa fa-pencil"></i></a></td><td><a style="color: #0000FF" onclick="showDeleteComponentModal(' + item.id + ', \'' + item.circuitNomenclature + '\')"><i class="fa fa-times"></i></a></td></tr>';
    });

    $('#dtIsolationResult tbody').append(safeResponseFilter(trHTML));

    if (pageNumber == undefined) {
        $('#dtIsolationResult').dataTable({
            "bPaginate": true,
            "bSort": false,
            "bFilter": false,
            "info": false,
            "bLengthChange": false,
            "autoWidth": false,
            "pageLength": 20,
            "displayStart": (0) * 20
        });
    }
    else {
        $('#dtIsolationResult').dataTable({
            "bPaginate": true,
            "bSort": false,
            "bFilter": false,
            "info": false,
            "bLengthChange": false,
            "autoWidth": false,
            "pageLength": 20,
            "displayStart": (pageNumber) * 20
        });
    }
    $('#dtIsolationResult').show();
    $('#dtIsolationResult_paginate').show();
}

function buildSearchResultHtml(jsonResponse) {
    var trHTML = '';
    $('#dtSearchResult tbody').html('');
    $.each(jsonResponse, function (i, item) {
        trHTML += '<tr>' +
            '<td class="itemIdClass">' + '<input type="checkbox" value="' + item.id + '" />' +
            '</td><td class="circuitIdClass">' + item.circuitId + '</td><td class="circuitNameClass">' + item.circuitNomenclature +
            '</td><td class="panelClass">' + ((item.panel == null) ? '' : item.panel) +
            '</td><td class="locationClass">' + ((item.row == null) ? '' : item.row) + ((item.row != null && item.column != null) ? '-' : '') + ((item.column == null) ? '' : item.column) + '</td></tr>';
    });

    $('#dtSearchResult tbody').append(safeResponseFilter(trHTML));
}

function buildAtaSearchResultHtml(jsonResponse) {
    let trHTML = '';
    $('#dtSearchResult tbody').html('');
    $.each(jsonResponse, function (i, item) {
        trHTML += '<tr onclick="passSelectedComponentToAtaChapter(this)"><td class="hecpWrap">' + item.hecpAtaNumber + '</td><td class="hecpWrap">' + item.hecpAtaTitle +
            '</td><td id="' + item.id + '"><input type="radio" value="' + item.id + '"/>' +
            '</td></tr>';
    });

    $('#dtSearchResult tbody').append(safeResponseFilter(trHTML));
}

function passSelectedComponentToAtaChapter(el) {
    $(el).find('td').each(function (index) {
        switch (index) {
            case 0:
                $('#' + 'HecpATAChapters_' + stepID + '_Number').val(this.innerText);
                break;
            case 1:
                $('#' + 'HecpATAChapters_' + stepID + '_Title').val(this.innerText);
                break;
            case 2:
                $('#' + 'HecpATAChapters_' + stepID + 'HecpAtaMasterId').val(this.id);
                break;
            default:
            // code block
        }
    });
  
    addNewAtaChapter(stepID, program, hecpATAId);
    setIsDirtyTrue();
    $('#searchAtaModal').modal('hide');
};

function passSelectedIsolationsToDeactivation() {
    var arrSelectedIsolations = [];

    $('#dtSearchResult tbody>tr').each(function (a, b) {
        if (($('.itemIdClass', b)[0].children[0].checked) == true) {
            var itemId = $('.itemIdClass', b)[0].children[0].value;
            var circuitId = $('.circuitIdClass', b).text();
            var circuitName = $('.circuitNameClass', b).text();
            var panel = $('.panelClass', b).text();
            var location = $('.locationClass', b).text();

            arrSelectedIsolations.push({ ItemId: itemId, CircuitId: circuitId, CircuitName: circuitName, Panel: panel, Location: location });
        }
    });
    console.log(JSON.stringify(arrSelectedIsolations));

    for (var i = 1; i <= arrSelectedIsolations.length; i++) {
        addNewUIIsolationToStep(stepID, isolationID);
        $('#' + circuitDeatilTextBoxId + 'step-' + stepID + '-isolation-' + isolationID).val(arrSelectedIsolations[i - 1].ItemId);
        $('#' + circuitIdTextBoxID + 'step-' + stepID + '-isolation-' + isolationID).val(arrSelectedIsolations[i - 1].ItemId);
        $('#' + circuitNameTextBoxID + 'step-' + stepID + '-isolation-' + isolationID).val(arrSelectedIsolations[i - 1].CircuitId);
        $('#' + circuitDescTextBoxID + 'step-' + stepID + '-isolation-' + isolationID).val(arrSelectedIsolations[i - 1].CircuitName);
        $('#' + circuitPanelTextBoxID + 'step-' + stepID + '-isolation-' + isolationID).val(arrSelectedIsolations[i - 1].Panel);
        $('#' + circuitLocationTextBoxID + 'step-' + stepID + '-isolation-' + isolationID).val(arrSelectedIsolations[i - 1].Location);
        isolationID = isolationID + 1;
    }

    validateDeactivationStepsForm();
    setIsDirtyTrue();
    $('.input-circuit_state').removeClass("hecpHideCursor");
    $('#searchIsolationsModal').modal('hide');
    checkIsolationLimit();
}

function passSelectedIsolationsToTryout() {
    var arrSelectedIsolations = [];
    $('#dtSearchResult tbody>tr').each(function (a, b) {
        if (($('.itemIdClass', b)[0].children[0].checked) == true) {
            var itemId = $('.itemIdClass', b)[0].children[0].value;
            var circuitId = $('.circuitIdClass', b).text();
            var circuitName = $('.circuitNameClass', b).text();
            var panel = $('.panelClass', b).text();
            var location = $('.locationClass', b).text();

            arrSelectedIsolations.push({ ItemId: itemId, CircuitId: circuitId, CircuitName: circuitName, Panel: panel, Location: location });
        }
    });
    console.log(JSON.stringify(arrSelectedIsolations));

    for (var i = 1; i <= arrSelectedIsolations.length; i++) {
        addNewTryoutUIIsolationToStep(stepTryoutID, isolationTryoutID);
        $('#' + circuitDeatilTextBoxId + 'step-' + stepTryoutID + '-isolation-' + isolationTryoutID).val(arrSelectedIsolations[i - 1].ItemId);
        $('#' + circuitIdTextBoxID + 'step-' + stepTryoutID + '-isolation-' + isolationTryoutID).val(arrSelectedIsolations[i - 1].ItemId);
        $('#' + circuitNameTextBoxID + 'step-' + stepTryoutID + '-isolation-' + isolationTryoutID).val(arrSelectedIsolations[i - 1].CircuitId);
        $('#' + circuitDescTextBoxID + 'step-' + stepTryoutID + '-isolation-' + isolationTryoutID).val(arrSelectedIsolations[i - 1].CircuitName);
        $('#' + circuitPanelTextBoxID + 'step-' + stepTryoutID + '-isolation-' + isolationTryoutID).val(arrSelectedIsolations[i - 1].Panel);
        $('#' + circuitLocationTextBoxID + 'step-' + stepTryoutID + '-isolation-' + isolationTryoutID).val(arrSelectedIsolations[i - 1].Location);
        isolationTryoutID = isolationTryoutID + 1;
    }

    validateTryoutProcedure();
    setIsDirtyTrue();
    $('.input-circuit_state').removeClass("hecpHideCursor");
    $('#searchIsolationsModal').modal('hide');
    checkIsolationLimit();
}

function passSelectedComponentToDeactivation(el) {
    $(el).find('td').each(function (index) {
        switch (index) {
            case 0:
                $('#' + circuitDeatilTextBoxId + 'step-' + stepID + '-isolation-' + isolationID).val(this.innerText);
                $('#' + circuitIdTextBoxID + 'step-' + stepID + '-isolation-' + isolationID).val(this.innerText);
                break;
            case 1:
                $('#' + circuitNameTextBoxID + 'step-' + stepID + '-isolation-' + isolationID).val(this.innerText);
                break;
            case 2:
                $('#' + circuitDescTextBoxID + 'step-' + stepID + '-isolation-' + isolationID).val(this.innerText);
                break;
            case 3:
                $('#' + circuitPanelTextBoxID + 'step-' + stepID + '-isolation-' + isolationID).val(this.innerText);
                break;
            case 4:
                $('#' + circuitLocationTextBoxID + 'step-' + stepID + '-isolation-' + isolationID).val(this.innerText);
                break;
            default:
            // code block
        }
    });

    addNewUIIsolationToStep(stepID, isolationID + 1);
    validateDeactivationStepsForm();
    setIsDirtyTrue();
    $('.input-circuit_state').removeClass("hecpHideCursor");
    $('#searchIsolationsModal').modal('hide');
}

function addOtherComponentData() {
    if ($.trim($('#otherCompoenentName').val()) === "") {
        alert("Please add a name & then click on Add");
    }
    else {
        var circuitId = $.trim($('#otherCompoenentId').val());
        var circuitName = $.trim($('#otherCompoenentName').val());
        var circuitPanel = $.trim($('#otherCompoenentPanel').val());
        var circuitLocation = $.trim($('#otherCompoenentLocation').val());

        //Circuit Id is blank but Circuit name has value, then check if this step has any component with same name
        if (circuitId == null || circuitId == '') {
            var ifExists = false;
            $.each($("input[id^='input-system_circuit_nomenclature-step-" + stepID + "']"), function (i, item) {
                if (item.value == circuitName) {
                    ifExists = true;
                    return;
                }
            });

            if (!ifExists) {
                addNewUIIsolationToStep(stepID, isolationID);
                $('#' + circuitDeatilTextBoxId + 'step-' + stepID + '-isolation-' + isolationID).val(-1);
                $('#' + circuitIdTextBoxID + 'step-' + stepID + '-isolation-' + isolationID).val(-1);
                $('#' + circuitNameTextBoxID + 'step-' + stepID + '-isolation-' + isolationID).val($.trim($('#otherCompoenentId').val()));
                $('#' + circuitDescTextBoxID + 'step-' + stepID + '-isolation-' + isolationID).val($.trim($('#otherCompoenentName').val()));
                $('#' + circuitPanelTextBoxID + 'step-' + stepID + '-isolation-' + isolationID).val($.trim($('#otherCompoenentPanel').val()));
                $('#' + circuitLocationTextBoxID + 'step-' + stepID + '-isolation-' + isolationID).val($.trim($('#otherCompoenentLocation').val()));
            }
        }
        //Circuit Id has value
        else {
            var ifExists = false;
            const allData = [];
            var rows = $("#row-hecp-deactivation-step-" + stepID + " .row.wrapper-discrete-isolation-row");
            //if any of the isolations in this step has exactly similar properties, dont add it again to the grid 
            if (rows !== null) {
                $.each($(rows), function (i, v) {
                    $.each($(v), function (index, value) {
                        var idValue = $("#input-system_circuit_id-step-" + stepID + "-isolation-" + i).val();
                        var name = $("#input-system_circuit_nomenclature-step-" + stepID + "-isolation-" + i).val();
                        var panel = $("#input-system_circuit_panel-step-" + stepID + "-isolation-" + i).val();
                        var location = $("#input-system_circuit_location-step-" + stepID + "-isolation-" + i).val();

                        allData.push(idValue + "-" + name + "-" + panel + "-" + location);
                    });
                });

                var newIsolationString = circuitId + "-" + circuitName + "-" + circuitPanel + "-" + circuitLocation;
                ifExists = allData.indexOf(newIsolationString) !== -1;
                if (ifExists) {
                    toastr.error("Similar Isolation Already Exists In this Step", "Error");
                }
            }

            if (!ifExists) {
                addNewUIIsolationToStep(stepID, isolationID);
                $('#' + circuitDeatilTextBoxId + 'step-' + stepID + '-isolation-' + isolationID).val(-1);
                $('#' + circuitIdTextBoxID + 'step-' + stepID + '-isolation-' + isolationID).val(-1);
                $('#' + circuitNameTextBoxID + 'step-' + stepID + '-isolation-' + isolationID).val($.trim($('#otherCompoenentId').val()));
                $('#' + circuitDescTextBoxID + 'step-' + stepID + '-isolation-' + isolationID).val($.trim($('#otherCompoenentName').val()));
                $('#' + circuitPanelTextBoxID + 'step-' + stepID + '-isolation-' + isolationID).val($.trim($('#otherCompoenentPanel').val()));
                $('#' + circuitLocationTextBoxID + 'step-' + stepID + '-isolation-' + isolationID).val($.trim($('#otherCompoenentLocation').val()));
            }
        }

        validateDeactivationStepsForm();
        setIsDirtyTrue();
        $('.input-circuit_state').removeClass("hecpHideCursor");
        $('#searchIsolationsModal').modal('hide');
        checkIsolationLimit();
    }
}

function addOtherTryoutComponentData() {
    if ($.trim($('#otherCompoenentName').val()) === "") {
        alert("Please add a name & then click on Add");
    }
    else {
        var circuitId = $.trim($('#otherCompoenentId').val());
        var circuitName = $.trim($('#otherCompoenentName').val());
        var circuitPanel = $.trim($('#otherCompoenentPanel').val());
        var circuitLocation = $.trim($('#otherCompoenentLocation').val());

        //Circuit Id is blank but Circuit name has value, then check if this step has any component with same name
        if (circuitId == null || circuitId == '') {
            var ifExists = false;
            $.each($("input[id^='input-system_circuit_nomenclature-step-" + stepID + "']"), function (i, item) {
                if (item.value == circuitName) {
                    ifExists = true;
                    return;
                }
            });

            if (!ifExists) {
                addNewTryoutUIIsolationToStep(stepTryoutID, isolationTryoutID);
                $('#' + circuitDeatilTextBoxId + 'step-' + stepTryoutID + '-isolation-' + isolationTryoutID).val(-1);
                $('#' + circuitIdTextBoxID + 'step-' + stepTryoutID + '-isolation-' + isolationTryoutID).val(-1);
                $('#' + circuitNameTextBoxID + 'step-' + stepTryoutID + '-isolation-' + isolationTryoutID).val($.trim($('#otherCompoenentId').val()));
                $('#' + circuitDescTextBoxID + 'step-' + stepTryoutID + '-isolation-' + isolationTryoutID).val($.trim($('#otherCompoenentName').val()));
                $('#' + circuitPanelTextBoxID + 'step-' + stepTryoutID + '-isolation-' + isolationTryoutID).val($.trim($('#otherCompoenentPanel').val()));
                $('#' + circuitLocationTextBoxID + 'step-' + stepTryoutID + '-isolation-' + isolationTryoutID).val($.trim($('#otherCompoenentLocation').val()));
            }
        }
        //Circuit Id has value, then check if this step has any component with same id
        else {
            var ifExists = false;
            const allData = [];
            const rows = $("#row-hecp-tryout-step-" + stepID + " .row.wrapper-discrete-isolation-row");
            //if any of the isolations in this step has exactly similar properties, dont add it again to the grid
            if (rows !== null) {
                $.each($(rows), function (i, v) {
                    $.each($(v), function (index, value) {
                        var idValue = $("#input-system_circuit_id-step-" + stepID + "-isolation-" + i).val();
                        var name = $("#input-system_circuit_nomenclature-step-" + stepID + "-isolation-" + i).val();
                        var panel = $("#input-system_circuit_panel-step-" + stepID + "-isolation-" + i).val();
                        var location = $("#input-system_circuit_location-step-" + stepID + "-isolation-" + i).val();

                        allData.push(idValue + "-" + name + "-" + panel + "-" + location);
                    });
                });

                var newIsolationString = circuitId + "-" + circuitName + "-" + circuitPanel + "-" + circuitLocation;
                ifExists = allData.indexOf(newIsolationString) !== -1;
                if (ifExists) {
                    toastr.error("Similar Isolation Already Exists In this Step", "Error");
                }
            }


            if (!ifExists) {
                addNewTryoutUIIsolationToStep(stepTryoutID, isolationTryoutID);
                $('#' + circuitDeatilTextBoxId + 'step-' + stepTryoutID + '-isolation-' + isolationTryoutID).val(-1);
                $('#' + circuitIdTextBoxID + 'step-' + stepTryoutID + '-isolation-' + isolationTryoutID).val(-1);
                $('#' + circuitNameTextBoxID + 'step-' + stepTryoutID + '-isolation-' + isolationTryoutID).val($.trim($('#otherCompoenentId').val()));
                $('#' + circuitDescTextBoxID + 'step-' + stepTryoutID + '-isolation-' + isolationTryoutID).val($.trim($('#otherCompoenentName').val()));
                $('#' + circuitPanelTextBoxID + 'step-' + stepTryoutID + '-isolation-' + isolationTryoutID).val($.trim($('#otherCompoenentPanel').val()));
                $('#' + circuitLocationTextBoxID + 'step-' + stepTryoutID + '-isolation-' + isolationTryoutID).val($.trim($('#otherCompoenentLocation').val()));
            }
        }

        validateTryoutProcedure();
        setIsDirtyTrue();
        $('.input-circuit_state').removeClass("hecpHideCursor");
        $('#searchIsolationsModal').modal('hide');
        checkIsolationLimit();
    }
}

function addAtaChapterData() {
    if ($.trim($('#otherCompoenentNumber').val()) === "" || $.trim($('#otherCompoenentTitle').val()) === "") {
        alert("Please add both ATA number and title.");
    }
    else {
        let ataNum = $.trim($('#otherCompoenentNumber').val());
        let ataText = $.trim($('#otherCompoenentTitle').val());
        $.ajax({
            url: '/HECP/AddAtaChapterDetails?program=' + $.trim(program) + '&title=' + $.trim(ataText) + '&number=' + $.trim(ataNum),
            type: 'get',
            dataType: 'json',
            async: false,
            cache: false,
            success: function (response) {
                console.log(response);
                if (response.hecpAtaNumber != '' && response.hecpAtaTitle != '' && response.id != 0) {
                    $('#' + 'HecpATAChapters_' + stepID + 'HecpAtaMasterId').val(response.id);
                    $('#' + 'HecpATAChapters_' + stepID + '_Number').val(response.hecpAtaNumber);
                    $('#' + 'HecpATAChapters_' + stepID + '_Title').val(response.hecpAtaTitle);
                    addNewAtaChapter(stepID, program, hecpATAId);
                    setIsDirtyTrue();

                    $('#searchAtaModal').modal('hide');
                }
            },
            error: function (e) {
                console.log(e);
                toastr.error("Error fetching search result.", "Error");
            }
        });
    }
}

function removeDeactivationImage(stepNumber, imageCounter) {
    var stepOrder = getStepOrder(5);
    $stepRow = $('#row-hecp-deactivation-step-' + stepNumber);
    var data = new FormData($('#hecp-deactivation-steps-form')[0]);
    tinymce.remove();
    $.ajax({
        url: '/Hecp/RemoveImageFromHecpDeactivationStep/' + stepNumber + '/' + imageCounter + '/' + stepOrder,
        type: 'post',
        data: data,
        processData: false,
        contentType: false,
        success: function (response) {
            $stepRow.hide(0, function () {
                var safeResponse = safeResponseFilter(response);
                $('#hecp-content').html(safeResponse);
                validateDeactivationStepsForm();
                setIsDirtyTrue();
                initTinyMceForDeactivation();
            });
        },
        error: function () {
            initTinyMceForDeactivation();
            console.log;
        }
    });
}

function removeTryoutImage(stepNumber, imageCounter) {
    var stepOrder = getStepOrder(6);
    var data = new FormData($('#steps-form')[0]);
    $stepRow = $('#row-hecp-tryout-step-' + stepNumber);
    tinymce.remove();
    $.ajax({
        url: '/Hecp/RemoveImageFromTryoutStep/' + stepNumber + '/' + imageCounter + '/' + stepOrder,
        type: 'post',
        data: data,
        processData: false,
        contentType: false,
        success: function (response) {
            $stepRow.hide(0, function () {
                var safeResponse = safeResponseFilter(response);
                $('#hecp-content').html(safeResponse);
                setIsDirtyTrue();
                validateTryoutProcedure();
                initTinyMceForTryout();
            });
        },
        error: function () {
            initTinyMceForTryout();
            console.log;
        }
    });
}

function toggleAddButton(inputField) {
    var val = inputField.value.trim();

    // Disable Add button
    if (val === '') {
        $('#addComponent').prop('disabled', true);
    }
    // Enable Add button
    else if (val != '') {
        $('#addComponent').prop('disabled', false);;
    }
}

function toggleAtaAddButton() {
    var num = $('#otherCompoenentNumber').val().trim();
    var title = $('#otherCompoenentTitle').val().trim();

    // Disable Add button
    if (num === '' || title === '') {
        $('#addComponent').prop('disabled', true);
    }
    // Enable Add button
    else if (num != '' && title != '') {
        $('#addComponent').prop('disabled', false);;
    }
}

function toggleFilterIsolationButton(inputField) {
    var val = inputField.value.trim();
    // Disable Search button
    if (val === '') {
        $('#filterIsolationComponents').prop('disabled', true);
    }
    // Enable Search button
    else if (val != '') {
        $('#filterIsolationComponents').prop('disabled', false);;
    }
}

function clearfilterIsolationComponentData() {
    $('#dtIsolationResult_paginate').hide();
    $('#dtIsolationResult').hide();
    $('input[name="filterCriteria"]').prop('checked', false);
    $('#manage-isolation-id').prop('disabled', true).val('');
    $('#manage-isolation-panel').prop('disabled', true);
    $('#manage-isolation-name').prop('disabled', true).val('');
}

function toggleSearchButton(inputField) {
    var val = inputField.value.trim();

    // Disable Search button
    if (val === '') {
        $('#searchComponent').prop('disabled', true);
    }
    // Enable Search button
    else if (val != '') {
        $('#searchComponent').prop('disabled', false);;
    }
}

function toggleAtaSearchButton() {
    var ataTitle = $('#ataTitle').val()?.trim() || "";
    var ataNumber = $('#ataNumber').val()?.trim() || "";

    // Disable Search button
    if (ataTitle === '' && ataNumber === '') {
        $('#searchAtaComponent').prop('disabled', true);
    }
    // Enable Search button
    else
    {
        $('#searchAtaComponent').prop('disabled', false);;
    }
}

/*Search isolations*/
function markItar(hecpId, name, value) {
    $.ajax({
        url: '/Hecp/UpdateHecpItar?hecpId=' + hecpId + '&isItar=' + value,
        type: 'get',
        dataType: 'html',
        cache: false,
        success: function (response) {
            if (response == "true") {
                if (value == true)
                    toastr.success("HECP " + name + " marked as ITAR", "Success", TOAST_OPTIONS_SHORT_TIMEOUT);
                else
                    toastr.success("HECP " + name + " marked as non-ITAR", "Success", TOAST_OPTIONS_SHORT_TIMEOUT);
            }
            else {
                toastr.error("An error occured. Please try again after some time.", "Error");
            }
        },
        error: function (error) {
            console.log(error);
        }
    });
};

function markHECPEngineered(hecpId, name, isEngineered) {
    $("#mark-engineer-loader-" + hecpId).show();
    $("#mark-engineer-loader-" + hecpId)[0].style.display = 'inline-block';
    $.ajax({
        url: '/Hecp/UpdateHecpEngineeredStatus?hecpId=' + hecpId + '&isEngineered=' + isEngineered,
        type: 'get',
        dataType: 'html',
        cache: false,
        success: function (response) {
            if (response == "true") {
                if (isEngineered)
                    toastr.success("HECP - " + name + " marked as Engineered", "Success", TOAST_OPTIONS_SHORT_TIMEOUT);
                else
                    toastr.success("HECP - " + name + " marked as non-Engineered", "Success", TOAST_OPTIONS_SHORT_TIMEOUT);
            }
            else {
                toastr.error("An error occured. Please try again after some time.", "Error");
                if (!isEngineered) {
                    $("#engineer-check-" + hecpId)[0].checked = true;
                }
                else {
                    $("#engineer-check-" + hecpId)[0].checked = false;
                }
            }
            $("#mark-engineer-loader-" + hecpId).hide();
        },
        error: function (error) {
            console.log(error);
            $("#mark-engineer-loader-" + hecpId).hide();
        }
    });
};

/*START: Request Review*/
function requestReview(hecpId, comments, type, revision, bemsId, role) {
    if (type < 3 && checkIsCommentsBlank(comments)) return;

    $("#hecp-requestreview-btn").attr("disabled", true);
    var encodedComments = encodeURIComponent(comments);

    $.ajax({
        url: '/Hecp/SaveRevisionHistory?hecpId=' + hecpId + '&comments=' + encodedComments + '&type=' + type + '&revision=' + revision + '&bemsId=' + bemsId + '&role=' + role,
        type: 'get',
        dataType: 'html',
        cache: false,
        success: function (response) {
            if (response) {
                if (type == 1 || type == 2) {
                    $('#RequestUpdateModel').modal({
                        backdrop: 'static',
                        keyboard: false,
                        show: true
                    });
                }
            }
            else {
                toastr.error("An error occured. Please try again after some time.", "Error");
            }
        },
        error: function (error) {
            console.log(error);
        }
    });
}

function checkIsCommentsBlank(currentVal) {
    if ($.trim(currentVal) == '') {
        $('#hecp-review-comments').trigger('focus');
        document.getElementById("errReviewComments").style.visibility = "visible";
        return true;
    }
    return false;

}

function validateComments() {
    if ($("#hecp-review-comments").value != '') {
        document.getElementById("errReviewComments").style.visibility = "hidden";
    }
}

function validateRevisionComments() {
    if ($("#hecp-revision-comments").value != '') {
        document.getElementById("errRevisionComments").style.visibility = "hidden";
    }
}

$('.convert-to-local-time-extra-long').each(function () {
    $(this).html(UTCDateStringToLocalExtraLongTime($(this).html()));
});


function UTCDateStringToLocalExtraLongTime(dateString) {
    var timezone = moment.tz(moment.tz.guess()).zoneAbbr();
    var date = moment.utc(dateString);
    if (date.isValid() != false) {
        return date.local().format("M/D/YYYY   hh:mm:ss A") + " " + timezone;
    }
    return "";
}

$('.convert-to-local-time-long').each(function () {
    $(this).html(UTCDateStringToLocalLongTime($(this).html()));
});


function UTCDateStringToLocalLongTime(dateString) {
    var timezone = moment.tz(moment.tz.guess()).zoneAbbr();
    var date = moment.utc(dateString);
    if (date.isValid() != false) {
        return date.local().format("MM/DD/YYYY HH:mm") + " " + timezone;
    }
    return "";
}

$('.convert-to-local-time-short').each(function () {
    $(this).html(UTCDateStringToLocalShortTime($(this).html()));
});


function UTCDateStringToLocalShortTime(dateString) {
    var timezone = moment.tz(moment.tz.guess()).zoneAbbr();
    var date = moment.utc(dateString);
    if (date.isValid() != false) {
        return date.local().format("MM/DD/YYYY") + " " + timezone;
    }
    return "";
}

/*END: Request Review*/
function showDeleteApproverModal(id) {
    $('#inputDeleteApproverId').val("");
    $('#lblDeleteApproverConfirmation').html("");
    $('#lblApproverDeleted').html("");

    $('#inputDeleteApproverId').val(id);
    $('#lblDeleteApproverConfirmation').html("Do you want to delete the HECP Approver?");

    $('#deleteApproverModal').modal('show');
}

function deleteApprover() {
    var approverId = $('#inputDeleteApproverId').val();

    $.ajax({
        url: '/Admin/DeleteApprover/' + approverId,
        type: 'GET',
        dataType: 'json',
        cache: false,
        async: true,
        success: function (response) {
            if (response.status == 'SUCCESS') {
                $('#lblApproverDeleted').html("HECP Approver deleted.");
                var apprTable = $('#hecpApprDtls').DataTable();
                var pageNumber = apprTable.page.info().page;
                loadApproverTable(response.data, pageNumber);
                $('#approverDeletedModal').modal('show');
            } else {
                toastr["error"]("An error occured.Please try again after sometime.", "Delete Approver Error", TOAST_OPTIONS);
            }
        },
        error: function () {
            toastr["error"]("Failed to find the HECP approver due to some error.", "Delete Approver Error", TOAST_OPTIONS);
        }
    });

    $('#deleteApproverModal').modal('hide');
}

function showDeleteComponentModal(id, componentId) {
    $('#inputDeleteComponentId').val("");
    $('#inputDeleteComponentTableId').val("");
    $('#lblDeleteComponentConfirmation').html("");
    $('#lblAssociatedHecp').html("");
    $('#lblComponentDeleted').html("");

    $('#inputDeleteComponentId').val(componentId);
    $('#inputDeleteComponentTableId').val(id);
    $('#lblDeleteComponentConfirmation').html("Do you want to delete Component <b>'" + componentId + "</b>'?");

    $('#deleteComponentModal').modal('show');
}

function deleteComponent() {
    var componentIdToDelete = $('#inputDeleteComponentId').val();
    var componentTableToDelete = $('#inputDeleteComponentTableId').val();

    $.ajax({
        url: '/HECP/DeleteComponent?componentId=' + componentTableToDelete,
        type: 'GET',
        dataType: 'json',
        cache: false,
        async: true,
        success: function (objectResult) {
            if (objectResult.status == 'SUCCESS') {
                $('#lblComponentDeleted').html(safeResponseFilter("Component <b>'" + componentIdToDelete + "</b>' deleted."));
                $('#componentDeletedModal').modal('show');
            } else {
                $('#lblAssociatedHecp').html(safeResponseFilter("Cannot delete <b>'" + componentIdToDelete + "</b>' as it is being used by HECP(s) <b>" + objectResult.message + "</b>"));
                $('#associatedHecpModal').modal('show');
            }
        },
        error: function () {
            toastr["error"]("Failed to find the component due to some error.", "DeleteComponent Error", TOAST_OPTIONS);
        }
    });

    $('#deleteComponentModal').modal('hide');
}

/* Delete Hecp */

function showDeleteHecpModal(hecpId, name, revision, hasInworkRevision) {
    var decodedhecpName = name;
    $('#inputDeleteHecpId').val("");
    $('#inputDeleteHecpName').val("");
    $('#lblDeleteHecpConfirmation').html("");
    $('#lblHecpTaggedToLoto').html("");
    $('#lblHecpDeleted').html("");

    $('#inputDeleteHecpId').val(hecpId);
    $('#inputDeleteHecpName').val(decodedhecpName);

    let confirmationMsg =
        "<ul><li>Are you sure you want to delete HECP -<b>'" + decodedhecpName + "'</b> (Revision -<b>" + revision + "</b>)? </li>" +
        "<li>This action will permanently delete only this revision and cannot be undone.</li></ul>";

    if (hasInworkRevision) {
        confirmationMsg += "<ul><li>Note:</b> There is an in-work revision for this HECP; deleting this revision will not delete the in-work revision.</li></ul>";
    }

    $('#lblDeleteHecpConfirmation').html(confirmationMsg);
    $('#deleteHecpModal').modal('show');
}

function deleteHecp() {
    var hecpIdToDelete = $('#inputDeleteHecpId').val();
    var hecpnameToDelete = $('#inputDeleteHecpName').val();
    let isPublished = getIsPublished();
    //find LOTO for this hecp, if found don't delete and show appropriate message, if not found delete hecp and show message
    $.ajax({
        url: '/Loto/IsHecpDeletable?hecpId=' + hecpIdToDelete,
        type: 'GET',
        dataType: 'json',
        cache: false,
        async: true,
        success: function (objectResult) {
            if (objectResult.status === 'SUCCESS') {
                if (objectResult.data) {
                    $.ajax({
                        url: "/Hecp/DeleteHecp?hecpId=" + hecpIdToDelete + '&isPublished=' + isPublished,
                        type: 'GET',
                        dataType: 'html',
                        cache: false,
                        async: true,
                        success: function (response) {
                            if (response === "true") {
                                $('#lblHecpDeleted').html("HECP <b>'" + hecpnameToDelete + "</b>' deleted.");
                                $('#hecpDeletedModal').modal('show');

                                setTimeout("location.reload();", 3000);
                            }
                        },
                        error: function (xhr) {
                            console.log(xhr);
                            toastr.error(xhr, "Error", TOAST_OPTIONS);
                        }
                    });
                } else {
                    $('#lblHecpTaggedToLoto').html("HECP <b>'" + hecpnameToDelete + "</b>' cannot be deleted as it is associated with one or more LOTO(s).");
                    $('#hecpTaggedToLotoModal').modal('show');
                }
            }
            else {
                toastr["error"]("Failed to find Loto for hecp due to some error.", "LOTO Error", TOAST_OPTIONS);
            }
        },
        error: function () {
            toastr["error"]("Failed to find Loto for hecp due to some error.", "LOTO Error", TOAST_OPTIONS);
        }
    });

    $('#deleteHecpModal').modal('hide');
}

/* Delete Hecp */

/* Edit Published HECP Pop Up Implementation */

function showPublishedHecpEditWarning(hecpId, targetStep) {
    $('#inputEditHecpId').val(hecpId);
    $('#inputTargetStep').val(targetStep);

    $('#editHecpModal').modal('show');
}

function proceedToEditAfterWarning() {
    var hecpid = $('#inputEditHecpId').val();
    var targetStep = $('#inputTargetStep').val();
    $('#editHecpModal').modal('hide');
    if (hecpid > 0) {
        let navigateToURL = '/Hecp/EditPublishedHecp?hecpId=' + hecpid;
        window.location.href = navigateToURL;
    }
}

/* Edit Published HECP Pop Up Implementation */

/* Edit In-Work HECP Pop Up Implementation */

function showInworkHecpEditWarning(hecpId, targetStep, currentAuthor) {
    $('#inputEditHecpId').val(hecpId);
    $('#inputTargetStep').val(targetStep);
    if (currentAuthor) {
        $('#currentAuthor').text(currentAuthor);
        $('#currentAuthorContainer').show();
    } else {
        $('#currentAuthorContainer').hide();
    }
    $('#editInWorkHecpModal').modal('show');
}

function proceedToInWorkEditAfterWarning() {
    var hecpid = $('#inputEditHecpId').val();
    var targetStep = $('#inputTargetStep').val();
    $('#editHecpModal').modal('hide');
    viewPage(hecpid, targetStep, true);
}

/* Edit In-Work HECP Pop Up Implementation */

function showMigrationDataCheckWarning(currentAuthor) {
    if (currentAuthor) {
        $('#currentAuthorDataMigration').text(currentAuthor);
        $('#currentAuthorContainerDataMigration').show();
    } else {
        $('#currentAuthorContainerDataMigration').hide();
    }
    $('#hecpDataMigrationCheckModal').modal('show');
}

/*Isolation Components*/
function validateIsolationComponents(type) {
    if (type == 1) {
        var name = $.trim($("#circuit-component-name").val());
        if (!(String.isNullOrEmpty(name))) {
            $('#save-component-button').removeAttr('disabled');
        }
        else { $('#save-component-button').attr('disabled', 'disabled'); }
    }
    else if (type == 2) {
        var name = $.trim($("#edit-circuit-component-name").val());
        if (!(String.isNullOrEmpty(name))) {
            $('#edit-save-component-button').removeAttr('disabled');
        }
        else {
            $('#edit-save-component-button').attr('disabled', 'disabled');
        }
    }
}

function showIsolationComponentsAdd(isShow) {
    if (isShow) {
        $('#circuit-component-id').val('');
        $('#circuit-component-name').val('');
        $('#circuit-component-panel').val('');
        $('#circuit-component-location').val('');
        $('#save-component-button').attr('disabled', 'disabled');
        $('#addIsolationComponent').show();
        $('#addComponentsDiv').hide();
        $('#editIsolationComponent').hide();
    }
    else {
        $('#addComponentsDiv').show();
        $('#addIsolationComponent').hide();
    }
}

function AddIsolationComponents(program) {
    var program = $('#manage-isolation-program-select').val();
    if ($.trim(program) == "") {
        toastr.warning("Please select the program.", "Warning");
        return false;
    }
    var isolationComponent = {
        CircuitId: $.trim($('#circuit-component-id').val()),
        CircuitNomenclature: $.trim($('#circuit-component-name').val()),
        Location: $.trim($('#circuit-component-location').val()),
        Panel: $.trim($('#circuit-component-panel').val()),
        Program: $.trim(program)
    };

    $.ajax({
        url: "/HECP/AddIsolationComponents",
        type: 'POST',
        data: JSON.stringify(isolationComponent),
        dataType: 'json',
        contentType: 'application/json; charset=utf-8',
        error: function (xhr) {
            var errorMsg = xhr.Status == 400 ? xhr.responseText : "";
            toastr.error(errorMsg, "Failed to Save Isolation Components", TOAST_OPTIONS);
        },
        success: function (response) {
            if (response.message == "Duplicate Component.") {
                if (!String.isNullOrEmpty(response.data.circuitId)) {
                    toastr["error"]("Isolation Component with ID " + response.data.circuitId + " already exists.", "Error", TOAST_OPTIONS);
                }
                else {
                    toastr["error"]("Isolation Component with Name " + response.data.circuitNomenclature + " already exists.", "Error", TOAST_OPTIONS);
                }
            }
            else {
                toastr.success("Saved Isolation Components", "Success");
                $('#circuit-component-id').val('');
                $('#circuit-component-name').val('');
                $('#circuit-component-panel').val('');
                $('#circuit-component-location').val('');
                $('#save-component-button').attr('disabled', 'disabled');
                filterIsolationComponentData(true);
            }
        },
        complete: function () {

        }
    });

}
function showEditComponent(id, cId, name, panel, row) {
    collapseAddComponents();
    $('#editIsolationComponent').show();
    $('#edit-circuit-id').val(id);
    $('#edit-circuit-component-id').val(cId);
    $('#edit-circuit-component-name').val(name);
    $('#edit-circuit-component-panel').val(panel);
    $('#edit-circuit-component-location').val(row);
    $('#edit-circuit-component-id').focus();
}
function hideEditComponent() {
    $('#editIsolationComponent').hide();
}
function UpdateIsolationComponents(program) {
    var program = $('#manage-isolation-program-select').val();
    if ($.trim(program) == "") {
        toastr.warning("Please select the program.", "Warning");
        return false;
    }
    var isolationComponent = {
        Id: $.trim($('#edit-circuit-id').val()),
        CircuitId: $.trim($('#edit-circuit-component-id').val()),
        CircuitNomenclature: $.trim($('#edit-circuit-component-name').val()),
        Location: $.trim($('#edit-circuit-component-location').val()),
        Panel: $.trim($('#edit-circuit-component-panel').val()),
        Program: $.trim(program)
    };

    $.ajax({
        url: "/HECP/UpdateIsolationComponents",
        type: 'POST',
        data: JSON.stringify(isolationComponent),
        dataType: 'json',
        contentType: 'application/json; charset=utf-8',
        error: function (xhr) {
            var errorMsg = xhr.Status == 400 ? xhr.responseText : "";
            toastr.error(errorMsg, "Failed to Update Isolation Components", TOAST_OPTIONS);
        },
        success: function (response) {
            if (response.message == "Duplicate Component.") {
                if (!String.isNullOrEmpty(response.data.circuitId)) {
                    toastr["error"]("Isolation Component with ID " + response.data.circuitId + " already exists.", "Error", TOAST_OPTIONS);
                }
                else {
                    toastr["error"]("Isolation Component with Name " + response.data.circuitNomenclature + " already exists.", "Error", TOAST_OPTIONS);
                }
            }
            else {
                toastr.success("Updated Isolation Components", "Success");
                $('#editIsolationComponent').hide();
                var componentTable = $('#dtIsolationResult').DataTable();
                var pageNumber = componentTable.page.info().page;
                filterIsolationComponentData(false, pageNumber);
            }
        },
        complete: function () {

        }
    });
}

function collapseAddComponents() {
    var panel = document.getElementsByClassName("panel1");
    if (panel.length > 0) {
        if (panel[0].style.maxHeight) {
            panel[0].style.maxHeight = null;
        }
    }
}

/*Isolation Components*/

/* ITAR */
function updateItar(hecpId, value) {
    $.ajax({
        url: '/Hecp/UpdateHecpItar?hecpId=' + hecpId + '&isItar=' + value,
        type: 'get',
        dataType: 'html',
        cache: false,
        success: function (response) {
            if (response == "true") {
                if (value == true)
                    toastr.success("HECP marked as ITAR", "Success", TOAST_OPTIONS_SHORT_TIMEOUT);
                else
                    toastr.success("HECP marked as non-ITAR", "Success", TOAST_OPTIONS_SHORT_TIMEOUT);
            }
            else {
                toastr.error("An error occured. Please try again after some time.", "Error");
            }
        },
        error: function (error) {
            console.log(error);
        }
    });
}
/* ITAR */
function saveItarPrograms() {

    var programsArr = [];
    $("#nonItarList option").each(function () {
        programsArr.push({
            "ProgramId": $(this).val(), "IsItarRestricted": false
        });
    });

    $("#itarList option").each(function () {
        programsArr.push({
            "ProgramId": $(this).val(), "IsItarRestricted": true
        });
    });
    var request = JSON.stringify({ 'Programs': programsArr });

    $.ajax({
        contentType: 'application/json; charset=utf-8',
        dataType: 'json',
        type: 'POST',
        url: '/Admin/SaveItarPrograms',
        data: request,
        success: function (response) {
            if (response.status == 'SUCCESS') {
                toastr.success("Saved ITAR Programs", "Success");
            } else {
                toastr['error']("An error occured while saving ITAR Programs", 'Error', TOAST_OPTIONS);
            }

        },
        error: function (msg) {
            console.log("Error: " + msg);
            toastr['error']("An error occured while saving ITAR Programs", 'Error', TOAST_OPTIONS);
        },
    });
}

function FilterItarHecp() {
    var hecpName = $.trim($('#Itar-Hecp-Name').val());
    hecpName = hecpName.trim().replace(/ /g, '%20');

    var selectedProgram = $('#Itar-Hecp-Program-Select option:selected').text();
    selectedProgram = selectedProgram.trim().replace(/ /g, '%20');

    var hecpATA = $.trim($('#Itar-Hecp-ATA').val());
    hecpATA = hecpATA.trim().replace(/ /g, '%20');

    var hecpType = $('#Itar-Hecp-DocType-Select option:selected').text();
    hecpType = hecpType.trim().replace(/ /g, '%20');
    $.ajax({
        url: '/Admin/FilterItarHecps?Program=' + selectedProgram + '&name=' + hecpName + '&ATA=' + hecpATA + '&docType=' + hecpType,
        type: 'GET',
        success: function (response) {
            //$("#itar-hecps").load('/Admin/FilterItarHecps?Program=' + selectedProgram + '&name=' + hecpName + '&ATA=' + hecpATA + '&docType=' + hecpType);
            loadItarHEcp(response)
        },
        error: function () {
            toastr['error']("An error occured while updating ITAR Hecp List. Please try again", 'Error', TOAST_OPTIONS);
        },
    });
}

function resetHecpItarFilters() {
    $.ajax({
        url: '/Admin/FilterItarHecps?Program=' + "" + '&name=' + "" + '&ATA=' + "" + '&docType=' + "",
        type: 'GET',
        success: function (response) {
            //$("#itar-hecps").load('/Admin/FilterItarHecps?Program=' + "" + '&name=' + "" + '&ATA=' + "" + '&docType=' + "");
            loadItarHEcp(response);
            $('#Itar-Hecp-Name').val('');
            $('#Itar-Hecp-ATA').val('');
            $('#Itar-Hecp-Program-Select').prop('selectedIndex', 0);
            $('#Itar-Hecp-DocType-Select').prop('selectedIndex', 0);
        },
        error: function () {
            toastr['error']("An error occured while trying to reset ITAR Hecp List filter. Please try again", 'Error', TOAST_OPTIONS);
        },
    });
}

function loadItarHEcp(data) {
    if ($.fn.dataTable.isDataTable('#itarHecpList')) {
        table = $('#itarHecpList').DataTable();
        table.destroy();
    }
    var trHTML = '';
    $('#itarHecpList tbody').html('');
    $.each(data, function (i, item) {
        var checkboxText = "";
        var revisionText = "";
        var hecpName = "";
        var hecpProgram = "";
        if (item.name == null) {
            hecpName = "";
        }
        else {
            hecpName = "'" + item.name + "'";
        }
        if (item.isItar == true) {
            checkboxText = '<input id="itar_reviewandsign" onchange="markItar(' + item.id + ',' + hecpName + ', this.checked' + ');" type="checkbox" checked />';
        }
        else {
            checkboxText = '<input id="itar_reviewandsign" onchange="markItar(' + item.id + ',' + hecpName + ',this.checked' + ');" type="checkbox" />';
        }
        if (item.revision == null) {
            revisionText = "";
        }
        else {
            revisionText = item.revision;
        }
        if (item.program == null) {
            hecpProgram = "";
        }
        else {
            hecpProgram = item.program;
        }
        trHTML += '<tr><td style="display:none;">' + item.id + '</td ><td><a href="/Hecp/ViewDetails/?hecpId=' + item.id + '&targetStep= 8" target="_blank" style="color: #0000FF">' + item.name + '</a></td><td>' + hecpProgram + '</td><td>' + revisionText +
            '</td><td>' + item.status.displayName + '</td><td>' + checkboxText + '</td></tr>';
    });

    $('#itarHecpList tbody').append(safeResponseFilter(trHTML));

    $('#itarHecpList').dataTable({
        "bPaginate": true,
        "bSort": false,
        "bFilter": false,
        "info": false,
        "bLengthChange": false,
        "autoWidth": false,
        "pageLength": 20,
    });
}

function validateHecpFilters() {

    let HecpFormFilterData = new FormData(document.getElementById("HecpFilterForm"));
    let HecpName = HecpFormFilterData.get("HecpName");
    let SiteName = HecpFormFilterData.get("SiteName");
    let AtaChapterNumber = HecpFormFilterData.get("AtaChapterNumber");
    let AtaChapterTitle = HecpFormFilterData.get("AtaChapterTitle");
    let AffectedSystem = HecpFormFilterData.get("AffectedSystem");
    let HecpStatus = HecpFormFilterData.get("HecpStatusName");
    let IsEngineered = HecpFormFilterData.get("isEngineered");
    
    if (HecpName || AtaChapterNumber || AtaChapterTitle || AffectedSystem || SiteName || HecpStatus || IsEngineered) {
        $('#loading-hecp').show();
        return true;
    }
    return false;
}

function resetHecpFilters(status) {
    var $container = status ? $('#published-partial') : $('#draft-partial');
    $container.data('loaded', false);
    $.ajax({
        url: '/Hecp/ClearHecpFilters',
        type: 'POST',
        data: { isPublishedList: status },
        success: function () {
            // Reload tab data after clearing filters
            FilteredHecpDataTab(1, status);
        },
        error: function () {
            toastr.error("Failed to reset filters on server.");
        }
    });
}

function toggleRemainingCommentsPerRevision(revision) {
    $('.comments-Revision-' + revision + '-Normal').toggle();
    var weight = $('.comment-element-Revision-' + revision).css("font-weight");

    if (weight == "300") {
        $('.comment-element-Revision-' + revision).css("font-weight", "bold");
        $('.comment-element-Revision-' + revision).css("font-weight", "bold");
    }
    else {
        $('.comment-element-Revision-' + revision).css("font-weight", "300");
        $('.comment-element-Revision-' + revision).css("font-weight", "300");
    }
}
function validateLineName() {
    var re = new RegExp("^[a-zA-Z0-9() -]*$");
    if (!re.test($('#aircraftForm-lineNumber').val())) {
        document.getElementById("error").innerHTML = "Invalid name format - Valid name can contain Alphanumeric characters, '()' , '-' ";
        return false;
    }
    else {
        return true;
    }
}

function validateHECPFilterButtonPublished() {
    let HecpName = $("#HecpNameIdPublished").val();
    let AtaChapterNumberId = $("#AtaChapterNumberIdPublished").val();
    let AtaChapterTitleId = $("#AtaChapterTitleIdPublished").val();
    let AffectedSystemId = $("#AffectedSystemIdPublished").val();
    let Site = $('#site-select-filter-published option:selected')[0].value;
    let IsEngineered = $('#HecpTypePublished option:selected')[0].value;
    let CircuitId = $("#CircuitId").val();
    let CircuitName = $("#CircuitName").val();

    if (CircuitId && !CircuitName) {
        disableFilterButton(".search-hecp-filter-published");
        return;
    }

    if (HecpName !== "" || AtaChapterNumberId !== "" || AtaChapterTitleId !== "" || AffectedSystemId !== "" || Site !== "" || IsEngineered !== "" || (CircuitId && CircuitName)) {
        enableFilterButton(".search-hecp-filter-published");
    }
    else {
        disableFilterButton(".search-hecp-filter-published");
    }
}

function validateHECPFilterButton() {
    let HecpName = $("#HecpNameId").val();
    let AtaChapterNumberId = $("#AtaChapterNumberId").val();
    let AtaChapterTitleId = $("#AtaChapterTitleId").val();
    let AffectedSystemId = $("#AffectedSystemId").val();
    let Site = $('#site-select-filter option:selected')[0].value;
    let HecpStatus = $('#HecpStatusName').val();
    let IsEngineered = $('#HecpType option:selected')[0].value;

    if (HecpName !== "" || AtaChapterNumberId !== "" || AtaChapterTitleId !== "" || AffectedSystemId !== "" || Site !== "" || HecpStatus !== "" || IsEngineered !== "") {
        enableFilterButton(".search-hecp-filter");
    }
    else {
        disableFilterButton(".search-hecp-filter");
    }
}

function GetFilteredHecpData(pageNumber, sortOption = null) {
    let HecpName = $("#HecpNameId").val();
    let AtaChapterNumberId = $("#AtaChapterNumberId").val();
    let AtaChapterTitleId = $("#AtaChapterTitleId").val();
    let AffectedSystemId = $("#AffectedSystemId").val();
    let Site = $('#site-select-filter option:selected')[0].value;
    let Program = $("#Program").val();
    let HecpStatus = $('#HecpStatusName option:selected')[0].value;
    let isEngineered = "";
    if ($('#HecpType option:selected')[0].value === "true") {
        isEngineered = true;
    }
    else if ($('#HecpType option:selected')[0].value === "false") {
        isEngineered = false;
    }
    
    if (sortOption == null) {
        sortOption = GetSortOption();
    }
    $('#loading-hecp').show();
    let isPublishedList = getIsPublished();

    $.ajax({
        url: '/Hecp/ViewHecpList?program=' + Program + "&hecpName=" + HecpName + "&siteName=" + Site + "&ataChapterNumber=" + AtaChapterNumberId + "&ataChapterTitle=" + AtaChapterTitleId + "&affectedSystem=" + AffectedSystemId + "&hecpStatusName=" + HecpStatus + "&isPublishedList=" + isPublishedList + "&CircuitId=&CircuitName=&pageNumber=" + pageNumber + "&isEngineered=" + isEngineered + "&sortOption=" + sortOption,
        type: 'GET',
        success: function (objectResult) {
            if (isPublishedList) {
                $('#hecp-list-table-published').html(safeResponseFilter(objectResult));
            }
            else {
                $('#hecp-list-table-draft').html(safeResponseFilter(objectResult));
            }
            $('.convert-to-local-time-short').each(function () {
                $(this).html(UTCDateStringToLocalShortTime($(this).html()));
            });
            if (sortOption != null) {
                SetSortClass(sortOption);
            }
            $('#loading-hecp').hide();
        },
        error: function () {
            toastr["error"]("Failed to get the list of Hecps due to some error.", "Get Hecp list Error", TOAST_OPTIONS);
        }
    });
}

function GetPaginatedHecpData(pageNumber,sortOption = null) {
    let isPublishedList = getIsPublished();
    if (isPublishedList) {
        GetFilteredHecpDataPublished(pageNumber, sortOption)
    }
    else {
        GetFilteredHecpData(pageNumber, sortOption)
    }
}

function GetFilteredHecpDataPublished(pageNumber, sortOption = null) {
    let HecpName = $("#HecpNameIdPublished").val();
    let AtaChapterNumberId = $("#AtaChapterNumberIdPublished").val();
    let AtaChapterTitleId = $("#AtaChapterTitleIdPublished").val();
    let AffectedSystemId = $("#AffectedSystemIdPublished").val();
    let Site = $('#site-select-filter-published option:selected')[0].value;
    let Program = $("#Program").val();
    let CircuitId = $("#CircuitId").val() || "";
    let CircuitName = $("#CircuitName").val() || "";
    let isEngineered = "";
    if ($('#HecpTypePublished option:selected')[0].value === "true") {
        isEngineered = true;
    }
    else if ($('#HecpTypePublished option:selected')[0].value === "false") {
        isEngineered = false;
    }
    
    if (sortOption == null) {
        sortOption = GetSortOption();
    }
    $('#loading-hecp-published').show();
    let isPublishedList = getIsPublished();

    $.ajax({
        url: '/Hecp/ViewHecpList?program=' + Program + "&hecpName=" + HecpName + "&siteName=" + Site + "&ataChapterNumber=" + AtaChapterNumberId + "&ataChapterTitle=" + AtaChapterTitleId + "&affectedSystem=" + AffectedSystemId + "&hecpStatusName=&isPublishedList=" + isPublishedList + "&CircuitId=" + CircuitId + "&CircuitName=" + CircuitName + "&pageNumber=" + pageNumber + "&isEngineered=" + isEngineered + "&sortOption=" + sortOption,
        type: 'GET',
        success: function (objectResult) {
            if (isPublishedList) {
                $('#hecp-list-table-published').html(safeResponseFilter(objectResult));
            }
            else {
                $('#hecp-list-table-draft').html(safeResponseFilter(objectResult));
            }
            $('.convert-to-local-time-short').each(function () {
                $(this).html(UTCDateStringToLocalShortTime($(this).html()));
            });
            if (sortOption != null) {
                SetSortClass(sortOption);
            }
            $('#loading-hecp-published').hide();
        },
        error: function () {
            toastr["error"]("Failed to get the list of Hecps due to some error.", "Get Hecp list Error", TOAST_OPTIONS);
        }
    });
}

function SetSortClass(sortOption) {
    let sortByNameButtonId = $("#hecpNameSort");
    let sortByHecpIdButtonId = $("#hecpNumberSort");
    let sortByHecpDateButtonId = $("#hecpDateSort");

    switch (sortOption) {
        case "NameDesc":
            sortByNameButtonId.removeClass("sortByNameAsc");
            sortByNameButtonId.addClass("sortByNameDesc");
            break;
        case "NameAsc":
            sortByNameButtonId.removeClass("sortByNameDesc");
            sortByNameButtonId.addClass("sortByNameAsc");
            break;
        case "IdAsc":
            sortByHecpIdButtonId.removeClass("sortByIdDesc");
            sortByHecpIdButtonId.addClass("sortByIdAsc");
            break;
        case "IdDesc":
            sortByHecpIdButtonId.removeClass("sortByIdAsc");
            sortByHecpIdButtonId.addClass("sortByIdDesc");
            break;
        case "PublishedDateDesc":
            sortByHecpDateButtonId.removeClass("sortByDateAsc");
            sortByHecpDateButtonId.addClass("sortByDateDesc");
            break;
        case "PublishedDateAsc":
            sortByHecpDateButtonId.removeClass("sortByDateDesc");
            sortByHecpDateButtonId.addClass("sortByDateAsc");
            break;
    }

    $("#Hecp-name-loading").hide();
    $("#Hecp-number-loading").hide();
    $("#Hecp-date-loading").hide();
}

function GetSortOption() {
    let sortByNameButtonId = $("#hecpNameSort");
    let sortByHecpIdButtonId = $("#hecpNumberSort");
    let sortByHecpDateButtonId = $("#hecpDateSort");
    let sortOption = null;
    if (sortByNameButtonId.attr('class').includes("sortByNameAsc")) {
        sortOption = "NameAsc";
    }
    else if (sortByNameButtonId.attr('class').includes("sortByNameDesc")) {
        sortOption = "NameDesc";
    }
    else if (sortByHecpIdButtonId.attr('class').includes("sortByIdDesc")) {
        sortOption = "IdDesc";
    }
    else if (sortByHecpIdButtonId.attr('class').includes("sortByIdAsc")) {
        sortOption = "IdAsc";
    }
    else if (sortByHecpDateButtonId.length > 0 && sortByHecpDateButtonId.attr('class').includes("sortByDateAsc")) {
        sortOption = "PublishedDateAsc";
    }
    else if (sortByHecpDateButtonId.length > 0 && sortByHecpDateButtonId.attr('class').includes("sortByDateDesc")) {
        sortOption = "PublishedDateDesc";
    }
    return sortOption;
}

function GetFilteredAndSortedHecpDataByName(pageNumber) {
    $("#Hecp-name-loading").show();
    $("#Hecp-name-loading")[0].style.display = 'inline-block'
    let sortButtonId = $("#hecpNameSort");
    let isPublishedList = getIsPublished();
    if (sortButtonId.attr('class').includes("sortByNameAsc") || sortButtonId.attr('class') === "copy-btn") {
        if (isPublishedList) {
            GetFilteredHecpDataPublished(pageNumber, "NameDesc")
        }
        else {
            GetFilteredHecpData(pageNumber, "NameDesc")
        }
    }
    else if (sortButtonId.attr('class').includes("sortByNameDesc")) {
        if (isPublishedList) {
            GetFilteredHecpDataPublished(pageNumber, "NameAsc")
        }
        else {
            GetFilteredHecpData(pageNumber, "NameAsc")
        }
    }
}

function GetFilteredAndSortedHecpDataById(pageNumber) {
    $("#Hecp-number-loading").show();
    $("#Hecp-number-loading")[0].style.display = 'inline-block'
    let sortButtonId = $("#hecpNumberSort");
    let isPublishedList = getIsPublished();
    if (sortButtonId.attr('class').includes("sortByIdAsc") || sortButtonId.attr('class') === "copy-btn") {
        if (isPublishedList) {
            GetFilteredHecpDataPublished(pageNumber, "IdDesc")
        }
        else {
            GetFilteredHecpData(pageNumber, "IdDesc")
        }
    }
    else if (sortButtonId.attr('class').includes("sortByIdDesc")) {
        if (isPublishedList) {
            GetFilteredHecpDataPublished(pageNumber, "IdAsc")
        }
        else {
            GetFilteredHecpData(pageNumber, "IdAsc")
        }
    }
}

function GetFilteredAndSortedHecpDataByDate(pageNumber) {
    $("#Hecp-date-loading").show();
    $("#Hecp-date-loading")[0].style.display = 'inline-block';
    let sortButtonId = $("#hecpDateSort");
    let isPublishedList = getIsPublished();
    if (sortButtonId.attr('class').includes("sortByDateAsc") || sortButtonId.attr('class') === "copy-btn") {
        if (isPublishedList) {
            GetFilteredHecpDataPublished(pageNumber, "PublishedDateDesc")
        }
        else {
            GetFilteredHecpData(pageNumber, "PublishedDateDesc")
        }
    }
    else if (sortButtonId.attr('class').includes("sortByDateDesc")) {
        if (isPublishedList) {
            GetFilteredHecpDataPublished(pageNumber, "PublishedDateAsc")
        }
        else {
            GetFilteredHecpData(pageNumber, "PublishedDateAsc")
        }
    }
}

function FilteredHecpDataTab(pageNumber, status) {
    let Program = $("#Program").val();
    var isPublishedList = status;
    if (isPublishedList) {
        $("#published-inst").removeAttr('hidden');
    }
    else {
        $("#published-inst").attr('hidden', true);
    }
    const url = new URL(window.location);
    url.searchParams.set('isPublishedList', isPublishedList);
    window.history.replaceState(null, '', url);

    var $container = isPublishedList ? $('#published-partial') : $('#draft-partial');
    if ($container.data('loaded')) {
        $('.convert-to-local-time-short').each(function () {
            const utcDateStr = $(this).data('utc');
            if (utcDateStr) {
                const localDateStr = UTCDateStringToLocalShortTime(utcDateStr);
                $(this).html(localDateStr);
            }
        });
        return;
    }
    $('.hecp-list-table').hide();
    if (isPublishedList) {
        $('#loading-hecp-dashboard').show();
    }
    else {
        $('#loading-hecp-dashboard-draft').show();
    }
  
    $.ajax({
        url: '/Hecp/ViewHecpTab?program=' + Program + "&hecpName=&siteName=&ataChapterNumber=&ataChapterTitle=&affectedSystem=&hecpStatusName=&isPublishedList=" + isPublishedList + "&pageNumber=" + pageNumber + "&isEngineered=&sortOption=",
        type: 'GET',
        success: function (objectResult) {
            if (isPublishedList) {
                $('#published-partial').html(safeResponseFilter(objectResult));
                $('#loading-hecp-dasboard').hide();
                $('.hecp-list-table').show();

            }
            else {
                $('#draft-partial').html(safeResponseFilter(objectResult));
                $('#loading-hecp-dashboard-draft').hide();
                $('.hecp-list-table').show();
            }
            $container.data('loaded', true);
            $('.convert-to-local-time-short').each(function () {
                $(this).html(UTCDateStringToLocalShortTime($(this).html()));
            });
            
        },
        error: function () {
            toastr["error"]("Failed to get the list of Hecps due to some error.", "Get Hecp list Error", TOAST_OPTIONS);
        }
    });
}

function getIsPublished() {
    if ($('#published-tab').hasClass('active')) {
        return true;
    } else if('#draft-tab') {
        return false;
    }
}

function copyToClipBoard(copyText) {
    navigator.clipboard.writeText(copyText);
}

function updateAuthorPopup(hecpId, hecpName, createdBy, createdByBemsId) {
    $("#updateAuthorHecpNameLabel").text(hecpName);
    $("#updateAuthorHecpId").val(hecpId);
    if (createdBy) {
        $("#updateAuthorHecpCurrentAuthorNameLabel").text(createdBy + " (" + createdByBemsId + ")");
    }
    else {
        $("#updateAuthorHecpCurrentAuthorNameLabel").text(createdByBemsId);
    }
    // Load the user list
    $.ajax({
        url: '/Admin/GetAllUserList',
        type: 'GET',
        success: function (data) {
            $("#hecpAuthorUserListContainer").html(data);
            $("#hecpAuthorUpdateSearch").on("keyup", function () {
                filterUsersForAuthorUpdate();
            });
            $("#editHecpAuthorPopup").modal('show');
        },
        error: function () {
            toastr['error']("Error loading user list");
        }
    });
    hecpAuthorUpdateCompleted = false
}

function filterUsersForAuthorUpdate() {
    let input = $("#hecpAuthorUpdateSearch").val().trim().toLowerCase();
    var count = 0;
    $("#dropdownList .user-items").each(function () {
        var option = $(this).text().toLowerCase();

        if (option !== '' && option.includes(input)) {    
            var highlighted = $(this).text().replace(new RegExp(input, 'gi'), function (match) {
                return '<span class="author-update-highlight">' + match + '</span>';
            })
            $(this).html(highlighted).show()
            count++
        }
        else if(input == ''){
            $(this).show();
        }
        else {
            $(this).hide();
        }
    });
    if (count === 0) {
        $("#noResultMessage").removeAttr('hidden');
        $("#hecpAuthorUpdateButton").prop('disabled', true);
    }
    else {
        $("#noResultMessage").attr('hidden', true);
        $("#hecpAuthorUpdateButton").prop('disabled', false);
    }
}

function filterHecpApprovers() {
    var input = $('#searchInput').val().toLowerCase();
    var userItems = $('.user-item');
    var noResults = $('#noResults');
    var hasResults = false;

    userItems.each(function () {
        var userId = $(this).data('userid');
        var userName = $(this).find('label').text().toLowerCase();

        // Check if the userId or userName matches the search input
        if (userId.toString().includes(input) || userName.includes(input)) {
            $(this).show();
            highlightMatch($(this), input);
            hasResults = true; // Found a matching user
        } else {
            $(this).hide();
        }
    });

    // Show or hide the "No Results Found" message
    if (!hasResults) {
        noResults.show();
    } else {
        noResults.hide();
    }
}

function highlightMatch(item, search) {
    var label = item.find('label');
    var text = label.text();
    var regex = new RegExp('(' + search + ')', 'gi');
    var highlightedText = text.replace(regex, '<span class="author-update-highlight">$1</span>');
    label.html(highlightedText);
}

function showDropDown() {
    $('#hecpAuthorUpdateSearch').val('');
    $("#dropdownList li").each(function () {
        $(this).html($(this).text());
        $(this).show();
    });
    $('#dropdownContent').toggle();
    $('#hecpAuthorUpdateSearch').focus();
}

function enterSelect(event) {
    if (event.key === 'Enter') {
        event.preventDefault();
        var topItem = $('.user-items:visible').first();
        if (topItem.length > 0) {
            selectAuthor(topItem[0]);
        }
    }
}

function selectAuthor(input) {
    $('#dropdownDisplay').text(input.innerText);
    $('#dropdownDisplay').attr("userbemsid", input.getAttribute("data-userbemsid"))
    $('#dropdownDisplay').attr("username", input.getAttribute("data-username")) 
    $('#dropdownContent').hide();
    $('#hecpAuthorUpdateSearch').val('');
    $("#hecpAuthorUpdateButton").prop('disabled', false);
}

function submitHecpAuthorUpdateForm() {
    var authorUpdateMessage = $("#authorChangeMessage");
    var newAuthorBemsId = $('#dropdownDisplay')[0].getAttribute("userbemsid");
    var newAuthorName = $('#dropdownDisplay')[0].getAttribute("username");
    var hecpName = $("#updateAuthorHecpNameLabel").text();
    var hecpId = $("#updateAuthorHecpId").val();

    $.ajax({
        url: '/Hecp/UpdateHecpAuthor?hecpId=' + hecpId + "&newAuthorBemsId=" + newAuthorBemsId + "&newAuthorName=" + newAuthorName + "&hecpName=" + hecpName,
        type: 'post',
        async: false,
        cache: false,
        processData: false,
        contentType: false,
        success: function (response) {
            $(authorUpdateMessage).text("");
            $("#editHecpAuthorModalBody").hide();
            if (response) {
                let message = newAuthorName + '(' + newAuthorBemsId + ') has been successfully assigned as the Author of the HECP: ' + hecpName;
                $(authorUpdateMessage).text(message)
                $(authorUpdateMessage).removeAttr('hidden');
            } else {
                $(authorUpdateMessage).text("Could not update author. Please try again!")
                $(authorUpdateMessage).removeAttr('hidden');
            }
            $("#hecpAutherUpdateresult").show()
            $("#hecpAutherUpdateresult").removeAttr('hidden');
            hecpAuthorUpdateCompleted = true;
        },
        error: function (xhr) {
            console.log('Error: ' + xhr);
            toastr.error("An error has occurred when attempting to Update the author.", "Error");
            hecpAuthorUpdateCompleted = true;
        }
    });
}

function closeTheAuthorPopup(pageNumber) {
    if (hecpAuthorUpdateCompleted) {
        getHecpForProgram(pageNumber)
    }    
}

function initTinyMceForDeactivation() {
        tinymce.init({
            selector: '.tinymce-editor',
            license_key: 'gpl',
            browser_spellcheck: true,
            contextmenu: false,
            menubar: false,
            statusbar: true,
            branding: false,
            resize: true,
            height: 250,
            plugins: " anchor autolink insertdatetime link lists preview visualblocks advlist",
            toolbar1: 'bold italic underline strikethrough fontfamily fontsize  | forecolor backcolor | bullist numlist | lineheight| outdent indent | alignleft aligncenter alignright alignjustify ',
            toolbar2: 'link image | preview | undo redo ',
            remove_linebreaks: true,
            paste_preprocess: function (plugin, args) {
                if (/<img/.test(args.content)) {
                    args.content = args.content.replace(/<img .*?>/g, '');
                    toastr.warning("Please use upload button below", "Image pasting not supported!!")
                }                
            },
            setup: function (editor) {
            editor.on('input', function (e) {
                deactivationStepOnInput(editor);
            });
            editor.on('change', function (e) {
                deactivationStepOnChange(editor);
            });
        }
    });
}
function initTinyMceForScopePartial() {
    tinymce.init({
        selector: 'textarea',
        license_key: 'gpl',
        browser_spellcheck: true,
        contextmenu: false,
        menubar: false,
        statusbar: true,
        branding: false,
        resize: true,
        height: 250,
        plugins: " anchor autolink insertdatetime link lists preview visualblocks advlist",
        toolbar1: 'bold italic underline strikethrough fontfamily fontsize  | forecolor backcolor | bullist numlist | lineheight| outdent indent | alignleft aligncenter alignright alignjustify ',
        toolbar2: 'link image | preview | undo redo ',
        remove_linebreaks: true,
        paste_preprocess: function (plugin, args) {
            if (/<img/.test(args.content)) {
                args.content = args.content.replace(/<img .*?>/g, '');
                toastr.warning("Please use upload button below", "Image pasting not supported!!")
            }
        },
        setup: function (editor) {
            editor.on('change', function (e) {
                setIsDirtyTrue();
                editor.save();
            });
        }
    });
}
function initTinyMceForTryout() {
    tinymce.init({
        selector: '.tinymce-editor',
        license_key: 'gpl',
        browser_spellcheck: true,
        contextmenu: false,
        menubar: false,
        statusbar: true,
        branding: false,
        resize: true,
        height: 250,
        plugins: " anchor autolink insertdatetime link lists preview visualblocks advlist",
        toolbar1: 'bold italic underline strikethrough fontfamily fontsize  | forecolor backcolor | bullist numlist | lineheight| outdent indent | alignleft aligncenter alignright alignjustify ',
        toolbar2: 'link image | preview | undo redo ',
        remove_linebreaks: true,
        paste_preprocess: function (plugin, args) {
            if (/<img/.test(args.content)) {
                args.content = args.content.replace(/<img .*?>/g, '');
                toastr.warning("Please use upload button below", "Image pasting not supported!!")
            }
        },
        setup: function (editor) {
            editor.on('input', function (e) {
                tryoutStepOnInput(editor);
            });
            editor.on('change', function (e) {
                tryoutStepOnChange(editor);
            });
        }
    });
}
function initTinymceForReactivation() {
    tinymce.init({
        selector: 'textarea',
        license_key: 'gpl',
        browser_spellcheck: true,
        contextmenu: false,
        menubar: false,
        statusbar: true,
        branding: false,
        resize: true,
        height: 250,
        plugins: " anchor autolink insertdatetime link lists preview visualblocks advlist",
        toolbar1: 'bold italic underline strikethrough fontfamily fontsize  | forecolor backcolor | bullist numlist | lineheight| outdent indent | alignleft aligncenter alignright alignjustify ',
        toolbar2: 'link image | preview | undo redo ',
        remove_linebreaks: true,
        paste_preprocess: function (plugin, args) {
            if (/<img/.test(args.content)) {
                args.content = args.content.replace(/<img .*?>/g, '');
                toastr.warning("Do not Paste Image", "Image pasting not supported!!")
            }
        },
        setup: function (editor) {
            editor.on('input', function (e) {
                reactivationOnInput(editor);
            });
            editor.on('change', function (e) {
                reactivationOnChange(editor);
            });
        }
    });
}

function moveStepUp(stepElement) {
    isDirty = true;
    tinymce.remove();
    let wrapper = $(stepElement).closest('li')
    wrapper.insertBefore(wrapper.prev())
    renameStepNumbers()
}

function moveStepDown(stepElement) {
    isDirty = true;
    tinymce.remove();
    let wrapper = $(stepElement).closest('li')
    wrapper.insertAfter(wrapper.next())
    renameStepNumbers()
}

function renameStepNumbers() {
    let stepElements;
    let currentStep = $('#current-step').val();
    if (currentStep == 5) {
        initTinyMceForDeactivation();
        stepElements = $("#wrapper-deactivation-steps").children().find(".row-hecp-deactivation-step");
        $.each(stepElements, function (i, v) {
            let steptoshow = i + 1;
            $(v).find(".deactstepnumber").text("Step " + steptoshow);
        });
        disableMoveStepButtons(
            'wrapper-deactivation-steps',
            'row-hecp-deactivation-step',
            'deact-step-up-btn',
            'deact-step-down-btn',
            'deact-move-step'
        );
    }
    else if (currentStep == 6) {
        initTinyMceForTryout();
        stepElements = $("#wrapper-discrete-steps").children().find(".row-hecp-step");

        $.each(stepElements, function (i, v) {
            let steptoshow = i + 1;
            $(v).find(".tryout-procedure-step-label").text("Step " + steptoshow);
        });
        disableMoveStepButtons(
            'wrapper-discrete-steps',
            'row-hecp-step',
            'tryout-step-up-btn',
            'tryout-step-down-btn',
            'tryout-move-step'
        );
    }
    else if (currentStep == 7) {
        initTinymceForReactivation();
        stepElements = $("#wrapper-reactivation-steps").children().find(".row-hecp-reactivation-step");

        $.each(stepElements, function (i, v) {
            let steptoshow = i + 1;
            $(v).find(".reactstepnumber").text("Step " + steptoshow);
        });
        disableMoveStepButtons(
            'wrapper-reactivation-steps',
            'row-hecp-reactivation-step',
            'react-step-up-btn',
            'react-step-down-btn',
            'react-move-step'
        );
    }
}

function disableMoveStepButtons(wrapperId, rowClass, upBtnId, downBtnId, moveStepClass) {
    let stepElements = $(`#${wrapperId}`).children().find(`.${rowClass}`);
    stepElements.find(`#${upBtnId}, #${downBtnId}`).css('visibility', 'visible');
    stepElements.first().find(`#${upBtnId}`).css('visibility', 'hidden');
    stepElements.last().find(`#${downBtnId}`).css('visibility', 'hidden');
    $(`.${moveStepClass}`).css('visibility', stepElements.length === 1 ? 'hidden' : 'visible');
}

function checkIsolationLimit() {
    if ($('.input-system_circuit').length > 200) {
        toastr.warning("You are approaching the maximum limit for isolations on this page. Adding more may lead to performance issues. Please proceed with caution.", "Warning", TOAST_OPTIONS);
    }
}
function toggleAccordion(program) {
    if ($('#circuit-id').is(':hidden')) {
        $('#circuit-id').attr('hidden', false);
        $('#circuit-name').attr('hidden', false);
        getCircuitIds(program);
    }
    else {
        $('#CircuitId').empty();
        $('#CircuitName').empty();
        $('#circuit-id').attr('hidden', true);
        $('#circuit-name').attr('hidden', true);
    }
}

function getCircuitIds(program) {
    $('#CircuitName').attr('disabled', true);
    $.ajax({
        url: '/Hecp/GetPublishedCircuitIds/',
        type: 'GET',
        data: { program: program },
        success: function (response) {
            if (response) {
                    let circuitIds = response;
                    circuitIds = circuitIds
                        .filter(function (item, pos, arr) {
                            return arr.indexOf(item) === pos;
                        })
                        .sort();
                    $("#CircuitId").html("<option value='' selected></option>");

                    $.each(circuitIds, function (i, value) {
                        $("#CircuitId").append("<option value='" + value + "'>" + value + "</option>");
                    });

            } else {
                if (window.toastr) {
                    toastr['error']("No circuit IDs returned for program " + program, "Error", TOAST_OPTIONS);
                }
            }

        },
        error: function (e) {
            console.log(e);
            if (window.toastr) {
                toastr['error']("Failed to retrieve circuit IDs for program " + program, "Error", TOAST_OPTIONS);
            }
        }
    });
}

function getCircuitDetails(program) {
    $('#CircuitName').removeAttr('disabled');
    $('#circuit-loading').show && $('#circuit-loading').show();
    let circuitId = $("#CircuitId").val();
    $.ajax({
        url: '/Hecp/GetPublishedCircuitDetails/',
        type: 'GET',
        data: { program: program, circuitId: circuitId },
        success: function (response) {
            if (response) {
                let circuitName = response;
                circuitName = circuitName
                    .filter(function (item, pos, arr) {
                        return arr.indexOf(item) === pos;
                    })
                    .sort();
                $("#CircuitName").html("<option value='' selected></option>");

                $.each(circuitName, function (i, value) {
                    $("#CircuitName").append("<option value='" + value + "'>" + value + "</option>");
                });


            } else {
                if (window.toastr) {
                    toastr['error']("No circuit Names returned for program " + program, "Error", TOAST_OPTIONS);
                }
            }

            $('#circuit-loading').hide && $('#circuit-loading').hide();
        },
        error: function (e) {
            console.log(e);
            if (window.toastr) {
                toastr['error']("Failed to retrieve circuit names for program " + program, "Error", TOAST_OPTIONS);
            }
            $('#circuit-loading').hide && $('#circuit-loading').hide();
        }
    });
}

function toggleHighlight(button) {
    var $row = $(button).closest('tr');

    if ($row.hasClass('table-primary')) {
        $row.removeClass('table-primary');
    } else {
        $('tr.table-primary').removeClass('table-primary');
        $row.addClass('table-primary');
    }
}

function migrateHecp(button, hecpId) {
    $.ajax({
        url: '/Hecp/MigrateHecp?hecpId=' + hecpId,
        type: 'get',
        dataType: 'html',
        cache: false,
        success: function (response) {
            if (response == "true") {
                toastr.success("HECP Migrated successfully", "Success", TOAST_OPTIONS_SHORT_TIMEOUT);
                // Disable and gray out the clicked button
                $(button).removeClass('migrate-btn').attr('disabled', 'disabled').css({
                    'cursor': 'not-allowed',
                    'opacity': '0.6'
                });
            }
            else {
                toastr.error("An error occured. Please try again after some time.", "Error");
            }
        },
        error: function (error) {
            console.log(error);
        }
    });
};
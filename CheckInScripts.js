function focusTraveler() {
    $('#personType').attr('value', 'Traveler');

    $('#Visitor-Tab').hide(200);
    $('#Home-Team-Tab').hide(200);
    $('#Traveler-Tab').show(200, function () {
        $('#traveler-name').show(150, function () {
            $('#traveler-name').prop("disabled", false);
            $('#traveler-name').focus();
            $('#traveler-job').show(150).prop("disabled", false);
        });
    });
    $('#home-name').val("").prop("disabled", true);
    $('#home-job').val("").prop("disabled", true);
    $('#visitor-badge').val("").prop('disabled', true);
    $('#visitor-name').val("").prop("disabled", true);
    $('#visitor-job').val("").prop("disabled", true);
    $('#visitor-label').removeClass('active');
    $('#hometeam-label').removeClass('active');
    emptyWorkAreasOnTabSwitch();
}

function focusVisitor() {
    $('#personType').attr('value', 'Visitor');
    $('#Traveler-Tab').hide(200);
    $('#Home-Team-Tab').hide(200);
    $('#Visitor-Tab').show(200, function () {
        if ($('#name-toggle-checkbox').is(':checked')) {
            $('#visitor-name').show(150, function () {
                $('#visitor-badge').prop("disabled", true);
                $('#visitor-name').prop("disabled", false);
                $('#visitor-name').focus();
                $('#visitor-job').show(150).prop("disabled", false);
            });
        }
        else {
            $('#visitor-badge').show(150, function () {
                $('#visitor-name').prop("disabled", true);
                $('#visitor-badge').prop("disabled", false);
                $('#visitor-badge').focus();
                $('#visitor-job').show(150).prop("disabled", false);
            });
        }
    });
    $('#traveler-name').val("").prop("disabled", true);
    $('#traveler-job').val("").prop("disabled", true);
    $('#home-name').val("").prop("disabled", true);
    $('#home-job').val("").prop("disabled", true);
    $('#traveler-label').removeClass('active');
    $('#hometeam-label').removeClass('active');
    emptyWorkAreasOnTabSwitch();
}

function focusHomeTeam() {
    $('#personType').attr('value', 'Home Team');
    $('#Traveler-Tab').hide(200);
    $('#Visitor-Tab').hide(200);
    $('#Home-Team-Tab').show(200, function () {
        $('#home-name').show(150, function () {
            $('#home-name').prop("disabled", false)
            $('#home-name').focus();
            $('#home-job').show(150).prop("disabled", false);
        });
    });
    $('#traveler-name').val("").prop("disabled", true);
    $('#traveler-job').val("").prop("disabled", true);
    $('#visitor-badge').val("").prop('disabled', true);
    $('#visitor-name').val("").prop("disabled", true);
    $('#visitor-job').val("").prop("disabled", true);
    $('#traveler-label').removeClass('active');
    $('#visitor-label').removeClass('active');
    emptyWorkAreasOnTabSwitch();
}

$(function () {
    $('#traveler-name').focus();
});

function autoFocusOnCheckIn() {
    $('#traveler-name').focus();
}

function updateNumPersonnelCheckedIn() {
    $("#number-of-personnel").html("(" + $('#checked_in_personnel_table > tr').length + ")");
}

function focusOnCheckInForm() {
    $(".badgeFieldCheckIn").focus();
}


function OnReturnClick() {
    OnCancelClick();
}

function toggleCompletedTrainingAccordion() {
    if ($('#completed-trainings-details').is(':hidden')) {
        $('#completed-trainings-details').attr('hidden', false);
        $('#completed-trainings-details').show();
        $('#accordion-icon').removeClass('fa-chevron-down').addClass('fa-chevron-up');
    }
    else {
        $('#completed-trainings-details').attr('hidden', true);
        $('#completed-trainings-details').hide();
        $('#accordion-icon').removeClass('fa-chevron-up').addClass('fa-chevron-down');
    }
}

function toggleIncompleteTrainingAccordion() {
    if ($('#incomplete-trainings-details').is(':hidden')) {
        $('#incomplete-trainings-details').attr('hidden', false);
        $('#incomplete-trainings-details').show();
        $('#incomplete-accordion-icon').removeClass('fa-chevron-down').addClass('fa-chevron-up');
    }
    else {
        $('#incomplete-trainings-details').attr('hidden', true);
        $('#incomplete-trainings-details').hide();
        $('#incomplete-accordion-icon').removeClass('fa-chevron-up').addClass('fa-chevron-down');
    }
}

function checkoutPerson(site, id, program, lineNumber) {    
    $(".btn-checkout-person-by-record .fa-sign-out").hide();
    $(".btn-checkout-person-by-record").attr('disabled', 'disabled');
    $("#checkout-person-by-record-loader").css("display","inline-block");

    $.ajax({
        url: encodeURI('/CheckOut/' + site + '/' + id + '?program=' + program + '&lineNumber=' + lineNumber),
        type: 'GET',
        dataType: 'html',
        async: true,
        cache: false,
        success: function (data) {
            toastr["success"](data, "Check-Out Successful", TOAST_OPTIONS_SHORT_TIMEOUT);
            hideCheckOutModal();
            showCheckedInPersonnelHelper(program, lineNumber);            
        },
        error: function (errorData) {
            toastr["error"]("Error. " + errorData.responseText, "Check-Out Failed", TOAST_OPTIONS);
        },
        complete: function () {
            $(".btn-checkout-person-by-record").removeAttr('disabled');
            $("#checkout-person-by-record-loader").hide();
            $(".btn-checkout-person-by-record .fa-sign-out").show();
        }
    });
}

function checkoutPersonByBEMSorBadge(site, program, lineNumber) {
    var bemsOrBadgeNumber = $('#bemsOrBadgeForcheckoutByBemsOrBadge').val();
  
    $(".CheckOutRecord").attr('disabled', 'disabled').addClass('disable');

    $.ajax({
        url: encodeURI('/CheckOutByBemsOrBadge?site=' + site + '&program=' + program + '&lineNumber=' + lineNumber + '&bemsOrBadgeNumber=' + bemsOrBadgeNumber),
        type: 'GET',
        dataType: 'html',
        async: true,
        cache: false,
        success: function (data) {
            toastr["success"](data, "Check-Out Successful", TOAST_OPTIONS);
            showCheckedInPersonnelHelper(program, lineNumber);

            hideCheckOutModal();
        },
        error: function (errorData) {
            var errorMessage = "Unable to check out user.";
            $('#CheckOutBtn').attr('disabled', 'disabled');

            if (errorData != null && errorData.responseText != null && errorData.responseText != "") {
                errorMessage = errorData.responseText;
            }

            console.log(errorData);
            toastr["error"]("Error. " + errorMessage, "Check-Out Failed", TOAST_OPTIONS);
            $('#bemsOrBadgeForcheckoutByBemsOrBadge').val('');
            $('#bemsOrBadgeForcheckoutByBemsOrBadge').trigger("focus");
        },
        complete: function () {
            $(".CheckOutRecord").removeAttr('disabled').removeClass('disable');
        }
    });
}

function hideCheckOutModal() {
    $('.modal-backdrop.fade.show').hide();
    $('#checkOutPersonModal').hide("slow");
}

function PopulateCheckOutModal(site, program, lineNumber, id, name) {
    $('#checkOutMessage').html('Are you sure you want to check out ' + decodeURI(name) + ' from Line ' + lineNumber);
    $('#checkOutAction').html('<input type="button" class="btn btn-secondary-gray btn-rounded" value="Cancel" data-dismiss="modal"/>');
    $('#checkOutAction').append('<button onclick="checkoutPerson(\'' + site + '\',' + id + ',\'' + program + '\',\'' + lineNumber + '\')" class="btn btn-primary btn-rounded btn-checkout-person-by-record"><i class="fa fa-sign-out prefix right-space-xsm"></i><img width="25" height="25" style="display:none;margin-right: .2rem;" class="img-responsive" id="checkout-person-by-record-loader" alt="Loading..." src="/images/loading.gif">Check Out</button> ');
};

function validateCheckInButton() {
    let isTravelFormValid =
        $('#travelerOption').hasClass('active') &&
        $.trim($('#traveler-name').val()) !== '' &&
        $.trim($('#traveler-job').val()) !== '' &&
        $('.work-area:checked').length !== 0;

    let isVisitorFormValid =
        $('#visitorOption').hasClass('active') &&
        ($.trim($('#visitor-badge').val()) !== '' || $.trim($('#visitor-name').val()) !== '') &&
        $('.work-area:checked').length !== 0;

    let isHomeTeamFormValid =
        $('#homeTeamOption').hasClass('active') &&
        $.trim($('#home-name').val()) !== '' &&
        $('.work-area:checked').length !== 0;

    let isBCBadgeEnabledAndHasValue = true;
    if (typeof($('#bc-badge').val()) !== 'undefined' && $('#bc-badge').val().trim() === '') {
        isBCBadgeEnabledAndHasValue = false;
    }

    var isCCPinEnabledAndHasValue = false;
    var onlyNumberRegex = new RegExp("^[0-9]{4}$");
    if (typeof ($('#cc-pin').val()) !== 'undefined' && $('#cc-pin').val().trim() !== '' && onlyNumberRegex.test($('#cc-pin').val())) {
        isCCPinEnabledAndHasValue = true;
    }
        
    if ((isTravelFormValid || isVisitorFormValid || isHomeTeamFormValid) && (isBCBadgeEnabledAndHasValue || isCCPinEnabledAndHasValue)) {
        $('#check-in-btn').removeAttr('disabled');

    } else {
        $('#check-in-btn').attr('disabled', 'disabled');
    }
}

function createIdentityPopup(bemsID) {
    $("#profileDetails").html("");
    $("#acknowledgedBEMS").text(bemsID);
    if (bemsID !== "") {
        let script = getInsiteWidgetScript(bemsID, 'profileDetails');
        $("#profileDetails").append(script);
    }
}

function validateFilteringButtons() {
    if (isFilteringFormValid()) {
        enableFilterButton("#applyReportFilter");
        enableFilterButton("#resetReportFilter");
    }
    else {
        disableFilterButton("#applyReportFilter");
        disableFilterButton("#resetReportFilter");
    }
}

function validateExportToExcelButton() {
    if (isFilteringFormValid()) {
        enableFilterButton("#btnExportToExcel");
    }
    else {
        disableFilterButton("#btnExportToExcel");
    }
}

function isFilteringFormValid() {
    return ($.trim($("#FromDate").val()) !== '' && $.trim($("#ToDate").val()) !== '' &&
        $.trim($('#checkIn-Program-Select option:selected').text()) !== '' &&
        $.trim($('#line-select-report option:selected').text()) !== '');
}

function enableFilterButton(id) {
    $(id).removeAttr('disabled');
}

function disableFilterButton(id) {
    $(id).attr('disabled', 'disabled');
}

function showCheckedInPersonnel(model, lineNumber, isCurrentUserTheBC) {
    var url = '/CheckIn/GetCheckedInPersonnelData/' + model + '/' + lineNumber + '/' + isCurrentUserTheBC;

    $.ajax({
        url: encodeURI(url),
        type: 'GET',
        dataType: 'html',
        success: function (data) {
            $('#personnel-checkin-list').html(safeResponseFilter(data));
            var cnt = $('#checked_in_personnel_table > tr').length;
            if (cnt !== 0) {
                updateNumPersonnelCheckedIn();
                convertAllDatesToLocalTime();
            }
        }
    });
};

function toggleCCDetailsForCheckIn(data) {
    $(".ccCreds .badgeFieldCheckIn").val('');
    $(".ccCreds").hide();
    $("#" + $(data).val()).removeAttr("hidden");
    $("#" + $(data).val()).show();
    $("#" + $(data).val()).find(".badgeFieldCheckIn").focus() 
    validateCheckInButton();
}

/* Check In Report */

function getCheckInReportModal(action, controller, proceedText, title) {
    $.ajax({
        url: '/CheckIn/GetCheckInReportModal/' + action + '/' + controller + '/' + proceedText + '/' + title,
        type: 'GET',
        success: function (response) {
            $("#manageLinesModalDiv").html(safeResponseFilter(response));
            $('#manageCheckInReportModal').modal('show');
        },
        error: function (e) {
            console.log(e);
            toastr.error("Unable to show the Manage Check-in Report modal.", "Error");
        }
    });
};

function validateCheckInReportModal() {
    var isModalValid = $.trim($('#manageCheckInReport-site-select option:selected').text()) !== '';

    if (isModalValid) {
        $('#manageCheckInReportSubmit').removeAttr('disabled');
    } else {
        $('#manageCheckInReportSubmit').attr('disabled', 'disabled');
    }
}
function ShowConfirmIdentity() {
    let bemsID = $("#home-name").val() || $("#traveler-name").val() || $("#visitor-badge").val() || $("#visitor-name").val();
    if (!$('#name-toggle-checkbox').is(':checked')) {
        $("#IdentityAcknowledgement").show();
        $("#IdentityAcknowledgement").addClass("show");
        disableFilterButton("#check-in-btn");
        if (isBadgeNumber(bemsID)) {
            hideAcknowledgeTextAndProfileDetails('#acknowledgeBemsText', '#profileDetails', '.identity-acknowledgement', '16rem');
        }
        else {
            showAcknowledgeTextAndProfileDetails('#acknowledgeBemsText', '#profileDetails', '.identity-acknowledgement', '32rem')
            createIdentityPopup(bemsID);
        }
    }
    else {
        SubmitCheckInForm();
    }
}

function closeConfirmIdentity() {
    enableFilterButton("#check-in-btn");
    $("#IdentityAcknowledgement").hide();
    $("#IdentityAcknowledgement").removeClass("show");
}

function SubmitCheckInForm() {
    closeConfirmIdentity();
    $('#CheckInForm').submit();
}

function FilterWorkArea() {
    let searchstring = $('#work-area-search').val().trim();
    let allWorkAreas = $(".work-area-label.w-100");
    $(allWorkAreas).each(function () {
        let checkbox = $(this).find('input');

        clearSearchHighlight(this);

        if ($(this).text().toUpperCase().indexOf(searchstring.toUpperCase()) != -1) {
            //highlight search text
            let highlightedText = $(this).text().replace(new RegExp(`(${searchstring})`, 'gi'), `<span class="highlight-work-area">$1</span>`);
            $(this).html(checkbox.prop('outerHTML') + highlightedText);
            this.childNodes[0].checked = checkedWA.includes(checkbox[0].name);
          
            $(this).show();
        }
        else {
            $(this).hide();
        }
    });
    $(".work-area-container").scrollTop(0);
  
    $('#clear-work-area').toggle(searchstring.length > 0); //show x icon when search string is entered
    //if no options match search text
    const visibleOptions = $('.work-areas-select label:visible');
    let noResultsDiv = $('.no-wa-results');
    if (visibleOptions.length === 0) { //check for this div should appear only once
        if (noResultsDiv.length === 0) {
            $('.work-area-container').append('<div class="no-wa-results" style="">No results found.</div>');
        }
    }
    else {
        noResultsDiv.remove();
    }
}

function validateCheckOutButton() {

    if ($('#bemsOrBadgeForcheckoutByBemsOrBadge').val().trim() !== '') {
        $('#CheckOutBtn').removeAttr('disabled');
    }
    else {
        $('#CheckOutBtn').attr('disabled', 'disabled');
    }
}

function checkboxSelectionLimit(checkbox) {
    let checkboxes = $('.work-area:checked');
    let searchBar = $('#work-area-search');
    //show toastr warning and close the drop-down box
    if (checkboxes.length === 3) {
        toastr.warning("Maximum 3 Work Areas can be selected.");
        $('#scope-work-area-div').removeClass('visible');
    }
    //disable unchecked checkboxes when limit is reached
    $('.work-area').each(function () {
        let label = $(this).closest('label');
        if (!$(this).is(':checked')) {
            $(this).prop('disabled', checkboxes.length === 3);
            //adding tooltip to the disabled options
            if (checkboxes.length === 3) {
                label.attr('title', 'Maximum selection reached'); //tooltip text
            }
            else {
                label.removeAttr('title');
            }
        }
        else {
            $(this).prop('disabled', false); //enable selected checkboxes
        }
    })
    if ($("#work-area-search").val().trim() !== '') { 
    searchBar.focus();
    }
}

function sendWorkAreaIdList() {
    let WorkAreaIdList = [];
    let checkBox = $('.work-area:checked');
    for (let i = 0; i < checkBox.length; i++) {
        WorkAreaIdList.push([$('.work-area:checked')[i].value]);
    }
    return WorkAreaIdList;
}

let checkedWA = [];
function showSelectedWorkAreas() {
    let selectedValues = [];
    let checkboxes = $('.work-area:checked');
    $.each(checkboxes, function (i, v) {
        if (!selectedValues.includes(checkboxes[i].name)) {
            selectedValues.push(checkboxes[i].name);
        }
    });
    checkedWA = selectedValues;
    $('#work-area-span').text(selectedValues.join(', '));
}
/* Check In Report */

$(document).on("keypress", "#work-area-search", function (e) {
    let visibleWA = $(".work-area-label.w-100:visible");
    if ((e.keyCode == 13 || e.which == '13') && visibleWA.length === 1) {
        $(visibleWA).click();
    }
});

function showAllWorkAreas() {
    //show all work areas when the drop-down list is opened
    let checkboxes = $('.work-area');
    $.each(checkboxes, function (i, v) {
        if (checkedWA.includes(checkboxes[i].name)) {
            $(this).prop("checked", true);
        }
        else {
            $(this).prop("checked", false);
        }
    });
    $('.work-areas-select label').show();
    $('.no-wa-results').remove(); //remove no results message if it exists
}

function clearWorkAreaSearch() {
    //onclick of x should clear the search field
    $('#work-area-search').val('');
    showAllWorkAreas();
    $(this).hide();
    $('#work-area-search').focus();
    clearSearchHighlight('.work-areas-select');
}

function clearSearchHighlight(element) {
    $(element).find('.highlight-work-area').each(function () {
        $(this).replaceWith($(this).text());
    });
}

function resetWorkAreas() {
    checkedWA = [];
}

function emptyWorkAreasOnTabSwitch() {
    resetWorkAreas();
    $('#work-area-span').text('');
    $('.work-area').each(function () {
        $(this).prop('disabled', false); //enable selected checkboxes
    });

}


Controller

public async Task<HTTPResponseWrapper<List<IMyLearningDataResponse>>> GetMyLearningData(int bemsId, string shieldActionName)
        {
            try
            {
                List<IMyLearningDataResponse> data = await _myLearningDataService.GetMyLearningDataResponseList(bemsId, shieldActionName);
                return new HTTPResponseWrapper<List<IMyLearningDataResponse>> { Data = data, Status = "Success" };
            }
            catch (Exception ex)
            {
                return new HTTPResponseWrapper<List<IMyLearningDataResponse>> { Data = Enumerable.Empty<IMyLearningDataResponse>().ToList(), Status = "Failed", Message = ex.Message };
            }
        }



Service:

using Newtonsoft.Json;
using Shield.Common;
using Shield.Common.Constants.WebResponse;
using Shield.Common.Models.MyLearning;
using Shield.Common.Models.MyLearning.Interfaces;
using Shield.Common.Models.WebResponse;
using Shield.Services.External.Data.Interfaces;
using Shield.Services.External.Models.DataModels;
using Shield.Services.External.Services.Interfaces;

namespace Shield.Services.External.Services.Implementations
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MyLearningDataService"/> class.
    /// </summary>
    /// <param name="myLearningDAO">
    /// The my learning dao.
    /// </param>
    /// <param name="httpClientService">
    /// The http client service.
    /// </param>
    public class MyLearningDataService(IMyLearningDAO myLearningDAO,ITrainingDetailsDAO trainingDetailsDAO, HttpClientService httpClientService) : IMyLearningDataService
    {
        /// <summary>
        /// The _my learning dao.
        /// </summary>
        private readonly IMyLearningDAO _myLearningDAO = myLearningDAO;
        private readonly ITrainingDetailsDAO _trainingDetailsDAO = trainingDetailsDAO;

        /// <summary>
        /// The _client service.
        /// </summary>
        private HttpClientService _clientService = httpClientService;

        /// <summary>
        /// The get my learning data response list.
        /// </summary>
        /// <param name="bemsId">
        /// The bems id.
        /// </param>
        /// <param name="trainingIdList">
        /// The training id list.
        /// </param>
        /// <returns>
        /// The <see cref="Task{MyLearningDataResponse}"/>.
        /// </returns>
        public async Task<List<IMyLearningDataResponse>> GetMyLearningDataResponseList(int bemsId, string shieldActionName)
        {
            List<IMyLearningDataResponse> myLearningDataResponseList = [];
            List<int> trainingListWithInsiteException = [];
            List<TrainingMasterData> trainingList = _trainingDetailsDAO.GetTrainingIdsFromModuleActionTypeAsync(shieldActionName);
            foreach (var training in trainingList)
            {
                bool isTrainingValid = false;
                string certCode = training.TrainingId.ToString();
                string trainingName = training.Name;
                try
                {
                    //isTrainingValid = await GetIsTrainingValidFromMyLearningAPIAsync(bemsId, certCode);
                    trainingListWithInsiteException.Add(training.Id); // should remove this and uncomment the above line.
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"My Learning API call threw an error for BemsID [{bemsId}] and Training [{certCode}] with Exception {ex}");
                    trainingListWithInsiteException.Add(training.Id);
                }
                finally
                {
                    myLearningDataResponseList.Add(new MyLearningDataResponse
                    {
                        CertCode = certCode,
                        Name = trainingName,
                        IsTrainingValid = isTrainingValid
                    });
                }
            }

            if (trainingListWithInsiteException.Count > 0)
            {
                try
                {
                    Console.Out.WriteLine($"My Learning Shield DB Called for BemsID [{bemsId}] & Training [{string.Join(',', trainingListWithInsiteException)}]");
                    List<MyLearningDataModel> validTrainingsFromDB = _myLearningDAO.GetMyLearningDataModelList(bemsId, trainingListWithInsiteException);

                    foreach (MyLearningDataModel myLearningData in validTrainingsFromDB)
                    {
                        var trainingCode = _trainingDetailsDAO.GetCertCodeFromTrainingMasterId(myLearningData.TrainingMasterDataId);

                        myLearningDataResponseList.FirstOrDefault(x => x.CertCode == trainingCode).IsTrainingValid = true;
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"DB call threw an error for BemsID [{bemsId}] and Training [{string.Join(',', trainingListWithInsiteException)}] with Exception {ex}");
                }
            }

            return myLearningDataResponseList;
        }

        /// <summary>
        /// The get is training valid from my learning API.
        /// </summary>
        /// <param name="bemsId">
        /// The bems id.
        /// </param>
        /// <param name="training">
        /// The training.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        private async Task<bool> GetIsTrainingValidFromMyLearningAPIAsync(int bemsId, string training)
        {
            string myLearningCertificationURL = Environment.GetEnvironmentVariable("MY_LEARNING_API").Replace("[BEMS_ID]", bemsId.ToString());
            myLearningCertificationURL = $"{myLearningCertificationURL}/{training}";
            HttpResponseMessage response = await _clientService.GetClient().GetAsync(new Uri(myLearningCertificationURL));
            MyLearningResponse myLearningResponse = JsonConvert.DeserializeObject<MyLearningResponse>(await response.Content.ReadAsStringAsync());
            if (myLearningResponse == null)
            {
                Console.Error.WriteLine("My Learning Response is null.");
            }
            else
            {
                Console.Out.WriteLine($"My Learning Response is {myLearningResponse.LM_CERT_IND_RESP_Z?.LM_VALID_STTS}");
            }

            return string.Equals(myLearningResponse.LM_CERT_IND_RESP_Z.LM_VALID_STTS, MyLearningResponseConstants.Valid, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}


DAO:

using Shield.Services.External.Models.DataModels;
using Shield.Services.External.Models;
using Shield.Services.External.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Shield.Services.External.Data.Implementations
{
    public class TrainingDetailsDAO(ExternalDataContext externalDataContext) : ITrainingDetailsDAO
    {
        private ExternalDataContext _externalDataContext = externalDataContext;
        public List<TrainingMasterData> GetTrainingIdsFromModuleActionTypeAsync(string moduleActionType)
        {
            if (string.IsNullOrWhiteSpace(moduleActionType))
                return new List<TrainingMasterData>();

            var module = _externalDataContext.ShieldTasksMaster
                         .AsNoTracking()
                         .FirstOrDefault(m => m.Name == moduleActionType);

            if (module == null)
                return new List<TrainingMasterData>();

            List<int> trainingIds = _externalDataContext.TaskAndTrainingsMapping
                             .AsNoTracking()
                             .Where(map => map.ModuleId == module.Id && map.IsActive)
                             .Select(map => map.TrainingId)
                             .Distinct()
                             .ToList();

            if (!trainingIds.Any())
                return new List<TrainingMasterData>();

            List<TrainingMasterData> trainings = _externalDataContext.TrainingMasterData
                            .AsNoTracking()
                            .Where(t => trainingIds.Contains(t.Id))
                            .ToList();

            return trainings;
        }

        public string GetCertCodeFromTrainingMasterId(int trainingMasterId)
        {

            var certCode = _externalDataContext.TrainingMasterData
                             .AsNoTracking()
                             .Where(a => a.Id == trainingMasterId)
                             .Select(a => a.TrainingId)
                             .FirstOrDefault();
            if (certCode == null)    
                return string.Empty;

            return certCode;
        }
    }
}


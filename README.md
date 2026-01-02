//Getting training data when the CC didn't override or proceeded after verifying training data
if (!vm.overrideTraining && !vm.trainingConfirmation && doTrainingChecks)
{
    string trainingActions = (isVisitor && visitorUsingBemsId) ? Shield.Common.Constants.ShieldTasksName.CHECKIN_NON_BOEING : Shield.Common.Constants.ShieldTasksName.CHECKIN_BOEING;
    TrainingInfo trainingInfo = await _externalService.GetMyLearningDataAsync(rec.BemsId, rec.BadgeNumber, trainingActions);

    //show error message when there is no BemsId for the given BadgeData
    if (trainingInfo?.BemsId == 0 && rec.Name == null)
    {
        _toastNotification.AddErrorToastMessage("Unable to Find Badge Data For This Badge.Please Try Using BEMSID.");
        return PartialView("Partials/CheckInPartial", vm);
    }

    bool hasIncompleteTraining = trainingInfo.MyLearningDataResponse.Any(x => !x.IsTrainingValid);
    bool allTrainingsIncomplete = trainingInfo.MyLearningDataResponse.All(x => !x.IsTrainingValid);
    bool any77517Valid = trainingInfo.MyLearningDataResponse.Any(x =>
        x.CertCode != null &&
        x.CertCode.Trim().StartsWith("77517", StringComparison.Ordinal) &&
        x.IsTrainingValid);

    if (hasIncompleteTraining)
    {
        if (allTrainingsIncomplete)
        {
            vm.overrideTraining = true;
            Console.WriteLine("Override training popup is shown");
        }
        else if (any77517Valid)
        {
            // At least one 77517 valid — direct check-in.
            Console.WriteLine("77517 valid — direct check-in");
        }
        else
        {
            // Some trainings incomplete, none of the 77517 valid => show proceed popup.
            vm.trainingConfirmation = true;
            Console.WriteLine("Training confirmation with proceed button popup is shown");
        }

        if (vm.overrideTraining || vm.trainingConfirmation)
        {
            User user = trainingInfo.BemsId != 0 ? await _userService.GetUserByBemsidAsync(trainingInfo.BemsId) : new Models.CommonModels.User();
            vm.UserTrainingData = trainingInfo.MyLearningDataResponse
                .OrderBy(t => System.Text.RegularExpressions.Regex.Match(t.CertCode, @"^\d+").Value)
                .ThenBy(t => t.CertCode)
                .ToList();
            vm.recordDisplayName = trainingInfo.BemsId != 0 ? user.DisplayName : rec.Name;
            return PartialView("Partials/TrainingStatusPartial", vm);
        }
    }
} // <--- Training validation block ends here

// Check-in happens here for ALL cases:
// 1. Direct check-in (no incomplete training or has valid 77517)
// 2. After clicking Override/Proceed (vm.overrideTraining or vm.trainingConfirmation = true)
response = await _checkInService.PostCheckinAsync(rec);

if (response == null)
{
    ViewBag.Status = "Failed";
    ViewBag.Message = "Unable to reach Check In Service, please try again.";
    return PartialView("Partials/CheckInPartial", vm);
}
else if (response.Status.Equals(Shield.Common.Constants.ShieldHttpWrapper.Status.SUCCESS))
{
    ViewBag.Status = "Success";
    ViewBag.Message = response.Message;
    CheckInPartialViewModel newVM = _checkInTranslator.GetNewCheckInPartialVMAfterSuccess(vm);
    return PartialView("Partials/CheckInPartial", newVM);
}
else if (response.Status.Equals(Shield.Common.Constants.ShieldHttpWrapper.Status.NOT_MODIFIED))
{
    vm.checkOutNeededFlag = true;
    vm.bemsId = response.Data.BemsId;
    ViewData["ResponseMessage"] = response.Message;
    return PartialView("Partials/CheckOutCheckInPartial", vm);
}
else
{
    _toastNotification.AddErrorToastMessage(response.Message);
    return PartialView("Partials/CheckInPartial", vm);
}

This is my updated controller function ok. And changes is related to trainings only.

First of all, there is two buttons in TrainingStatusPartial ok. 
i) Proceed Button - It occurs if any trainings is/are completed among TR06005, 85914, 77517AE, 77517PAE and 77517GC. Or All Trainings Valid then also proceed button appears ok.
ii) Override button - It occurs when all the above mentioned trainings are incompleted.

SCENARIOS FOR TEST CASES:

1. For BoeingAE: 

*DIRECT CHECKIN:
    i) All Trainings Completed/valid (TR06005, 85914, 77517AE, 77517PAE and 77517GC) - Direct CheckIn.
    ii) TR6005 valid/completed, Any of (77517AE, 77517PAE and 77517GC) valid/completed, 85914 invalid/notcompleted - Direct CheckIn
    iii) TR06005 Invalid, Any of (77517AE, 77517PAE and 77517GC) valid/completed, 85914 valid - Direct CheckIn
    iv) TR06005 Invalid, Any of (77517AE, 77517PAE and 77517GC) valid/completed, 85914 Invalid - Direct CheckIn
     
So for the above cases, there should be one method with above combinations of mock training data ok. So before making the new function, check if we have any function/method in test case that do directly checkin functionality after training checks  or see accordingly whether the proceed button should be visible with trainingstatuspartial or we should not show trainingstatus partial according to the controller logic ok.

*PROCEED BUTTON WITH TRAININGSTATUSPARTIAL:
     i) TR06005 valid, All (77517AE, 77517PAE and 77517GC) Invalid/Incompleted, 85914 valid - TrainingStatusPartial with proceed button should be shown.
    ii) TR06005 Invalid, All (77517AE, 77517PAE and 77517GC) Invalid/Incompleted, 85914 valid - TrainingStatusPartial with proceed button should be shown.
    iii) TR06005 valid, All (77517AE, 77517PAE and 77517GC) Invalid/Incompleted, 85914 Invalid - TrainingStatusPartial with proceed button should be shown.


Here also, check if we have any existing function which shows trainingStatusPartial with proceed button or not and then inside one method create all the three combinations of above trainings data as mock data.

*OVERRIDE BUTTON WITH TRAINING STATUS PARTIAL:
     i) All Trainings Incompleted/Invalid (TR06005, 85914, 77517AE, 77517PAE and 77517GC) - TrainingStatusPartial with override button should be shown.

Here also, check if we have any existing function which shows trainingStatusPartial with override button or not and then inside one method create the above combination of above trainings data as mock data.

2. For NonBoeingAE: (That is Visitor)

The logic is written only for visitorUsingName not for visitorUsingBemsID in controller logic ok. We will add the logic later by doing the training check and there the parameter will be CHECKIN_NON_BOEING in below code:
TrainingInfo trainingInfo = await _externalService.GetMyLearningDataAsync(rec.BemsId, rec.BadgeNumber, "CHECKIN_BOEING");

SCENARIOS FOR TEST CASES:
* DIRECT CHECKIN:
    i) Using Name - Direct CheckIn.
 * Using BemsID - Show override pop basically TrainingStatusPartial with Override Button if training (77517X36106) is Invalid.
                                    Else Direct CheckIn , if training is valid.

So see carefully the existing test cases if we have any for visitor (Non BoeingAE) and then create according to the above scenarios.

For other function method which uses old 2 training codes and where there are true like for example : Should_SendsTranslatedCheckInRecordToCheckInService_when_TranslatationIsValid() - In this we use when both trainings are valid, so now it should be combinations accoridng to the above scenarios ok. I guess here all trainings valid combination will be there only. You check properly and analyze and then give ok.
So for existing test cases also, where we used old two training codes, make it modified according to the above scenarios if needed or relatable ok.

Understand everything and deeply analyze and think ok. After this i will give my existing CheckInControllerTests.cs and then after you start giving me the modified or new test cases.

Don't do anything extra. I want all the test cases should be correct and optimal. ALL THE BEST MY CHAMPION.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NToastNotify;
using Shield.Common;
using Shield.Common.Models.Common;
using Shield.Ui.App.Controllers;
using Shield.Ui.App.Models.CheckInModels;
using Shield.Ui.App.Models.CommonModels;
using Shield.Ui.App.Services;
using Shield.Ui.App.Translators;
using Shield.Ui.App.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CheckOutRequest = Shield.Ui.App.Models.CheckInModels.CheckOutRequest;

namespace Shield.Ui.App.Tests
{
    using Shield.Common.Constants;
    using Shield.Common.Models.MyLearning;
    using Shield.Common.Models.MyLearning.Interfaces;
    using CheckInRecord = Shield.Ui.App.Models.CommonModels.CheckInRecord;
    using Status = Shield.Common.Constants.ShieldHttpWrapper.Status;

    [TestClass]
    public class CheckInControllerTests
    {
        private Mock<HttpClientService> _mockHttpClientService;
        private Mock<CheckInService> _mockCheckInService;
        private Mock<UserService> _mockUserService;
        private Mock<LotoService> _mockLotoService;
        private Mock<AirplaneDataService> _mockAirplaneService;
        private Mock<IToastNotification> _mockToastService;
        private Mock<CheckInTranslator> _mockCheckInTranslator;
        private Mock<SessionService> _mockSessionService;
        private Mock<ExternalService> _mockExternalService;

        private DateTime _now = DateTime.Now;

        private Shield.Ui.App.Models.CommonModels.Aircraft _aircraftWithAssignedBC;
        private User _bcUser;

        private CheckInController _controller;

        [TestInitialize]
        public void SetUp()
        {
            _mockHttpClientService = new Mock<HttpClientService>();
            _mockExternalService = new Mock<ExternalService>(_mockHttpClientService.Object);
            _mockCheckInService = new Mock<CheckInService>(_mockHttpClientService.Object, _mockExternalService.Object);
            _mockUserService = new Mock<UserService>(_mockHttpClientService.Object);
            _mockLotoService = new Mock<LotoService>(_mockHttpClientService.Object);
            _mockAirplaneService = new Mock<AirplaneDataService>(_mockHttpClientService.Object, _mockLotoService.Object);
            _mockToastService = new Mock<IToastNotification>();
            _mockCheckInTranslator = new Mock<CheckInTranslator>();
            _mockSessionService = new Mock<SessionService>();
            _mockSessionService.Setup(s => s.GetUserFromSession(It.IsAny<HttpContext>())).Returns(new User());

            _bcUser = new User
            {
                BemsId = 2519949,
                Role = new Shield.Common.Models.Users.Role
                {
                    Name = Common.Constants.BARGE_COORDINATOR
                },
                UserPin = new UserPin
                {
                    BemsId = 2519949,
                    Pin = "pin1"
                }
            };

            _aircraftWithAssignedBC = new Shield.Ui.App.Models.CommonModels.Aircraft
            {
                AssignedBargeCoordinatorBems = _bcUser.BemsId,
                Site = "BSC",
                Model = "787",
                LineNumber = "500"
            };

            _controller = new CheckInController(_mockCheckInService.Object, _mockUserService.Object, _mockAirplaneService.Object, _mockToastService.Object, _mockCheckInTranslator.Object, _mockSessionService.Object, _mockExternalService.Object);

            // Set environment variables for LOTO Pilot purposes
            Environment.SetEnvironmentVariable("PROGRAMSITES_PILOT_LOTO_AND_DISCRETE", "[{\"site\": \"Renton\", \"program\": \"P-8\"}, {\"site\": \"BSC\", \"program\": \"787\"}]");
        }

        #region CheckIn User

        [TestMethod]
        public async Task Should_TranslateViewModel_when_GivenAValidViewModel()
        {
            _mockSessionService.Setup(s => s.GetUserFromSession(It.IsAny<HttpContext>())).Returns(_bcUser);

            var vm = new CheckInPartialViewModel()
            {
                CurrentUser = new User { BemsId = 123 }
            };

            var response = await _controller.CheckInUser(vm);
            _mockCheckInTranslator.Verify(t => t.GetCheckInRecordFromViewModel(vm, It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<bool>()));
        }

        [TestMethod]
        public async Task Should_TranslateViewModel_AND_SetStatusToCheckIn()
        {
            _mockSessionService.Setup(s => s.GetUserFromSession(It.IsAny<HttpContext>())).Returns(_bcUser);

            CheckInPartialViewModel vm = new CheckInPartialViewModel()
            {
                CurrentUser = new User { BemsId = 123 }
            };

            var response = await _controller.CheckInUser(vm);

            _mockCheckInTranslator.Verify(t => t.GetCheckInRecordFromViewModel(It.IsAny<CheckInPartialViewModel>(), It.IsAny<DateTime>(), "Check In", It.IsAny<bool>()));
        }

        [TestMethod]
        public async Task Should_TranslateViewModel_AND_SetSystemAction()
        {
            _mockSessionService.Setup(s => s.GetUserFromSession(It.IsAny<HttpContext>())).Returns(_bcUser);

            CheckInPartialViewModel vm = new CheckInPartialViewModel()
            {
                CurrentUser = new User { BemsId = 123 }
            };

            var response = await _controller.CheckInUser(vm);

            _mockCheckInTranslator.Verify(t => t.GetCheckInRecordFromViewModel(It.IsAny<CheckInPartialViewModel>(), It.IsAny<DateTime>(), It.IsAny<string>(), false));
        }

        [TestMethod]
        public async Task Should_SendsTranslatedCheckInRecordToCheckInService_when_TranslatationIsValid()
        {
            var returnedRecord = new CheckInRecord() { BemsId = 2519949, BadgeNumber = null };
            TrainingInfo trainingInfo = new()
            {
                BemsId = 2519949,
                MyLearningDataResponse = new List<IMyLearningDataResponse>()
                {
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_AWARENESS_TRAINING_FOR_AFFECTED_PERSONS,
                        IsTrainingValid = true
                    },
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_CONTROL,
                        IsTrainingValid = true
                    }
                }
            };
            _mockCheckInTranslator.Setup(t => t.GetCheckInRecordFromViewModel(It.IsAny<CheckInPartialViewModel>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(returnedRecord);
            _mockSessionService.Setup(s => s.GetUserFromSession(It.IsAny<HttpContext>())).Returns(_bcUser);
            _mockExternalService.Setup(s => s.GetMyLearningDataAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(trainingInfo);

            CheckInPartialViewModel vm = new CheckInPartialViewModel()
            {
                CurrentUser = new User { BemsId = 123 },
            };

            var response = await _controller.CheckInUser(vm);

            _mockCheckInService.Verify(s => s.PostCheckinAsync(returnedRecord));
        }

        [TestMethod]
        public async Task Should_SetWorkAreasFromServicesToView_when_CheckIn()
        {
            var workAreas = new List<WorkArea>();
            _mockAirplaneService.Setup(s => s.GetActiveWorkAreasAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult<IList<WorkArea>>(workAreas));
            _mockSessionService.Setup(s => s.GetUserFromSession(It.IsAny<HttpContext>())).Returns(_bcUser);

            var vm = new CheckInPartialViewModel();

            var response = await _controller.CheckInUser(vm);

            _mockAirplaneService.Verify(s => s.GetActiveWorkAreasAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            Assert.AreEqual(workAreas, vm.WorkAreas);
        }

        [TestMethod]
        public async Task Should_DisplayErrorMessageInToast_when_CheckInServiceReturnsNullResponse()
        {
            CheckInPartialViewModel vm = new CheckInPartialViewModel
            {
                bemsId = 2519949,
                CurrentUser = new User { BemsId = 123 },
            };

            TrainingInfo trainingInfo = new()
            {
                BemsId = 2519949,
                MyLearningDataResponse = new List<IMyLearningDataResponse>()
                {
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_AWARENESS_TRAINING_FOR_AFFECTED_PERSONS,
                        IsTrainingValid = true
                    },
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_CONTROL,
                        IsTrainingValid = true
                    }
                }
            };

            _mockCheckInService.Setup(x => x.PostCheckinAsync(It.IsAny<Shield.Ui.App.Models.CommonModels.CheckInRecord>())).Returns(Task.FromResult<HTTPResponseWrapper<Shield.Ui.App.Models.CommonModels.CheckInRecord>>(null));
            _mockSessionService.Setup(m => m.GetUserFromSession(It.IsAny<HttpContext>())).Returns(_bcUser);
            _mockExternalService.Setup(s => s.GetMyLearningDataAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(trainingInfo);
            _mockCheckInTranslator.Setup(s => s.GetCheckInRecordFromViewModel(It.IsAny<CheckInPartialViewModel>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(new CheckInRecord() { BemsId = 2519949, BadgeNumber = null });

            var result = await _controller.CheckInUser(vm);
            var viewResult = (PartialViewResult)result;

            Assert.IsTrue(viewResult.ViewData["Status"].Equals("Failed"));
            Assert.IsTrue(viewResult.ViewData["Message"].Equals("Unable to reach Check In Service, please try again."));
        }

        [TestMethod]
        public async Task Should_SendTranslatedViewModel_when_SuccessToCheckIn()
        {
            var successResponse = new HTTPResponseWrapper<Shield.Ui.App.Models.CommonModels.CheckInRecord>
            {
                Status = Shield.Common.Constants.ShieldHttpWrapper.Status.SUCCESS,
                Message = "WE DID IT!"
            };

            CheckInPartialViewModel vm = new CheckInPartialViewModel()
            {
                CurrentUser = new User { BemsId = 234 }
            };

            _mockCheckInTranslator.Setup(t => t.GetNewCheckInPartialVMAfterSuccess(It.IsAny<CheckInPartialViewModel>())).Returns(vm);
            _mockCheckInService.Setup(x => x.PostCheckinAsync(It.IsAny<Shield.Ui.App.Models.CommonModels.CheckInRecord>())).Returns(Task.FromResult<HTTPResponseWrapper<Shield.Ui.App.Models.CommonModels.CheckInRecord>>(successResponse));
            _mockSessionService.Setup(s => s.GetUserFromSession(It.IsAny<HttpContext>())).Returns(_bcUser);

            var result = await _controller.CheckInUser(vm) as PartialViewResult;
            var model = (CheckInPartialViewModel)result.Model;

            Assert.AreEqual(vm, model);

        }

        [TestMethod]
        public async Task Should_DisplayErrorToast_when_FailedToCheckIn()
        {
            var failedRes = new HTTPResponseWrapper<Shield.Ui.App.Models.CommonModels.CheckInRecord>
            {
                Status = Shield.Common.Constants.ShieldHttpWrapper.Status.FAILED,
                Message = "Check in Failed"
            };
            TrainingInfo trainingInfo = new()
            {
                BemsId = 123456,
                MyLearningDataResponse = new List<IMyLearningDataResponse>()
                {
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_AWARENESS_TRAINING_FOR_AFFECTED_PERSONS,
                        IsTrainingValid = true
                    },
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_CONTROL,
                        IsTrainingValid = true
                    }
                }
            };

            _mockCheckInService.Setup(x => x.PostCheckinAsync(It.IsAny<Models.CommonModels.CheckInRecord>())).Returns(Task.FromResult<HTTPResponseWrapper<Shield.Ui.App.Models.CommonModels.CheckInRecord>>(failedRes));
            _mockSessionService.Setup(s => s.GetUserFromSession(It.IsAny<HttpContext>())).Returns(_bcUser);
            _mockExternalService.Setup(s => s.GetMyLearningDataAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(trainingInfo);
            _mockCheckInTranslator.Setup(s => s.GetCheckInRecordFromViewModel(It.IsAny<CheckInPartialViewModel>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(new CheckInRecord() { BemsId = 2519949, BadgeNumber = null });

            CheckInPartialViewModel vm = new CheckInPartialViewModel()
            {
                CurrentUser = new User { BemsId = 123 },
                bemsId = 123456
            };

            var result = await _controller.CheckInUser(vm);
            var viewResult = (PartialViewResult)result;
            CheckInPartialViewModel model = viewResult.Model as CheckInPartialViewModel;

            Assert.AreEqual(model.CurrentUser.BemsId, vm.CurrentUser.BemsId);
            _mockToastService.Verify(x => x.AddErrorToastMessage(failedRes.Message, It.IsAny<LibraryOptions>()));
        }

        [TestMethod]
        public async Task Should_ReturntheTrainingStausPartialView_When_No_Trainings_Are_Done()
        {
            TrainingInfo trainingInfo = new()
            {
                BemsId = 123456,
                MyLearningDataResponse = new List<IMyLearningDataResponse>()
                {
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_AWARENESS_TRAINING_FOR_AFFECTED_PERSONS,
                        IsTrainingValid = false
                    },
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_CONTROL,
                        IsTrainingValid = false
                    }
                }
            };
            CheckInPartialViewModel vm = new CheckInPartialViewModel
            {
                overrideTraining = false,
                CurrentUser = new User { BemsId = 2830715 },
                bemsId = 123456
            };

            _mockExternalService.Setup(s => s.GetMyLearningDataAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(trainingInfo);
            _mockCheckInTranslator.Setup(s => s.GetCheckInRecordFromViewModel(It.IsAny<CheckInPartialViewModel>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(new CheckInRecord() { BemsId = 123456, BadgeNumber = null });
            _mockUserService.Setup(s => s.GetUserByBemsidAsync(It.IsAny<int>())).ReturnsAsync(new User { DisplayName = "Test_User" });

            var result = await _controller.CheckInUser(vm);
            var viewResult = (PartialViewResult)result;
            var model = (CheckInPartialViewModel)viewResult.Model;

            Assert.AreEqual("Partials/TrainingStatusPartial", viewResult.ViewName);
            Assert.AreEqual(trainingInfo.MyLearningDataResponse, model.UserTrainingData);
            Assert.AreEqual("Test_User", model.recordDisplayName);
            Assert.IsTrue(model.overrideTraining);
        }

        [TestMethod]
        public async Task Should_ReturntheTrainingStausPartialView_When_LOTO_Trainings_Is_Not_Done()
        {
            TrainingInfo trainingInfo = new()
            {
                BemsId = 123456,
                MyLearningDataResponse = new List<IMyLearningDataResponse>()
                {
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_AWARENESS_TRAINING_FOR_AFFECTED_PERSONS,
                        IsTrainingValid = true
                    },
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_CONTROL,
                        IsTrainingValid = false
                    }
                }
            };
            CheckInPartialViewModel vm = new CheckInPartialViewModel
            {
                overrideTraining = false,
                CurrentUser = new User { BemsId = 2830715 },
                bemsId = 123456
            };

            _mockExternalService.Setup(s => s.GetMyLearningDataAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(trainingInfo);
            _mockCheckInTranslator.Setup(s => s.GetCheckInRecordFromViewModel(It.IsAny<CheckInPartialViewModel>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(new CheckInRecord() { BemsId = 123456, BadgeNumber = null });
            _mockUserService.Setup(s => s.GetUserByBemsidAsync(It.IsAny<int>())).ReturnsAsync(new User { DisplayName = "Test_User" });

            var result = await _controller.CheckInUser(vm);
            var viewResult = (PartialViewResult)result;
            var model = (CheckInPartialViewModel)viewResult.Model;

            Assert.AreEqual("Partials/TrainingStatusPartial", viewResult.ViewName);
            Assert.AreEqual(trainingInfo.MyLearningDataResponse, model.UserTrainingData);
            Assert.AreEqual("Test_User", model.recordDisplayName);
            Assert.IsTrue(model.trainingConfirmation);
        }

        [TestMethod]
        public async Task Should_PersistWorkAreaData_When_ReturningThe_TrainingStausPartialView()
        {
            TrainingInfo trainingInfo = new()
            {
                BemsId = 123456,
                MyLearningDataResponse = new List<IMyLearningDataResponse>()
                {
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_AWARENESS_TRAINING_FOR_AFFECTED_PERSONS,
                        IsTrainingValid = true
                    },
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_CONTROL,
                        IsTrainingValid = false
                    }
                }
            };
            CheckInPartialViewModel vm = new CheckInPartialViewModel
            {
                overrideTraining = false,
                CurrentUser = new User { BemsId = 2830715 },
                bemsId = 123456,
                WorkAreaIdString = "10, 11"
            };

            _mockExternalService.Setup(s => s.GetMyLearningDataAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(trainingInfo);
            _mockCheckInTranslator.Setup(s => s.GetCheckInRecordFromViewModel(It.IsAny<CheckInPartialViewModel>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(new CheckInRecord() { BemsId = 123456, BadgeNumber = null });
            _mockUserService.Setup(s => s.GetUserByBemsidAsync(It.IsAny<int>())).ReturnsAsync(new User { DisplayName = "Test_User" });

            ActionResult result = await _controller.CheckInUser(vm);
            PartialViewResult viewResult = (PartialViewResult)result;
            CheckInPartialViewModel model = (CheckInPartialViewModel)viewResult.Model;

            Assert.AreEqual("Partials/TrainingStatusPartial", viewResult.ViewName);
            Assert.IsTrue(model.trainingConfirmation);
            Assert.IsNotNull(model.WorkAreaIdString);
            Assert.AreEqual(2, model.workArea.Count);
        }

        [TestMethod]
        public async Task Should_ReturntheCheckInPartialView_When_No_BemsId_For_BadgeData()
        {
            TrainingInfo trainingInfo = new()
            {
                BemsId = 0,
                MyLearningDataResponse = new List<IMyLearningDataResponse>()
                {
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_AWARENESS_TRAINING_FOR_AFFECTED_PERSONS,
                        IsTrainingValid = true
                    },
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_CONTROL,
                        IsTrainingValid = false
                    }
                }
            };
            CheckInPartialViewModel vm = new CheckInPartialViewModel
            {
                overrideTraining = false,
                CurrentUser = new User { BemsId = 2830715 },
                nameOrBems = "123456789"
            };

            _mockExternalService.Setup(s => s.GetMyLearningDataAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(trainingInfo);
            _mockCheckInTranslator.Setup(s => s.GetCheckInRecordFromViewModel(It.IsAny<CheckInPartialViewModel>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(new CheckInRecord() { BemsId = 0, BadgeNumber = "123456789" });

            var result = await _controller.CheckInUser(vm);
            var viewResult = (PartialViewResult)result;

            Assert.AreEqual("Partials/CheckInPartial", viewResult.ViewName);
        }

        [TestMethod]
        public async Task Should_ReturntheTrainingStausPartial_For_Visitor_Who_Entered_Name()
        {
            TrainingInfo trainingInfo = new()
            {
                BemsId = 0,
                MyLearningDataResponse = new List<IMyLearningDataResponse>()
                {
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_AWARENESS_TRAINING_FOR_AFFECTED_PERSONS,
                        IsTrainingValid = false
                    },
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_CONTROL,
                        IsTrainingValid = false
                    }
                }
            };
            CheckInPartialViewModel vm = new CheckInPartialViewModel
            {
                overrideTraining = false,
                CurrentUser = new User { BemsId = 2830715 },
                nameOrBems = "Test_User"
            };

            _mockExternalService.Setup(s => s.GetMyLearningDataAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(trainingInfo);
            _mockCheckInTranslator.Setup(s => s.GetCheckInRecordFromViewModel(It.IsAny<CheckInPartialViewModel>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(new CheckInRecord() { BemsId = 0, BadgeNumber = null, Name = "Test_User" });

            var result = await _controller.CheckInUser(vm);
            var viewResult = (PartialViewResult)result;
            var model = (CheckInPartialViewModel)viewResult.Model;

            Assert.AreEqual("Partials/TrainingStatusPartial", viewResult.ViewName);
            Assert.AreEqual(trainingInfo.MyLearningDataResponse, model.UserTrainingData);
            Assert.AreEqual("Test_User", model.recordDisplayName);
            Assert.IsTrue(model.overrideTraining);
        }

        [TestMethod]
        public async Task Should_ReturnErrorPartial_when_FailedToCheckIn()
        {
            var failedRes = new HTTPResponseWrapper<Shield.Ui.App.Models.CommonModels.CheckInRecord>
            {
                Status = Shield.Common.Constants.ShieldHttpWrapper.Status.FAILED,
                Message = "Check in Failed"
            };

            HTTPResponseWrapper<int> bemsWrapper = new HTTPResponseWrapper<int>
            {
                Data = 123
            };
            TrainingInfo trainingInfo = new()
            {
                BemsId = 2519949,
                MyLearningDataResponse = new List<IMyLearningDataResponse>()
                {
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_AWARENESS_TRAINING_FOR_AFFECTED_PERSONS,
                        IsTrainingValid = true
                    },
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_CONTROL,
                        IsTrainingValid = true
                    }
                }
            };

            _mockExternalService.Setup(s => s.GetBemsIdFromBemsIdOrBadgeNumber(It.IsAny<string>())).ReturnsAsync(bemsWrapper);
            _mockCheckInService.Setup(x => x.PostCheckinAsync(It.IsAny<Shield.Ui.App.Models.CommonModels.CheckInRecord>())).Returns(Task.FromResult<HTTPResponseWrapper<Shield.Ui.App.Models.CommonModels.CheckInRecord>>(failedRes));
            _mockExternalService.Setup(s => s.GetMyLearningDataAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(trainingInfo);
            _mockCheckInTranslator.Setup(s => s.GetCheckInRecordFromViewModel(It.IsAny<CheckInPartialViewModel>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(new CheckInRecord() { BemsId = 2519949, BadgeNumber = null });

            CheckInPartialViewModel vm = new CheckInPartialViewModel();
            vm.overrideTraining = false;
            vm.CurrentUser = new User { BemsId = 2830715 };
            var result = await _controller.CheckInUser(vm);
            var viewResult = (PartialViewResult)result;

            Assert.AreEqual("Partials/CheckInPartial", viewResult.ViewName);
            _mockToastService.Verify(x => x.AddErrorToastMessage(failedRes.Message, It.IsAny<LibraryOptions>()));
        }

        [TestMethod]
        public async Task Should_ReturnCheckOutCheckInPartial_when_UserAlreadyCheckedIntoAnotherLine()
        {
            var errorMessage = "User 2519949 is currently checked in on Line 100. They must check out there in order to check in here.";
            var alreadyCheckedInResult = new HTTPResponseWrapper<Shield.Ui.App.Models.CommonModels.CheckInRecord>
            {
                Status = Shield.Common.Constants.ShieldHttpWrapper.Status.NOT_MODIFIED,
                Reason = Shield.Common.Constants.ShieldHttpWrapper.Reason.ALREADY_EXISTS,
                Message = errorMessage,
                Data = new Shield.Ui.App.Models.CommonModels.CheckInRecord
                {
                    BemsId = 2519949
                }
            };

            TrainingInfo trainingInfo = new()
            {
                BemsId = 2519949,
                MyLearningDataResponse = new List<IMyLearningDataResponse>()
                {
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_AWARENESS_TRAINING_FOR_AFFECTED_PERSONS,
                        IsTrainingValid = true
                    },
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_CONTROL,
                        IsTrainingValid = true
                    }
                }
            };

            HTTPResponseWrapper<int> bemsWrapper = new HTTPResponseWrapper<int>
            {
                Data = 2519949
            };

            _mockCheckInService.Setup(x => x.PostCheckinAsync(It.IsAny<Shield.Ui.App.Models.CommonModels.CheckInRecord>())).Returns(Task.FromResult(alreadyCheckedInResult));
            _mockSessionService.Setup(s => s.GetUserFromSession(It.IsAny<HttpContext>())).Returns(_bcUser);
            _mockExternalService.Setup(s => s.GetBemsIdFromBemsIdOrBadgeNumber(It.IsAny<string>())).ReturnsAsync(bemsWrapper);
            _mockExternalService.Setup(s => s.GetMyLearningDataAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string   >())).ReturnsAsync(trainingInfo);
            _mockCheckInTranslator.Setup(s => s.GetCheckInRecordFromViewModel(It.IsAny<CheckInPartialViewModel>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(new CheckInRecord() { BemsId = 2519949, BadgeNumber = null });

            CheckInPartialViewModel vm = new CheckInPartialViewModel()
            {
                overrideTraining = false,
                CurrentUser = new User { BemsId = 123 }
            };

            var result = await _controller.CheckInUser(vm);
            var viewResult = (PartialViewResult)result;

            Assert.AreEqual("Partials/CheckOutCheckInPartial", viewResult.ViewName);
            Assert.AreEqual(viewResult.Model, vm);
        }

        [TestMethod]
        public async Task Should_PersistWorkAreaData_When_Returning_CheckOutCheckInPartial()
        {
            var errorMessage = "User 2519949 is currently checked in on Line 100. They must check out there in order to check in here.";
            var alreadyCheckedInResult = new HTTPResponseWrapper<Shield.Ui.App.Models.CommonModels.CheckInRecord>
            {
                Status = Shield.Common.Constants.ShieldHttpWrapper.Status.NOT_MODIFIED,
                Reason = Shield.Common.Constants.ShieldHttpWrapper.Reason.ALREADY_EXISTS,
                Message = errorMessage,
                Data = new Shield.Ui.App.Models.CommonModels.CheckInRecord
                {
                    BemsId = 2519949,
                    WorkAreaIdString = "10, 11"
                }
            };

            TrainingInfo trainingInfo = new()
            {
                BemsId = 2519949,
                MyLearningDataResponse = new List<IMyLearningDataResponse>()
                {
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_AWARENESS_TRAINING_FOR_AFFECTED_PERSONS,
                        IsTrainingValid = true
                    },
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_CONTROL,
                        IsTrainingValid = true
                    }
                }
            };

            HTTPResponseWrapper<int> bemsWrapper = new HTTPResponseWrapper<int>
            {
                Data = 2519949
            };

            _mockCheckInService.Setup(x => x.PostCheckinAsync(It.IsAny<Shield.Ui.App.Models.CommonModels.CheckInRecord>())).Returns(Task.FromResult(alreadyCheckedInResult));
            _mockSessionService.Setup(s => s.GetUserFromSession(It.IsAny<HttpContext>())).Returns(_bcUser);
            _mockExternalService.Setup(s => s.GetBemsIdFromBemsIdOrBadgeNumber(It.IsAny<string>())).ReturnsAsync(bemsWrapper);
            _mockExternalService.Setup(s => s.GetMyLearningDataAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(trainingInfo);
            _mockCheckInTranslator.Setup(s => s.GetCheckInRecordFromViewModel(It.IsAny<CheckInPartialViewModel>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(new CheckInRecord() { BemsId = 2519949, BadgeNumber = null, WorkAreaIdString = "10, 11" });

            CheckInPartialViewModel vm = new CheckInPartialViewModel()
            {
                overrideTraining = false,
                CurrentUser = new User { BemsId = 123 },
                WorkAreaIdString = "10, 11"
            };

            ActionResult result = await _controller.CheckInUser(vm);
            PartialViewResult viewResult = (PartialViewResult)result;
            CheckInPartialViewModel model = (CheckInPartialViewModel)viewResult.Model;

            Assert.AreEqual("Partials/CheckOutCheckInPartial", viewResult.ViewName);
            Assert.AreEqual(model, vm);
            Assert.IsNotNull(model.WorkAreaIdString);
            Assert.AreEqual(2, model.workArea.Count);
        }

        [TestMethod]
        public async Task Should_ReturnErrorPartial_when_CheckOutUserByBemsFails()
        {
            var errorResult = new HTTPResponseWrapper<Shield.Ui.App.Models.CommonModels.CheckInRecord>
            {
                Status = Shield.Common.Constants.ShieldHttpWrapper.Status.FAILED,
                Message = "Failed to check out user",
                Data = null
            };

            CheckInPartialViewModel vm = new CheckInPartialViewModel
            {
                bemsId = 123,
                checkOutNeededFlag = true,
                CurrentUser = new User { BemsId = 2830715 }
            };

            _mockCheckInService.Setup(x => x.PostCheckOutUserByBems(It.IsAny<int>())).Returns(Task.FromResult(errorResult));

            var result = await _controller.CheckInUser(vm) as PartialViewResult;

            Assert.AreEqual("../Shared/Error/ErrorPartial", result.ViewName);
            Assert.AreEqual("Failed to check out user", result.Model);
        }

        [TestMethod]
        public async Task Should_CheckOutUser_when_CheckInUserCalled_and_CheckOutFlagIsTrue()
        {
            var checkOutResponse = new HTTPResponseWrapper<Models.CommonModels.CheckInRecord>
            {
                Status = Shield.Common.Constants.ShieldHttpWrapper.Status.SUCCESS,
                Message = string.Empty,
                Data = new Models.CommonModels.CheckInRecord()
            };

            var checkinPartialVM = new CheckInPartialViewModel
            {
                bemsId = 2519949,
                checkOutNeededFlag = true,
                CurrentUser = new User { BemsId = 2830715 }
            };

            TrainingInfo trainingInfo = new()
            {
                BemsId = 2519949,
                MyLearningDataResponse = new List<IMyLearningDataResponse>()
                {
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_AWARENESS_TRAINING_FOR_AFFECTED_PERSONS,
                        IsTrainingValid = true
                    },
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_CONTROL,
                        IsTrainingValid = true
                    }
                }
            };

            _mockCheckInService.Setup(s => s.PostCheckOutUserByBems(checkinPartialVM.bemsId)).Returns(Task.FromResult(checkOutResponse));
            _mockSessionService.Setup(s => s.GetUserFromSession(It.IsAny<HttpContext>())).Returns(_bcUser);
            _mockExternalService.Setup(s => s.GetMyLearningDataAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(trainingInfo);
            _mockCheckInTranslator.Setup(s => s.GetCheckInRecordFromViewModel(It.IsAny<CheckInPartialViewModel>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(new CheckInRecord() { BemsId = 2519949, BadgeNumber = null });

            PartialViewResult viewResult = await _controller.CheckInUser(checkinPartialVM) as PartialViewResult;

            _mockCheckInService.Verify(s => s.PostCheckOutUserByBems(checkinPartialVM.bemsId));
            _mockCheckInService.Verify(s => s.PostCheckinAsync(It.IsAny<Models.CommonModels.CheckInRecord>()));
            _mockExternalService.Verify(s => s.GetMyLearningDataAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }

        [TestMethod]
        public async Task Should_ReturnCheckInPartial_when_CheckInPartialModelIsNotValid()
        {
            // This is how you make the model invalid
            _controller.ModelState.AddModelError("fakeError", "NO DONT DO THIS!");

            CheckInPartialViewModel vm = new CheckInPartialViewModel();
            var result = await _controller.CheckInUser(vm);
            PartialViewResult viewResult = result as PartialViewResult;

            Assert.AreEqual("Partials/CheckInPartial", viewResult.ViewName);
        }

        [TestMethod]
        public async Task Should_ReturnAValidView_when_RouteToCheckInPageIsSuccessful()
        {
            var apRes = new Shield.Ui.App.Models.CommonModels.Aircraft
            {
                AssignedBargeCoordinatorBems = 88888,
                Site = "BSC",
                Model = "787",
                LineNumber = "500"
            };
            var bcRes = new User
            {
                BemsId = 2519949
            };

            _mockAirplaneService.Setup(aps => aps.GetAirplaneByModelLineNumberAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(apRes));
            _mockUserService.Setup(us => us.GetUserByBemsidAsync(It.IsAny<int>())).Returns(Task.FromResult(bcRes));
            _mockSessionService.Setup(s => s.GetInt(It.IsAny<HttpContext>(), It.IsAny<string>())).Returns(123456);

            ViewResult res = await _controller.CheckIn("787", "500") as ViewResult;
            AircraftHeaderViewModel model = (AircraftHeaderViewModel)res.Model;

            // TODO
            Assert.AreEqual(model.Aircraft, apRes);
            Assert.AreEqual(model.BargeCoordinator, bcRes);
        }

        [TestMethod]
        public async Task Should_ReturnARedirectToSelectLine_when_RouteToCheckInPageFails()
        {
            _mockAirplaneService.Setup(aps => aps.GetAirplaneByModelLineNumberAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult<Shield.Ui.App.Models.CommonModels.Aircraft>(null));


            RedirectToActionResult res = await _controller.CheckIn("787", "500") as RedirectToActionResult;

            Assert.AreEqual("SelectLine", res.ActionName);
            Assert.AreEqual("Admin", res.ControllerName);
        }

        [TestMethod]
        public async Task Should_NotCheckIn_And_ReturnMessage_When_ThisLinesBCIsNotLoggedIn_And_CheckInDoesNotProvideBadgeOfTheBC()
        {
            CheckInPartialViewModel vm = new CheckInPartialViewModel()
            {
                CurrentUser = new User(),
                assignedBCBems = _bcUser.BemsId
            };

            _mockSessionService.Setup(s => s.GetUserFromSession(It.IsAny<HttpContext>())).Returns(new User());
            _mockUserService.Setup(s => s.IsValidPin(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(new HTTPResponseWrapper<bool>());
            var result = await _controller.CheckInUser(vm) as PartialViewResult;

            Assert.AreEqual("Partials/CheckInPartial", result.ViewName);
            _mockToastService.Verify(x => x.AddErrorToastMessage("Person Not Checked In. Scan The Correct Badge or Enter the correct PIN of This Line's CC.", It.IsAny<LibraryOptions>()));
        }

        [TestMethod]
        public async Task Should_NotCheckIn_And_ReturnMessage_When_BC_PIN_Doesnot_Match()
        {
            CheckInPartialViewModel vm = new CheckInPartialViewModel()
            {
                CurrentUser = new User(),
                assignedBCBems = _bcUser.BemsId,
                ConfirmingCCPin = "5679",
                assignedCCPin = "BCe0hOVnvgQJ2lRmUD092kCqmtYz+U0G+B7rfNVcz/M="
            };
            TrainingInfo trainingInfo = new()
            {
                BemsId = 2519949,
                MyLearningDataResponse = new List<IMyLearningDataResponse>()
                {
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_AWARENESS_TRAINING_FOR_AFFECTED_PERSONS,
                        IsTrainingValid = true
                    },
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_CONTROL,
                        IsTrainingValid = true
                    }
                }
            };

            IList<WorkArea> workAreaList = new List<WorkArea> { new WorkArea { Area = "some-work-area" } };
            _mockAirplaneService.Setup(s => s.GetActiveWorkAreasAsync(vm.site, vm.program)).ReturnsAsync(workAreaList).Verifiable();
            _mockSessionService.Setup(s => s.GetUserFromSession(It.IsAny<HttpContext>())).Returns(new User()).Verifiable();
            _mockExternalService.Setup(s => s.GetMyLearningDataAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(trainingInfo);
            _mockCheckInTranslator.Setup(s => s.GetCheckInRecordFromViewModel(It.IsAny<CheckInPartialViewModel>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(new CheckInRecord() { BemsId = 2519949, BadgeNumber = null });

            // Act
            var result = await this._controller.CheckInUser(vm) as PartialViewResult;

            Assert.AreEqual("Partials/CheckInPartial", result.ViewName);
            _mockCheckInService.Verify(s => s.PostCheckinAsync(It.IsAny<CheckInRecord>()), Times.Never);
        }

        [TestMethod]
        public async Task Should_NotCheckIn_And_ReturnMessage_When_BC_Badge_not_Found_In_External_Service()
        {
            CheckInPartialViewModel vm = new CheckInPartialViewModel
            {
                personType = "Home Team",
                site = "some-site",
                program = "some-program",
                ConfirmingBCBadge = "3322323",
                checkOutNeededFlag = true,
                assignedBCBems = 3322323,
                overrideTraining = false,
            };
            TrainingInfo trainingInfo = new()
            {
                BemsId = 2519949,
                MyLearningDataResponse = new List<IMyLearningDataResponse>()
                {
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_AWARENESS_TRAINING_FOR_AFFECTED_PERSONS,
                        IsTrainingValid = true
                    },
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_CONTROL,
                        IsTrainingValid = true
                    }
                }
            };
            HTTPResponseWrapper<bool> response = new HTTPResponseWrapper<bool>()
            {
                Data = false,
                Message = "Person Not Checked In. Scan The Correct Badge or Enter the correct PIN of This Line's CC."
            };
            IList<WorkArea> workAreaList = new List<WorkArea> { new() { Area = "some-work-area" } };
            _mockAirplaneService.Setup(s => s.GetActiveWorkAreasAsync(vm.site, vm.program)).ReturnsAsync(workAreaList).Verifiable();
            _mockSessionService.Setup(s => s.GetUserFromSession(It.IsAny<HttpContext>())).Returns(new User()).Verifiable();
            _mockCheckInService.Setup(s => s.PostCheckOutUserByBems(vm.bemsId))
                .ReturnsAsync(new HTTPResponseWrapper<CheckInRecord> { Status = Status.SUCCESS }).Verifiable();
            _mockExternalService.Setup(s => s.GetMyLearningDataAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(trainingInfo);
            _mockCheckInTranslator.Setup(s => s.GetCheckInRecordFromViewModel(It.IsAny<CheckInPartialViewModel>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(new CheckInRecord() { BemsId = 2519949, BadgeNumber = null });
            _mockExternalService.Setup(s => s.IsValidBadge(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(response);
            var result = await _controller.CheckInUser(vm) as PartialViewResult;

            Assert.AreEqual("Partials/CheckInPartial", result.ViewName);
            _mockToastService.Verify(x => x.AddErrorToastMessage("Person Not Checked In. Scan The Correct Badge or Enter the correct PIN of This Line's CC.", It.IsAny<LibraryOptions>()));
        }

        [TestMethod]
        public async Task Should_Return_Data_When_BC_Badge_Matches_Assigned_BEMS()
        {
            CheckInPartialViewModel vm = new CheckInPartialViewModel()
            {
                CurrentUser = new User(),
                assignedBCBems = _bcUser.BemsId,
                ConfirmingBCBadge = "8020450947"
            };
            HTTPResponseWrapper<bool> response = new HTTPResponseWrapper<bool>()
            {
                Data = true,
                Message = "BCs PIN Matches"
            };
            _mockSessionService.Setup(s => s.GetUserFromSession(It.IsAny<HttpContext>())).Returns(new User());
            _mockExternalService.Setup(s => s.IsValidBadge(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(response);

            var result = await _controller.CheckInUser(vm) as PartialViewResult;
            _mockCheckInTranslator.Verify(x => x.GetCheckInRecordFromViewModel(vm, It.IsAny<DateTime>(), "Check In", false));
        }

        [TestMethod]
        public async Task Should_Return_Data_When_BC_PIN_Matches_AssignedBC_PIN()
        {
            CheckInPartialViewModel vm = new CheckInPartialViewModel()
            {
                CurrentUser = new User(),
                assignedBCBems = _bcUser.BemsId,
                ConfirmingCCPin = "5678",
                assignedCCPin = "BCe0hOVnvgQJ2lRmUD092kCqmtYz+U0G+B7rfNVcz/M="
            };
            HTTPResponseWrapper<bool> response = new HTTPResponseWrapper<bool>()
            {
                Data = true,
                Message = "BCs PIN Matches"
            };
            _mockSessionService.Setup(s => s.GetUserFromSession(It.IsAny<HttpContext>())).Returns(new User());
            
            _mockCheckInService.Setup(x => x.PostCheckinAsync(It.IsAny<CheckInRecord>())).ReturnsAsync(new HTTPResponseWrapper<CheckInRecord>
            {
                Status = Status.SUCCESS
            });
            _mockUserService.Setup(s => s.IsValidPin(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(response);
            var result = await _controller.CheckInUser(vm) as PartialViewResult;

            _mockCheckInTranslator.Verify(x => x.GetCheckInRecordFromViewModel(vm, It.IsAny<DateTime>(), "Check In", false));
            Assert.AreEqual("Partials/CheckInPartial", result.ViewName);
        }

        [TestMethod]
        public async Task Should_NotCheckIn_And_ReturnMessage_When_AssignedBCPin_Is_Null()
        {
            CheckInPartialViewModel vm = new CheckInPartialViewModel()
            {
                CurrentUser = new User(),
                assignedBCBems = _bcUser.BemsId,
                ConfirmingCCPin = null,
                assignedCCPin = "BCe0hOVnvgQJ2lRmUD092kCqmtYz+U0G+B7rfNVcz/M="
            };

            _mockSessionService.Setup(s => s.GetUserFromSession(It.IsAny<HttpContext>())).Returns(new User());
            _mockUserService.Setup(s => s.IsValidPin(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(new HTTPResponseWrapper<bool>());
            var result = await _controller.CheckInUser(vm) as PartialViewResult;

            Assert.AreEqual("Partials/CheckInPartial", result.ViewName);
            _mockToastService.Verify(x => x.AddErrorToastMessage("Person Not Checked In. Scan The Correct Badge or Enter the correct PIN of This Line's CC.", It.IsAny<LibraryOptions>()));
        }

        [TestMethod]
        public async Task Should_CheckInUser_Handle_Exception_From_External_Service()
        {
            CheckInPartialViewModel vm = new CheckInPartialViewModel()
            {
                CurrentUser = new User(),
                assignedBCBems = _bcUser.BemsId,
                ConfirmingCCPin = "5679",
                ConfirmingBCBadge = "8020450947"
            };
            HTTPResponseWrapper<bool> response = new HTTPResponseWrapper<bool>()
            {
                Message = "Person Not Checked In. Scan The Correct Badge or Enter the correct PIN of This Line's CC."
            };
            _mockSessionService.Setup(s => s.GetUserFromSession(It.IsAny<HttpContext>())).Returns(new User());
            //_mockExternalService.Setup(x => x.GetBemsIdFromBemsIdOrBadgeNumber(It.IsAny<string>())).Throws(new Exception());
            _mockExternalService.Setup(s => s.GetBemsIdFromBemsIdOrBadgeNumber(It.IsAny<string>())).ReturnsAsync(new HTTPResponseWrapper<int>());

            _mockExternalService.Setup(s => s.IsValidBadge(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(response);

            var result = await _controller.CheckInUser(vm) as PartialViewResult;
            Assert.AreEqual("Partials/CheckInPartial", result.ViewName);
            _mockToastService.Verify(x => x.AddErrorToastMessage("Person Not Checked In. Scan The Correct Badge or Enter the correct PIN of This Line's CC.", It.IsAny<LibraryOptions>()));
        }

        #endregion Check In User

        #region Checkout

        [TestMethod]
        public async Task Should_Return500WithDefaultErrorMessage_When_CheckOutServiceReturnsNullResponse()
        {

            _mockCheckInService.Setup(x => x.PostCheckOutAsync(It.IsAny<int>())).Returns(Task.FromResult<HTTPResponseWrapper<CheckInRecord>>(null));

            ObjectResult result = await _controller.CheckOut("", "", "1", 1) as ObjectResult;

            _mockCheckInService.Verify(m => m.PostCheckOutAsync(1));

            Assert.AreEqual(500, result.StatusCode);
            Assert.AreEqual("Unable to check out user, please try again.", result.Value);
        }

        [TestMethod]
        public async Task Should_Return500WithErrorMessageFromCheckInService_When_FailsToCheckOut()
        {
            var failedRes = new HTTPResponseWrapper<CheckInRecord>
            {
                Status = Shield.Common.Constants.ShieldHttpWrapper.Status.FAILED,
                Message = "Failed To Check Out"
            };

            _mockCheckInService.Setup(x => x.PostCheckOutAsync(It.IsAny<int>())).Returns(Task.FromResult<HTTPResponseWrapper<CheckInRecord>>(failedRes));


            ObjectResult result = await _controller.CheckOut("", "", "1", 1) as ObjectResult;

            _mockCheckInService.Verify(m => m.PostCheckOutAsync(1));

            Assert.AreEqual(500, result.StatusCode);
            Assert.AreEqual(failedRes.Message, result.Value);
        }

        // test the successful path for check out
        [TestMethod]
        public async Task Should_Return200AndSuccessWithMessageFromCheckInService_When_CheckOutSucceeds()
        {
            var succesfulRes = new HTTPResponseWrapper<CheckInRecord>
            {
                Status = Shield.Common.Constants.ShieldHttpWrapper.Status.SUCCESS,
                Message = "Successfully checked in user x to line y"
            };

            _mockCheckInService.Setup(x => x.PostCheckOutAsync(It.IsAny<int>())).Returns(Task.FromResult<HTTPResponseWrapper<CheckInRecord>>(succesfulRes));


            ObjectResult result = await _controller.CheckOut("", "", "1", 1) as ObjectResult;

            _mockCheckInService.Verify(m => m.PostCheckOutAsync(1));

            Assert.AreEqual(200, result.StatusCode);
            Assert.AreEqual(succesfulRes.Message, result.Value);
        }

        [TestMethod]
        public async Task Should_CallCheckOutAndReturnSuccess_When_CheckOutByBemsOrBadgeSucceeds()
        {
            string site = "BSC";
            string program = "787";
            string lineNumber = "1234";
            string bemsOrBadgeNumber = "8020450947";

            CheckOutRequest checkoutRequest = new CheckOutRequest()
            {
                BemsOrBadgeNumber = "8020450947",
                LineNumber = "1234",
                Program = "787"
            };

            var succeedsRes = new HTTPResponseWrapper<CheckInRecord>
            {
                Status = Shield.Common.Constants.ShieldHttpWrapper.Status.SUCCESS,
                Message = "Checked out some name from Site: BSC,  Program: 787,  Line: 1234."
            };
            _mockCheckInService.Setup(x => x.PostCheckOutRequestAsync(It.IsAny<CheckOutRequest>())).Returns(Task.FromResult<HTTPResponseWrapper<CheckInRecord>>(succeedsRes));



            ObjectResult response = await _controller.CheckOutByBemsOrBadge(site, program, lineNumber, bemsOrBadgeNumber) as ObjectResult;

            Assert.AreEqual("Successfully " + succeedsRes.Message, response.Value);
            Assert.AreEqual(200, response.StatusCode);
        }

        [TestMethod]
        public async Task Should_CallCheckOutAndReturnError_When_CheckOutByBemsOrBadgeFails()
        {
            string site = "BSC";
            string program = "787";
            string lineNumber = "1234";
            string bemsOrBadgeNumber = "8020450947";

            CheckOutRequest checkoutRequest = new CheckOutRequest()
            {
                BemsOrBadgeNumber = "8020450947",
                LineNumber = "1234",
                Program = "787"
            };

            var failedRes = new HTTPResponseWrapper<CheckInRecord>
            {
                Status = Shield.Common.Constants.ShieldHttpWrapper.Status.FAILED,
                Message = "Failed to check out user with BEMSID " + checkoutRequest.BemsOrBadgeNumber + "."
            };
            _mockCheckInService.Setup(x => x.PostCheckOutRequestAsync(It.IsAny<CheckOutRequest>())).Returns(Task.FromResult<HTTPResponseWrapper<CheckInRecord>>(failedRes));
            
            ObjectResult response = await _controller.CheckOutByBemsOrBadge(site, program, lineNumber, bemsOrBadgeNumber) as ObjectResult;

            Assert.AreEqual(failedRes.Message, response.Value);
            Assert.AreEqual(500, response.StatusCode);
        }

        [TestMethod]
        public async Task Should_CallCheckOutAndReturnError_When_CheckOutByBemsOrBadgeFailsToReachCheckinService()
        {
            string site = "BSC";
            string program = "787";
            string lineNumber = "1234";
            string bemsOrBadgeNumber = "8020450947";

            CheckOutRequest checkoutRequest = new CheckOutRequest()
            {
                BemsOrBadgeNumber = "8020450947",
                LineNumber = "1234",
                Program = "787"
            };
            _mockCheckInService.Setup(x => x.PostCheckOutRequestAsync(It.IsAny<CheckOutRequest>())).ReturnsAsync((HTTPResponseWrapper<CheckInRecord>) null);

            ObjectResult response = await _controller.CheckOutByBemsOrBadge(site, program, lineNumber, bemsOrBadgeNumber) as ObjectResult;

            Assert.AreEqual("Cannot reach Check-In Service.", response.Value);
            Assert.AreEqual(500, response.StatusCode);
        }

        [TestMethod]
        public async Task Should_MakeACheckOutCallWithBemsId_When_StringIsBemsId()
        {
            _mockCheckInService.Setup(cs => cs.PostCheckOutRequestAsync(It.IsAny<CheckOutRequest>())).ReturnsAsync(new HTTPResponseWrapper<CheckInRecord> { Data = new CheckInRecord() });

            var result = await _controller.CheckOutByBemsOrBadge("site", "program", "1", "2183740");

            _mockCheckInService.Verify(cs => cs.PostCheckOutRequestAsync(It.Is<CheckOutRequest>(req =>
                req.Program.Equals("program") &&
                req.LineNumber == "1" &&
                req.BemsOrBadgeNumber.Equals("2183740")
            )));
        }

        [TestMethod]
        public async Task Should_MakeACheckOutCallWithBadge_When_StringIsBadge()
        {
            _mockCheckInService.Setup(cs => cs.PostCheckOutRequestAsync(It.IsAny<CheckOutRequest>())).ReturnsAsync(new HTTPResponseWrapper<CheckInRecord> { Data = new CheckInRecord() });

            var result = await _controller.CheckOutByBemsOrBadge("site", "program", "1", "802.0255944");

            _mockCheckInService.Verify(cs => cs.PostCheckOutRequestAsync(It.Is<CheckOutRequest>(req =>
                req.Program.Equals("program") &&
                req.LineNumber == "1" &&
                req.BemsOrBadgeNumber.Equals("8020255944")
            )));
        }

        #endregion Checkout

        #region CheckIn Partial

        [TestMethod]
        public async Task Should_DisplayAListOfWorkAreas_when_TheCheckinPartialPageLoads()
        {
            IList<WorkArea> succesfulRes = new List<WorkArea>()
            {
                new WorkArea
                {
                    Id = 10,
                    Area = "41 Section"
                },
                new WorkArea
                {
                    Id = 11,
                    Area = "48 Section"
                },
                new WorkArea
                {
                    Id = 12,
                    Area = "APU Stand"
                }
            };

            _mockAirplaneService.Setup(aPs => aPs.GetAirplaneByModelLineNumberAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult<Shield.Ui.App.Models.CommonModels.Aircraft>(new Shield.Ui.App.Models.CommonModels.Aircraft()));
            _mockAirplaneService.Setup(aPs => aPs.GetActiveWorkAreasAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult<IList<WorkArea>>(succesfulRes));
            _mockSessionService.Setup(s => s.GetUserFromSession(It.IsAny<HttpContext>())).Returns(_bcUser);

            var result = await _controller.CheckInPartial("Renton", "737", "1", 1);

            _mockAirplaneService.Verify(aPs => aPs.GetActiveWorkAreasAsync("Renton", "737"));

            var viewResult = (PartialViewResult)result;
            var model = (CheckInPartialViewModel)viewResult.Model;

            Assert.IsTrue(model.WorkAreas == succesfulRes);
        }

        [TestMethod]
        public async Task Should_SetAssignedBCBemsInViewModel_when_CheckInPartialLoads()
        {
            _mockAirplaneService.Setup(aPs => aPs.GetAirplaneByModelLineNumberAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult<Shield.Ui.App.Models.CommonModels.Aircraft>(new Shield.Ui.App.Models.CommonModels.Aircraft()));
            _mockAirplaneService.Setup(aPs => aPs.GetActiveWorkAreasAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult<IList<WorkArea>>(new List<WorkArea>()));
            _mockSessionService.Setup(s => s.GetUserFromSession(It.IsAny<HttpContext>())).Returns(_bcUser);

            var result = await _controller.CheckInPartial("Renton", "737", "1", 2);
            var viewResult = (PartialViewResult)result;
            var model = (CheckInPartialViewModel)viewResult.Model;

            Assert.IsTrue(model.assignedBCBems == 2);
        }

        [TestMethod]
        public async Task Should_SetAssignedBCPinInViewModel_when_CheckInPartialLoads()
        {
            _mockAirplaneService.Setup(aPs => aPs.GetAirplaneByModelLineNumberAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult<Shield.Ui.App.Models.CommonModels.Aircraft>(new Shield.Ui.App.Models.CommonModels.Aircraft()));
            _mockAirplaneService.Setup(aPs => aPs.GetActiveWorkAreasAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult<IList<WorkArea>>(new List<WorkArea>()));
            _mockSessionService.Setup(s => s.GetUserFromSession(It.IsAny<HttpContext>())).Returns(_bcUser);

            var result = await _controller.CheckInPartial("Renton", "737", "1", 2, "pin1");
            var viewResult = (PartialViewResult)result;
            var model = (CheckInPartialViewModel)viewResult.Model;

            Assert.IsTrue(model.assignedCCPin == "pin1");
        }

        #endregion CheckIn Partial

        #region CheckIn Initialize ViewModel

        [TestMethod]
        public async Task Should_InitializeCheckinViewModel()
        {
            var bcUser = _bcUser;

            var airplaneWithAssignedBC = _aircraftWithAssignedBC;
            var assignedAircraft = new List<Shield.Ui.App.Models.CommonModels.Aircraft>
            {
                new Shield.Ui.App.Models.CommonModels.Aircraft
                {
                    LineNumber = _aircraftWithAssignedBC.LineNumber
                },
                new Shield.Ui.App.Models.CommonModels.Aircraft{
                    LineNumber = _aircraftWithAssignedBC.LineNumber + 5, // arbitrary + 5 to create unique line number
                }
            };

            _mockSessionService.Setup(s => s.GetUserFromSession(It.IsAny<HttpContext>())).Returns(bcUser);
            _mockAirplaneService.Setup(aps => aps.GetAirplaneByModelLineNumberAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(airplaneWithAssignedBC));
            _mockUserService.Setup(us => us.GetUserByBemsidAsync(It.IsAny<int>())).Returns(Task.FromResult(bcUser));
            _mockAirplaneService.Setup(s => s.GetListOfAircraftByBCBems(It.IsAny<int>())).ReturnsAsync(assignedAircraft);

            var res = await _controller.InitializeCheckInPageContent(_aircraftWithAssignedBC.Model, _aircraftWithAssignedBC.LineNumber);

            _mockSessionService.Verify(s => s.GetUserFromSession(It.IsAny<HttpContext>()));
            _mockAirplaneService.Verify(ap => ap.GetListOfAircraftByBCBems(bcUser.BemsId));

            Assert.AreEqual(assignedAircraft[0].LineNumber, res.BCAssignedAircraft[0].LineNumber);
            Assert.AreEqual(assignedAircraft[1].LineNumber, res.BCAssignedAircraft[1].LineNumber);
            Assert.AreEqual(true, res.IsCurrentUserBC);
            Assert.AreEqual(false, res.IsCurrentUserGC);
            Assert.AreEqual(true, res.IsCurrentUserTheBC);
            Assert.AreEqual(bcUser.BemsId, res.BargeCoordinator.BemsId);
            Assert.AreEqual(airplaneWithAssignedBC.AssignedBargeCoordinatorBems, res.Aircraft.AssignedBargeCoordinatorBems);
            Assert.AreEqual(airplaneWithAssignedBC.Site, res.Aircraft.Site);
            Assert.AreEqual(airplaneWithAssignedBC.Model, res.Aircraft.Model);
            Assert.AreEqual(airplaneWithAssignedBC.LineNumber, res.Aircraft.LineNumber);
            Assert.AreEqual("CheckIn", res.controller);
            Assert.AreEqual("GoToCheckin", res.action);
            Assert.IsNotNull(res.SessionService);
        }

        [TestMethod]
        public async Task Should_FailToInitializeCheckinViewModel_when_AirplaneServiceReturnsNull()
        {
            _mockAirplaneService.Setup(aps => aps.GetAirplaneByModelLineNumberAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult<Shield.Ui.App.Models.CommonModels.Aircraft>(null));

            var res = await _controller.InitializeCheckInPageContent("787", "500");
            _mockAirplaneService.Verify(ap => ap.GetAirplaneByModelLineNumberAsync("787", "500"));

            Assert.IsNull(res);
        }

        [TestMethod]
        public async Task Should_InitializeViewModelWithNewUserAsBC_when_BCIsNotValid()
        {
            var apRes = new Shield.Ui.App.Models.CommonModels.Aircraft
            {
                AssignedBargeCoordinatorBems = null,
                Site = "BSC",
                Model = "787",
                LineNumber = "500"
            };

            _mockAirplaneService.Setup(aps => aps.GetAirplaneByModelLineNumberAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult<Shield.Ui.App.Models.CommonModels.Aircraft>(apRes));
            _mockUserService.Setup(us => us.GetUserByBemsidAsync(It.IsAny<int>())).Returns(Task.FromResult<User>(null));

            var res = await _controller.InitializeCheckInPageContent("787", "500");
            _mockUserService.Verify(s => s.GetUserByBemsidAsync(0));

            Assert.AreEqual(0, res.BargeCoordinator.BemsId);
            Assert.IsFalse(res.IsCurrentUserBC);
            Assert.IsFalse(res.IsCurrentUserGC);
            Assert.IsFalse(res.IsCurrentUserTheBC);
            Assert.AreEqual(apRes.AssignedBargeCoordinatorBems, res.Aircraft.AssignedBargeCoordinatorBems);
            Assert.AreEqual(apRes.Site, res.Aircraft.Site);
            Assert.AreEqual(apRes.Model, res.Aircraft.Model);
            Assert.AreEqual(apRes.LineNumber, res.Aircraft.LineNumber);
            Assert.AreEqual("CheckIn", res.controller);
            Assert.AreEqual("GoToCheckin", res.action);
            Assert.IsNotNull(res.SessionService);
        }

        [TestMethod]
        public async Task Should_SetCheckInMessage_To_Null_When_GCAndBCHaveClaimedLine_And_CurrentUserIsBCForThatLine()
        {
            User currentUser = new User()
            {
                BemsId = 1111111
            };

            User groupCoordinatorOfLine = new User() { BemsId = 3333333 };

            Shield.Ui.App.Models.CommonModels.Aircraft aircraft = new Shield.Ui.App.Models.CommonModels.Aircraft()
            {
                Model = "787",
                LineNumber = "555",
                AssignedBargeCoordinatorBems = currentUser.BemsId,
                AssignedGroupCoordinatorBems = groupCoordinatorOfLine.BemsId
            };

            _mockSessionService.Setup(s => s.GetUserFromSession(It.IsAny<HttpContext>())).Returns(currentUser);
            _mockAirplaneService.Setup(aps => aps.GetAirplaneByModelLineNumberAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(aircraft));
            _mockUserService.Setup(us => us.GetUserByBemsidAsync(It.IsAny<int>())).ReturnsAsync(currentUser);

            var result = await _controller.InitializeCheckInPageContent(aircraft.Model, aircraft.LineNumber);

            Assert.AreEqual(aircraft.AssignedBargeCoordinatorBems, result.BargeCoordinator.BemsId);
            Assert.AreEqual(true, result.IsCurrentUserTheBC);
            Assert.IsNull(result.CheckInMessage);
        }

        [TestMethod]
        public async Task Should_SetCheckInMessage_To_BCNeedsToClaimLine_When_GCHasClaimedLine_And_CurrentUserIsNotTheBCForThatLine()
        {
            User currentUser = new User() { BemsId = 1111111 };
            User bargeCoordinatorOfLine = new User() { BemsId = 2222222 };
            User groupCoordinatorOfLine = new User() { BemsId = 3333333 };

            Shield.Ui.App.Models.CommonModels.Aircraft aircraft = new Shield.Ui.App.Models.CommonModels.Aircraft()
            {
                Model = "787",
                LineNumber = "555",
                AssignedBargeCoordinatorBems = bargeCoordinatorOfLine.BemsId,
                AssignedGroupCoordinatorBems = groupCoordinatorOfLine.BemsId
            };

            _mockSessionService.Setup(s => s.GetUserFromSession(It.IsAny<HttpContext>())).Returns(currentUser);
            _mockAirplaneService.Setup(aps => aps.GetAirplaneByModelLineNumberAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(aircraft));
            _mockUserService.Setup(us => us.GetUserByBemsidAsync(It.IsAny<int>())).ReturnsAsync(bargeCoordinatorOfLine);

            var result = await _controller.InitializeCheckInPageContent(aircraft.Model, aircraft.LineNumber);

            Assert.AreEqual(aircraft.AssignedBargeCoordinatorBems, result.BargeCoordinator.BemsId);
            Assert.AreEqual(false, result.IsCurrentUserTheBC);
            Assert.AreEqual("CC needs to Log In and Claim Line", result.CheckInMessage);
        }

        [TestMethod]
        public async Task Should_SetCheckInMessage_To_GCNeedsToClaimLine_When_GCHasNotClaimedLine_And_CurrentUserIsTheBCForThatLine()
        {
            User currentUser = new User()
            {
                BemsId = 1111111
            };

            Shield.Ui.App.Models.CommonModels.Aircraft aircraft = new Shield.Ui.App.Models.CommonModels.Aircraft()
            {
                Model = "787",
                Site = "BSC",
                LineNumber = "555",
                AssignedBargeCoordinatorBems = currentUser.BemsId
            };

            _mockSessionService.Setup(s => s.GetUserFromSession(It.IsAny<HttpContext>())).Returns(currentUser);
            _mockAirplaneService.Setup(aps => aps.GetAirplaneByModelLineNumberAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(aircraft));
            _mockUserService.Setup(us => us.GetUserByBemsidAsync(currentUser.BemsId)).Returns(Task.FromResult(currentUser));

            var result = await _controller.InitializeCheckInPageContent(aircraft.Model, aircraft.LineNumber);

            Assert.AreEqual(null, result.GroupCoordinator);
            Assert.AreEqual(currentUser.BemsId, result.BargeCoordinator.BemsId);
            Assert.AreEqual(true, result.IsCurrentUserTheBC);
            Assert.AreEqual("A GC needs to claim the line", result.CheckInMessage);
        }

        [TestMethod]
        public async Task Should_SetCheckInMessage_To_BCAndGCNeedToClaimLine_When_GCHasNotClaimedLine_And_CurrentUserIsNotTheBCForThatLine()
        {
            User currentUser = new User()
            {
                BemsId = 1111111
            };

            User bcAssignedToAircraft = new User()
            {
                BemsId = 5555555
            };

            Shield.Ui.App.Models.CommonModels.Aircraft aircraft = new Shield.Ui.App.Models.CommonModels.Aircraft()
            {
                Model = "787",
                Site = "BSC",
                LineNumber = "555",
                AssignedBargeCoordinatorBems = bcAssignedToAircraft.BemsId
            };

            _mockSessionService.Setup(s => s.GetUserFromSession(It.IsAny<HttpContext>())).Returns(currentUser);
            _mockAirplaneService.Setup(aps => aps.GetAirplaneByModelLineNumberAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(aircraft));
            _mockUserService.Setup(us => us.GetUserByBemsidAsync(bcAssignedToAircraft.BemsId)).Returns(Task.FromResult(bcAssignedToAircraft));

            var result = await _controller.InitializeCheckInPageContent(aircraft.Model, aircraft.LineNumber);

            Assert.AreEqual(null, result.GroupCoordinator);
            Assert.AreEqual(bcAssignedToAircraft.BemsId, result.BargeCoordinator.BemsId);
            Assert.AreEqual(false, result.IsCurrentUserTheBC);
            Assert.AreEqual("CC needs to Log In and Claim Line and GC needs to Claim Line", result.CheckInMessage);
        }

        #endregion CheckIn Initialize ViewModel

        #region Set Select Line Variable

        [TestMethod]
        public void Should_SetProgramAndLineSessionVariable_When_AUserGoesToCheckIn()
        {
            _mockUserService.Setup(u => u.GetUsersAsync()).Returns(Task.FromResult<List<User>>(new List<User>()));
            _mockAirplaneService.Setup(ads => ads.GetActiveAirplaneBySiteAsync(It.IsAny<string>())).ReturnsAsync(new List<Shield.Ui.App.Models.CommonModels.Aircraft>());
            _mockSessionService.Setup(s => s.GetUserFromSession(It.IsAny<HttpContext>())).Returns(new User());

            RedirectToActionResult result = _controller.GoToCheckin("program", "123", "site") as RedirectToActionResult;

            _mockSessionService.Verify(s => s.SetString(It.IsAny<HttpContext>(), "selectedSite", "site"));
            _mockSessionService.Verify(s => s.SetString(It.IsAny<HttpContext>(), "selectedProgram", "program"));
            _mockSessionService.Verify(s => s.SetString(It.IsAny<HttpContext>(), "selectedLineNumber", "123"));

            Assert.AreEqual("CheckIn", result.ActionName);
        }

        [TestMethod]
        public void Should_NotSetProgramAndLineAndSiteSessionVariable_When_NoArgumentsSuppliedToGoToCheckin()
        {
            _mockUserService.Setup(u => u.GetUsersAsync()).Returns(Task.FromResult<List<User>>(new List<User>()));
            _mockAirplaneService.Setup(ads => ads.GetActiveAirplaneBySiteAsync(It.IsAny<string>())).ReturnsAsync(new List<Shield.Ui.App.Models.CommonModels.Aircraft>());
            _mockSessionService.Setup(s => s.GetUserFromSession(It.IsAny<HttpContext>())).Returns(new User());

            RedirectToActionResult result = _controller.GoToCheckin(null, "0", null) as RedirectToActionResult;

            _mockSessionService.Verify(s => s.SetString(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _mockSessionService.Verify(s => s.SetString(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _mockSessionService.Verify(s => s.SetString(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            Assert.AreEqual("Index", result.ActionName);
            Assert.AreEqual("Home", result.ControllerName);
        }

        #endregion Get Programs By Site

        #region CheckIn History

        [TestMethod]
        public async Task Should_ReturnAViewOfCheckInHistory()
        {
            Shield.Ui.App.Models.CheckInModels.CheckInTransaction rec1 = new Shield.Ui.App.Models.CheckInModels.CheckInTransaction
            {
                LineNumber = "123",
                Action = "Action 1",
                Date = _now,
                Program = "737"
            };

            Shield.Ui.App.Models.CheckInModels.CheckInTransaction rec2 = new Shield.Ui.App.Models.CheckInModels.CheckInTransaction
            {
                LineNumber = "123",
                Action = "Action 2",
                Date = _now.AddHours(-6),
                Program = "737"
            };

            Shield.Ui.App.Models.CheckInModels.CheckInTransaction rec3 = new Shield.Ui.App.Models.CheckInModels.CheckInTransaction
            {
                LineNumber = "123",
                Action = "Action 3",
                Date = _now.AddHours(-3),
                Program = "737"
            };

            PagingWrapper<Shield.Ui.App.Models.CheckInModels.CheckInTransaction> wrapper = new PagingWrapper<Shield.Ui.App.Models.CheckInModels.CheckInTransaction>
            {
                Data = new List<Shield.Ui.App.Models.CheckInModels.CheckInTransaction>
                {
                    rec1,
                    rec2,
                    rec3
                }
            };

            _mockCheckInService.Setup(s => s.GetCheckInHistoryByProgramAndLineNumber(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(wrapper);


            var result = await _controller.GetCheckInHistory("737", "123", 1);

            _mockCheckInService.Verify(s => s.GetCheckInHistoryByProgramAndLineNumber("737", "123", 1));

            var viewResult = (ViewResult)result;
            var model = (PagingWrapper<Shield.Ui.App.Models.CheckInModels.CheckInTransaction>)viewResult.Model;
            List<Shield.Ui.App.Models.CheckInModels.CheckInTransaction> ModelData = model.Data;


            Assert.AreEqual(3, ModelData.Count);

            Assert.AreEqual(rec1.Date, ModelData[0].Date);
            Assert.AreEqual(rec1.Program, ModelData[0].Program);
            Assert.AreEqual(rec1.LineNumber, ModelData[0].LineNumber);
            Assert.AreEqual("Action 1", ModelData[0].Action);

            Assert.AreEqual(rec3.Date, ModelData[1].Date);
            Assert.AreEqual(rec3.Program, ModelData[1].Program);
            Assert.AreEqual(rec3.LineNumber, ModelData[1].LineNumber);
            Assert.AreEqual("Action 3", ModelData[1].Action);

            Assert.AreEqual(rec2.Date, ModelData[2].Date);
            Assert.AreEqual(rec2.Program, ModelData[2].Program);
            Assert.AreEqual(rec2.LineNumber, ModelData[2].LineNumber);
            Assert.AreEqual("Action 2", ModelData[2].Action);
        }

        [TestMethod]
        public async Task Should_ReturnAViewOfCheckInHistoryWithEmptyList_When_ServiceThrowsError()
        {
            _mockCheckInService.Setup(s => s.GetCheckInHistoryByProgramAndLineNumber(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>())).ThrowsAsync(new Exception());


            var result = await _controller.GetCheckInHistory("737", "123", 1);

            _mockCheckInService.Verify(s => s.GetCheckInHistoryByProgramAndLineNumber("737", "123", 1));

            var viewResult = (ViewResult)result;
            var model = (PagingWrapper<Shield.Ui.App.Models.CheckInModels.CheckInTransaction>)viewResult.Model;

            Assert.IsNotNull(model);
            Assert.AreEqual(null, model.Data);
        }

        #endregion CheckIn History

        #region CheckIn Report

        [TestMethod]
        public async Task Should_ReturnAViewOfCheckInReport()
        {
            CheckInReport rec1 = new CheckInReport
            {
                Name = "Test A",
                BemsId = 8888888,
                WorkArea = "cockpit",
                Program = "P-8",
                Site = "Renton",
                LineNumber = "9999",
                JobNumber = "J-01",
                Activity = "Check-in",
                ManagerBemsId = 1234567,
                ManagerName = "John Dev",
                Date = _now.AddHours(-6)

            };

            CheckInReport rec2 = new CheckInReport
            {
                Name = "Test B",
                BemsId = 8888889,
                WorkArea = "wings",
                Program = "P-8",
                Site = "Renton",
                LineNumber = "9999",
                JobNumber = "J-02",
                Activity = "Check-in",
                ManagerBemsId = 123456,
                ManagerName = "John Levy",
                Date = _now.AddHours(-6)

            };

            PagingWrapper<CheckInReport> wrapper = new PagingWrapper<CheckInReport>
            {
                Data = new List<CheckInReport>
                {
                    rec1,
                    rec2
                }
            };

            List<Shield.Ui.App.Models.CommonModels.Aircraft> aircrafts = new List<Shield.Ui.App.Models.CommonModels.Aircraft>() {
             new Shield.Ui.App.Models.CommonModels.Aircraft
             {
                 Id = 1,
                 LineNumber = "9999",
                 Model = "P-8"
             }
            };

            IList<WorkArea> workAreas = new List<WorkArea>
            {
                new WorkArea
                {
                    Id = 10,
                    Area = "41 Section"
                },
                new WorkArea
                {
                    Id = 11,
                    Area = "48 Section"
                }
            };

            var httpContext = new DefaultHttpContext();
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            tempData.Add("site", "site");
            _controller.TempData = tempData;

            _mockCheckInService.Setup(s => s.GetFilteredCheckInReport(It.IsAny<CheckInReport>())).ReturnsAsync(wrapper);
            _mockAirplaneService.Setup(s => s.GetActiveAirplaneBySiteAsync(It.IsAny<string>())).ReturnsAsync(aircrafts);
            _mockAirplaneService.Setup(s => s.GetWorkAreasAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(workAreas);

            // On page load the api is called without any selectd program
            var resultOnPageLoad = await _controller.ViewCheckinReportsForSite("Renton", null, null, 0, null, null, 0, "Asia/Kolkata", "John Dev", "1234567", 1);

            var viewresultOnPageLoad = (ViewResult)resultOnPageLoad;
            var modelOnPageLoad = (CheckInReportViewModel)viewresultOnPageLoad.Model;
            List<CheckInReport> ModelDataOnPageLoad = modelOnPageLoad.CheckInReports.Data;

            // This api should not be called when no Program selected
            _mockAirplaneService.Verify(x => x.GetWorkAreasAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
            Assert.AreEqual(0, modelOnPageLoad.WorkAreas.Count);

            var result = await _controller.ViewCheckinReportsForSite("Renton", "P-8", null, 0, null, null, 0, "Asia/Kolkata", null, "0", 1);

            var viewResult = (ViewResult)result;
            var model = (CheckInReportViewModel)viewResult.Model;
            List<CheckInReport> ModelData = model.CheckInReports.Data;

            Assert.AreEqual(2, ModelData.Count);
            Assert.AreEqual(1, model.LineNumbers.Count);
            Assert.AreEqual("9999", model.LineNumbers[0]);
            Assert.AreEqual(1, model.Programs.Count);
            Assert.AreEqual("P-8", model.Programs[0]);
            Assert.AreEqual(2, model.WorkAreas.Count);
            Assert.AreEqual("41 Section", model.WorkAreas[0].Area);
            Assert.AreEqual("48 Section", model.WorkAreas[1].Area);

            Assert.AreEqual(rec1.Name, ModelData[0].Name);
            Assert.AreEqual(rec1.BemsId, ModelData[0].BemsId);
            Assert.AreEqual(rec1.WorkArea, ModelData[0].WorkArea);
            Assert.AreEqual(rec1.Program, ModelData[0].Program);
            Assert.AreEqual(rec1.Site, ModelData[0].Site);
            Assert.AreEqual(rec1.LineNumber, ModelData[0].LineNumber);
            Assert.AreEqual(rec1.JobNumber, ModelData[0].JobNumber);
            Assert.AreEqual(rec1.Activity, ModelData[0].Activity);
            Assert.AreEqual(rec1.Date, ModelData[0].Date);
            Assert.AreEqual(rec1.ManagerName, ModelData[0].ManagerName);
            Assert.AreEqual(rec1.ManagerBemsId, ModelData[0].ManagerBemsId);

            Assert.AreEqual(rec2.Name, ModelData[1].Name);
            Assert.AreEqual(rec2.BemsId, ModelData[1].BemsId);
            Assert.AreEqual(rec2.WorkArea, ModelData[1].WorkArea);
            Assert.AreEqual(rec2.Program, ModelData[1].Program);
            Assert.AreEqual(rec2.Site, ModelData[1].Site);
            Assert.AreEqual(rec2.LineNumber, ModelData[1].LineNumber);
            Assert.AreEqual(rec2.JobNumber, ModelData[1].JobNumber);
            Assert.AreEqual(rec2.Activity, ModelData[1].Activity);
            Assert.AreEqual(rec2.Date, ModelData[1].Date);
            Assert.AreEqual(rec2.ManagerName, ModelData[1].ManagerName);
            Assert.AreEqual(rec2.ManagerBemsId, ModelData[1].ManagerBemsId);

            var resultAfterpageload = await _controller.ViewCheckinReportsForSite("Renton", "P-8", "9999", 11, null, null, 8888888, "Asia/Kolkata", "John Dev", "1234567", 1);

            var viewResultAfterpageload = (ViewResult)resultAfterpageload;
            var modelAfterpageload = (CheckInReportViewModel)viewResultAfterpageload.Model;
            List<CheckInReport> ModelDataAfterpageload = modelAfterpageload.CheckInReports.Data;

            Assert.AreEqual(1234567, modelAfterpageload.ManagerBemsId);
            Assert.AreEqual("John Dev", modelAfterpageload.ManagerName);
            Assert.AreEqual("41 Section", modelAfterpageload.WorkAreas[0].Area);
            Assert.AreEqual("48 Section", modelAfterpageload.WorkAreas[1].Area);
            Assert.AreEqual(rec1.LineNumber, ModelDataAfterpageload[0].LineNumber);
            Assert.AreEqual(rec2.LineNumber, ModelDataAfterpageload[1].LineNumber);
            Assert.AreEqual(rec1.WorkArea, ModelDataAfterpageload[0].WorkArea);
            Assert.AreEqual(rec2.WorkArea, ModelDataAfterpageload[1].WorkArea);
        }

        [TestMethod]
        public async Task Should_ReturnAViewOfCheckInReportWithEmptyList_When_ServiceThrowsError()
        {
            _mockCheckInService.Setup(s => s.GetFilteredCheckInReport(It.IsAny<CheckInReport>())).ThrowsAsync(new Exception());


            var result = await _controller.ViewCheckinReportsForSite("Renton", null, null, 0, null, null, 0, null, null, "Asia/Kolkata", 1);

            var viewResult = (ViewResult)result;
            var model = (CheckInReportViewModel)viewResult.Model;

            Assert.IsNotNull(model.CheckInReports);
            Assert.AreEqual(null, model.CheckInReports.Data);
        }

        [TestMethod]
        public async Task Should_ReturnAViewOfCheckInReport_After_Filtering()
        {
            CheckInReport rec1 = new CheckInReport
            {
                Name = "Test A",
                BemsId = 8888888,
                WorkArea = "cockpit",
                Program = "P-8",
                Site = "Renton",
                LineNumber = "9999",
                JobNumber = "J-01",
                Activity = "Check-in",
                ManagerBemsId = 1234567,
                ManagerName = "John Dev",
                Date = new DateTime(2022, 07, 06)

            };

            CheckInReport rec2 = new CheckInReport
            {
                Name = "Test B",
                BemsId = 8888889,
                WorkArea = "wings",
                Program = "P-8",
                Site = "Renton",
                LineNumber = "1111",
                JobNumber = "J-02",
                Activity = "Check-in",
                ManagerBemsId = 123456,
                ManagerName = "John Levy",
                Date = new DateTime(2022, 07, 07)

            };

            CheckInReport rec3 = new CheckInReport
            {
                Name = "Test C",
                BemsId = 8888899,
                WorkArea = "wings",
                Program = "777",
                Site = "Renton",
                LineNumber = "8888",
                JobNumber = "J-03",
                Activity = "Check-out",
                ManagerBemsId = 123455,
                ManagerName = "John",
                Date = new DateTime(2022, 07, 06)

            };

            CheckInReport rec4 = new CheckInReport
            {
                Name = "Test D",
                BemsId = 7777777,
                WorkArea = "Engine",
                Program = "P-8",
                Site = "Renton",
                LineNumber = "1111",
                JobNumber = "J-04",
                Activity = "Check-out",
                ManagerBemsId = 123456,
                ManagerName = "John Dev",
                Date = new DateTime(2022, 07, 05)

            };

            PagingWrapper<CheckInReport> wrapper = new PagingWrapper<CheckInReport>
            {
                Data = new List<CheckInReport>
                {
                    rec1,
                    rec4
                }
            };

            var checkinReport = new CheckInReportViewModel()
            {
                FromDate = new DateTime(2022, 07, 04).ToString(),
                ToDate = new DateTime(2022, 07, 06).ToString(),
                SelectProgram = "P-8",
                SelectLineNumber = "1111"
            };

            List<Shield.Ui.App.Models.CommonModels.Aircraft> aircrafts = new List<Shield.Ui.App.Models.CommonModels.Aircraft>() {
             new Shield.Ui.App.Models.CommonModels.Aircraft
             {
                 Id = 1,
                 LineNumber = "9999",
                 Model = "P-8"
             },
             new Shield.Ui.App.Models.CommonModels.Aircraft
             {
                 Id =2,
                 LineNumber = "1111",
                 Model = "P-8"
             }
            };

            var httpContext = new DefaultHttpContext();
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            tempData.Add("site", "site");
            _controller.TempData = tempData;

            _mockCheckInService.Setup(s => s.GetFilteredCheckInReport(It.IsAny<CheckInReport>())).ReturnsAsync(wrapper);
            _mockAirplaneService.Setup(s => s.GetActiveAirplaneBySiteAsync(It.IsAny<string>())).ReturnsAsync(aircrafts);

            var result = await _controller.FilterCheckinReportsForSite(checkinReport);

            var viewResult = (ViewResult)result;
            var model = (CheckInReportViewModel)viewResult.Model;
            List<CheckInReport> ModelData = model.CheckInReports.Data;

            Assert.AreEqual(2, ModelData.Count);
            Assert.AreEqual(2, model.LineNumbers.Count);
            Assert.AreEqual(1, model.Programs.Count);
            Assert.AreEqual("9999", model.LineNumbers[0]);
            Assert.AreEqual("1111", model.LineNumbers[1]);
            Assert.AreEqual(1, model.Programs.Count);
            Assert.AreEqual("P-8", model.Programs[0]);

            Assert.AreEqual(rec1.Name, ModelData[0].Name);
            Assert.AreEqual(rec1.BemsId, ModelData[0].BemsId);
            Assert.AreEqual(rec1.WorkArea, ModelData[0].WorkArea);
            Assert.AreEqual(rec1.Program, ModelData[0].Program);
            Assert.AreEqual(rec1.Site, ModelData[0].Site);
            Assert.AreEqual(rec1.LineNumber, ModelData[0].LineNumber);
            Assert.AreEqual(rec1.JobNumber, ModelData[0].JobNumber);
            Assert.AreEqual(rec1.Activity, ModelData[0].Activity);
            Assert.AreEqual(rec1.Date, ModelData[0].Date);
            Assert.AreEqual(rec1.ManagerName, ModelData[0].ManagerName);
            Assert.AreEqual(rec1.ManagerBemsId, ModelData[0].ManagerBemsId);

            Assert.AreEqual(rec4.Name, ModelData[1].Name);
            Assert.AreEqual(rec4.BemsId, ModelData[1].BemsId);
            Assert.AreEqual(rec4.WorkArea, ModelData[1].WorkArea);
            Assert.AreEqual(rec4.Program, ModelData[1].Program);
            Assert.AreEqual(rec4.Site, ModelData[1].Site);
            Assert.AreEqual(rec4.LineNumber, ModelData[1].LineNumber);
            Assert.AreEqual(rec4.JobNumber, ModelData[1].JobNumber);
            Assert.AreEqual(rec4.Activity, ModelData[1].Activity);
            Assert.AreEqual(rec4.Date, ModelData[1].Date);
            Assert.AreEqual(rec4.ManagerName, ModelData[1].ManagerName);
            Assert.AreEqual(rec4.ManagerBemsId, ModelData[1].ManagerBemsId);
        }

        [TestMethod]
        public async Task Should_ReturnAViewOfCheckInReport_When_Filtering_UsingWorkArea()
        {
            CheckInReport rec1 = new CheckInReport
            {
                Name = "Test A",
                BemsId = 8888888,
                WorkArea = "cockpit",
                Program = "P-8",
                Site = "Renton",
                LineNumber = "9999",
                JobNumber = "J-01",
                Activity = "Check-in",
                ManagerBemsId = 1234567,
                ManagerName = "John Dev",
                Date = new DateTime(2022, 07, 06)

            };

            CheckInReport rec2 = new CheckInReport
            {
                Name = "Test B",
                BemsId = 8888889,
                WorkArea = "41 Section",
                Program = "P-8",
                Site = "Renton",
                LineNumber = "1111",
                JobNumber = "J-02",
                Activity = "Check-in",
                ManagerBemsId = 123456,
                ManagerName = "John Levy",
                Date = new DateTime(2022, 07, 07)

            };

            CheckInReport rec3 = new CheckInReport
            {
                Name = "Test C",
                BemsId = 8888899,
                WorkArea = "41 Section",
                Program = "P-8",
                Site = "Renton",
                LineNumber = "1111",
                JobNumber = "J-03",
                Activity = "Check-out",
                ManagerBemsId = 123455,
                ManagerName = "John",
                Date = new DateTime(2022, 07, 06)

            };

            CheckInReport rec4 = new CheckInReport
            {
                Name = "Test D",
                BemsId = 7777777,
                WorkArea = "Engine",
                Program = "P-8",
                Site = "Renton",
                LineNumber = "1111",
                JobNumber = "J-04",
                Activity = "Check-out",
                ManagerBemsId = 123456,
                ManagerName = "John Dev",
                Date = new DateTime(2022, 07, 05)

            };

            PagingWrapper<CheckInReport> wrapper = new PagingWrapper<CheckInReport>
            {
                Data = new List<CheckInReport>
                {
                    rec2,
                    rec3
                }
            };

            var checkinReport = new CheckInReportViewModel()
            {
                FromDate = new DateTime(2022, 07, 04).ToString(),
                ToDate = new DateTime(2022, 07, 06).ToString(),
                SelectProgram = "P-8",
                SelectLineNumber = "1111",
                SelectedWorkAreaId = 10
            };

            List<Shield.Ui.App.Models.CommonModels.Aircraft> aircrafts = new List<Shield.Ui.App.Models.CommonModels.Aircraft>() {
             new Shield.Ui.App.Models.CommonModels.Aircraft
             {
                 Id = 1,
                 LineNumber = "9999",
                 Model = "P-8"
             },
             new Shield.Ui.App.Models.CommonModels.Aircraft
             {
                 Id =2,
                 LineNumber = "1111",
                 Model = "P-8"
             }
            };

            List<WorkArea> workAreas = new List<WorkArea>
            {
                new WorkArea
                {
                    Id = 10,
                    Area = "41 Section"
                },
                new WorkArea
                {
                    Id = 11,
                    Area = "48 Section"
                }
            };

            var httpContext = new DefaultHttpContext();
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            tempData.Add("site", "site");
            _controller.TempData = tempData;

            _mockCheckInService.Setup(s => s.GetFilteredCheckInReport(It.IsAny<CheckInReport>())).ReturnsAsync(wrapper);
            _mockAirplaneService.Setup(s => s.GetActiveAirplaneBySiteAsync(It.IsAny<string>())).ReturnsAsync(aircrafts);
            _mockAirplaneService.Setup(s => s.GetWorkAreaById(It.IsAny<int>())).ReturnsAsync(workAreas[0].Area);

            var result = await _controller.FilterCheckinReportsForSite(checkinReport);

            var viewResult = (ViewResult)result;
            var model = (CheckInReportViewModel)viewResult.Model;
            List<CheckInReport> ModelData = model.CheckInReports.Data;

            Assert.AreEqual(2, ModelData.Count);
            Assert.AreEqual(2, model.LineNumbers.Count);
            Assert.AreEqual(1, model.Programs.Count);
            Assert.AreEqual("9999", model.LineNumbers[0]);
            Assert.AreEqual("1111", model.LineNumbers[1]);
            Assert.AreEqual(1, model.Programs.Count);
            Assert.AreEqual("P-8", model.Programs[0]);

            Assert.AreEqual(rec2.Name, ModelData[0].Name);
            Assert.AreEqual(rec2.BemsId, ModelData[0].BemsId);
            Assert.AreEqual(rec2.WorkArea, ModelData[0].WorkArea);
            Assert.AreEqual(rec2.Program, ModelData[0].Program);
            Assert.AreEqual(rec2.Site, ModelData[0].Site);
            Assert.AreEqual(rec2.LineNumber, ModelData[0].LineNumber);
            Assert.AreEqual(rec2.JobNumber, ModelData[0].JobNumber);
            Assert.AreEqual(rec2.Activity, ModelData[0].Activity);
            Assert.AreEqual(rec2.Date, ModelData[0].Date);
            Assert.AreEqual(rec2.ManagerName, ModelData[0].ManagerName);
            Assert.AreEqual(rec2.ManagerBemsId, ModelData[0].ManagerBemsId);

            Assert.AreEqual(rec3.Name, ModelData[1].Name);
            Assert.AreEqual(rec3.BemsId, ModelData[1].BemsId);
            Assert.AreEqual(rec3.WorkArea, ModelData[1].WorkArea);
            Assert.AreEqual(rec3.Program, ModelData[1].Program);
            Assert.AreEqual(rec3.Site, ModelData[1].Site);
            Assert.AreEqual(rec3.LineNumber, ModelData[1].LineNumber);
            Assert.AreEqual(rec3.JobNumber, ModelData[1].JobNumber);
            Assert.AreEqual(rec3.Activity, ModelData[1].Activity);
            Assert.AreEqual(rec3.Date, ModelData[1].Date);
            Assert.AreEqual(rec3.ManagerName, ModelData[1].ManagerName);
            Assert.AreEqual(rec3.ManagerBemsId, ModelData[1].ManagerBemsId);
        }

        [TestMethod]
        public async Task Should_ReturnAViewOfCheckInReportWithEmptyList_When_ServiceThrowsError_After_Filtering()
        {
            var checkinReport = new CheckInReportViewModel()
            {
                FromDate = new DateTime(2022, 07, 04).ToString(),
                ToDate = new DateTime(2022, 07, 06).ToString(),
                SelectProgram = "P-8",
                SelectLineNumber = "1111"
            };
            _mockCheckInService.Setup(s => s.GetFilteredCheckInReport(It.IsAny<CheckInReport>())).ThrowsAsync(new Exception());
            _mockSessionService.Setup(s => s.GetString(It.IsAny<HttpContext>(), It.IsAny<string>())).Returns("site");

            var result = await _controller.FilterCheckinReportsForSite(checkinReport);

            var viewResult = (ViewResult)result;
            var model = (CheckInReportViewModel)viewResult.Model;

            Assert.IsNull(model.CheckInReports);
        }

        [TestMethod]
        public async Task Should_Return_Excel_File()
        {
            CheckInReport rec1 = new CheckInReport
            {
                Name = "Test A",
                BemsId = 8888888,
                WorkArea = "cockpit",
                Program = "P-8",
                Site = "Renton",
                LineNumber = "9999",
                JobNumber = "J-01",
                Activity = "Check-in",
                ManagerBemsId = 1234567,
                ManagerName = "John Dev",
                Date = _now.ToUniversalTime().AddHours(-6)
            };

            CheckInReport rec2 = new CheckInReport
            {
                Name = "Test B",
                BemsId = 8888889,
                WorkArea = "wings",
                Program = "P-8",
                Site = "Renton",
                LineNumber = "9999",
                JobNumber = "J-02",
                Activity = "Check-in",
                ManagerBemsId = 123456,
                ManagerName = "John Levy",
                Date = _now.ToUniversalTime().AddHours(-6)

            };

            List<CheckInReport> checkInReportList = new List<CheckInReport>
            {
                rec1,
                rec2
            };

            var httpContext = new DefaultHttpContext();
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            tempData.Add("site", "site");
            _controller.TempData = tempData;

            _mockCheckInService.Setup(s => s.GetFilteredCheckInReportForExcel(It.IsAny<CheckInReport>())).ReturnsAsync(checkInReportList);

            var result = await _controller.ExportReportToExcel("P-8", "9999", null, 0, null, null, "Asia/Kolkata", "0", null);
            var file = (FileStreamResult)result;


            Assert.AreEqual("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.ContentType);
            Assert.IsTrue(file.FileStream.Length > 0);
            Assert.IsTrue(file.FileDownloadName.Contains("CheckInReport") && file.FileDownloadName.Contains(".xlsx"));

        }

        #endregion
    }
}

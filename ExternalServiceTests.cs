using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Shield.Common;
using Shield.Common.Models.Common;
using Shield.Ui.App.Models.ExternalModels;
using Shield.Ui.App.Services;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Shield.Ui.App.Tests
{
    using Shield.Common.Constants;
    using Shield.Common.Models.MyLearning;
    using Shield.Ui.App.Models.CheckInModels;
    using System.Collections.Generic;

    [TestClass]
    public class ExternalServiceTests
    {
        private Mock<IHttpClient> _mockClient;
        private Mock<HttpClientService> _mockClientService;
        private ExternalService _externalService;

        [TestInitialize]
        public void Setup()
        {
            EnvironmentHelper.ExternalServiceAddress = "https://test/test/";
            _mockClient = new Mock<IHttpClient>();
            _mockClientService = new Mock<HttpClientService>();
            _externalService = new ExternalService(_mockClientService.Object);
        }

        [TestMethod]
        public async Task GetBemsIdFromBemsIdOrBadgeNumber_ShouldReturn_SuccessWithBemsId_When_ValidBadgeNumber()
        {
            string bemsIdOrBadgeNumber = "0123456789";

            HTTPResponseWrapper<BadgeDataResponse> responseWrapper = new HTTPResponseWrapper<BadgeDataResponse>()
            {
                Status = Shield.Common.Constants.ShieldHttpWrapper.Status.SUCCESS,
                Data = new BadgeDataResponse()
                {
                    BemsId = 2830715
                },
                Reason = "A_REASON",
                Message = "A_MESSAGE"
            };

            HttpResponseMessage responseMessage = new HttpResponseMessage()
            {
                Content = new StringContent(JsonConvert.SerializeObject(responseWrapper), Encoding.UTF8, "application/json")
            };

            _mockClient.Setup(hc => hc.GetAsync(It.IsAny<Uri>())).ReturnsAsync(responseMessage);
            _mockClientService.Setup(hcs => hcs.GetClient()).Returns(_mockClient.Object);

            var result = await _externalService.GetBemsIdFromBemsIdOrBadgeNumber(bemsIdOrBadgeNumber);

            Assert.AreEqual(Shield.Common.Constants.ShieldHttpWrapper.Status.SUCCESS, result.Status);
            Assert.AreEqual(responseWrapper.Reason, result.Reason);
            Assert.AreEqual(responseWrapper.Message, result.Message);
            Assert.AreEqual(2830715, result.Data);
        }

        [TestMethod]
        public async Task GetBemsIdFromBemsIdOrBadgeNumber_ShouldReturn_SuccessWithBemsId_When_ValidBadgeNumber_having_dot()
        {
            string bemsIdOrBadgeNumber = "040.0067622";

            HTTPResponseWrapper<BadgeDataResponse> responseWrapper = new HTTPResponseWrapper<BadgeDataResponse>()
            {
                Status = Shield.Common.Constants.ShieldHttpWrapper.Status.SUCCESS,
                Data = new BadgeDataResponse()
                {
                    BemsId = 2830715
                },
                Reason = "A_REASON",
                Message = "A_MESSAGE"
            };

            HttpResponseMessage responseMessage = new HttpResponseMessage()
            {
                Content = new StringContent(JsonConvert.SerializeObject(responseWrapper), Encoding.UTF8, "application/json")
            };

            _mockClient.Setup(hc => hc.GetAsync(It.IsAny<Uri>())).ReturnsAsync(responseMessage);
            _mockClientService.Setup(hcs => hcs.GetClient()).Returns(_mockClient.Object);

            var result = await _externalService.GetBemsIdFromBemsIdOrBadgeNumber(bemsIdOrBadgeNumber);

            Assert.AreEqual(Shield.Common.Constants.ShieldHttpWrapper.Status.SUCCESS, result.Status);
            Assert.AreEqual(responseWrapper.Reason, result.Reason);
            Assert.AreEqual(responseWrapper.Message, result.Message);
            Assert.AreEqual(2830715, result.Data);
        }

        [TestMethod]
        public async Task GetBemsIdFromBemsIdOrBadgeNumber_ShouldReturn_SuccessWithBemsId_When_ValidBadgeNumber_having_10digit()
        {
            string bemsIdOrBadgeNumber = "1802599323";

            HTTPResponseWrapper<BadgeDataResponse> responseWrapper = new HTTPResponseWrapper<BadgeDataResponse>()
            {
                Status = Shield.Common.Constants.ShieldHttpWrapper.Status.SUCCESS,
                Data = new BadgeDataResponse()
                {
                    BemsId = 2830715
                },
                Reason = "A_REASON",
                Message = "A_MESSAGE"
            };

            HttpResponseMessage responseMessage = new HttpResponseMessage()
            {
                Content = new StringContent(JsonConvert.SerializeObject(responseWrapper), Encoding.UTF8, "application/json")
            };

            _mockClient.Setup(hc => hc.GetAsync(It.IsAny<Uri>())).ReturnsAsync(responseMessage);
            _mockClientService.Setup(hcs => hcs.GetClient()).Returns(_mockClient.Object);

            var result = await _externalService.GetBemsIdFromBemsIdOrBadgeNumber(bemsIdOrBadgeNumber);

            Assert.AreEqual(Shield.Common.Constants.ShieldHttpWrapper.Status.SUCCESS, result.Status);
            Assert.AreEqual(responseWrapper.Reason, result.Reason);
            Assert.AreEqual(responseWrapper.Message, result.Message);
            Assert.AreEqual(2830715, result.Data);
        }

        [TestMethod]
        public async Task GetBemsIdFromBemsIdOrBadgeNumber_ShouldReturn_SuccessWrapperWithBems_When_PassedBemsId()
        {
            string bemsIdOrBadgeNumber = "2830715";

            _mockClientService.Setup(hcs => hcs.GetClient()).Returns(_mockClient.Object);

            var result = await _externalService.GetBemsIdFromBemsIdOrBadgeNumber(bemsIdOrBadgeNumber);

            _mockClient.Verify(m => m.GetAsync(It.IsAny<Uri>()), Times.Never);

            Assert.AreEqual(Shield.Common.Constants.ShieldHttpWrapper.Status.SUCCESS, result.Status);
            Assert.AreEqual(2830715, result.Data);
        }

        /// <summary>
        /// The get bemsId from bemsId or badge number should return error message when bems is string.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [TestMethod]
        public async Task GetBemsIdFromBemsIdOrBadgeNumber_ShouldReturn_ErrorMessage_WhenBemsIsString()
        {
            // Arrange
            const string BemsIdOrBadgeNumber = "string";
            const string ExpectedMessage = $"Failed to check out user with BEMSID/Badge: {BemsIdOrBadgeNumber}.";

            // Act
            HTTPResponseWrapper<int> result = await _externalService.GetBemsIdFromBemsIdOrBadgeNumber(BemsIdOrBadgeNumber);

            // Assert
            Assert.AreEqual(Shield.Common.Constants.ShieldHttpWrapper.Status.FAILED, result.Status);
            Assert.AreEqual(ExpectedMessage, result.Message);
        }

        /// <summary>
        /// The get bemsId from bemsId or badge number should return error message when bems is alpha numeric.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [TestMethod]
        public async Task GetBemsIdFromBemsIdOrBadgeNumber_ShouldReturn_ErrorMessage_WhenBemsIsAlphaNumeric()
        {
            // Arrange
            const string BemsIdOrBadgeNumber = "Test123";
            const string ExpectedMessage = $"Failed to check out user with BEMSID/Badge: {BemsIdOrBadgeNumber}.";

            // Act
            HTTPResponseWrapper<int> result = await _externalService.GetBemsIdFromBemsIdOrBadgeNumber(BemsIdOrBadgeNumber);

            // Assert
            Assert.AreEqual(Shield.Common.Constants.ShieldHttpWrapper.Status.FAILED, result.Status);
            Assert.AreEqual(ExpectedMessage, result.Message);
        }

        [TestMethod]
        public async Task Should_Return_MyLearningData_When_Valid_Training()
        {
            // Arrange
            const int BemsId = 1231231;
            List<MyLearningDataResponse> myLearningResponse = new()
            {
                new MyLearningDataResponse
                {
                    CertCode = "77517GC",
                    IsTrainingValid = true
                },
                new MyLearningDataResponse
                {
                    CertCode = "77517GC",
                    IsTrainingValid = true
                }
            };
            HTTPResponseWrapper<List<MyLearningDataResponse>> httpResponseWrapper = new()
            {
                Data = myLearningResponse
            };

            HttpResponseMessage httpResponse = new HttpResponseMessage
            {
                Content = new StringContent(JsonConvert.SerializeObject(httpResponseWrapper), Encoding.UTF8, "application/json")
            };
            List<string> courseCodeList = new List<string>
                        {
                            "77517GC",
                            "77517GC"
                        };

            _mockClientService.Setup(s => s.GetClient().GetAsync(It.IsAny<Uri>())).ReturnsAsync(httpResponse);

            // Act
            TrainingInfo result = await _externalService.GetMyLearningDataAsync(BemsId,null, "Boeing Checkin");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.MyLearningDataResponse.Count);
            Assert.AreEqual(BemsId, result.BemsId);
            Assert.IsTrue(result.MyLearningDataResponse[0].IsTrainingValid);
            Assert.IsTrue(result.MyLearningDataResponse[1].IsTrainingValid);
        }

        /// <summary>
        /// The should return my learning data when invalid training.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [TestMethod]
        public async Task Should_Return_MyLearningData_When_Invalid_Training()
        {
            // Arrange
            const int BemsId = 1231231;
            List<MyLearningDataResponse> myLearningResponse = new()
            {
                new MyLearningDataResponse
                {
                    CertCode = "77517GC",
                    IsTrainingValid = false
                },
                new MyLearningDataResponse
                {
                    CertCode = "77517GC",
                    IsTrainingValid = false
                }
            };
            HTTPResponseWrapper<List<MyLearningDataResponse>> httpResponseWrapper = new()
            {
                Data = myLearningResponse
            };

            HttpResponseMessage httpResponse = new HttpResponseMessage
            {
                Content = new StringContent(JsonConvert.SerializeObject(httpResponseWrapper), Encoding.UTF8, "application/json")
            };
            List<string> courseCodeList = new List<string>
                        {
                            "77517GC",
                            "77517GC"
                        };
            _mockClientService.Setup(s => s.GetClient().GetAsync(It.IsAny<Uri>())).ReturnsAsync(httpResponse);

            // Act
            TrainingInfo result = await _externalService.GetMyLearningDataAsync(BemsId, null, "Boeing Checkin");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.MyLearningDataResponse.Count);
            Assert.AreEqual(BemsId, result.BemsId);
            Assert.IsFalse(result.MyLearningDataResponse[0].IsTrainingValid);
            Assert.IsFalse(result.MyLearningDataResponse[1].IsTrainingValid);
        }

        /// <summary>
        /// The should return my learning data when T1 is valid and T2 is invalid training.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [TestMethod]
        public async Task Should_Return_MyLearningData_When_T1_Valid_And_T2_Invalid_Training()
        {
            // Arrange
            const int BemsId = 1231231;
            List<MyLearningDataResponse> myLearningResponse = new()
            {
                new MyLearningDataResponse
                {
                    CertCode = "77517GC",
                    IsTrainingValid = true
                },
                new MyLearningDataResponse
                {
                    CertCode = "77517GC",
                    IsTrainingValid = false
                }
            };
            HTTPResponseWrapper<List<MyLearningDataResponse>> httpResponseWrapper = new()
            {
                Data = myLearningResponse
            };

            HttpResponseMessage httpResponse = new HttpResponseMessage
            {
                Content = new StringContent(JsonConvert.SerializeObject(httpResponseWrapper), Encoding.UTF8, "application/json")
            };
            List<string> courseCodeList = new List<string>
                        {
                            "77517GC",
                            "77517GC"
                        };

            _mockClientService.Setup(s => s.GetClient().GetAsync(It.IsAny<Uri>())).ReturnsAsync(httpResponse);

            // Act
            TrainingInfo result = await _externalService.GetMyLearningDataAsync(BemsId, null, "Boeing Checkin");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.MyLearningDataResponse.Count);
            Assert.AreEqual(BemsId, result.BemsId);
            Assert.IsTrue(result.MyLearningDataResponse[0].IsTrainingValid);
            Assert.IsFalse(result.MyLearningDataResponse[1].IsTrainingValid);
        }

        /// <summary>
        /// The should return my learning data when client service throws exception.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [TestMethod]
        public async Task Should_Return_MyLearningData_When_ClientService_Throws_Exception()
        {
            // Arrange
            const int BemsId = 1231231;

            _mockClientService.Setup(s => s.GetClient().GetAsync(It.IsAny<Uri>())).ThrowsAsync(new Exception());

            // Act
            TrainingInfo result = await _externalService.GetMyLearningDataAsync(BemsId, null, null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1231231, result.BemsId);
            Assert.AreEqual(0, result.MyLearningDataResponse.Count);
        }

        /// <summary>
        /// The should return my learning data when json deserialize fails.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [TestMethod]
        public async Task Should_Return_MyLearningData_When_JsonDeserialize_Fails()
        {
            // Arrange
            const int BemsId = 1231231;

            HttpResponseMessage httpResponse = new HttpResponseMessage
            {
                Content = new StringContent(JsonConvert.SerializeObject(string.Empty), Encoding.UTF8, "application/json")
            };

            _mockClientService.Setup(s => s.GetClient().GetAsync(It.IsAny<Uri>())).ReturnsAsync(httpResponse);

            // Act
            TrainingInfo result = await _externalService.GetMyLearningDataAsync(BemsId, null, null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.MyLearningDataResponse.Count);
        }

        [TestMethod]
        public async Task Should_Return_MyLearningData_When_Badge_Number_Is_Given()
        {
            // Arrange
            const string BadgeNumber = "123456789";
            List<MyLearningDataResponse> myLearningResponse = new()
            {
                new MyLearningDataResponse
                {
                    CertCode = "77517GC",
                    IsTrainingValid = true
                },
                new MyLearningDataResponse
                {
                    CertCode = "77517GC",
                    IsTrainingValid = true
                }
            };
            HTTPResponseWrapper<List<MyLearningDataResponse>> httpResponseWrapper = new()
            {
                Data = myLearningResponse
            };
            HttpResponseMessage httpResponse = new HttpResponseMessage
            {
                Content = new StringContent(JsonConvert.SerializeObject(httpResponseWrapper), Encoding.UTF8, "application/json")
            };
            HTTPResponseWrapper<BadgeDataResponse> externalResponseWrapper = new HTTPResponseWrapper<BadgeDataResponse>()
            {
                Status = Shield.Common.Constants.ShieldHttpWrapper.Status.SUCCESS,
                Data = new BadgeDataResponse()
                {
                    BemsId = 123456
                }
            };
            List<string> courseCodeList = new List<string>
                        {
                            "77517GC",
                            "77517GC"
                        };
            HttpResponseMessage externalResponseMessage = new HttpResponseMessage()
            {
                Content = new StringContent(JsonConvert.SerializeObject(externalResponseWrapper), Encoding.UTF8, "application/json")
            };
            Uri uri = new Uri("https://test/test/mylearningdata/GetMyLearningData?bemsId=123456&trainingId=TR006005&trainingId=77517");

            _mockClientService.Setup(s => s.GetClient().GetAsync(uri)).ReturnsAsync(httpResponse);
            _mockClientService.Setup(s => s.GetClient().GetAsync(new Uri("https://test/test/badgedata/123456789"))).ReturnsAsync(externalResponseMessage);
            // Act
            TrainingInfo result = await _externalService.GetMyLearningDataAsync(0, BadgeNumber, "Boeing Checkin");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.MyLearningDataResponse.Count);
            Assert.AreEqual(123456, result.BemsId);
            Assert.IsTrue(result.MyLearningDataResponse[0].IsTrainingValid);
            Assert.IsTrue(result.MyLearningDataResponse[1].IsTrainingValid);
        }
    }
}

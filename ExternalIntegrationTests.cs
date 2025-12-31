using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shield.Common;
using Shield.Ui.App.Services;
using Shield.Ui.App.Tests.Helpers;
using System.Threading.Tasks;

namespace Shield.Ui.App.Tests.ServiceIntegration
{
    using Shield.Common.Constants.ShieldHttpWrapper;

    [TestClass]
    public class ExternalIntegrationTests
    {
        private HttpClientService _httpClientService;
        private ExternalService _externalService;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            TestHelper.TestSetup();

            ExternalIntegrationTests tests = new ExternalIntegrationTests();
            tests._httpClientService = new HttpClientService();

            tests._externalService = new ExternalService(tests._httpClientService);
        }

        [TestInitialize]
        public void TestSetup()
        {
            _httpClientService = new HttpClientService();
            _externalService = new ExternalService(_httpClientService);
        }

        [TestMethod]
        [TestCategory(TestConstants.INTEGRATION_TEST_STRING)]
        public async Task Should_GetBemsIdFromBadgeNumber()
        {
            string badgeNumber = "0400444354";

            var result = await _externalService.GetBemsIdFromBemsIdOrBadgeNumber(badgeNumber);
            Assert.AreEqual(3587137, result.Data);
        }

        /// <summary>
        /// The should return error for string get bems id from badge number.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [TestMethod]
        [TestCategory(TestConstants.INTEGRATION_TEST_STRING)]
        public async Task ShouldReturnErrorForString_GetBemsIdFromBadgeNumber()
        {
            // Arrange
            const string BadgeNumber = "string";
            const string ExpectedErrorMessage = $"Failed to check out user with BEMSID/Badge: {BadgeNumber}.";

            // Act
            var result = await _externalService.GetBemsIdFromBemsIdOrBadgeNumber(BadgeNumber);

            // Assert
            Assert.AreEqual(ExpectedErrorMessage, result.Message);
            Assert.AreEqual(Status.FAILED, result.Status);
        }
    }
}

using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shield.Common;
using Shield.Ui.App.Common;
using Shield.Ui.App.Models.CommonModels;
using Shield.Ui.App.Services;
using Shield.Ui.App.Tests.Helpers;
using Shield.Ui.App.Translators;
using Shield.Ui.App.ViewModels;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Shield.Ui.App.Tests.Views
{

    [TestClass]
    public class CheckInPartialTests
    {
        private CheckInTranslator _checkInTranslator;
        private HtmlParser _parser;
        private CheckInPartialViewModel _vm;
        private IDictionary<string, Object> _viewData;
        private HttpClient _client;
        private Mock<HttpClientService> _mockHttpClientService;
        private Mock<LotoService> _mockLotoService;
        private Mock<UserService> _mockUserService;
        private Mock<AirplaneDataService> _mockAirplaneDataService;
        private Mock<SessionService> _mockSessionService;
        private Mock<CheckInService> _mockCheckinService;
        private Mock<ExternalService> _mockExternalService;

        #region Setup

        [TestInitialize]
        public void SetUp()
        {
            _checkInTranslator = new CheckInTranslator();
            _parser = new HtmlParser();
            _vm = _checkInTranslator.GetDefaultCheckInPartialViewModel("12", "BSC", "787", new List<WorkArea>()
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
                                                                                            });
            _vm.CurrentUser = new Models.CommonModels.User();
            _viewData = new ExpandoObject() as IDictionary<string, Object>;
            _viewData.Add("Status", "Success");
            _viewData.Add("Message", "Check In Success, yo!");

            _mockHttpClientService = new Mock<HttpClientService>();
            _mockUserService = new Mock<UserService>(_mockHttpClientService.Object);
            _mockLotoService = new Mock<LotoService>(_mockHttpClientService.Object);
            _mockAirplaneDataService = new Mock<AirplaneDataService>(_mockHttpClientService.Object, _mockLotoService.Object);
            _mockSessionService = new Mock<SessionService>();
            _mockExternalService = new Mock<ExternalService>(_mockHttpClientService.Object);
            _mockCheckinService = new Mock<CheckInService>(_mockHttpClientService.Object, _mockExternalService.Object);

            string rootDir = TestHelper.GetUiBaseDirectory() + "Shield.Ui.App";

            var builder = new WebApplicationFactory<Program>()
                       .WithWebHostBuilder(builder =>
                       {
                           builder.ConfigureServices(services =>
                           {
                               services.AddTransient<UserService>(ctx => { return _mockUserService.Object; });
                               services.AddTransient<SessionService>(ctx => { return _mockSessionService.Object; });
                               services.AddTransient<AirplaneDataService>(ctx => { return _mockAirplaneDataService.Object; });
                               services.AddTransient<CheckInService>(ctx => { return _mockCheckinService.Object; });
                               services.AddTransient<ExternalService>(ctx => { return _mockExternalService.Object; });
                           });
                       });


            _client = builder.CreateClient();
            _client.DefaultRequestHeaders.Add("boeingbemsid", Constants.MockGCBems);
            Constants.MockHeaderFlag = false;
        }

        #endregion Setup

        #region Active Tab

        [TestMethod]
        public async Task Should_Make_HomeTeam_Tab_Default()
        {
            List<WorkArea> workAreas = new List<WorkArea>
            {
                new WorkArea() { Area = "area1" }
            };
            _mockAirplaneDataService.Setup(u => u.GetActiveWorkAreasAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(workAreas);

            var res = await _client.GetAsync("/CheckIn/CheckInPartial?site=BSC&model=787&lineNumber=12&bemsId=9999999");
            string output = await res.Content.ReadAsStringAsync();
            var doc = _parser.ParseDocument(output);

            var tLabel = doc.GetElementById("travelerOption");
            var vLabel = doc.GetElementById("visitorOption");
            var hLabel = doc.GetElementById("homeTeamOption");
            var tabs = doc.GetElementsByClassName("checkin-tab");
            var ccPinInputText = doc.GetElementById("cc-pin-input").GetElementsByTagName("label")[0].TextContent.Trim();
            var ccPinNotificationText = doc.GetElementById("cc-pin-input").GetElementsByTagName("span")[1].TextContent.Trim();

            Assert.IsFalse(tLabel.ClassList.Contains("active"));
            Assert.IsFalse(vLabel.ClassList.Contains("active"));
            Assert.IsTrue(hLabel.ClassList.Contains("active"));
            Assert.AreEqual("homeTeamOption", tabs[0].Children[0].Id);
            Assert.AreEqual("travelerOption", tabs[1].Children[0].Id);
            Assert.AreEqual("visitorOption", tabs[2].Children[0].Id);
            Assert.AreEqual("CC Enter PIN *", ccPinInputText);
            Assert.AreEqual("PIN to only be used by Check in Coordinators (CC).", ccPinNotificationText);
        }

        [TestMethod]
        public async Task Should_Make_HomeTeam_Tab_Default_With_PIN()
        {
            List<WorkArea> workAreas = new List<WorkArea>
            {
                new WorkArea() { Area = "area1" }
            };
            _mockAirplaneDataService.Setup(u => u.GetActiveWorkAreasAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(workAreas);

            var res = await _client.GetAsync("/CheckIn/CheckInPartial?site=BSC&model=787&lineNumber=12&bemsId=9999999&pin=pin1");
            string output = await res.Content.ReadAsStringAsync();
            var doc = _parser.ParseDocument(output);

            var tLabel = doc.GetElementById("assignedCCPin");

            Assert.IsNotNull(tLabel);
            Assert.AreEqual("pin1", tLabel.GetAttribute("value"));
        }

        [TestMethod]
        public async Task Should_Show_WorkAreaList_Dropdown()
        {
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
                },
                new WorkArea
                {
                    Id = 12,
                    Area = "Audit"
                },
                new WorkArea
                {
                    Id = 12,
                    Area = "APU Stand"
                }
            };
            _mockAirplaneDataService.Setup(u => u.GetActiveWorkAreasAsync("BSC", It.IsAny<string>())).ReturnsAsync(workAreas);

            HttpResponseMessage res = await _client.GetAsync("/CheckIn/CheckInPartial?site=BSC&model=787&lineNumber=12&bemsId=9999999&pin=pin1");
            string output = await res.Content.ReadAsStringAsync();
            IHtmlDocument doc = _parser.ParseDocument(output);

            Assert.IsNotNull(doc.GetElementById("work-area-span"));
            Assert.IsNotNull(doc.GetElementsByClassName("custom-select-shield"));
            Assert.IsNotNull(doc.GetElementsByClassName("work-areas-select"));
            Assert.AreEqual(4, doc.GetElementsByClassName("work-area").Length);
            Assert.IsNotNull(doc.GetElementById("check-in-btn").GetAttribute("disabled"));

            _mockAirplaneDataService.Setup(u => u.GetActiveWorkAreasAsync("NW Flight Test BT&E", It.IsAny<string>())).ReturnsAsync(workAreas);

            res = await _client.GetAsync("/CheckIn/CheckInPartial?site=NW%20Flight%20Test%20BT%26amp%3BE&model=787&lineNumber=12&bemsId=9999999&pin=pin1");
            output = await res.Content.ReadAsStringAsync();

            doc = _parser.ParseDocument(output);
            Assert.IsNotNull(doc.GetElementById("work-area-span"));
            Assert.IsNotNull(doc.GetElementsByClassName("custom-select-shield"));
            Assert.IsNotNull(doc.GetElementsByClassName("work-areas-select"));
            Assert.AreEqual(4, doc.GetElementsByClassName("work-area").Length);
            Assert.IsNotNull(doc.GetElementById("check-in-btn").GetAttribute("disabled"));
        }

        //[TestMethod]
        //public void Should_MakeVisitorTabActive_When_PersonTypeIsVisitor()
        //{
        //    _vm.personType = "Visitor";
        //    var htmlAsString = _engine.Parse("CheckInPartial.cshtml", _vm, _viewData as ExpandoObject);

        //    var doc = _parser.Parse(htmlAsString);
        //    var tLabel = doc.GetElementById("travelerOption");
        //    var vLabel = doc.GetElementById("visitorOption");
        //    var hLabel = doc.GetElementById("homeTeamOption");

        //    Assert.IsTrue(vLabel.ClassList.Contains("active"));
        //    Assert.IsFalse(tLabel.ClassList.Contains("active"));
        //    Assert.IsFalse(hLabel.ClassList.Contains("active"));
        //}

        //[TestMethod]
        //public void Should_MakeHomeTeamTabActive_When_PersonTypeIsHomeTeam()
        //{
        //    _vm.personType = "Home Team";
        //    var htmlAsString = _engine.Parse("CheckInPartial.cshtml", _vm, _viewData as ExpandoObject);

        //    var doc = _parser.Parse(htmlAsString);
        //    var tLabel = doc.GetElementById("travelerOption");
        //    var vLabel = doc.GetElementById("visitorOption");
        //    var hLabel = doc.GetElementById("homeTeamOption");

        //    Assert.IsTrue(hLabel.ClassList.Contains("active"));
        //    Assert.IsFalse(tLabel.ClassList.Contains("active"));
        //    Assert.IsFalse(vLabel.ClassList.Contains("active"));
        //}

        //#endregion Active Tab

        //#region Name Or Bems field

        //[TestMethod]
        //public void Should_HaveActiveClassOnNameOrBemsLabel_When_NameOrBemsIsNotNull()
        //{
        //    _vm.nameOrBems = "12345";

        //    //string htmlAsString = _engine.Parse("CheckInPartial.cshtml", _vm, _viewData as ExpandoObject);
        //    var path = File.ReadAllText(Path.Combine(pathToPartial1 + @"\CheckInPartial.cshtml"));
        //    string html = Engine.Razor.RunCompile(Path.Combine(pathToPartial1 + @"/CheckInPartial.cshtml"), typeof(CheckInPartialViewModel), _vm, null);

        //    var doc = _parser.Parse(html);
        //    var tLabel = doc.GetElementById("traveler-nameOrBems-label");
        //    var vLabel = doc.GetElementById("visitor-badge-label");
        //    var vLabel2 = doc.GetElementById("visitor-name-label");
        //    var hLabel = doc.GetElementById("home-nameOrBems-label");

        //    Assert.IsTrue(tLabel.ClassList.Contains("active"));
        //    Assert.IsTrue(vLabel.ClassList.Contains("active"));
        //    Assert.IsTrue(vLabel2.ClassList.Contains("active"));
        //    Assert.IsTrue(hLabel.ClassList.Contains("active"));
        //}

        //[TestMethod]
        //public void Should_NotHaveActiveClassOnNameOrBemsLabel_When_NameOrBemsIsNull()
        //{
        //    _vm.nameOrBems = null;

        //    string htmlAsString = _engine.Parse("CheckInPartial.cshtml", _vm, _viewData as ExpandoObject);

        //    var doc = _parser.Parse(htmlAsString);
        //    var tLabel = doc.GetElementById("traveler-nameOrBems-label");
        //    var vLabel = doc.GetElementById("visitor-badge-label");
        //    var vLabel2 = doc.GetElementById("visitor-name-label");
        //    var hLabel = doc.GetElementById("home-nameOrBems-label");

        //    Assert.IsFalse(tLabel.ClassList.Contains("active"));
        //    Assert.IsFalse(vLabel.ClassList.Contains("active"));
        //    Assert.IsFalse(vLabel2.ClassList.Contains("active"));
        //    Assert.IsFalse(hLabel.ClassList.Contains("active"));
        //}

        //#endregion Name Or Bems field

        //#region Job field

        //[TestMethod]
        //public void Should_HaveActiveClassOnJobLabel_When_JobIsNotNull()
        //{
        //    _vm.jobId = "12345";

        //    string htmlAsString = _engine.Parse("CheckInPartial.cshtml", _vm, _viewData as ExpandoObject);

        //    var doc = _parser.Parse(htmlAsString);
        //    var tLabel = doc.GetElementById("traveler-job-label");
        //    var vLabel = doc.GetElementById("visitor-job-label");
        //    var hLabel = doc.GetElementById("home-job-label");

        //    Assert.IsTrue(tLabel.ClassList.Contains("active"));
        //    Assert.IsTrue(vLabel.ClassList.Contains("active"));
        //    Assert.IsTrue(hLabel.ClassList.Contains("active"));
        //}


        //[TestMethod]
        //public void Should_NotHaveActiveClassOnJobLabel_When_JobIsNull()
        //{
        //    _vm.jobId = null;

        //    string htmlAsString = _engine.Parse("CheckInPartial.cshtml", _vm, _viewData as ExpandoObject);

        //    var doc = _parser.Parse(htmlAsString);
        //    var tLabel = doc.GetElementById("traveler-job-label");
        //    var vLabel = doc.GetElementById("visitor-job-label");
        //    var hLabel = doc.GetElementById("home-job-label");

        //    Assert.IsFalse(tLabel.ClassList.Contains("active"));
        //    Assert.IsFalse(vLabel.ClassList.Contains("active"));
        //    Assert.IsFalse(hLabel.ClassList.Contains("active"));
        //}

        //#endregion Job field

        //#region WorkArea

        //[TestMethod]
        //public void Should_HaveThreeChildren_When_ThereAreTwoWorkAreas()
        //{
        //    _vm.WorkAreas.Clear();
        //    _vm.WorkAreas.Add("Area1");
        //    _vm.WorkAreas.Add("Area2");

        //    var htmlAsString = _engine.Parse("CheckInPartial.cshtml", _vm, _viewData as ExpandoObject);

        //    var doc = _parser.Parse(htmlAsString);
        //    var workAreaList = doc.GetElementById("work-areas-select");

        //    Assert.AreEqual(3, workAreaList.ChildElementCount);
        //}

        //[TestMethod]
        //public void Should_HaveOneChild_When_ThereAreNoWorkAreas()
        //{
        //    _vm.WorkAreas.Clear();

        //    var htmlAsString = _engine.Parse("CheckInPartial.cshtml", _vm, _viewData as ExpandoObject);

        //    var doc = _parser.Parse(htmlAsString);
        //    var workAreaList = doc.GetElementById("work-areas-select");

        //    Assert.AreEqual(1, workAreaList.ChildElementCount);
        //}

        //#endregion WorkArea

        //#region BC Badge Field

        //[TestMethod]
        //public void Should_ShowBCBadgeField_When_NoOneIsLoggedIn()
        //{
        //    _vm.CurrentUser = new Models.CommonModels.User() { BemsId = 0 };

        //    string htmlAsString = _engine.Parse("CheckInPartial.cshtml", _vm, _viewData as ExpandoObject);

        //    var doc = _parser.Parse(htmlAsString);
        //    var bcBadgeField = doc.GetElementById("bc-badge-field");

        //    Assert.IsNotNull(bcBadgeField);
        //}


        //[TestMethod]
        //public void Should_NotShowBCBadgeField_When_TheBCIsLoggedIn()
        //{
        //    _vm.CurrentUser = new Models.CommonModels.User() { BemsId = 0123 };
        //    _vm.assignedBCBems = 0123;

        //    string htmlAsString = _engine.Parse("CheckInPartial.cshtml", _vm, _viewData as ExpandoObject);

        //    var doc = _parser.Parse(htmlAsString);
        //    var bcBadgeField = doc.GetElementById("bc-badge-field");

        //    Assert.IsNull(bcBadgeField);
        //}

        //#endregion

        //#region Reason for Visit
        //[TestMethod]
        //public void Should_Show_Reasons_For_Visit()
        //{
        //    _vm.personType = "Traveler";
        //    string htmlAsString = _engine.Parse("CheckInPartial.cshtml", _vm, _viewData as ExpandoObject);

        //    var doc = _parser.Parse(htmlAsString);
        //    var tLabel = doc.GetElementById("reason-to-visit");

        //    Assert.AreEqual(5, tLabel.ChildElementCount);
        //    Assert.AreEqual("Work", tLabel.Children[0].TextContent);
        //    Assert.AreEqual("Inspection", tLabel.Children[1].TextContent);
        //    Assert.AreEqual("Tour", tLabel.Children[2].TextContent);
        //    Assert.AreEqual("Production Support", tLabel.Children[3].TextContent);
        //    Assert.AreEqual("Presentation", tLabel.Children[4].TextContent);
        //}

        #endregion
    }

}

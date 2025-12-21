namespace Shield.Services.Loto.Tests.EndtoEnd
{
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Newtonsoft.Json;
    using shield.services.loto.models.Loto;
    using Shield.Common;
    using Shield.Common.Constants;
    using Shield.Common.Models.Common;
    using Shield.Common.Models.Loto.Shared;
    using Shield.Common.Models.MyLearning;
    using Shield.Common.Models.MyLearning.Interfaces;
    using Shield.Services.Loto.Data;
    using Shield.Services.Loto.Data.Impl;
    using Shield.Services.Loto.Models.CheckIn;
    using Shield.Services.Loto.Models.Common;
    using Shield.Services.Loto.Models.Loto;
    using Shield.Services.Loto.Services;
    using Shield.Services.Loto.Services.Interfaces;
    using Shield.Services.Loto.ShieldConstants;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Aircraft = Shield.Services.Loto.Models.Common.Aircraft;
    using Isolation = Shield.Services.Loto.Models.Loto.Isolation;
    using LotoTransaction = Shield.Services.Loto.Models.Loto.LotoTransaction;
    using Status = Shield.Services.Loto.Models.Common.Status;

    [TestClass]
    [TestCategory(nameof(TestCategory.EndtoEnd))]
    public class LotoEndToEndTests
    {
        private HttpClient _client;
        private LotoContext _context;
        private DateTime _now;
        private Mock<IHttpClient> _mockHttpClient;
        private Mock<HttpClientService> _mockHttpClientService;
        private Mock<UserService> _mockUserService;
        private Mock<ILotoDAO> _mockILotoDao;
        private Mock<LotoService> _mockLotoService;
        private Mock<IMyLearningDataService> _mockIMyLearningDataService;

        [TestInitialize]
        public void SetUp()
        {
            _mockHttpClient = new Mock<IHttpClient>();
            _mockHttpClientService = new Mock<HttpClientService>();
            _mockUserService = new Mock<UserService>(_mockHttpClientService.Object);
            _mockILotoDao = new Mock<ILotoDAO>();
            _now = DateTime.UtcNow;
            _mockIMyLearningDataService = new Mock<IMyLearningDataService>();

            _mockHttpClientService.Setup(c => c.GetClient()).Returns(_mockHttpClient.Object);

            string databaseName = Guid.NewGuid().ToString();
            var options = new DbContextOptionsBuilder<LotoContext>()
                    .UseInMemoryDatabase(databaseName);

            _context = new LotoContext(options.Options);
            _mockLotoService = new Mock<LotoService>();

            var builder = new WebApplicationFactory<Program>()
                    .WithWebHostBuilder(builder =>
                    {
                        builder.ConfigureServices(services =>
                        {
                            services.AddDbContext<LotoContext>(o => o.UseInMemoryDatabase(databaseName));

                            services.AddTransient<LotoService>(ctx =>
                                new LotoService(new LotoDAO(_context, _mockHttpClientService.Object),
                                new StatusDAO(_context),
                                new LotoTransactionService(new LotoTransactionDAO(_context), new LotoDAO(_context, _mockHttpClientService.Object), new UserService(_mockHttpClientService.Object)),
                                new IsolationService(
                                    new IsolationDAO(_context),
                                    new LotoTransactionService(new LotoTransactionDAO(_context), new LotoDAO(_context, _mockHttpClientService.Object), new UserService(_mockHttpClientService.Object)),
                                    new LotoDAO(_context, _mockHttpClientService.Object)),
                                new DiscreteDAO(_context),
                                new DiscreteService(new DiscreteDAO(_context), new IsolationDAO(_context), new UserService(_mockHttpClientService.Object)),
                                new MyLearningDataService(_mockHttpClientService.Object), new UserService(_mockHttpClientService.Object), new EmailService(new CheckInService(_mockHttpClientService.Object), new UserService(_mockHttpClientService.Object))));

                            services.AddTransient<ILotoTransactionService, LotoTransactionService>(ctx => 
                                new LotoTransactionService(new LotoTransactionDAO(_context),
                                new LotoDAO(_context, _mockHttpClientService.Object), new UserService(_mockHttpClientService.Object)));

                            services.AddTransient<IStatusService, StatusService>(ctx =>
                                new StatusService(new LotoDAO(_context, _mockHttpClientService.Object),
                                new StatusDAO(_context),
                                new DiscreteDAO(_context),
                                new LotoTransactionService(new LotoTransactionDAO(_context), new LotoDAO(_context, _mockHttpClientService.Object), new UserService(_mockHttpClientService.Object)),
                                new IsolationService(
                                    new IsolationDAO(_context),
                                    new LotoTransactionService(new LotoTransactionDAO(_context), new LotoDAO(_context, _mockHttpClientService.Object), new UserService(_mockHttpClientService.Object)),
                                    new LotoDAO(_context, _mockHttpClientService.Object)),
                                new LotoService(new LotoDAO(_context, _mockHttpClientService.Object),
                                new StatusDAO(_context),
                                new LotoTransactionService(new LotoTransactionDAO(_context), new LotoDAO(_context, _mockHttpClientService.Object), new UserService(_mockHttpClientService.Object)),
                                new IsolationService(
                                    new IsolationDAO(_context),
                                    new LotoTransactionService(new LotoTransactionDAO(_context), new LotoDAO(_context, _mockHttpClientService.Object), new UserService(_mockHttpClientService.Object)),
                                    new LotoDAO(_context, _mockHttpClientService.Object)),
                                new DiscreteDAO(_context),
                                new DiscreteService(new DiscreteDAO(_context), new IsolationDAO(_context), new UserService(_mockHttpClientService.Object)),
                                new MyLearningDataService(_mockHttpClientService.Object), new UserService(_mockHttpClientService.Object), new EmailService(new CheckInService(_mockHttpClientService.Object), new UserService(_mockHttpClientService.Object)))));
                        });
                    });

            _client = builder.CreateClient();
            _client.DefaultRequestHeaders.Add("LotoApiKey", Constants.LotoServiceAPIKey);

            EnvironmentHelper.UserServiceAddress = "https://shieldservicesusers.taspre-phx.apps.boeing.com/api/";
        }

        [TestCleanup]
        public void Teardown()
        {
            _context.Dispose();
        }

        [TestMethod]
        public async Task Should_GetLotosByModelAndLine()
        {
            using (_context)
            {
                List<LotoDetails> lotoList = new()
                {
                    new LotoDetails()
                    {
                        Reason = "Anna",
                        LineNumber = "123",
                        Model = "737",
                        WorkPackage = "WP",
                        Discrete = new Models.Discrete.Discrete() { Id = 5 }
                    },
                    new LotoDetails()
                    {
                        CreatedAt = DateTime.Now,
                        Id = 2,
                        LineNumber = "123",
                        Model = "737",
                        Reason = "Because",
                        Site = "Renton",
                        WorkPackage = "12345678",
                        ActiveAEs = new List<LotoAE>() { new LotoAE { AEBemsId = 2519949, LotoId = 1 } }
                    },
                    new LotoDetails()
                    {
                        CreatedAt = DateTime.Now,
                        Id = 3,
                        LineNumber = "123",
                        Model = "747",
                        Reason = "Because",
                        Site = "Renton",
                        WorkPackage = "12345678",
                        ActiveAEs = new List<LotoAE>() { new LotoAE { AEBemsId = 2519949, LotoId = 1 } }
                    }
                };

                List<LotoAssociatedHecps> lotoAssociatedHecps = new()
                {
                    new LotoAssociatedHecps
                    {
                        Ata = "ATA1",
                        HecpTableId= null,
                        HECPTitle = "Discrete",
                        Id= 1,
                        LotoId= 1,
                        LotoIsolationsDiscreteHecp = new List<LotoIsolationsDiscreteHecp>()
                        {
                            new LotoIsolationsDiscreteHecp
                            {
                                Id = 1,
                                InstallDateTime = DateTime.Now,
                                InstalledByBemsId = 123,
                                IsLocked = true,
                                SystemCircuitId = "123",
                                Tag = "456"
                            }
                        }
                    },
                    new LotoAssociatedHecps
                    {
                        Ata = "ATA2",
                        HecpTableId= 1,
                        HECPTitle = "HECP",
                        Id= 2,
                        LotoId= 1,
                        LotoIsolationsDiscreteHecp = Enumerable.Empty<LotoIsolationsDiscreteHecp>().ToList(),
                    }
                };

                List<LotoAssociatedModelData> lotoAssociatedModelDataList = new()
                {
                    new LotoAssociatedModelData
                    {
                        Id = 15,
                        MinorModelId = 20,
                        LotoId = 1
                    },
                    new LotoAssociatedModelData
                    {
                        Id = 16,
                        MinorModelId = null,
                        LotoId = 2
                    },
                    new LotoAssociatedModelData
                    {
                        Id = 17,
                        MinorModelId = 45,
                        LotoId = 1
                    }
                };

                _context.AddRange(lotoList);
                _context.AddRange(lotoAssociatedHecps);
                _context.AddRange(lotoAssociatedModelDataList);
                _context.SaveChanges();

                var response = _client.GetAsync("/api/Loto/Program/737/LineNumber/123");

                string value = await response.Result.Content.ReadAsStringAsync();
                var responseList = JsonConvert.DeserializeObject<HTTPResponseWrapper<List<LotoDetails>>>(value);
                Assert.AreEqual(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
                Assert.AreEqual("LOTOs retrieved", responseList.Message);
                Assert.AreEqual(2, responseList.Data.Count);
                Assert.AreEqual("WP", responseList.Data[0].WorkPackage);
                Assert.AreEqual(5, responseList.Data[0].Discrete.Id);
                Assert.AreEqual(2, responseList.Data[0].LotoAssociatedModelDataList.Count);
                Assert.AreEqual(1, responseList.Data[1].LotoAssociatedModelDataList.Count);
                Assert.AreEqual(lotoAssociatedModelDataList[0].MinorModelId, responseList.Data[0].LotoAssociatedModelDataList[0].MinorModelId);
                Assert.AreEqual(lotoAssociatedModelDataList[2].MinorModelId, responseList.Data[0].LotoAssociatedModelDataList[1].MinorModelId);
                Assert.AreEqual(lotoAssociatedModelDataList[1].MinorModelId, responseList.Data[1].LotoAssociatedModelDataList[0].MinorModelId);
            }
        }

        [TestMethod]
        public async Task Should_GetLotoById()
        {
            using (var context = _context)
            {
                var updatedRecord = context.Add<LotoDetails>(new LotoDetails()
                {
                    Id = 2,
                    Reason = "Anna",
                    LineNumber = "123",
                    Model = "model",
                    WorkPackage = "WP",
                    Status = new Status
                    {
                        Description = "D",
                        DisplayName = "D",
                        Id = 1
                    },
                    Discrete = new Models.Discrete.Discrete() { Id = 5 }
                });

                var id = updatedRecord.Entity.Id;

                context.Add<LotoDetails>(new LotoDetails()
                {
                    Reason = "Dolfo!!!!!",
                    LineNumber = "456",
                    Model = "whatever",
                    WorkPackage = "PW",
                    Status = new Status
                    {
                        Description = "D",
                        DisplayName = "D",
                        Id = 2
                    }
                });

                List <LotoAssociatedModelData> lotoAssociatedModelDataList = new ()
                {
                    new LotoAssociatedModelData
                    {
                        MinorModelId = 15,
                        LotoId = 1
                    },
                    new LotoAssociatedModelData
                    {
                        MinorModelId = 16,
                        LotoId = 2
                    },
                    new LotoAssociatedModelData
                    {
                        MinorModelId = 17,
                        LotoId = 2
                    }
                };
                context.AddRange(lotoAssociatedModelDataList);
                context.SaveChanges();

                var response = _client.GetAsync("/api/Loto/LotoDetail/LotoId/" + id.ToString());

                string value = await response.Result.Content.ReadAsStringAsync();
                var responseWrapper = JsonConvert.DeserializeObject<HTTPResponseWrapper<LotoDetails>>(value);
                Assert.AreEqual(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
                Assert.AreEqual("Success", responseWrapper.Status);
                Assert.AreEqual("", responseWrapper.Message);
                Assert.AreEqual(id, responseWrapper.Data.Id);
                Assert.AreEqual("Anna", responseWrapper.Data.Reason);
                Assert.AreEqual(5, responseWrapper.Data.Discrete.Id);
                Assert.AreEqual(2, responseWrapper.Data.LotoAssociatedModelDataList.Count);
                Assert.AreEqual(16, responseWrapper.Data.LotoAssociatedModelDataList[0].MinorModelId);
                Assert.AreEqual(17, responseWrapper.Data.LotoAssociatedModelDataList[1].MinorModelId);

                context.Dispose();
            }
        }

        [TestMethod]
        public async Task Should_GetLotoById_For_Multiple_Hecps_Old_Loto()
        {
            using (var context = _context)
            {
                List<LotoDetails> lotos = new List<LotoDetails>()
                {
                    new LotoDetails()
                    {
                        Id = 1,
                        HecpTableId = 1,
                        HECPTitle = "HECP1",
                        HECPRevisionLetter ="A",
                        Ata = "ATA1",
                        WorkPackage = "Load"
                    },
                    new LotoDetails()
                    {
                        Id = 2,
                        HecpTableId = 1,
                        HECPTitle = "HECP2",
                        HECPRevisionLetter ="A",
                        Ata = "ATA1",
                        WorkPackage = "Fuel"
                    }
                };

                List<Isolation> isolations = new List<Isolation>()
                {
                    new Isolation()
                    {
                        Id = 1,
                        LotoId = lotos[0].Id,
                        IsLocked = true,
                        InstalledByBemsId = 111,
                        InstallDateTime = DateTime.Now,
                        SystemCircuitId = "1"
                    },
                    new Isolation()
                    {
                        Id= 2,
                        LotoId = lotos[1].Id,
                        IsLocked = true,
                        InstalledByBemsId = 11,
                        InstallDateTime = DateTime.Now,
                        SystemCircuitId = "1",
                    }
                };

                List<LotoAssociatedModelData> lotoAssociatedModelDataList = new()
                {
                    new LotoAssociatedModelData
                    {
                        MinorModelId = 15,
                        LotoId = 2
                    },
                    new LotoAssociatedModelData
                    {
                        MinorModelId = null,
                        LotoId = 1
                    },
                    new LotoAssociatedModelData
                    {
                        MinorModelId = 17,
                        LotoId = 2
                    }
                };
                context.AddRange(lotoAssociatedModelDataList);
                context.AddRange(isolations);
                context.AddRange(lotos);
                context.SaveChanges();

                var response = _client.GetAsync("/api/Loto/LotoDetail/LotoId/" + lotos[0].Id.ToString());

                string value = await response.Result.Content.ReadAsStringAsync();
                var responseWrapper = JsonConvert.DeserializeObject<HTTPResponseWrapper<LotoDetails>>(value);
                Assert.AreEqual(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
                Assert.AreEqual("Success", responseWrapper.Status);
                Assert.AreEqual("", responseWrapper.Message);
                Assert.AreEqual(lotos[0].Id, responseWrapper.Data.Id);
                Assert.AreEqual(lotos[0].WorkPackage, responseWrapper.Data.WorkPackage);
                Assert.IsNull(responseWrapper.Data.HECPTitle);
                Assert.IsNull(responseWrapper.Data.HecpTableId);
                Assert.IsNull(responseWrapper.Data.HECPRevisionLetter);
                Assert.IsNull(responseWrapper.Data.Ata);
                Assert.AreEqual(1, responseWrapper.Data.LotoAssociatedHecps.Count);
                Assert.AreEqual(1, responseWrapper.Data.LotoAssociatedHecps[0].LotoIsolationsDiscreteHecp.Count);
                Assert.AreEqual("ATA1", responseWrapper.Data.LotoAssociatedHecps[0].Ata);
                Assert.AreEqual("HECP1", responseWrapper.Data.LotoAssociatedHecps[0].HECPTitle);
                Assert.AreEqual(1, responseWrapper.Data.LotoAssociatedHecps[0].HecpTableId);
                Assert.AreEqual("A", responseWrapper.Data.LotoAssociatedHecps[0].HECPRevisionLetter);
                Assert.AreEqual(1, responseWrapper.Data.LotoAssociatedHecps[0].LotoId);
                Assert.AreEqual(111, responseWrapper.Data.LotoAssociatedHecps[0].LotoIsolationsDiscreteHecp[0].InstalledByBemsId);
                Assert.AreEqual("1", responseWrapper.Data.LotoAssociatedHecps[0].LotoIsolationsDiscreteHecp[0].SystemCircuitId);
                Assert.AreEqual(1, responseWrapper.Data.LotoAssociatedModelDataList.Count);
                Assert.AreEqual(lotoAssociatedModelDataList[1].MinorModelId, responseWrapper.Data.LotoAssociatedModelDataList[0].MinorModelId);
                context.Dispose();
            }
        }

        [TestMethod]
        public async Task Should_GetLotoById_For_Multiple_Hecps()
        {
            using (var context = _context)
            {
                LotoDetails loto1 = new LotoDetails()
                {
                    CreatedAt = DateTime.Now,
                    Id = 1,
                    LineNumber = "123",
                    Model = "737",
                    Reason = "Because",
                    Site = "Renton",
                    WorkPackage = "WP1",
                    ActiveAEs = new List<LotoAE>() { new LotoAE { AEBemsId = 2519949, LotoId = 1 } },
                    Status = new Status { Id = 2, Description = "Active" },
                    LotoAssociatedModelDataList = new List<LotoAssociatedModelData>()
                    {
                        new LotoAssociatedModelData
                        {
                            Id = 3,
                            MinorModelId = 5
                        }
                    }
                };
                LotoDetails loto2 = new LotoDetails()
                {
                    CreatedAt = DateTime.Now,
                    Id = 2,
                    LineNumber = "123",
                    Model = "737",
                    Reason = "Because",
                    Site = "Renton",
                    WorkPackage = "WP2",
                    ActiveAEs = new List<LotoAE>() { new LotoAE { AEBemsId = 123456, LotoId = 2 } },
                    Status = new Status { Id = 3, Description = "Transfer" },
                };

                List<LotoDetails> lotos = new List<LotoDetails>
                {
                    loto1,
                    loto2
                };

                List<LotoAssociatedHecps> lotoAssociatedHecps = new List<LotoAssociatedHecps>()
                {
                    new LotoAssociatedHecps
                    {
                        Id = 1,
                        Ata = "ATA1",
                        HECPTitle = "HECP1",
                        HECPRevisionLetter = "A",
                        LotoId= 1,
                        HecpTableId = 1
                    },
                    new LotoAssociatedHecps
                    {
                        Id = 2,
                        Ata = "ATA2",
                        HECPTitle = "Dicrete1",
                        HECPRevisionLetter = "NEW",
                        LotoId= 1,
                        HecpTableId = null
                    },
                    new LotoAssociatedHecps
                    {
                        Id = 3,
                        Ata = "ATA2",
                        HECPTitle = "HECP2",
                        HECPRevisionLetter = "A",
                        LotoId= 2,
                        HecpTableId = 1
                    },
                    new LotoAssociatedHecps
                    {
                        Id = 4,
                        Ata = "ATA3",
                        HECPTitle = "Discrete2",
                        HECPRevisionLetter = "NEW",
                        LotoId= 2,
                        HecpTableId = null
                    }
                };

                List<LotoIsolationsDiscreteHecp> lotoIsolationsDiscreteHecps = new List<LotoIsolationsDiscreteHecp>
                {
                    new LotoIsolationsDiscreteHecp
                    {
                        Id = 1,
                        CircuitNomenclature = "A",
                        LotoAssociatedId = 2,
                        SystemCircuitId = "ID1"
                    },
                    new LotoIsolationsDiscreteHecp
                    {
                        Id = 2,
                        CircuitNomenclature = "B",
                        LotoAssociatedId = 2,
                        SystemCircuitId = "ID2"
                    },
                    new LotoIsolationsDiscreteHecp
                    {
                        Id = 3,
                        CircuitNomenclature = "C",
                        LotoAssociatedId = 4,
                        SystemCircuitId = "ID3"
                    },
                    new LotoIsolationsDiscreteHecp
                    {
                        Id = 4,
                        CircuitNomenclature = "D",
                        LotoAssociatedId = 4,
                        SystemCircuitId = "ID4"
                    }
                };

                context.AddRange(lotos);
                context.AddRange(lotoAssociatedHecps);
                context.AddRange(lotoIsolationsDiscreteHecps);
                context.SaveChanges();

                var response = _client.GetAsync("/api/Loto/LotoDetail/LotoId/" + lotos[0].Id.ToString());

                string value = await response.Result.Content.ReadAsStringAsync();
                var responseWrapper = JsonConvert.DeserializeObject<HTTPResponseWrapper<LotoDetails>>(value);
                Assert.AreEqual(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
                Assert.AreEqual("Success", responseWrapper.Status);
                Assert.AreEqual("", responseWrapper.Message);
                VerifyLoto(lotos[0], responseWrapper.Data);
                VerifyLotoAssociatedHecps(lotoAssociatedHecps[0], responseWrapper.Data.LotoAssociatedHecps[0]);
                VerifyLotoAssociatedHecps(lotoAssociatedHecps[1], responseWrapper.Data.LotoAssociatedHecps[1]);
                VerifyLotoIsolationsDiscreteHecps(lotoIsolationsDiscreteHecps[0], responseWrapper.Data.LotoAssociatedHecps[1].LotoIsolationsDiscreteHecp[0]);
                VerifyLotoIsolationsDiscreteHecps(lotoIsolationsDiscreteHecps[1], responseWrapper.Data.LotoAssociatedHecps[1].LotoIsolationsDiscreteHecp[1]);
                Assert.AreEqual(lotos[0].LotoAssociatedModelDataList[0].MinorModelId, responseWrapper.Data.LotoAssociatedModelDataList[0].MinorModelId);

                context.Dispose();
            }
        }

        [TestMethod]
        public async Task Should_UpdateTheLotoTransactionWithGCTransaction()
        {
            using (var context = _context)
            {
                var updatedRecord = context.Add<LotoDetails>(new LotoDetails()
                {
                    Reason = "Anna",
                    LineNumber = "123",
                    Model = "model",
                    WorkPackage = "WP",
                    Status = new Status
                    {
                        Description = "Active",
                        DisplayName = "Active",
                        Id = 2
                    }
                });
                var id = updatedRecord.Entity.Id;

                LotoTransaction transaction = context.Add<LotoTransaction>(new LotoTransaction()
                {
                    Action = "action1",
                    Date = _now,
                    LotoId = id,
                    Loto = updatedRecord.Entity
                }).Entity;

                context.SaveChanges();

                UpdateGCRequest req = new UpdateGCRequest()
                {
                    LineNumber = "123",
                    Program = "model",
                    PreviousGCBemsId = 1111,
                    NextGCBemsId = 2222
                };

                StringContent content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");

                // Mock User service call
                Shield.Common.Models.Users.User user1 = new Shield.Common.Models.Users.User
                {

                    BemsId = 1111,
                    DisplayName = "User Name",
                    FirstName = "User",
                    LastName = "Name",
                    RoleId = 1
                };
                GetResponse<Shield.Common.Models.Users.User> response1 = new GetResponse<Shield.Common.Models.Users.User>()
                {
                    data = user1
                };
                var resMessage1 = new HttpResponseMessage
                {
                    Content = new StringContent(JsonConvert.SerializeObject(response1), Encoding.UTF8, "application/json"),
                    StatusCode = System.Net.HttpStatusCode.OK
                };

                Uri uri1 = new Uri(EnvironmentHelper.UserServiceAddress + "Users/1111");
                _mockHttpClient.Setup(hc => hc.GetAsync(uri1)).ReturnsAsync(resMessage1);

                Shield.Common.Models.Users.User user2 = new Shield.Common.Models.Users.User
                {
                    BemsId = 2222,
                    DisplayName = "User Name",
                    FirstName = "User",
                    LastName = "Name",
                    RoleId = 1
                };
                GetResponse<Common.Models.Users.User> response2 = new GetResponse<Common.Models.Users.User>()
                {
                    data = user2
                };
                var resMessage2 = new HttpResponseMessage
                {
                    Content = new StringContent(JsonConvert.SerializeObject(response2), Encoding.UTF8, "application/json"),
                    StatusCode = System.Net.HttpStatusCode.OK
                };

                Uri uri2 = new Uri(EnvironmentHelper.UserServiceAddress + "Users/2222");
                _mockHttpClient.Setup(hc => hc.GetAsync(uri2)).ReturnsAsync(resMessage2);

                var response = _client.PostAsync("/api/LotoTransaction/GCTransaction/", content);

                string value = await response.Result.Content.ReadAsStringAsync();
                bool result = JsonConvert.DeserializeObject<bool>(value);

                Assert.AreEqual(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
                Assert.AreEqual(true, result);

                List<LotoTransaction> transactions = context.LotoTransaction.ToList().OrderBy(t => t.Date).ToList();

                Assert.AreEqual(3, transactions.Count);

                Assert.AreEqual("User Name (1111) has left this Line #123 as GC", transactions[1].Action);

                Assert.AreEqual("User Name (2222) Claimed Responsibility for this Line #123 and its LOTOs as Group Coordinator", transactions[2].Action);


                context.Dispose();
            }
        }

        [TestMethod]
        public async Task Should_SaveNewLoto()
        {
            using (var context = _context)
            {
                var newLoto = new Shield.Services.Loto.Models.Common.CreateLotoRequest()
                {
                    Site = "Renton",
                    Model = "747",
                    LineNumber = "123",
                    CreatedByBemsId = 222,
                    CreatedByName = "Uncle Bobby",
                    WorkPackage = "WP",
                    Reason = "coz",
                    AssociatedMinorModelIdList =  new List<int>{10, 12}
                };
                var content = new StringContent(JsonConvert.SerializeObject(newLoto), Encoding.UTF8, "application/json");

                var response = _client.PostAsync("/api/Loto", content);
                string value = await response.Result.Content.ReadAsStringAsync();
                var responseWrapper = JsonConvert.DeserializeObject<HTTPResponseWrapper<LotoDetails>>(value);

                // We need IActionResult to return different StatusCode
                //Assert.AreEqual(System.Net.HttpStatusCode.Created, response.Result.StatusCode);
                Assert.AreEqual("Success", responseWrapper.Status);
                Assert.AreEqual("Created new LOTO.", responseWrapper.Message);
                Assert.AreEqual("Renton", responseWrapper.Data.Site);
                Assert.AreEqual("WP", responseWrapper.Data.WorkPackage);

                var lotoList = await context.LotoData.ToListAsync();
                Assert.AreEqual(1, lotoList.Count);
                Assert.AreEqual("WP", lotoList[0].WorkPackage);
                Assert.AreEqual(newLoto.AssociatedMinorModelIdList[0], lotoList[0].LotoAssociatedModelDataList[0].MinorModelId);
                Assert.AreEqual(newLoto.AssociatedMinorModelIdList[1], lotoList[0].LotoAssociatedModelDataList[1].MinorModelId);
                context.Dispose();
            }
        }

        [TestMethod]
        public async Task Should_SaveNewLotoFromDiscrete_And_UpdateDiscreteStatusToAssignedToLoto()
        {
            using (var context = _context)
            {
                context.Add(new Status() { Id = 20, Description = Shield.Common.Constants.Status.ASSIGNED_TO_LOTO_DESCRIPTION });
                context.Add(new Models.Discrete.Discrete() { Id = 5 });
                context.SaveChanges();

                var newLoto = new Shield.Services.Loto.Models.Common.CreateLotoRequest()
                {
                    CreatedByBemsId = 222,
                    CreatedByName = "Uncle Bobby",
                    AssociatedMinorModelIdList = new List<int>{10},
                    Discrete = new Shield.Common.Models.Loto.Shared.Discrete()
                    {
                        Id = 5
                    }
                };

                var content = new StringContent(JsonConvert.SerializeObject(newLoto), Encoding.UTF8, "application/json");

                var response = _client.PostAsync("/api/Loto/FromDiscrete", content);
                string value = await response.Result.Content.ReadAsStringAsync();
                var responseWrapper = JsonConvert.DeserializeObject<HTTPResponseWrapper<LotoDetails>>(value);

                LotoDetails result = responseWrapper.Data;

                Assert.AreEqual("Success", responseWrapper.Status);
                Assert.AreEqual("Created new LOTO from Discrete.", responseWrapper.Message);
                Assert.IsNotNull(responseWrapper.Data.Discrete);

                Assert.AreEqual(Shield.Common.Constants.Status.ASSIGNED_TO_LOTO_DESCRIPTION, result.Discrete.Status.Description);

                context.Dispose();
            }
        }

        [TestMethod]
        public async Task Should_UpdateExistingLoto()
        {
            using (var context = _context)
            {

                LotoDetails oldLoto = new LotoDetails()
                {
                    Reason = "why this is a post",
                    WorkPackage = "WP",
                    CreatedAt = DateTime.Now,
                    LineNumber = "333",
                    LotoAssociatedHecps= new List<LotoAssociatedHecps>(),
                    LotoAssociatedModelDataList = new List<LotoAssociatedModelData>()
                };
                var updatedRecord = context.LotoData.Add(oldLoto);
                
                context.SaveChanges();
                context.Entry<LotoDetails>(oldLoto).State = EntityState.Detached;

                LotoDetails newLoto = new LotoDetails()
                {
                    Id = updatedRecord.Entity.Id,
                    Reason = "it should be a put",
                    WorkPackage = "WP",
                    CreatedAt = DateTime.Now,
                    LineNumber = "333",
                    LotoAssociatedHecps = new List<LotoAssociatedHecps>(),
                    LotoAssociatedModelDataList = new List<LotoAssociatedModelData>()
                };

                StringContent content = new StringContent(JsonConvert.SerializeObject(newLoto), Encoding.UTF8, "application/json");
                var response = _client.PutAsync("api/Loto", content);

                string value = await response.Result.Content.ReadAsStringAsync();
                var responseWrapper = JsonConvert.DeserializeObject<HTTPResponseWrapper<LotoDetails>>(value);

                var updatedLoto = responseWrapper.Data;

                // We need IActionResult to return different StatusCode
                //Assert.AreEqual(System.Net.HttpStatusCode.Created, response.Result.StatusCode);
                Assert.AreEqual("LOTO for Line 333 has been updated!", responseWrapper.Message);

                Assert.AreEqual("it should be a put", updatedLoto.Reason);
                Assert.AreEqual(updatedRecord.Entity.Id, updatedLoto.Id);

                var lotoList = await context.LotoData.ToListAsync();

                var getResponse = _client.GetAsync("/api/Loto/LotoDetail/LotoId/" + updatedRecord.Entity.Id.ToString());

                var getValue = await getResponse.Result.Content.ReadAsStringAsync();
                var getResponseWrapper = JsonConvert.DeserializeObject<HTTPResponseWrapper<LotoDetails>>(getValue);

                Assert.AreEqual(updatedRecord.Entity.Id, getResponseWrapper.Data.Id);
                Assert.AreEqual("it should be a put", getResponseWrapper.Data.Reason);

                context.Dispose();
            }
        }

        [TestMethod]
        public async Task Should_UpdateExistingLoto_With_Multiple_Hecps()
        {
            using (var context = _context)
            {
                LotoDetails oldLoto = new LotoDetails()
                {
                    CreatedAt = DateTime.Now,
                    Id = 1,
                    LineNumber = "123",
                    Model = "737",
                    Reason = "Because",
                    Site = "Renton",
                    WorkPackage = "WP1",
                    ActiveAEs = new List<LotoAE>() { new LotoAE { AEBemsId = 2519949, LotoId = 1 } }
                };
                List<LotoAssociatedHecps> lotoAssociatedHecps = new List<LotoAssociatedHecps>()
                {
                    new LotoAssociatedHecps
                    {
                        Id = 1,
                        Ata = "ATA1",
                        HECPTitle = "HECP1",
                        HECPRevisionLetter = "A",
                        LotoId= 1,
                        HecpTableId = 1
                    },
                    new LotoAssociatedHecps
                    {
                        Id = 2,
                        Ata = "ATA2",
                        HECPTitle = "Dicrete1",
                        HECPRevisionLetter = "NEW",
                        LotoId= 1,
                        HecpTableId = null
                    },
                    new LotoAssociatedHecps
                    {
                        Id = 5,
                        Ata = "ATA5",
                        HECPTitle = "HECP5",
                        HECPRevisionLetter = "A",
                        LotoId= 1,
                        HecpTableId = 5
                    },
                };
                List<LotoIsolationsDiscreteHecp> lotoIsolationsDiscreteHecps = new List<LotoIsolationsDiscreteHecp>
                {
                    new LotoIsolationsDiscreteHecp
                    {
                        Id = 1,
                        CircuitNomenclature = "A",
                        LotoAssociatedId = 2,
                        SystemCircuitId = "ID1"
                    },
                    new LotoIsolationsDiscreteHecp
                    {
                        Id = 2,
                        CircuitNomenclature = "B",
                        LotoAssociatedId = 2,
                        SystemCircuitId = "ID2"
                    }
                };
                List<Models.Hecp.HecpIsolationTag> hecpIsolationTags = new List<Models.Hecp.HecpIsolationTag>
                {
                    new Models.Hecp.HecpIsolationTag
                    {
                        Id = 1,
                        CircuitName = "Circuit1",
                        CircuitPanel ="Panel1",
                        LotoId = 1,
                        HecpId = 1
                    },
                    new Models.Hecp.HecpIsolationTag
                    {
                        Id = 2,
                        CircuitName = "Circuit1",
                        CircuitPanel ="Panel1",
                        LotoId = 1,
                        HecpId = 5
                    }
                };

                context.Add(oldLoto);
                context.AddRange(lotoAssociatedHecps);
                context.AddRange(lotoIsolationsDiscreteHecps);
                context.AddRange(hecpIsolationTags);
                context.SaveChanges();
                context.Entry<LotoDetails>(oldLoto).State = EntityState.Detached;

                LotoDetails newLoto = new LotoDetails()
                {
                    CreatedAt = DateTime.Now,
                    Id = 1,
                    LineNumber = "123",
                    Model = "737",
                    Reason = "Because",
                    Site = "Renton",
                    WorkPackage = "WP1",
                    ActiveAEs = new List<LotoAE>() { new LotoAE { AEBemsId = 2519949, LotoId = 1 } },
                    LotoAssociatedHecps = new List<LotoAssociatedHecps>
                    {
                        new LotoAssociatedHecps
                        {
                            Id = 1,
                            Ata = "ATA1",
                            HECPTitle = "HECP1",
                            HECPRevisionLetter = "A",
                            LotoId= 1,
                            HecpTableId = 1
                        },
                        new LotoAssociatedHecps
                        {
                            Ata = "ATA3",
                            HECPTitle = "Dicrete2",
                            HECPRevisionLetter = "NEW",
                            LotoId= 1,
                            HecpTableId = null                          
                        },
                        new LotoAssociatedHecps
                        {
                            Ata = "ATA4",
                            HECPTitle = "HECP2",
                            HECPRevisionLetter = "B",
                            LotoId= 1,
                            HecpTableId = 2
                        }
                    }
                };

                StringContent content = new StringContent(JsonConvert.SerializeObject(newLoto), Encoding.UTF8, "application/json");
                var response = _client.PutAsync("api/Loto", content);

                string value = await response.Result.Content.ReadAsStringAsync();
                var responseWrapper = JsonConvert.DeserializeObject<HTTPResponseWrapper<LotoDetails>>(value);

                var updatedLoto = responseWrapper.Data;

                // We need IActionResult to return different StatusCode
                Assert.AreEqual("LOTO for Line 123 has been updated!", responseWrapper.Message);

                Assert.AreEqual("Because", updatedLoto.Reason);
                Assert.AreEqual(oldLoto.Id, updatedLoto.Id);
                VerifyLotoAssociatedHecps(newLoto.LotoAssociatedHecps[0], updatedLoto.LotoAssociatedHecps[0]);
                VerifyLotoAssociatedHecps(newLoto.LotoAssociatedHecps[1], updatedLoto.LotoAssociatedHecps[1]);
                VerifyLotoAssociatedHecps(newLoto.LotoAssociatedHecps[2], updatedLoto.LotoAssociatedHecps[2]);

                var lotoList = await context.LotoData.ToListAsync();

                var getResponse = _client.GetAsync("/api/Loto/LotoDetail/LotoId/" + oldLoto.Id.ToString());

                var getValue = await getResponse.Result.Content.ReadAsStringAsync();
                var getResponseWrapper = JsonConvert.DeserializeObject<HTTPResponseWrapper<LotoDetails>>(getValue);

                Assert.AreEqual(oldLoto.Id, getResponseWrapper.Data.Id);
                Assert.AreEqual("Because", getResponseWrapper.Data.Reason);
                Assert.IsNull(_context.HecpIsolationTag.Where(i => i.LotoId == 1 && i.HecpId == 5).FirstOrDefault());

                context.Dispose();
            }
        }

        [TestMethod]
        public async Task Should_SignPAETOExistingLoto()
        {
            using (var context = _context)
            {

                LotoDetails oldLoto = new LotoDetails()
                {
                    Reason = "why this is a post",
                    WorkPackage = "WP",
                    CreatedAt = DateTime.Now,
                    LineNumber = "333",
                    Status = new Status { Id = 1, DisplayName = "Needs Lockout", Description = "NeedsLockout" }
                };
                var updatedRecord = context.Add(oldLoto);
                context.SaveChanges();

                var signInPAERequest = new Shield.Common.Models.Loto.SigningPAERequest()
                {
                    BemsId = 555,
                    LotoId = updatedRecord.Entity.Id
                };
                TrainingInfo trainingInfo = new()
                {
                    BemsId = 2519949,
                    MyLearningDataResponse = new List<IMyLearningDataResponse>()
                {
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.ANNUAL_REQUIRED_LOTO_FIELD_OBSERVATION_TRAINING,
                        IsTrainingValid = true
                    },
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_CONTROL,
                        IsTrainingValid = true
                    }
                }
                };
                StringContent content = new StringContent(JsonConvert.SerializeObject(signInPAERequest), Encoding.UTF8, "application/json");
                var response = _client.PostAsync("api/Loto/PAESignIn", content);
                _mockIMyLearningDataService.Setup(s => s.GetMyLearningDataAsync(It.IsAny<int>(), It.IsAny<List<string>>())).ReturnsAsync(trainingInfo);

                string value = await response.Result.Content.ReadAsStringAsync();
                var responseWrapper = JsonConvert.DeserializeObject<HTTPResponseWrapper<LotoDetails>>(value);

                var updatedLoto = responseWrapper.Data;

                Assert.AreEqual(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
                Assert.AreEqual($"Signed in {signInPAERequest.PAEName} as an PAE.", responseWrapper.Message);

                context.Dispose();
            }
        }

        [TestMethod]
        public async Task Should_SignAEToExistingLoto()
        {
            using (var context = _context)
            {
                List<MyLearningDataResponse> myLearningDataResponse = new List<MyLearningDataResponse>
                {
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_CONTROL,
                        IsTrainingValid = true
                    }
                };
                HTTPResponseWrapper<List<MyLearningDataResponse>> httpResponseWrapper = new HTTPResponseWrapper<List<MyLearningDataResponse>>
                {
                    Data = myLearningDataResponse,
                    Message = "The training is valid",
                    Status = "200"
                };

                HttpResponseMessage httpResponse = new HttpResponseMessage
                {
                    Content = new StringContent(JsonConvert.SerializeObject(httpResponseWrapper))
                };
                TrainingInfo trainingInfo = new()
                {
                    BemsId = 2519949,
                    MyLearningDataResponse = new List<IMyLearningDataResponse>()
                {
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.ANNUAL_REQUIRED_LOTO_FIELD_OBSERVATION_TRAINING,
                        IsTrainingValid = true
                    },
                    new MyLearningDataResponse
                    {
                        CertCode = TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_CONTROL,
                        IsTrainingValid = false
                    }
                }
                };

                Environment.SetEnvironmentVariable("EXTERNAL_SERVICE_URL", "https://shieldservicesexternal.taspre-phx.apps.boeing.com/api/");

                _mockHttpClient.Setup(s => s.GetAsync(It.IsAny<Uri>())).ReturnsAsync(httpResponse);
                _mockHttpClientService.Setup(s => s.GetClient()).Returns(_mockHttpClient.Object);

                LotoDetails loto = new LotoDetails()
                {
                    Reason = "why this is a post",
                    WorkPackage = "WP",
                    CreatedAt = DateTime.Now,
                    LineNumber = "333"
                };

                var updatedRecord = context.Add(loto);
                context.SaveChanges();
                int lotoId = updatedRecord.Entity.Id;

                var signInAERequest = new Shield.Common.Models.Loto.SigningAERequest()
                {
                    AEBemsId = 18888,
                    AEName = "Jesus",
                    LotoId = lotoId
                };

                StringContent content = new StringContent(JsonConvert.SerializeObject(signInAERequest), Encoding.UTF8, "application/json");
                var response = _client.PostAsync("api/Loto/AESignIn", content);
                _mockIMyLearningDataService.Setup(s => s.GetMyLearningDataAsync(It.IsAny<int>(), It.IsAny<List<string>>())).ReturnsAsync(trainingInfo);

                var value = await response.Result.Content.ReadAsStringAsync();
                HTTPResponseWrapper<LotoAE> responseWrapper = JsonConvert.DeserializeObject<HTTPResponseWrapper<LotoAE>>(value);

                Assert.AreEqual(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
                Assert.AreEqual($"Signed in {signInAERequest.AEName} as an AE.", responseWrapper.Message);

                context.Dispose();
            }
        }

        [TestMethod]
        public async Task Should_SignOutAEFromExistingLoto()
        {
            using (var context = _context)
            {
                LotoAE ae = new LotoAE
                {
                    AEBemsId = 123
                };
                LotoAE ae2 = new LotoAE
                {
                    AEBemsId = 1234
                };

                LotoDetails loto = new LotoDetails()
                {
                    Reason = "why this is a post",
                    WorkPackage = "WP",
                    CreatedAt = DateTime.Now,
                    LineNumber = "333"
                };

                var updatedRecord = context.Add(loto);
                context.SaveChanges();
                int lotoId = updatedRecord.Entity.Id;

                ae.LotoId = lotoId;
                ae2.LotoId = lotoId;

                context.Add(ae);
                context.Add(ae2);
                context.SaveChanges();

                Shield.Common.Models.Loto.SigningAERequest request = new Shield.Common.Models.Loto.SigningAERequest()
                {
                    AEBemsId = 123,
                    LotoId = lotoId
                };
                
                StringContent content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

                var response = _client.PutAsync("api/Loto/AESignOut", content);

                var value = await response.Result.Content.ReadAsStringAsync();
                var newListOfAEs = JsonConvert.DeserializeObject<List<LotoAE>>(value);

                //// We need IActionResult to return different StatusCode
                Assert.AreEqual(1, newListOfAEs.Count);

                var aeResponse = _client.GetAsync("api/Loto/LotoAEs/LotoId/" + lotoId);
                var aeValue = await aeResponse.Result.Content.ReadAsStringAsync();
                var aeResponseWrapper = JsonConvert.DeserializeObject<HTTPResponseWrapper<List<LotoAE>>>(aeValue);

                List<LotoAE> aeList = aeResponseWrapper.Data;

                Assert.IsNull(aeList.Find(a => a.AEBemsId == 123));

                context.Dispose();

            }
        }

        [TestMethod]
        public void Should_ReturnError_When_SigningOutAnAEThatIsNotOnTheLoto()
        {
            using (var context = _context)
            {
                LotoDetails loto = new LotoDetails()
                {
                    Reason = "why this is a post",
                    WorkPackage = "WP",
                    CreatedAt = DateTime.Now,
                    LineNumber = "333"
                };

                var updatedRecord = context.Add(loto);
                context.SaveChanges();
                int lotoId = updatedRecord.Entity.Id;

                Shield.Common.Models.Loto.SigningAERequest request = new Shield.Common.Models.Loto.SigningAERequest()
                {
                    AEBemsId = 123,
                    LotoId = lotoId
                };

                StringContent content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

                var response = _client.PutAsync("api/Loto/AESignOut", content);

                Assert.AreEqual(System.Net.HttpStatusCode.InternalServerError, response.Result.StatusCode);

                context.Dispose();
            }
        }

        [TestMethod]
        public async Task Should_ChangeTheStatusToTransfer()
        {
            using (var context = _context)
            {
                LotoDetails loto = new LotoDetails()
                {
                    Reason = "why this is a post",
                    WorkPackage = "WP",
                    CreatedAt = _now,
                    LineNumber = "33"
                };

                Status status = new Status
                {
                    Description = "Transfer",
                    DisplayName = "Transfer"
                };

                var updatedRecord = context.Add(loto);
                context.SaveChanges();

                context.Add(status);
                context.SaveChanges();

                int lotoId = updatedRecord.Entity.Id;
                Common.Models.Loto.StatusChangeRequest req = new Common.Models.Loto.StatusChangeRequest()
                {
                    BemsId = 1234,
                    DisplayName = "First Last",
                    Id = lotoId
                };

                StringContent content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
                var response = _client.PostAsync("api/Status/Transfer", content);

                var value = await response.Result.Content.ReadAsStringAsync();
                LotoDetails result = JsonConvert.DeserializeObject<LotoDetails>(value);

                Assert.AreEqual("why this is a post", result.Reason);
                Assert.AreEqual("WP", result.WorkPackage);
                Assert.AreEqual(_now, result.CreatedAt);
                Assert.AreEqual("33", result.LineNumber);
                Assert.AreEqual("Transfer", result.Status.Description);
                Assert.AreEqual("Transfer", result.Status.DisplayName);
                Assert.AreEqual(1, result.Status.Id);

                context.Dispose();
            }
        }

        [TestMethod]
        public async Task Should_ChangeTheStatusToActive()
        {
            using (var context = _context)
            {
                LotoDetails loto = new LotoDetails()
                {
                    Reason = "why this is a post",
                    WorkPackage = "WP",
                    CreatedAt = _now,
                    LineNumber = "333",
                    Status = new Status()
                };

                Status status = new Status
                {
                    Description = "Active",
                    DisplayName = "Active"
                };

                var updatedRecord = context.Add(loto);
                context.SaveChanges();
                context.Entry<LotoDetails>(loto).State = EntityState.Detached;

                context.Add(status);
                context.SaveChanges();
                
                context.Entry<Status>(status).State = EntityState.Detached;

                int lotoId = updatedRecord.Entity.Id;

                Shield.Common.Models.Loto.StatusChangeRequest req = new Shield.Common.Models.Loto.StatusChangeRequest()
                {
                    BemsId = 1234,
                    DisplayName = "First Last",
                    Id = lotoId
                };

                StringContent content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
                var response = _client.PostAsync("api/Status/Lockout", content);

                var value = await response.Result.Content.ReadAsStringAsync();
                LotoDetails result = JsonConvert.DeserializeObject<LotoDetails>(value);

                Assert.AreEqual("why this is a post", result.Reason);
                Assert.AreEqual("WP", result.WorkPackage);
                Assert.AreEqual(_now, result.CreatedAt);
                Assert.AreEqual("333", result.LineNumber);
                Assert.AreEqual("Active", result.Status.Description);
                Assert.AreEqual("Active", result.Status.DisplayName);

                context.Dispose();
            }
        }

        [TestMethod]
        public async Task Should_ReturnCountOfLotosThatAreNotOfStatusCompleted()
        {
            using (var context = _context)
            {
                LotoDetails loto1 = new LotoDetails()
                {
                    Reason = "why this is a post",
                    WorkPackage = "WP",
                    CreatedAt = _now,
                    LineNumber = "333",
                    Model = "777",
                    Status = new Status()
                    {
                        Description = "Not Completed",
                        Id = 1,
                        DisplayName = "Not Completed"
                    }
                };

                LotoDetails loto2 = new LotoDetails()
                {
                    Reason = "why this is a post",
                    WorkPackage = "WP",
                    CreatedAt = _now,
                    LineNumber = "333",
                    Model = "777",
                    Status = new Status()
                    {
                        Description = "Completed",
                        Id = 2,
                        DisplayName = "Completed"
                    }
                };

                LotoDetails loto3 = new LotoDetails()
                {
                    Reason = "why this is a post",
                    WorkPackage = "WP",
                    CreatedAt = _now,
                    LineNumber = "333",
                    Model = "777",
                    Status = new Status()
                    {
                        Description = "Active",
                        Id = 3,
                        DisplayName = "Active"
                    }
                };

                List<Aircraft> aircraftList = new List<Aircraft>()
                {
                    new Aircraft
                    {
                        Id = 10,
                        Model = "777",
                        LineNumber = "333"
                    },
                    new Aircraft
                    {
                        Id = 11,
                        Model = "787",
                        LineNumber = "222"
                    }
                };


                context.Add(loto1);
                context.Add(loto2);
                context.Add(loto3);
                context.SaveChanges();

                StringContent content = new StringContent(JsonConvert.SerializeObject(aircraftList), Encoding.UTF8, "application/json");
                var response = _client.PostAsync("api/Loto/AirplaneLotoCount", content);

                var value = await response.Result.Content.ReadAsStringAsync();
                Dictionary<int, int> lotoCountDict = JsonConvert.DeserializeObject<Dictionary<int, int>> (value);

                Assert.AreEqual(1, lotoCountDict[10]);
                Assert.AreEqual(0, lotoCountDict[11]);

                context.Dispose();
            }
        }

        [TestMethod]
        public async Task Should_GetActiveIsolationsByModelAndLine()
        {
            using (var context = _context)
            {
                var lotoUpdated1 = context.Add<LotoDetails>(new LotoDetails()
                {
                    Reason = "Anna",
                    LineNumber = "123",
                    Model = "model",
                    WorkPackage = "WP1",
                    Status = new Status { Id = 1, Description = "NeedsLockout" },
                    HECPTitle = "Title1"
                }).Entity;

                context.Add<Isolation>(new Isolation()
                {
                    LotoId = lotoUpdated1.Id,
                    IsLocked = true,
                    InstalledByBemsId = 111,
                    InstallDateTime = DateTime.Now,
                    SystemCircuitId = "1",
                });

                var lotoUpdated2 = context.Add<LotoDetails>(new LotoDetails()
                {
                    Reason = "Anna",
                    LineNumber = "123",
                    Model = "model",
                    WorkPackage = "WP2",
                    Status = new Status { Id = 3, Description = "Transfer" },
                    HECPTitle = "Title2"
                }).Entity;

                context.SaveChanges();

                context.Add<Isolation>(new Isolation()
                {
                    LotoId = lotoUpdated2.Id,
                    IsLocked = true,
                    InstalledByBemsId = 222,
                    InstallDateTime = DateTime.Now,
                    SystemCircuitId = "2"
                });

                var lotoUpdated3 = context.Add<LotoDetails>(new LotoDetails()
                {
                    Reason = "Anna",
                    LineNumber = "123",
                    Model = "model",
                    WorkPackage = "WP3",
                    Status = new Status { Id = 2, Description = "Active" },
                    HECPTitle = "Title3"
                }).Entity;

                context.Add<Isolation>(new Isolation()
                {
                    LotoId = lotoUpdated3.Id,
                    IsLocked = true,
                    InstalledByBemsId = 333,
                    InstallDateTime = DateTime.Now,
                    SystemCircuitId = "3"
                });

                var lotoUpdated4 = context.Add<LotoDetails>(new LotoDetails()
                {
                    Reason = "Anna",
                    LineNumber = "123",
                    Model = "model",
                    WorkPackage = "WP4",
                    Status = new Status { Id = 4, Description = "Completed" },
                    HECPTitle = "Title4"
                }).Entity;

                context.Add<Isolation>(new Isolation()
                {
                    LotoId = lotoUpdated4.Id,
                    IsLocked = true,
                    InstalledByBemsId = 444,
                    InstallDateTime = DateTime.Now,
                    SystemCircuitId = "4"
                });

                context.SaveChanges();

                var response = _client.GetAsync("/api/Loto/ActiveIsolations/Program/model/LineNumber/123");

                string value = await response.Result.Content.ReadAsStringAsync();
                List<LotoDetails> result = JsonConvert.DeserializeObject<List<LotoDetails>>(value);

                Assert.AreEqual(System.Net.HttpStatusCode.OK, response.Result.StatusCode);

                Assert.AreEqual(2, result.Count);

                Assert.IsTrue(result.Exists(v => v.Isolations.Exists(i => i.SystemCircuitId.Equals("2"))), "System 2 is not in the List");
                Assert.IsTrue(result.Exists(v => v.Isolations.Exists(i => i.SystemCircuitId.Equals("3"))), "System 3 is not in the List");

                Assert.IsFalse(result.Exists(v => v.Isolations.Exists(i => i.SystemCircuitId.Equals("1"))), "System 1 is in the List");
                Assert.IsFalse(result.Exists(v => v.Isolations.Exists(i => i.SystemCircuitId.Equals("4"))), "System 4 is in the List");

                context.Dispose();
            }
        }

        [TestMethod]
        public async Task Should_GetActiveIsolationsByModelAndLine_For_MultipleHecps()
        {
            using (var context = _context)
            {
                LotoDetails loto1 = new LotoDetails()
                {
                    CreatedAt = DateTime.Now,
                    Id = 1,
                    LineNumber = "123",
                    Model = "737",
                    Reason = "Because",
                    Site = "Renton",
                    WorkPackage = "WP1",
                    ActiveAEs = new List<LotoAE>() { new LotoAE { AEBemsId = 2519949, LotoId = 1 } },
                    Status = new Status { Id = 2, Description = "Active" },
                };
                LotoDetails loto2 = new LotoDetails()
                {
                    CreatedAt = DateTime.Now,
                    Id = 2,
                    LineNumber = "123",
                    Model = "737",
                    Reason = "Because",
                    Site = "Renton",
                    WorkPackage = "WP2",
                    ActiveAEs = new List<LotoAE>() { new LotoAE { AEBemsId = 123456, LotoId = 2 } },
                    Status = new Status { Id = 3, Description = "Transfer" },
                };
                LotoDetails loto3 = new LotoDetails()
                {
                    CreatedAt = DateTime.Now,
                    Id = 3,
                    LineNumber = "123",
                    Model = "737",
                    Reason = "Because",
                    Site = "Renton",
                    WorkPackage = "WP3",
                    ActiveAEs = new List<LotoAE>() { new LotoAE { AEBemsId = 987654, LotoId = 3 } },
                    Status = new Status { Id = 1, Description = "NeedsLockout" },
                };

                List<LotoDetails> lotos = new List<LotoDetails>
                {
                    loto1,
                    loto2,
                    loto3
                };

                List<LotoAssociatedHecps> lotoAssociatedHecps = new List<LotoAssociatedHecps>()
                {
                    new LotoAssociatedHecps
                    {
                        Id = 1,
                        Ata = "ATA1",
                        HECPTitle = "HECP1",
                        HECPRevisionLetter = "A",
                        LotoId= 1,
                        HecpTableId = 1
                    },
                    new LotoAssociatedHecps
                    {
                        Id = 2,
                        Ata = "ATA2",
                        HECPTitle = "Dicrete1",
                        HECPRevisionLetter = "NEW",
                        LotoId= 1,
                        HecpTableId = null
                    },
                    new LotoAssociatedHecps
                    {
                        Id = 3,
                        Ata = "ATA2",
                        HECPTitle = "HECP2",
                        HECPRevisionLetter = "A",
                        LotoId= 2,
                        HecpTableId = 1
                    },
                    new LotoAssociatedHecps
                    {
                        Id = 4,
                        Ata = "ATA3",
                        HECPTitle = "Discrete2",
                        HECPRevisionLetter = "NEW",
                        LotoId= 2,
                        HecpTableId = null
                    },
                    new LotoAssociatedHecps
                    {
                        Id = 5,
                        Ata = "ATA4",
                        HECPTitle = "Discrete3",
                        HECPRevisionLetter = "NEW",
                        LotoId= 3,
                        HecpTableId = null
                    },
                    new LotoAssociatedHecps
                    {
                        Id = 6,
                        Ata = "ATA3",
                        HECPTitle = "HECP3",
                        HECPRevisionLetter = "NEW",
                        LotoId= 3,
                        HecpTableId = 3
                    }
                };

                List<LotoIsolationsDiscreteHecp> lotoIsolationsDiscreteHecps = new List<LotoIsolationsDiscreteHecp>
                {
                    new LotoIsolationsDiscreteHecp
                    {
                        Id = 1,
                        CircuitNomenclature = "A",
                        LotoAssociatedId = 2,
                        SystemCircuitId = "ID1"
                    },
                    new LotoIsolationsDiscreteHecp
                    {
                        Id = 2,
                        CircuitNomenclature = "B",
                        LotoAssociatedId = 2,
                        SystemCircuitId = "ID2"
                    },
                    new LotoIsolationsDiscreteHecp
                    {
                        Id = 3,
                        CircuitNomenclature = "C",
                        LotoAssociatedId = 4,
                        SystemCircuitId = "ID3"
                    },
                    new LotoIsolationsDiscreteHecp
                    {
                        Id = 4,
                        CircuitNomenclature = "D",
                        LotoAssociatedId = 4,
                        SystemCircuitId = "ID4"
                    },
                    new LotoIsolationsDiscreteHecp
                    {
                        Id = 5,
                        CircuitNomenclature = "D",
                        LotoAssociatedId = 5,
                        SystemCircuitId = "ID4"
                    },
                    new LotoIsolationsDiscreteHecp
                    {
                        Id = 6,
                        CircuitNomenclature = "E",
                        LotoAssociatedId = 5,
                        SystemCircuitId = "ID3"
                    }
                };

                List<Models.Hecp.HecpIsolationTag> hecpIsolationTags = new List<Models.Hecp.HecpIsolationTag>
                {
                    new Models.Hecp.HecpIsolationTag
                    {
                        Id = 1,
                        CircuitName = "Circuit1",
                        CircuitPanel ="Panel1",
                        LotoId= 1
                    },
                    new Models.Hecp.HecpIsolationTag
                    {
                        Id = 2,
                        CircuitName = "Circuit2",
                        CircuitPanel ="Panel2",
                        LotoId= 1
                    },
                    new Models.Hecp.HecpIsolationTag
                    {
                        Id = 3,
                        CircuitName = "Circuit3",
                        CircuitPanel ="Panel3",
                        LotoId= 2
                    },
                    new Models.Hecp.HecpIsolationTag
                    {
                        Id = 4,
                        CircuitName = "Circuit4",
                        CircuitPanel ="Panel4",
                        LotoId= 5
                    },

                };
                
                context.AddRange(lotos);
                context.AddRange(lotoAssociatedHecps);
                context.AddRange(lotoIsolationsDiscreteHecps);
                context.AddRange(hecpIsolationTags);
                context.SaveChanges();

                var response = _client.GetAsync("/api/Loto/ActiveIsolations/Program/737/LineNumber/123");

                string value = await response.Result.Content.ReadAsStringAsync();
                List<LotoDetails> result = JsonConvert.DeserializeObject<List<LotoDetails>>(value);

                Assert.AreEqual(System.Net.HttpStatusCode.OK, response.Result.StatusCode);

                Assert.AreEqual(2, result.Count);
                VerifyLoto(loto1, result[0]);
                VerifyLoto(loto2, result[1]);
                VerifyIsolationTag(hecpIsolationTags[0], result[0].HecpIsolationTag[0]);
                VerifyIsolationTag(hecpIsolationTags[1], result[0].HecpIsolationTag[1]);
                VerifyIsolationTag(hecpIsolationTags[2], result[1].HecpIsolationTag[0]);
                VerifyLotoAssociatedHecps(lotoAssociatedHecps[0], result[0].LotoAssociatedHecps[0]);
                VerifyLotoAssociatedHecps(lotoAssociatedHecps[1], result[0].LotoAssociatedHecps[1]);
                VerifyLotoAssociatedHecps(lotoAssociatedHecps[2], result[1].LotoAssociatedHecps[0]);
                VerifyLotoAssociatedHecps(lotoAssociatedHecps[3], result[1].LotoAssociatedHecps[1]);
                VerifyLotoIsolationsDiscreteHecps(lotoIsolationsDiscreteHecps[0], result[0].LotoAssociatedHecps[1].LotoIsolationsDiscreteHecp[0]);
                VerifyLotoIsolationsDiscreteHecps(lotoIsolationsDiscreteHecps[1], result[0].LotoAssociatedHecps[1].LotoIsolationsDiscreteHecp[1]);
                VerifyLotoIsolationsDiscreteHecps(lotoIsolationsDiscreteHecps[2], result[1].LotoAssociatedHecps[1].LotoIsolationsDiscreteHecp[0]);
                VerifyLotoIsolationsDiscreteHecps(lotoIsolationsDiscreteHecps[3], result[1].LotoAssociatedHecps[1].LotoIsolationsDiscreteHecp[1]);
                context.Dispose();
            }
        }

        [TestMethod]
        public async Task Should_GetAll_Conflicted_Loto_Isolations()
        {
            using (var context = _context)
            {
                var isolationTags = new List<Models.Hecp.HecpIsolationTag>()
                {
                    new Models.Hecp.HecpIsolationTag
                    {
                        Id = 1,
                        HecpId = 30,
                        LotoId = 2,
                        HecpIsolationId = 6,
                        CircuitId = "CC450",
                        CircuitName = "CB-L BUS OFF RELAY",
                        CircuitPanel ="P-05",
                        CircuitLocation ="E-4",
                        State ="ON",
                        InstalledByBemsId =99999,
                        InstallDateTime = DateTime.Now,
                        IsLocked = true
                    },
                    new Models.Hecp.HecpIsolationTag
                    {
                        Id = 2,
                        HecpId =30,
                        LotoId =2,
                        HecpIsolationId =5,
                        CircuitId = "C01721",
                        CircuitName = "CB-OBSERVER SEAT LH OUTLET",
                        CircuitPanel ="P-04",
                        CircuitLocation ="E-5",
                        State ="OFF",
                        InstalledByBemsId =99999,
                        InstallDateTime = DateTime.Now,
                        IsLocked = true
                    },
                    new Models.Hecp.HecpIsolationTag
                    {
                        Id = 3,
                        HecpId = 31,
                        LotoId =2,
                        HecpIsolationId =5,
                        CircuitId = "C017200",
                        CircuitName = "CB-OBSERVER SEAT LH INLET",
                        CircuitPanel ="P-03",
                        CircuitLocation ="E-6",
                        State ="OFF",
                        InstalledByBemsId =99999,
                        InstallDateTime = DateTime.Now,
                        IsLocked = true
                    },
                    new Models.Hecp.HecpIsolationTag
                    {
                        Id = 4,
                        HecpId =30,
                        LotoId =3,
                        HecpIsolationId =6,
                        CircuitId = "CC450",
                        CircuitName = "CB-L BUS OFF RELAY",
                        CircuitPanel ="P-05",
                        CircuitLocation ="E-4",
                        State ="OFF",
                        InstalledByBemsId =99999,
                        InstallDateTime = DateTime.Now,
                        IsLocked = true
                    },
                    new Models.Hecp.HecpIsolationTag
                    {
                        Id = 5,
                        HecpId =30,
                        LotoId =3,
                        HecpIsolationId =5,
                        CircuitId = "C01721",
                        CircuitName = "CB-OBSERVER SEAT LH OUTLET",
                        CircuitPanel ="P-04",
                        CircuitLocation ="E-5",
                        State ="VARIABLE",
                        InstalledByBemsId =99999,
                        InstallDateTime = DateTime.Now,
                        IsLocked = true
                    },
                    new Models.Hecp.HecpIsolationTag
                    {
                        Id = 6,
                        HecpId = 31,
                        LotoId = 3,
                        HecpIsolationId =5,
                        CircuitId = "C017200",
                        CircuitName = "CB-OBSERVER SEAT LH INLET",
                        CircuitPanel ="P-03",
                        CircuitLocation ="E-6",
                        State ="OFF",
                        InstalledByBemsId =99999,
                        InstallDateTime = DateTime.Now,
                        IsLocked = true
                    }
                };

                LotoDetails loto1 = new LotoDetails()
                {
                    CreatedAt = DateTime.Now,
                    Id = 2,
                    LineNumber = "123",
                    Model = "737",
                    Reason = "Lockout",
                    Site = "Renton",
                    WorkPackage = "JO12345678",
                    ActiveAEs = new List<LotoAE>() { new LotoAE { AEBemsId = 2519949, LotoId = 1 } },
                    Status = new Status { Description = "Active" },
                    HecpIsolationTag = isolationTags
                };

                LotoDetails loto2 = new LotoDetails()
                {
                    CreatedAt = DateTime.Now,
                    Id = 3,
                    LineNumber = "123",
                    Model = "737",
                    Reason = "Because",
                    Site = "Renton",
                    WorkPackage = "12345678",
                    Status = new Status { Description = "Active" }
                };

                List<LotoDetails> lotos = new List<LotoDetails>
                {
                    loto1,
                    loto2
                };
                context.AddRange(lotos);
                context.AddRange(isolationTags);
                context.SaveChanges();

                List<int> hecpIds = new List<int>
                {
                    30,31
                };

                var content = new StringContent(JsonConvert.SerializeObject(hecpIds), Encoding.UTF8, "application/json");

                var response = _client.PostAsync("/api/Loto/ConflictIsolations/LotoId/2/program/737/lineNumber/123", content);
                string value = await response.Result.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<List<Models.Hecp.HecpIsolationTag>>(value);

                Assert.AreEqual(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
                VerifyIsolationTag(isolationTags[3], result[0]);
                VerifyIsolationTag(isolationTags[4], result[1]);

                context.Dispose();
            }
        }

        [TestMethod]
        public async Task Should_IsHECPDeletable_Return_True()
        {
            using (var context = _context)
            {
                List<LotoDetails> lotos = new List<LotoDetails>
                {
                    new LotoDetails
                    {
                        CreatedAt = DateTime.Now,
                        Id = 1,
                        LineNumber = "123",
                        Model = "737",
                        Reason = "Because",
                        Site = "Renton",
                        WorkPackage = "WP1",
                        ActiveAEs = new List<LotoAE>() { new LotoAE { AEBemsId = 2519949, LotoId = 1 } },
                        Status = new Status
                        {                            
                            Description = "Completed"
                        }
                    },
                    new LotoDetails
                    {
                        CreatedAt = DateTime.Now,
                        Id = 2,
                        LineNumber = "123",
                        Model = "737",
                        Reason = "Because",
                        Site = "Renton",
                        WorkPackage = "WP2",
                        ActiveAEs = new List<LotoAE>() { new LotoAE { AEBemsId = 2519949, LotoId = 1 } },
                        Status = new Status
                        {
                            Description = "Completed"
                        }
                    }
                };
                List<LotoAssociatedHecps> lotoAssociatedHecps = new List<LotoAssociatedHecps>()
                {
                    new LotoAssociatedHecps
                    {
                        Id = 10,
                        LotoId = 1,
                        HecpTableId = 2
                    },
                    new LotoAssociatedHecps
                    {
                        Id = 11,
                        LotoId = 2,
                        HecpTableId = 2
                    },
                };
                context.LotoData.AddRange(lotos);
                context.AddRange(lotoAssociatedHecps);
                context.SaveChanges();

                var response = _client.GetAsync("/api/Loto/IsHecpDeletable/HecpId/2");

                string value = await response.Result.Content.ReadAsStringAsync();
                HTTPResponseWrapper<bool> result = JsonConvert.DeserializeObject<HTTPResponseWrapper<bool>>(value);

                Assert.IsNotNull(result);
                Assert.IsTrue(result.Data);
                Assert.AreEqual(Shield.Common.Constants.ShieldHttpWrapper.Status.SUCCESS, result.Status);

                context.Dispose();
            }
        }

        [TestMethod]
        public async Task Should_IsHECPDeletable_Return_True_For_Old_Lotos()
        {
            using (var context = _context)
            {
                List<LotoDetails> lotos = new List<LotoDetails>
                {
                    new LotoDetails
                    {
                        CreatedAt = DateTime.Now,
                        Id = 1,
                        LineNumber = "123",
                        Model = "737",
                        Reason = "Because",
                        Site = "Renton",
                        WorkPackage = "WP1",
                        ActiveAEs = new List<LotoAE>() { new LotoAE { AEBemsId = 2519949, LotoId = 1 } },
                        Status = new Status
                        {
                            Description = "Completed"
                        },
                        HecpTableId = 2,
                    },
                    new LotoDetails
                    {
                        CreatedAt = DateTime.Now,
                        Id = 2,
                        LineNumber = "123",
                        Model = "737",
                        Reason = "Because",
                        Site = "Renton",
                        WorkPackage = "WP2",
                        ActiveAEs = new List<LotoAE>() { new LotoAE { AEBemsId = 2519949, LotoId = 1 } },
                        Status = new Status
                        {
                            Description = "Completed"
                        },
                        HecpTableId = 2
                    }
                };
                context.LotoData.AddRange(lotos);
                context.SaveChanges();

                var response = _client.GetAsync("/api/Loto/IsHecpDeletable/HecpId/2");

                string value = await response.Result.Content.ReadAsStringAsync();
                HTTPResponseWrapper<bool> result = JsonConvert.DeserializeObject<HTTPResponseWrapper<bool>>(value);

                Assert.IsNotNull(result);
                Assert.IsTrue(result.Data);
                Assert.AreEqual(Shield.Common.Constants.ShieldHttpWrapper.Status.SUCCESS, result.Status);
            }
        }

        [TestMethod]
        public async Task Should_IsHECPDeletable_Return_False()
        {
            using (var context = _context)
            {
                List<LotoDetails> lotos = new List<LotoDetails>
                {
                    new LotoDetails
                    {
                        CreatedAt = DateTime.Now,
                        Id = 1,
                        LineNumber = "123",
                        Model = "737",
                        Reason = "Because",
                        Site = "Renton",
                        WorkPackage = "WP1",
                        ActiveAEs = new List<LotoAE>() { new LotoAE { AEBemsId = 2519949, LotoId = 1 } },
                        Status = new Status
                        {
                            Description = "Needs Lockout"
                        }
                    },
                    new LotoDetails
                    {
                        CreatedAt = DateTime.Now,
                        Id = 2,
                        LineNumber = "123",
                        Model = "737",
                        Reason = "Because",
                        Site = "Renton",
                        WorkPackage = "WP2",
                        ActiveAEs = new List<LotoAE>() { new LotoAE { AEBemsId = 2519949, LotoId = 1 } },
                        Status = new Status
                        {
                            Description = "Complete"
                        }
                    }
                };
                List<LotoAssociatedHecps> lotoAssociatedHecps = new List<LotoAssociatedHecps>()
                {
                    new LotoAssociatedHecps
                    {
                        Id = 10,
                        LotoId = 1,
                        HecpTableId = 2
                    },
                    new LotoAssociatedHecps
                    {
                        Id = 11,
                        LotoId = 2,
                        HecpTableId = 2
                    },
                };
                context.LotoData.AddRange(lotos);
                context.AddRange(lotoAssociatedHecps);
                context.SaveChanges();

                var response = _client.GetAsync("/api/Loto/IsHecpDeletable/HecpId/2");

                string value = await response.Result.Content.ReadAsStringAsync();
                HTTPResponseWrapper<bool> result = JsonConvert.DeserializeObject<HTTPResponseWrapper<bool>>(value);

                Assert.IsNotNull(result);
                Assert.IsFalse(result.Data);
                Assert.AreEqual(Shield.Common.Constants.ShieldHttpWrapper.Status.SUCCESS, result.Status);
            }
        }

        [TestMethod]
        public async Task Should_IsHECPDeletable_Return_False_For_Old_Lotos()
        {
            using (var context = _context)
            {
                List<LotoDetails> lotos = new List<LotoDetails>
                {
                    new LotoDetails
                    {
                        CreatedAt = DateTime.Now,
                        Id = 1,
                        LineNumber = "123",
                        Model = "737",
                        Reason = "Because",
                        Site = "Renton",
                        WorkPackage = "WP1",
                        ActiveAEs = new List<LotoAE>() { new LotoAE { AEBemsId = 2519949, LotoId = 1 } },
                        Status = new Status
                        {
                            Description = "Needs Lockout"
                        },
                        HecpTableId = 2
                    },
                    new LotoDetails
                    {
                        CreatedAt = DateTime.Now,
                        Id = 2,
                        LineNumber = "123",
                        Model = "737",
                        Reason = "Because",
                        Site = "Renton",
                        WorkPackage = "WP2",
                        ActiveAEs = new List<LotoAE>() { new LotoAE { AEBemsId = 2519949, LotoId = 1 } },
                        Status = new Status
                        {
                            Description = "Complete"
                        },
                        HecpTableId = 2
                    }
                };
                context.LotoData.AddRange(lotos);
                context.SaveChanges();

                var response = _client.GetAsync("/api/Loto/IsHecpDeletable/HecpId/2");

                string value = await response.Result.Content.ReadAsStringAsync();
                HTTPResponseWrapper<bool> result = JsonConvert.DeserializeObject<HTTPResponseWrapper<bool>>(value);

                Assert.IsNotNull(result);
                Assert.IsFalse(result.Data);
                Assert.AreEqual(Shield.Common.Constants.ShieldHttpWrapper.Status.SUCCESS, result.Status);
            }
        }

        [TestMethod]
        public async Task Should_Edit_LotoJobInfo()
        {
            using (var context = _context)
            {
                Models.Common.CreateLotoRequest lotoJobInfo = new Models.Common.CreateLotoRequest
                {
                    LotoId = 1,
                    WorkPackage = "This is a test workpackage",
                    Reason = "Test Reason editing"
                };

                LotoDetails loto = new()
                {
                    Model = "787",
                    LineNumber = "500",
                    AssignedPAEBems = 123,
                    CreatedAt = DateTime.Now,
                    HecpTableId = null,
                    HECPRevisionLetter = null,
                    HECPTitle = null,
                    Id = 1,
                    Reason = "Test Reason",
                    Status = new Status(),
                    WorkPackage = "This is a test",
                    LotoAssociatedHecps = new List<LotoAssociatedHecps>
                    {
                        new ()
                        {
                            Id = 1,
                            HecpTableId = 5,
                            HECPRevisionLetter = "A",
                            HECPTitle = "SomeTitle",
                        }
                    },
                    LotoAssociatedModelDataList = new List<LotoAssociatedModelData>
                    {
                        new ()
                        {
                            LotoId = 1,
                            MinorModelId = 2
                        }
                    }
                };

                context.LotoData.AddRange(loto);
                context.SaveChanges();

                var content = new StringContent(JsonConvert.SerializeObject(lotoJobInfo), Encoding.UTF8, "application/json");

                var response = _client.PutAsync("api/Loto/UpdateJobInfo", content);

                string value = await response.Result.Content.ReadAsStringAsync();
                HTTPResponseWrapper<Loto> result = JsonConvert.DeserializeObject<HTTPResponseWrapper<Loto>>(value);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Data);
                Assert.AreEqual("Work Package and Reason for LOTO have been updated!", result.Message);
                Assert.AreEqual(Common.Constants.ShieldHttpWrapper.Status.SUCCESS, result.Status);
                Assert.AreEqual(lotoJobInfo.WorkPackage, result.Data.WorkPackage);
                Assert.AreEqual(lotoJobInfo.Reason, result.Data.Reason);
            }          
        }

        [TestMethod]
        public async Task Should_Return_Failed_InUpdating_LotoJobInfo_When_LotoId_IsZero()
        {
            using (var context = _context)
            {
                Models.Common.CreateLotoRequest lotoJobInfo = new Models.Common.CreateLotoRequest
                {
                    LotoId = 0,
                    WorkPackage = "This is a test workpackage",
                    Reason = "Test Reason editing"
                };

                LotoDetails loto = new()
                {
                    Model = "787",
                    LineNumber = "500",
                    AssignedPAEBems = 123,
                    CreatedAt = DateTime.Now,
                    HecpTableId = null,
                    HECPRevisionLetter = null,
                    HECPTitle = null,
                    Id = 1,
                    Reason = "Test Reason",
                    Status = new Status(),
                    WorkPackage = "This is a test",
                    LotoAssociatedHecps = new List<LotoAssociatedHecps>
                    {
                        new ()
                        {
                            Id = 1,
                            HecpTableId = 5,
                            HECPRevisionLetter = "A",
                            HECPTitle = "SomeTitle",
                        }
                    },
                    LotoAssociatedModelDataList = new List<LotoAssociatedModelData>
                    {
                        new ()
                        {
                            LotoId = 1,
                            MinorModelId = 2
                        }
                    }
                };

                context.LotoData.AddRange(loto);
                context.SaveChanges();

                var content = new StringContent(JsonConvert.SerializeObject(lotoJobInfo), Encoding.UTF8, "application/json");

                var response = _client.PutAsync("api/Loto/UpdateJobInfo", content);

                string value = await response.Result.Content.ReadAsStringAsync();
                HTTPResponseWrapper<Loto> result = JsonConvert.DeserializeObject<HTTPResponseWrapper<Loto>>(value);

                Assert.IsNotNull(result);
                Assert.IsNull(result.Data);
                Assert.AreEqual("Error updating Work Package and Reason!", result.Message);
                Assert.AreEqual(Common.Constants.ShieldHttpWrapper.Status.FAILED, result.Status);
            }
        }

        private static void VerifyLoto(LotoDetails actual, LotoDetails expected)
        {
            Assert.AreEqual(actual.Id, expected.Id);
            Assert.AreEqual(actual.LineNumber, expected.LineNumber);
            Assert.AreEqual(actual.Model, expected.Model);
            Assert.AreEqual(actual.Reason, expected.Reason);
            Assert.AreEqual(actual.Site, expected.Site);
            Assert.AreEqual(actual.WorkPackage, expected.WorkPackage);
            Assert.AreEqual(actual.ActiveAEs.Count, expected.ActiveAEs.Count);
            Assert.AreEqual(actual.Status.Id, expected.Status.Id);
        }
        private static void VerifyIsolationTag(Models.Hecp.HecpIsolationTag actual, Models.Hecp.HecpIsolationTag expected)
        {
            Assert.AreEqual(actual.Id, expected.Id);
            Assert.AreEqual(actual.CircuitName, expected.CircuitName);
            Assert.AreEqual(actual.CircuitPanel, expected.CircuitPanel);
            Assert.AreEqual(actual.LotoId, expected.LotoId);
        }
        private static void VerifyLotoAssociatedHecps(LotoAssociatedHecps expected, LotoAssociatedHecps actual)
        {
            Assert.AreEqual(expected.Ata, actual.Ata);
            Assert.AreEqual(expected.HECPRevisionLetter, actual.HECPRevisionLetter);
            Assert.AreEqual(expected.HECPTitle, actual.HECPTitle);
            Assert.AreEqual(expected.HecpTableId, actual.HecpTableId);
            Assert.AreEqual(expected.LotoId, actual.LotoId);
        }
        private static void VerifyLotoIsolationsDiscreteHecps(LotoIsolationsDiscreteHecp expected, LotoIsolationsDiscreteHecp actual)
        {
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.CircuitNomenclature, actual.CircuitNomenclature);
            Assert.AreEqual(expected.LotoAssociatedId, actual.LotoAssociatedId);
            Assert.AreEqual(expected.SystemCircuitId, actual.SystemCircuitId);
        }
    }

    class Something
    {
        public string Access_Token { get; set; }
    }
}


[TestMethod]
public async Task Should_NotSignInAE_When_AEIsAlreadySignedIntoSameLoto()
{
    using (var context = _context)
    {
        // Arrange
        LotoDetails loto = new LotoDetails()
        {
            Id = 1,
            Reason = "Test Loto",
            WorkPackage = "WP-123",
            CreatedAt = DateTime.Now,
            LineNumber = "500",
            Model = "787",
            Site = "Renton"
        };

        LotoAE existingAE = new LotoAE
        {
            AEBemsId = 12345,
            LotoId = 1,
            FullName = "John Doe"
        };

        context.Add(loto);
        context.Add(existingAE);
        context.SaveChanges();

        var signInAERequest = new Shield.Common.Models.Loto.SigningAERequest()
        {
            AEBemsId = 12345,
            AEName = "John Doe",
            LotoId = 1
        };

        StringContent content = new StringContent(JsonConvert.SerializeObject(signInAERequest), Encoding.UTF8, "application/json");

        // Act
        var response = _client.PostAsync("api/Loto/AESignIn", content);
        var value = await response.Result.Content.ReadAsStringAsync();
        HTTPResponseWrapper<LotoAE> responseWrapper = JsonConvert.DeserializeObject<HTTPResponseWrapper<LotoAE>>(value);

        // Assert
        Assert.AreEqual(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
        Assert.AreEqual(Shield.Common.Constants.ShieldHttpWrapper.Status.NOT_MODIFIED, responseWrapper.Status);
        Assert.AreEqual(Shield.Common.Constants.ShieldHttpWrapper.Reason.ALREADY_EXISTS, responseWrapper.Reason);
        Assert.IsNotNull(responseWrapper.Data);
        Assert.AreEqual(12345, responseWrapper.Data.AEBemsId);
        Assert.AreEqual(1, responseWrapper.Data.LotoId);
        Assert.AreEqual("John Doe", responseWrapper.Data.FullName);
    }
}

[TestMethod]
public async Task Should_NotSignInAE_When_AEIsAlreadySignedIntoDifferentLoto()
{
    using (var context = _context)
    {
        // Arrange
        LotoDetails loto1 = new LotoDetails()
        {
            Id = 1,
            Reason = "Existing Loto",
            WorkPackage = "WP-111",
            CreatedAt = DateTime.Now,
            LineNumber = "500",
            Model = "787",
            Site = "Renton"
        };

        LotoDetails loto2 = new LotoDetails()
        {
            Id = 2,
            Reason = "New Loto",
            WorkPackage = "WP-222",
            CreatedAt = DateTime.Now,
            LineNumber = "500",
            Model = "787",
            Site = "Renton"
        };

        LotoAE existingAE = new LotoAE
        {
            AEBemsId = 12345,
            LotoId = 1,
            FullName = "John Doe"
        };

        context.Add(loto1);
        context.Add(loto2);
        context.Add(existingAE);
        context.SaveChanges();

        var signInAERequest = new Shield.Common.Models.Loto.SigningAERequest()
        {
            AEBemsId = 12345,
            AEName = "John Doe",
            LotoId = 2
        };

        StringContent content = new StringContent(JsonConvert.SerializeObject(signInAERequest), Encoding.UTF8, "application/json");

        // Act
        var response = _client.PostAsync("api/Loto/AESignIn", content);
        var value = await response.Result.Content.ReadAsStringAsync();
        HTTPResponseWrapper<LotoAE> responseWrapper = JsonConvert.DeserializeObject<HTTPResponseWrapper<LotoAE>>(value);

        // Assert
        Assert.AreEqual(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
        Assert.AreEqual(Shield.Common.Constants.ShieldHttpWrapper.Status.NOT_MODIFIED, responseWrapper.Status);
        Assert.AreEqual(Shield.Common.Constants.ShieldHttpWrapper.Reason.ALREADY_EXISTS, responseWrapper.Reason);
        Assert.IsNotNull(responseWrapper.Data);
        Assert.AreEqual(12345, responseWrapper.Data.AEBemsId);
        Assert.AreEqual(1, responseWrapper.Data.LotoId);
        Assert.AreEqual("John Doe", responseWrapper.Data.FullName);
    }
}

using Newtonsoft.Json;
using Shield.Common;
using Shield.Common.Models.Common;
using Shield.Ui.App.Common;
using Shield.Ui.App.Models.ExternalModels;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Shield.Ui.App.Services
{
    using Shield.Common.Models.MyLearning;
    using Shield.Common.Models.MyLearning.Interfaces;
    using System.Collections.Generic;

    public class ExternalService
    {
        #region Getters/Setters/Declarations

        private HttpClientService _clientService;

        #endregion Getters/Setters/Declarations

        #region Constructor(s)

        public ExternalService(HttpClientService clientService)
        {
            _clientService = clientService;
        }

        #endregion Constructor(s)

        virtual public async Task<HTTPResponseWrapper<int>> GetBemsIdFromBemsIdOrBadgeNumber(string bemsIdOrBadgeNumber)
        {
            if (Helpers.IsBadgeNumber(bemsIdOrBadgeNumber))
            {
                var result = await GetBemsIdFromBadgeNumber(bemsIdOrBadgeNumber);

                return new HTTPResponseWrapper<int>
                {
                    Status = result.Status.ToUpper(),
                    Data = result.Data.BemsId,
                    Message = result.Message,
                    Reason = result.Reason
                };
            }

            if (int.TryParse(bemsIdOrBadgeNumber, out int dataValue))
            {
                return new HTTPResponseWrapper<int> { Status = Shield.Common.Constants.ShieldHttpWrapper.Status.SUCCESS, Data = dataValue };
            }

            return new HTTPResponseWrapper<int>
            {
                Status = Shield.Common.Constants.ShieldHttpWrapper.Status.FAILED,
                Message = $"Failed to check out user with BEMSID/Badge: {bemsIdOrBadgeNumber}.",
            };
        }

        public virtual async Task<HTTPResponseWrapper<bool>> IsValidBadge(int bemsId, string confirmingBadge)
        {
            HTTPResponseWrapper<bool> response = new HTTPResponseWrapper<bool>();
            const string defaultMessage = "Person Not Checked In. Scan The Correct Badge of This Line's CC.";
            response.Message = defaultMessage;

            try
            {
                if (confirmingBadge != null)
                {
                    confirmingBadge = confirmingBadge.Replace(".", "");
                    confirmingBadge = Helpers.ParseBadgeNumber(confirmingBadge);

                    if (Helpers.IsBadgeNumber(confirmingBadge))
                    {
                        HTTPResponseWrapper<int> confirmingBcBemsWrapper = await GetBemsIdFromBemsIdOrBadgeNumber(confirmingBadge);
                        response.Data = (bemsId == confirmingBcBemsWrapper.Data);
                        response.Message = confirmingBcBemsWrapper.Data == 0 ? confirmingBcBemsWrapper.Message : defaultMessage;
                        return response;
                    }
                    else
                    {
                        response.Message = defaultMessage;
                    }
                }
                return response;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return response;
            }
        }

        public virtual async Task<TrainingInfo> GetMyLearningDataAsync(int bemsId, string badgeNumber,string shieldActionName)
        {
            TrainingInfo trainingInfo = new();
            try
            {
                if (badgeNumber != null)
                {
                    var bemsResponse = await GetBemsIdFromBemsIdOrBadgeNumber(badgeNumber);

                    if (bemsResponse.Status == Shield.Common.Constants.ShieldHttpWrapper.Status.SUCCESS && bemsResponse is not null && bemsResponse.Data is not 0)
                    {
                        trainingInfo.BemsId = bemsResponse.Data;
                        bemsId = bemsResponse.Data;
                    }
                }
                else
                {
                    trainingInfo.BemsId = bemsId;
                }

                string path = EnvironmentHelper.ExternalServiceAddress + $"mylearningdata/GetMyLearningData?bemsId={trainingInfo.BemsId}&shieldActionName={shieldActionName}";

                Uri uriPath = new Uri(path);

                HttpResponseMessage res = await _clientService.GetClient().GetAsync(uriPath);

                if (res?.IsSuccessStatusCode == true)
                {
                    var deserializeResponse = JsonConvert.DeserializeObject<HTTPResponseWrapper<List<MyLearningDataResponse>>>(await res.Content.ReadAsStringAsync());
                    trainingInfo.MyLearningDataResponse.AddRange(deserializeResponse.Data);
                }
                else
                {
                    string errorMsg = JsonConvert.DeserializeObject<string>(await res.Content.ReadAsStringAsync());
                    Console.Error.WriteLine(errorMsg);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }

            return trainingInfo;
        }

        /// <summary>
        /// The get bems id from badge number.
        /// </summary>
        /// <param name="badgeNumber">
        /// The badge number.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        private async Task<HTTPResponseWrapper<BadgeDataResponse>> GetBemsIdFromBadgeNumber(string badgeNumber)
        {
            var client = _clientService.GetClient();

            Uri uriPath = new Uri(EnvironmentHelper.ExternalServiceAddress + $"badgedata/{badgeNumber}");
            HttpResponseMessage message = await client.GetAsync(uriPath);
            HTTPResponseWrapper<BadgeDataResponse> response = JsonConvert.DeserializeObject<HTTPResponseWrapper<BadgeDataResponse>>(await message.Content.ReadAsStringAsync());

            return response;
        }
    }
}



OLDER:

using Newtonsoft.Json;
using Shield.Common;
using Shield.Common.Models.Common;
using Shield.Ui.App.Common;
using Shield.Ui.App.Models.ExternalModels;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Shield.Ui.App.Services
{
    using Shield.Common.Models.MyLearning;
    using Shield.Common.Models.MyLearning.Interfaces;
    using System.Collections.Generic;

    public class ExternalService
    {
        #region Getters/Setters/Declarations

        private HttpClientService _clientService;

        #endregion Getters/Setters/Declarations

        #region Constructor(s)

        public ExternalService(HttpClientService clientService)
        {
            _clientService = clientService;
        }

        #endregion Constructor(s)

        virtual public async Task<HTTPResponseWrapper<int>> GetBemsIdFromBemsIdOrBadgeNumber(string bemsIdOrBadgeNumber)
        {
            if (Helpers.IsBadgeNumber(bemsIdOrBadgeNumber))
            {
                var result = await GetBemsIdFromBadgeNumber(bemsIdOrBadgeNumber);

                return new HTTPResponseWrapper<int>
                {
                    Status = result.Status.ToUpper(),
                    Data = result.Data.BemsId,
                    Message = result.Message,
                    Reason = result.Reason
                };
            }

            if (int.TryParse(bemsIdOrBadgeNumber, out int dataValue))
            {
                return new HTTPResponseWrapper<int> { Status = Shield.Common.Constants.ShieldHttpWrapper.Status.SUCCESS, Data = dataValue };
            }

            return new HTTPResponseWrapper<int>
            {
                Status = Shield.Common.Constants.ShieldHttpWrapper.Status.FAILED,
                Message = $"Failed to check out user with BEMSID/Badge: {bemsIdOrBadgeNumber}.",
            };
        }

        public virtual async Task<HTTPResponseWrapper<bool>> IsValidBadge(int bemsId, string confirmingBadge)
        {
            HTTPResponseWrapper<bool> response = new HTTPResponseWrapper<bool>();
            const string defaultMessage = "Person Not Checked In. Scan The Correct Badge of This Line's CC.";
            response.Message = defaultMessage;

            try
            {
                if (confirmingBadge != null)
                {
                    confirmingBadge = confirmingBadge.Replace(".", "");
                    confirmingBadge = Helpers.ParseBadgeNumber(confirmingBadge);

                    if (Helpers.IsBadgeNumber(confirmingBadge))
                    {
                        HTTPResponseWrapper<int> confirmingBcBemsWrapper = await GetBemsIdFromBemsIdOrBadgeNumber(confirmingBadge);
                        response.Data = (bemsId == confirmingBcBemsWrapper.Data);
                        response.Message = confirmingBcBemsWrapper.Data == 0 ? confirmingBcBemsWrapper.Message : defaultMessage;
                        return response;
                    }
                    else
                    {
                        response.Message = defaultMessage;
                    }
                }
                return response;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return response;
            }
        }

        public virtual async Task<TrainingInfo> GetMyLearningDataAsync(int bemsId, string badgeNumber,string shieldActionName)
        {
            TrainingInfo trainingInfo = new();
            try
            {
                if (badgeNumber != null)
                {
                    var bemsResponse = await GetBemsIdFromBemsIdOrBadgeNumber(badgeNumber);

                    if (bemsResponse.Status == Shield.Common.Constants.ShieldHttpWrapper.Status.SUCCESS && bemsResponse is not null && bemsResponse.Data is not 0)
                    {
                        trainingInfo.BemsId = bemsResponse.Data;
                        bemsId = bemsResponse.Data;
                    }
                }
                else
                {
                    trainingInfo.BemsId = bemsId;
                }

                string path = EnvironmentHelper.ExternalServiceAddress + $"mylearningdata/GetMyLearningData?bemsId={trainingInfo.BemsId}&shieldActionName={shieldActionName}";

                Uri uriPath = new Uri(path);

                HttpResponseMessage res = await _clientService.GetClient().GetAsync(uriPath);

                if (res?.IsSuccessStatusCode == true)
                {
                    var deserializeResponse = JsonConvert.DeserializeObject<HTTPResponseWrapper<List<MyLearningDataResponse>>>(await res.Content.ReadAsStringAsync());
                    trainingInfo.MyLearningDataResponse.AddRange(deserializeResponse.Data);
                }
                else
                {
                    string errorMsg = JsonConvert.DeserializeObject<string>(await res.Content.ReadAsStringAsync());
                    Console.Error.WriteLine(errorMsg);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }

            return trainingInfo;
        }

        /// <summary>
        /// The get bems id from badge number.
        /// </summary>
        /// <param name="badgeNumber">
        /// The badge number.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        private async Task<HTTPResponseWrapper<BadgeDataResponse>> GetBemsIdFromBadgeNumber(string badgeNumber)
        {
            var client = _clientService.GetClient();

            Uri uriPath = new Uri(EnvironmentHelper.ExternalServiceAddress + $"badgedata/{badgeNumber}");
            HttpResponseMessage message = await client.GetAsync(uriPath);
            HTTPResponseWrapper<BadgeDataResponse> response = JsonConvert.DeserializeObject<HTTPResponseWrapper<BadgeDataResponse>>(await message.Content.ReadAsStringAsync());

            return response;
        }
    }
}

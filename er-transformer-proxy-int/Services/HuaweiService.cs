using er_transformer_proxy_int.Data.Repository.Interfaces;
using er_transformer_proxy_int.Model;
using er_transformer_proxy_int.Model.Dto;
using er_transformer_proxy_int.Model.Huawei;
using er_transformer_proxy_int.Services.Interfaces;
using System;

namespace er_transformer_proxy_int.Services
{
    public class HuaweiService : IBrand
    {
        private readonly IHuaweiRepository _repository;

        public HuaweiService(IHuaweiRepository huaweiRepository)
        {
            _repository = huaweiRepository;
        }

        public async Task<DeviceData> GetDevicesAsync(string stationCode)
        {
            var devices = await _repository.GetDevListMethodAsync(stationCode);
            return devices;
        }


        public async Task<ResponseModel<SiteResume>> GetSiteDetailByPlantsAsync(string stationCode)
        {
            var mockResponse = new SiteResume { AvoidedEmmisions = (decimal)31.56, CoincidentSolarConsumptions = (decimal)72727.32, EnergyCoverage = (decimal)37.8, LastConnectionTimeStamp = DateTime.Now, LifetimeEnergyProdution = (decimal)76894.32, LifetimeEnergyConsumption = (decimal)134658.93 };
            return new ResponseModel<SiteResume> { ErrorMessage = null, Success = true, Data = mockResponse };
        }

        public async Task<ResponseModel<string>> GetRealTimeDeviceInfo(FiveMinutesRequest request)
        {
            var response = await _repository.GetRealTimeDeviceInfoAsync(request);

            return new ResponseModel<string> { ErrorMessage = response.ErrorMessage, Success = response.Success, Data = response.Data };
        }

        public async Task<ResponseModel<List<HealtCheckModel>>> GetStationHealtCheck(string request)
        {
            var response = await _repository.GetStationHealtCheck(request);

            if (response.Data is not null)
            {
                foreach (var item in response.Data)
                {
                    HealtcheckStateEnum healthState = (HealtcheckStateEnum)Enum.Parse(typeof(HealtcheckStateEnum), item.real_health_state.ToString());

                    item.real_health_state = healthState.ToString();
                }
            }

            return new ResponseModel<List<HealtCheckModel>> { ErrorMessage = response.ErrorMessage, Success = response.Success, Data = response.Data, ErrorCode= response.ErrorCode };
        }

        public async Task<ResponseModel<string>> GetMonthProjectResume(StationAndCollectTimeRequest request)
        {
            var response = await _repository.GetMonthProjectResumeAsync(request);

            return new ResponseModel<string> { ErrorMessage = response.ErrorMessage, Success = response.Success, Data = response.Data };
        }

        public async Task<ResponseModel<string>> GetHourlyProjectResume(StationAndCollectTimeRequest request)
        {
            var response = await _repository.GetHourlyProjectResumeAsync(request);

            return new ResponseModel<string> { ErrorMessage = response.ErrorMessage, Success = response.Success, Data = response.Data };
        }

        public async Task<ResponseModel<string>> GetDailyProjectResume(StationAndCollectTimeRequest request)
        {
            var response = await _repository.GetDailyProjectResumeAsync(request);

            return new ResponseModel<string> { ErrorMessage = response.ErrorMessage, Success = response.Success, Data = response.Data };
        }
    }
}
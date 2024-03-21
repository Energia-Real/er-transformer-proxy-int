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


        public async Task<ResponseModel> GetSiteDetailByPlantsAsync(string stationCode)
        {
            var mockResponse = new SiteResume { AvoidedEmmisions = (decimal)31.56, CoincidentSolarConsumptions = (decimal)72727.32, EnergyCoverage = (decimal)37.8, LastConnectionTimeStamp = DateTime.Now, LifetimeEnergyProdution = (decimal)76894.32, LifetimeEnergyConsumption = (decimal)134658.93 };
            return new ResponseModel { ErrorMessage = null, Success = true, Data = mockResponse };
        }
    }
}

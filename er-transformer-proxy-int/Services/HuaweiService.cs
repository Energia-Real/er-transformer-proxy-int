using er_transformer_proxy_int.Data.Repository.Interfaces;
using er_transformer_proxy_int.Model.Huawei;
using er_transformer_proxy_int.Services.Interfaces;
using System;

namespace er_transformer_proxy_int.Services
{
    public class HuaweiService: IBrand
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
    }
}

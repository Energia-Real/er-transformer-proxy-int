using er_transformer_proxy_int.Model.Huawei;

namespace er_transformer_proxy_int.Services.Interfaces
{
    public interface IBrand
    {
        Task<DeviceData> GetDevicesAsync(string stationCode);
    }
}

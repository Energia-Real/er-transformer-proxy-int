using er_transformer_proxy_int.Model;
using er_transformer_proxy_int.Model.Dto;
using er_transformer_proxy_int.Model.Gigawatt;
using er_transformer_proxy_int.Model.Huawei;

namespace er_transformer_proxy_int.Services.Interfaces
{
    public interface IBrand
    {
        Task<DeviceData> GetDevicesAsync(string stationCode);
        Task<ResponseModel<SiteResume>> GetSiteDetailByPlantsAsync(string stationCode);
        Task<ResponseModel<string>> GetRealTimeDeviceInfo(FiveMinutesRequest request);
        
        Task<ResponseModel<HealtCheckModel>> GetStationHealtCheck(string request);
    }
}

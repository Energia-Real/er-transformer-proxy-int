namespace er_transformer_proxy_int.Data.Repository.Interfaces
{
    using er_transformer_proxy_int.Model;
    using er_transformer_proxy_int.Model.Dto;
    using er_transformer_proxy_int.Model.Huawei;

    public interface IHuaweiRepository
    {
        Task<DeviceData> GetDevListMethodAsync(string stationCode);

        Task<ResponseModel<string>> GetRealTimeDeviceInfoAsync(FiveMinutesRequest request);

        Task<ResponseModel<HealtCheckModel>> GetStationHealtCheck(string stationCodes);
    }
}

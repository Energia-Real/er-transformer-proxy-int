namespace er_transformer_proxy_int.Data.Repository.Interfaces
{
    using er_transformer_proxy_int.Model;
    using er_transformer_proxy_int.Model.Dto;
    using er_transformer_proxy_int.Model.Huawei;

    public interface IHuaweiRepository
    {
        Task<DeviceData> GetDevListMethodAsync(string stationCode);

        Task<ResponseModel<string>> GetRealTimeDeviceInfoAsync(FiveMinutesRequest request);

        Task<ResponseModel<List<HealtCheckModel>>> GetStationHealtCheck(string stationCodes);
        Task<ResponseModel<string>> GetMonthProjectResumeAsync(StationAndCollectTimeRequest request);
        Task<ResponseModel<string>> GetDailyProjectResumeAsync(StationAndCollectTimeRequest request);
        Task<ResponseModel<string>> GetHourlyProjectResumeAsync(StationAndCollectTimeRequest request);
    }
}

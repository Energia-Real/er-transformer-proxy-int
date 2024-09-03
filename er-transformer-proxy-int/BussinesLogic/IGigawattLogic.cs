using er_transformer_proxy_int.Model.Gigawatt;
using er_transformer_proxy_int.Model;
using er_transformer_proxy_int.Model.Huawei;
using er_transformer_proxy_int.Model.Dto;
using er_transformer_proxy_int.Model.Request;

namespace er_transformer_proxy_int.BussinesLogic
{
    public interface IGigawattLogic
    {
        Task<ResponseModel<List<CommonTileResponse>>> GetSiteDetails(RequestModel request);

        Task<ResponseModel<List<CommonTileResponse>>> GetOverview(RequestModel request);

        Task<ResponseModel<HealtCheckModel>> GetStationHealtCheck(RequestModel request);
        Task<ResponseModel<List<CommonTileResponse>>> GetStationCapacity(RequestModel request);
        Task<string> UpdateMonthResume(RequestUpdateData? request);


        Task<List<MonthProjectResume>> GetMonthResume(RequestModel? request);

        Task<PlantDeviceResult> GetPlantDeviceDataFromMongo(RequestModel request);
        Task<ResponseModel<List<CommonTileResponse>>> GetGlobalSolarCoverage(RequestModel request);
        Task<bool> ReplicateToMongoDb();
        Task<bool> ReplicateMonthResumeToMongo();
        Task<bool> ReplicateHourlyResumeToMongo();
        Task<bool> ReplicateDailyResumeToMongo();
        Task<bool> ReplicateHealtCheckToMongo();
    }
}

using er_transformer_proxy_int.Model;
using er_transformer_proxy_int.Model.Dto;
using er_transformer_proxy_int.Model.Huawei;

namespace er_transformer_proxy_int.Data.Repository.Interfaces
{
    public interface IMongoRepository
    {
        Task<List<Device>> GetDeviceDataAsyncByCode(string stationCode);
        Task<List<Device>> GetDeviceDataAsync();
        Task<PlantDeviceResult> GetRepliedDataAsync(RequestModel request);
        Task<List<PlantDeviceResult>> GetRepliedDataListAsync(RequestModel request);
        Task<List<PlantDto>> GetPlantListAsync();

        Task<List<MonthProjectResume>> GetMonthProjectResumesAsync(RequestModel? requestModel);

        Task<HealtCheckModel> GetHealtCheackAsync(RequestModel request);

        Task InsertDeviceDataAsync(PlantDeviceResult device);
        Task InsertMonthResumeDataAsync(MonthProjectResume resume);
        Task InsertHourResumeDataAsync(HourProjectResume resume);

        Task InsertDayResumeDataAsync(DayProjectResume resume);
        Task InsertHealtCheck(List<HealtCheckModel> model);

        Task DeleteManyFromCollection(string collectionName);
        Task DeleteManyFromCollectionByDate(string collectionName, DateTime date);

        Task<List<DayProjectResume>> GetDailyRepliedDataAsync(RequestModel request);
    }
}

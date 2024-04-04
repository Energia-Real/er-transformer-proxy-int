using er_transformer_proxy_int.Model.Huawei;

namespace er_transformer_proxy_int.Data.Repository.Interfaces
{
    public interface IMongoRepository
    {
        Task<List<Device>> GetDeviceDataAsync(string stationCode);
    }
}

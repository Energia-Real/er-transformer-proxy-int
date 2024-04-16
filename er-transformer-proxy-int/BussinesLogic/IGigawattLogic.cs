using er_transformer_proxy_int.Model.Gigawatt;
using er_transformer_proxy_int.Model;
using er_transformer_proxy_int.Model.Huawei;

namespace er_transformer_proxy_int.BussinesLogic
{
    public interface IGigawattLogic
    {
        Task<ResponseModel<List<CommonTileResponse>>> GetSiteDetails(RequestModel request);

        Task<ResponseModel<List<CommonTileResponse>>> GetOverview(RequestModel request);

        Task<ResponseModel<HealtCheckModel>> GetStationHealtCheck(RequestModel request);
        Task<ResponseModel<List<CommonTileResponse>>> GetStationCapacity(RequestModel request);
    }
}

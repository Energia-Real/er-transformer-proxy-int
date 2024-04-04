using er_transformer_proxy_int.Model.Gigawatt;
using er_transformer_proxy_int.Model;

namespace er_transformer_proxy_int.BussinesLogic
{
    public interface IGigawattLogic
    {
        Task<ResponseModel<List<CommonTileResponse>>> GetSiteDetails(RequestModel request);

        Task<ResponseModel<List<CommonTileResponse>>> GetOverview(RequestModel request);
    }
}

using MongoDB.Driver;
using static System.Net.Mime.MediaTypeNames;

namespace er_transformer_proxy_int.Model.Request
{
    public class RequestUpdateData
    {
        public string StationCode { get; set; }
        public DateTime CollectTime { get; set; }
        public double InverterPower { get; set; }
        public string UserData { get; set; }
    }
}

using MongoDB.Bson;

namespace er_transformer_proxy_int.Model.Huawei
{
    public class HealtCheckModel
    {
        public ObjectId _id { get; set; }
        public string real_health_state { get; set; }
        public string day_power { get; set; }
        public string total_power { get; set; }
        public string day_income { get; set; }
        public string month_power { get; set; }
        public string total_income { get; set; }
        public string stationCode { get; set; }
        public DateTime collectTime { get; set; }
    }
}

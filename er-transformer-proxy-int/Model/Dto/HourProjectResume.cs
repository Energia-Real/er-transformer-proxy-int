using MongoDB.Bson;

namespace er_transformer_proxy_int.Model.Dto
{
    public class HourProjectResume
    {
        public ObjectId _id { get; set; }
        public string brandName { get; set; } = "brand";
        public string stationCode { get; set; }
        public DateTime repliedDateTime { get; set; }

        public List<HourResumeResponse> HourResume { get; set; }
    }
}

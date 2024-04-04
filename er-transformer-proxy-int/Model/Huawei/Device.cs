

namespace er_transformer_proxy_int.Model.Huawei
{
    using MongoDB.Bson;

    public class Device
    {
        public ObjectId _id { get; set; }
        public string? devDn { get; set; }
        public string? devName { get; set; }
        public int? devTypeId { get; set; }
        public string? esnCode { get; set; }
        public long? deviceId { get; set; } 
        public string? invType { get; set; }
        public double? latitude { get; set; }
        public double? longitude { get; set; }
        public int? optimizerNumber { get; set; }
        public string? softwareVersion { get; set; }
        public string? stationCode { get; set; }
    }
}

namespace er_transformer_proxy_int.Model.Dto
{
    public class PlantDeviceResult
    {
        public string brandName { get; set; } = "brand";
        public string stationCode { get; set; }
        public DateTime repliedDateTime { get; set; }
        public List<DeviceDataResponse<DeviceInverterDataItem>> invertersList { get; set; }
        public List<DeviceDataResponse<DeviceMetterDataItem>> metterList { get; set; }
    }
}

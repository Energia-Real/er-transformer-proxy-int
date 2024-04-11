namespace er_transformer_proxy_int.Model.Dto
{
    public class PlantDeviceResult
    {
        public List<DeviceDataResponse<DeviceInverterDataItem>> invertersList { get; set; }
        public List<DeviceDataResponse<DeviceMetterDataItem>> metterList { get; set; }
    }
}

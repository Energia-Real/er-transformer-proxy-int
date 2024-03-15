namespace er_transformer_proxy_int.Model.Huawei
{
    public class DeviceData
    {
        public List<Device> data { get; set; }
        public int failCode { get; set; }
        public string message { get; set; }
        public Params @params { get; set; }
        public bool success { get; set; }
    }
}

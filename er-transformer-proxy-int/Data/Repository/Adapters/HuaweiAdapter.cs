namespace er_transformer_proxy_int.Data.Repository.Adapters
{
    using er_transformer_proxy_int.Data.Repository.Interfaces;
    using er_transformer_proxy_int.Model;
    using er_transformer_proxy_int.Model.Dto;
    using er_transformer_proxy_int.Model.Huawei;
    using System.Text;
    using System.Text.Json;

    public class HuaweiAdapter : IHuaweiRepository
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        public HuaweiAdapter(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _configuration = configuration;
        }

        public async Task<DeviceData> GetDevListMethodAsync(string stationCode)
        {
            var api = _configuration["APIs:HuaweiApi"];
            var apiUrl = string.Format("{0}{1}", api, "getDevList");

            // Enviar solicitud a la API de dispositivo
            var requestBody = string.Format("\"{0}" + "\"", stationCode);
            var requestContent = new StringContent(requestBody, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(apiUrl, requestContent);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            // Procesar la respuesta de la API de dispositivo
            string responseContent = await response.Content.ReadAsStringAsync();
            var responsedata = JsonSerializer.Deserialize<DeviceData>(responseContent);
            return responsedata;
        }

        public async Task<JResponseModel> GetRealTimeDeviceInfoAsync(FiveMinutesRequest request)
        {
            var api = _configuration["APIs:HuaweiApi"];
            var apiUrl = string.Format("{0}{1}", api, "realTimeInfo");

            // Enviar solicitud a la API de dispositivo
            var requestBody = JsonSerializer.Serialize(request);
            var requestContent = new StringContent(requestBody, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(apiUrl, requestContent);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            // Procesar la respuesta de la API de dispositivo
            string responseContent = await response.Content.ReadAsStringAsync();
            var responsedata = JsonSerializer.Deserialize<JResponseModel>(responseContent);

            return JsonSerializer.Deserialize<JResponseModel>("");
        }
    }
}

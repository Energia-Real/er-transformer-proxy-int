namespace er_transformer_proxy_int.Controllers
{
    using er_transformer_proxy_int.Model;
    using er_transformer_proxy_int.Services.Interfaces;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("[controller]")]
    public class GigaWattProxyController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly IBrandFactory _inverterFactory;

        public GigaWattProxyController(IHttpClientFactory httpClientFactory, IConfiguration configuration, IBrandFactory inverterFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _configuration = configuration;
            _inverterFactory = inverterFactory;
        }

        [HttpPost]
        public async Task<IActionResult> GetDeviceList([FromBody] RequestModel request)
        {
            string brand = request.Brand;
            string plantCode = request.PlantCode;
            try
            {
                var inverter = _inverterFactory.Create(brand.ToLower());
                var devices = await inverter.GetDevicesAsync(plantCode);
                if (devices is null)
                {
                    return NoContent();
                }

                return Ok(devices);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}

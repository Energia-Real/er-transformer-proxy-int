using Newtonsoft.Json;
using ThirdParty.Json.LitJson;

namespace er_transformer_proxy_int.Model
{
    public class ResponseModel<T>
    {
        [Newtonsoft.Json.JsonProperty("success")]
        public bool Success { get; set; }

        [Newtonsoft.Json.JsonProperty("errorMessage")]
        public string? ErrorMessage { get; set; }

        [Newtonsoft.Json.JsonProperty("errorCode")]
        public int? ErrorCode { get; set; }

        [Newtonsoft.Json.JsonProperty("data")]
        public T? Data { get; set; }
    }


    public class JResponseModel
    {
        [Newtonsoft.Json.JsonProperty("success")]
        public bool Success { get; set; }

        [Newtonsoft.Json.JsonProperty("errorMessage")]
        public string? ErrorMessage { get; set; }

        [Newtonsoft.Json.JsonProperty("errorCode")]
        public int? ErrorCode { get; set; }
    }
}
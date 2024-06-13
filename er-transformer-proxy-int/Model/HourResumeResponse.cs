namespace er_transformer_proxy_int.Model
{

    using Newtonsoft.Json;

    public class HourResumeResponse
    {
        public DateTime CollectTime { get; set; }

        [JsonProperty("dischargeCap")]
        public double? DischargeCap { get; set; }

        [JsonProperty("radiation_intensity")]
        public double? RadiationIntensity { get; set; }

        [JsonProperty("inverter_power")]
        public double? InverterPower { get; set; }

        [JsonProperty("inverterYield")]
        public double? InverterYield { get; set; }

        [JsonProperty("power_profit")]
        public double? PowerProfit { get; set; }

        [JsonProperty("theory_power")]
        public double? TheoryPower { get; set; }

        [JsonProperty("PVYield")]
        public double? PVYield { get; set; }

        [JsonProperty("ongrid_power")]
        public double? OnGridPower { get; set; }

        [JsonProperty("chargeCap")]
        public double? ChargeCap { get; set; }

        [JsonProperty("selfProvide")]
        public double? SelfProvide { get; set; }
    }
}

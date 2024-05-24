namespace er_transformer_proxy_int.Model
{
    using Newtonsoft.Json;

    public class MonthResumeResponse
    {
        [JsonProperty("installed_capacity")]
        public double InstalledCapacity { get; set; }

        [JsonProperty("use_power")]
        public double UsePower { get; set; }

        [JsonProperty("inverter_power")]
        public double InverterPower { get; set; }

        [JsonProperty("selfUsePower")]
        public double SelfUsePower { get; set; }

        [JsonProperty("reduction_total_coal")]
        public double ReductionTotalCoal { get; set; }

        [JsonProperty("reduction_total_co2")]
        public double ReductionTotalCo2 { get; set; }

        [JsonProperty("ongrid_power")]
        public double OnGridPower { get; set; }

        [JsonProperty("buyPower")]
        public double BuyPower { get; set; }

        [JsonProperty("selfProvide")]
        public double SelfProvide { get; set; }

        [JsonProperty("perpower_ratio")]
        public double? PerPowerRatio { get; set; }
    }
}

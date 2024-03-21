namespace er_transformer_proxy_int.Model.Dto
{
    public class SiteResume
    {
        public decimal? LifetimeEnergyProdution { get; set; }

        public decimal? LifetimeEnergyConsumption { get; set; }

        public decimal? AvoidedEmmisions { get; set; }

        public decimal? EnergyCoverage { get; set; }

        public decimal? CoincidentSolarConsumptions { get; set; }

        public DateTime? LastConnectionTimeStamp { get; set;}
    }
}

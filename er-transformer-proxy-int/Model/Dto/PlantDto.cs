namespace er_transformer_proxy_int.Model.Dto
{
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization.Attributes;
    using Newtonsoft.Json;

    public class PlantDto
    {
        [BsonId]
        [JsonProperty("_id")]
        public ObjectId Id { get; set; }

        [BsonElement("capacity")]
        [JsonProperty("capacity")]
        public double Capacity { get; set; } // Changed from int to double

        [BsonElement("contactMethod")]
        [JsonProperty("contactMethod")]
        public string ContactMethod { get; set; }

        [BsonElement("contactPerson")]
        [JsonProperty("contactPerson")]
        public string ContactPerson { get; set; }

        [BsonElement("gridConnectionDate")]
        [JsonProperty("gridConnectionDate")]
        public DateTime GridConnectionDate { get; set; }

        [BsonElement("latitude")]
        [JsonProperty("latitude")]
        public string Latitude { get; set; }

        [BsonElement("longitude")]
        [JsonProperty("longitude")]
        public string Longitude { get; set; }

        [BsonElement("plantAddress")]
        [JsonProperty("plantAddress")]
        public string PlantAddress { get; set; }

        [BsonElement("plantCode")]
        [JsonProperty("plantCode")]
        public string PlantCode { get; set; }

        [BsonElement("plantName")]
        [JsonProperty("plantName")]
        public string PlantName { get; set; }
    }

}

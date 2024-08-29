using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace er_transformer_proxy_int.Model.Dto
{
    public class MonthProjectResume
    {
        public ObjectId _id { get; set; }
        public string brandName { get; set; } = "brand";
        public string stationCode { get; set; }
        public DateTime repliedDateTime { get; set; }

        public List<MonthResumeResponse> Monthresume { get; set; }
    }

    public class ProjectResume<T>
    {
        public ObjectId Id { get; set; }

        public string BrandName { get; set; } = "brand";

        public string StationCode { get; set; }

        public DateTime RepliedDateTime { get; set; }

        public List<T> Resume { get; set; }
    }

    // Definición del modelo para la nueva tabla (bitacora) en MongoDB
    public class UpdateLog
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("DateNow")]
        public DateTime DateNow { get; set; }

        [BsonElement("StationCode")]
        public string StationCode { get; set; }

        [BsonElement("OldValue")]
        public double? OldValue { get; set; }

        [BsonElement("NewValue")]
        public double NewValue { get; set; }//nombre de coleccion y campo a impactar usuario preguntar a aaron
    }
}


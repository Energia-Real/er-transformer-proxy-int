﻿using er_transformer_proxy_int.Data.Repository.Interfaces;
using er_transformer_proxy_int.Model.Huawei;
using MongoDB.Driver;

namespace er_transformer_proxy_int.Data.Repository.Adapters
{
    public class MongoAdapter : IMongoRepository
    {
        private readonly IConfiguration _configuration;

        private readonly MongoClient _MongoClient;
        private IMongoDatabase _database;

        public MongoAdapter(IConfiguration configuration)
        {
            _configuration = configuration;
            var connectionString = _configuration["ConnectionStrings:MongoGW"];
            var dataBase = _configuration["DataBases:MongoGWDataBase"];

            _MongoClient = new MongoClient(connectionString);
            _database = _MongoClient.GetDatabase(dataBase);
            _configuration = configuration;
        }

        public async Task<List<Device>> GetDeviceDataAsync(string stationCode)
        {
            try
            {
                var collection = _database.GetCollection<Device>("Devices");

                // Verificar si el índice existe en el campo devDn
                var indexKeysDefinition = Builders<Device>.IndexKeys.Ascending(x => x.stationCode);
                var indexModel = new CreateIndexModel<Device>(indexKeysDefinition);
                var indexExists = await collection.Indexes.CreateOneAsync(indexModel);

                // Si el índice no existe, crearlo
                if (string.IsNullOrEmpty(indexExists))
                {
                    await collection.Indexes.CreateOneAsync(indexModel);
                }

                // Crear un filtro para la consulta
                var filtro = Builders<Device>.Filter.Eq("stationCode", stationCode);

                // Realizar la consulta y obtener el primer resultado que cumpla con el filtro
                var resultado = await collection.Find(filtro).ToListAsync();

                return resultado;
            }
            catch (Exception ex)
            {
                // Manejar la excepción y devolver un resultado vacío o lanzarla nuevamente según sea necesario
                Console.WriteLine($"Error al conectar con la base de datos: {ex.Message}");
                return new List<Device>(); 
            }
        }
    }
}

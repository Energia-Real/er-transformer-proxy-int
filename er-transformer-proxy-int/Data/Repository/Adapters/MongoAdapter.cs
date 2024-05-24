using er_transformer_proxy_int.Data.Repository.Interfaces;
using er_transformer_proxy_int.Model;
using er_transformer_proxy_int.Model.Dto;
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

        public async Task<List<Device>> GetDeviceDataAsyncByCode(string stationCode)
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

        public async Task<PlantDeviceResult> GetRepliedDataAsync(RequestModel request)
        {
            try
            {
                var collection = _database.GetCollection<PlantDeviceResult>("RepliRealtimeData");

                // Crear un filtro para la consulta
                var filter = Builders<PlantDeviceResult>.Filter.And(
                    Builders<PlantDeviceResult>.Filter.Eq("brandName", request.Brand.ToLower()),
                    Builders<PlantDeviceResult>.Filter.Eq("stationCode", request.PlantCode)
                );

                // Ordenar los resultados por repliedDateTime de forma descendente
                var sort = Builders<PlantDeviceResult>.Sort.Descending("repliedDateTime");

                // Realizar la consulta y obtener el primer resultado
                var result = await collection.Find(filter).Sort(sort).Limit(1).ToListAsync();

                return result.FirstOrDefault();
            }
            catch (Exception ex)
            {
                // Manejar la excepción y devolver un resultado vacío o lanzarla nuevamente según sea necesario
                Console.WriteLine($"Error al conectar con la base de datos: {ex.Message}");
                return new PlantDeviceResult();
            }
        }

        public async Task<List<Device>> GetDeviceDataAsync()
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

                // Obtener todos los registros de la colección
                var resultado = await collection.Find(_ => true).ToListAsync();

                return resultado;
            }
            catch (Exception ex)
            {
                // Manejar la excepción y devolver un resultado vacío o lanzarla nuevamente según sea necesario
                Console.WriteLine($"Error al conectar con la base de datos: {ex.Message}");
                return new List<Device>();
            }
        }

        public async Task<List<PlantDto>> GetPlantListAsync()
        {
            try
            {
                var collection = _database.GetCollection<PlantDto>("Plants");

                // Verificar si el índice existe en el campo devDn
                var indexKeysDefinition = Builders<PlantDto>.IndexKeys.Ascending(x => x.PlantCode);
                var indexModel = new CreateIndexModel<PlantDto>(indexKeysDefinition);
                var indexExists = await collection.Indexes.CreateOneAsync(indexModel);

                // Si el índice no existe, crearlo
                if (string.IsNullOrEmpty(indexExists))
                {
                    await collection.Indexes.CreateOneAsync(indexModel);
                }

                // Obtener todos los registros de la colección
                var resultado = await collection.Find(_ => true).ToListAsync();

                return resultado;
            }
            catch (Exception ex)
            {
                // Manejar la excepción y devolver un resultado vacío o lanzarla nuevamente según sea necesario
                Console.WriteLine($"Error al conectar con la base de datos: {ex.Message}");
                return new List<PlantDto>();
            }
        }

        public async Task<List<MonthProjectResume>> GetMonthProjectResumesAsync(RequestModel? requestModel)
        {
            try
            {
                var collection = _database.GetCollection<MonthProjectResume>("RepliMonthProjectResume");

                // Verificar si el índice existe en el campo PlantCode
                var indexKeysDefinition = Builders<MonthProjectResume>.IndexKeys.Ascending(x => x.stationCode);
                var indexModel = new CreateIndexModel<MonthProjectResume>(indexKeysDefinition);
                var indexExists = await collection.Indexes.CreateOneAsync(indexModel);

                // Si el índice no existe, crearlo
                if (string.IsNullOrEmpty(indexExists))
                {
                    await collection.Indexes.CreateOneAsync(indexModel);
                }

                // Construir el filtro según el requestModel
                var filter = requestModel == null
                    ? Builders<MonthProjectResume>.Filter.Empty
                    : Builders<MonthProjectResume>.Filter.And(
                        Builders<MonthProjectResume>.Filter.Eq(x => x.brandName, requestModel.Brand),
                        Builders<MonthProjectResume>.Filter.Eq(x => x.stationCode, requestModel.PlantCode));

                // Obtener los registros de la colección según el filtro
                var resultado = await collection.Find(filter).ToListAsync();

                return resultado;
            }
            catch (Exception ex)
            {
                // Manejar la excepción y devolver un resultado vacío o lanzarla nuevamente según sea necesario
                Console.WriteLine($"Error al conectar con la base de datos: {ex.Message}");
                return new List<MonthProjectResume>();
            }
        }

        public async Task InsertDeviceDataAsync(PlantDeviceResult device)
        {
            try
            {
                var collection = _database.GetCollection<PlantDeviceResult>("RepliRealtimeData");
                device.repliedDateTime = DateTime.Now;

                // Insertar el dispositivo en la colección
                await collection.InsertOneAsync(device);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al insertar en la base de datos: {ex.Message}");
            }
        }

        public async Task InsertMonthResumeDataAsync(MonthProjectResume resume)
        {
            try
            {
                var collection = _database.GetCollection<MonthProjectResume>("RepliMonthProjectResume");

                // Delete all existing records in the collection
                await collection.DeleteManyAsync(Builders<MonthProjectResume>.Filter.Empty);

                // Set the repliedDateTime to the current time
                resume.repliedDateTime = DateTime.Now;

                // Insert the new record into the collection
                await collection.InsertOneAsync(resume);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al insertar en la base de datos: {ex.Message}");
            }
        }
    }
}

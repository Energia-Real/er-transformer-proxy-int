using er_transformer_proxy_int.Data.Repository.Interfaces;
using er_transformer_proxy_int.Model;
using er_transformer_proxy_int.Model.Dto;
using er_transformer_proxy_int.Model.Huawei;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;

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

                // Crear un filtro básico con las condiciones existentes
                var filters = new List<FilterDefinition<PlantDeviceResult>>
                {
                    Builders<PlantDeviceResult>.Filter.Eq("brandName", request.Brand.ToLower()),
                    Builders<PlantDeviceResult>.Filter.Eq("stationCode", request.PlantCode),
                    Builders<PlantDeviceResult>.Filter.Ne("invertersList", new List<DeviceDataResponse<DeviceInverterDataItem>>()),
                    Builders<PlantDeviceResult>.Filter.Ne("metterList", new List<DeviceDataResponse<DeviceMetterDataItem>>())
                };

                // Agregar filtro por fecha para abarcar todo el mes si StartDate no es el valor mínimo
                if (request.StartDate != DateTime.MinValue)
                {
                    var startDate = new DateTime(request.StartDate.Year, request.StartDate.Month, 1);
                    var endDate = new DateTime(request.EndDate.Year, request.EndDate.Month, DateTime.DaysInMonth(request.EndDate.Year, request.EndDate.Month), 23, 59, 59, 999);

                    filters.Add(Builders<PlantDeviceResult>.Filter.Gte("repliedDateTime", startDate));
                    filters.Add(Builders<PlantDeviceResult>.Filter.Lte("repliedDateTime", endDate));
                }

                var filter = Builders<PlantDeviceResult>.Filter.And(filters);

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

        public async Task<List<PlantDeviceResult>> GetRepliedDataListAsync(RequestModel request)
        {
            try
            {
                var collection = _database.GetCollection<PlantDeviceResult>("RepliRealtimeData");

                // Crear un filtro básico con las condiciones existentes
                var filters = new List<FilterDefinition<PlantDeviceResult>>
        {
            Builders<PlantDeviceResult>.Filter.Eq("brandName", request.Brand.ToLower()),
            Builders<PlantDeviceResult>.Filter.Eq("stationCode", request.PlantCode),
            Builders<PlantDeviceResult>.Filter.Ne("invertersList", new List<DeviceDataResponse<DeviceInverterDataItem>>()),
            Builders<PlantDeviceResult>.Filter.Ne("metterList", new List<DeviceDataResponse<DeviceMetterDataItem>>())
        };

                // Agregar filtro por fecha para abarcar todo el mes si StartDate no es el valor mínimo
                if (request.StartDate != DateTime.MinValue)
                {
                    var startDate = new DateTime(request.StartDate.Year, request.StartDate.Month, 1);
                    var endDate = new DateTime(request.EndDate.Year, request.EndDate.Month, DateTime.DaysInMonth(request.EndDate.Year, request.EndDate.Month), 23, 59, 59, 999);

                    filters.Add(Builders<PlantDeviceResult>.Filter.Gte("repliedDateTime", startDate));
                    filters.Add(Builders<PlantDeviceResult>.Filter.Lte("repliedDateTime", endDate));
                }

                var filter = Builders<PlantDeviceResult>.Filter.And(filters);

                // Realizar la consulta y agrupar por día, seleccionando el último registro de cada día
                var aggregate = await collection.Aggregate()
                    .Match(filter)
                    .SortByDescending(p => p.repliedDateTime)
                    .Group(new BsonDocument
                    {
                { "_id", new BsonDocument
                    {
                        { "year", new BsonDocument("$year", "$repliedDateTime") },
                        { "month", new BsonDocument("$month", "$repliedDateTime") },
                        { "day", new BsonDocument("$dayOfMonth", "$repliedDateTime") }
                    }
                },
                { "lastRecord", new BsonDocument("$first", "$$ROOT") }
                    })
                    .Sort(new BsonDocument("_id", 1))  // Ordenar por fecha ascendente
                    .Project(new BsonDocument
                    {
                { "_id", 0 },
                { "lastRecord", 1 }
                    })
                    .ToListAsync();

                // Extraer los resultados desde el campo lastRecord
                var result = aggregate.Select(record => BsonSerializer.Deserialize<PlantDeviceResult>(record["lastRecord"].AsBsonDocument)).ToList();

                return result;
            }
            catch (Exception ex)
            {
                // Manejar la excepción y devolver un resultado vacío o lanzarla nuevamente según sea necesario
                Console.WriteLine($"Error al conectar con la base de datos: {ex.Message}");
                return new List<PlantDeviceResult>();
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
                //await collection.Indexes.CreateOneAsync(indexModel); // No es necesario verificar si el índice existe

                // Construir el filtro según el requestModel
                var filters = new List<FilterDefinition<MonthProjectResume>>();

                if (requestModel != null)
                {
                    if (!string.IsNullOrEmpty(requestModel.Brand))
                    {
                        filters.Add(Builders<MonthProjectResume>.Filter.Eq(x => x.brandName, requestModel.Brand.ToLower()));
                    }

                    if (!string.IsNullOrEmpty(requestModel.PlantCode))
                    {
                        filters.Add(Builders<MonthProjectResume>.Filter.Eq(x => x.stationCode, requestModel.PlantCode));
                    }
                }

                var filter = filters.Count > 0
                    ? Builders<MonthProjectResume>.Filter.And(filters)
                    : Builders<MonthProjectResume>.Filter.Empty;

                // Obtener los registros de la colección según el filtro
                var resultados = await collection.Find(filter).ToListAsync();

                // Filtrar en memoria por el intervalo de fechas en los subdocumentos Monthresume
                if (requestModel != null && requestModel.StartDate != DateTime.MinValue && requestModel.EndDate != DateTime.MinValue)
                {
                    foreach (var resultado in resultados)
                    {
                        resultado.Monthresume = resultado.Monthresume
                            .Where(m => m.CollectTime >= requestModel.StartDate && m.CollectTime <= requestModel.EndDate)
                            .ToList();
                    }

                    // Filtrar los resultados que no tengan subdocumentos Monthresume dentro del rango de fechas
                    resultados = resultados.Where(r => r.Monthresume.Any()).ToList();
                }

                return resultados;
            }
            catch (Exception ex)
            {
                // Manejar la excepción y devolver un resultado vacío o lanzarla nuevamente según sea necesario
                Console.WriteLine($"Error al conectar con la base de datos: {ex.Message}");
                return new List<MonthProjectResume>();
            }
        }

        public async Task<HealtCheckModel> GetHealtCheackAsync(RequestModel request)
        {
            try
            {
                var collection = _database.GetCollection<HealtCheckModel>("RepliHealtCheck");

                // Verificar si el índice existe en el campo stationCode
                var indexKeysDefinition = Builders<HealtCheckModel>.IndexKeys.Ascending(x => x.stationCode);
                var indexModel = new CreateIndexModel<HealtCheckModel>(indexKeysDefinition);
                var indexExists = await collection.Indexes.CreateOneAsync(indexModel);

                // Si el índice no existe, crearlo
                if (string.IsNullOrEmpty(indexExists))
                {
                    await collection.Indexes.CreateOneAsync(indexModel);
                }

                var filter = Builders<HealtCheckModel>.Filter.Eq(x => x.stationCode, request.PlantCode);

                // Obtener el último registro basado en CollectTime
                var resultado = await collection.Find(filter)
                                                .SortByDescending(x => x.collectTime)
                                                .FirstOrDefaultAsync();

                return resultado;
            }
            catch (Exception ex)
            {
                // Manejar la excepción y devolver un resultado vacío o lanzarla nuevamente según sea necesario
                Console.WriteLine($"Error al conectar con la base de datos: {ex.Message}");
                return new HealtCheckModel();
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

        public async Task InsertHourResumeDataAsync(HourProjectResume resume)
        {
            try
            {
                var collection = _database.GetCollection<HourProjectResume>("RepliHourProjectResume");

                // Insert the new record into the collection
                await collection.InsertOneAsync(resume);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al insertar en la base de datos: {ex.Message}");
            }
        }

        public async Task InsertDayResumeDataAsync(DayProjectResume resume)
        {
            try
            {
                var collection = _database.GetCollection<DayProjectResume>("RepliDayProjectResume");

                // Insert the new record into the collection
                await collection.InsertOneAsync(resume);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al insertar en la base de datos: {ex.Message}");
            }
        }

        public async Task InsertHealtCheck(List<HealtCheckModel> resume)
        {
            try
            {
                var collection = _database.GetCollection<HealtCheckModel>("RepliHealtCheck");

                // Insert the new record into the collection
                await collection.InsertManyAsync(resume);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al insertar en la base de datos: {ex.Message}");
            }
        }

        public async Task DeleteManyFromCollection(string collectionName)
        {
            try
            {
                var collection = _database.GetCollection<MonthProjectResume>(collectionName);

                // Delete all existing records in the collection
                await collection.DeleteManyAsync(Builders<MonthProjectResume>.Filter.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al insertar en la base de datos: {ex.Message}");
            }
        }

        public async Task DeleteManyFromCollectionByDate(string collectionName, DateTime date)
        {
            try
            {
                var collection = _database.GetCollection<HourProjectResume>(collectionName);

                // Crear un filtro para eliminar documentos por fecha
                var filter = Builders<HourProjectResume>.Filter.Eq(doc => doc.repliedDateTime, date.Date);

                // Eliminar los documentos que coincidan con el filtro
                var result = await collection.DeleteManyAsync(filter);

                Console.WriteLine($"Se eliminaron {result.DeletedCount} documentos.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar de la base de datos: {ex.Message}");
            }
        }
    }
}

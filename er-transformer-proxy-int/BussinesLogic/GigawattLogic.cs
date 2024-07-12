using er_transformer_proxy_int.Data.Repository.Interfaces;
using er_transformer_proxy_int.Model;
using er_transformer_proxy_int.Model.Dto;
using er_transformer_proxy_int.Model.Gigawatt;
using er_transformer_proxy_int.Model.Huawei;
using er_transformer_proxy_int.Services.Interfaces;
using System.Data;
using System.Text.Json;
using MoreLinq;
using System.Text;
using System.Collections.Generic;
using Microsoft.AspNetCore.Connections.Features;

namespace er_transformer_proxy_int.BussinesLogic
{
    public class GigawattLogic(IMongoRepository repository, IBrandFactory inverterFactory) : IGigawattLogic
    {
        private const double factorEnergia = .438;
        private IMongoRepository _repository = repository;
        private readonly IBrandFactory _inverterFactory = inverterFactory;

        public async Task<ResponseModel<List<CommonTileResponse>>> GetSiteDetails(RequestModel request)
        {
            var response = new ResponseModel<List<CommonTileResponse>> { ErrorCode = 401, Success = false };
            var commonTiles = new List<CommonTileResponse>();

            try
            {
                var plantResponse = await _repository.GetRepliedDataListAsync(request);
                var dailyPlantResponse = await _repository.GetDailyRepliedDataAsync(request);

                if (plantResponse == null || !plantResponse.Any() || plantResponse.FirstOrDefault()?.metterList == null || !plantResponse.First().metterList.Any())

                {
                    response.Success = false;
                    commonTiles.Add(new CommonTileResponse { Title = "No hay datos en tiempo real para el periodo establecido", Value = DateTime.Now.ToString() });
                    response.Data = commonTiles;
                    response.ErrorCode = 204;
                    response.ErrorMessage = "No hay datos en tiempo real para el periodo establecido";
                    return response;
                }

                if (dailyPlantResponse == null || !dailyPlantResponse.Any() || dailyPlantResponse.FirstOrDefault()?.DayResume == null || !dailyPlantResponse.First().DayResume.Any())
                {
                    response.Success = false;
                    commonTiles.Add(new CommonTileResponse { Title = "No hay datos diarios para el periodo establecido", Value = DateTime.Now.ToString() });
                    response.Data = commonTiles;
                    response.ErrorCode = 204;
                    response.ErrorMessage = "No hay datos diarios para el periodo establecido";
                    return response;
                }

                // Asegurarse de tener el primero y el último registro correctamente
                var firstRecord = plantResponse.OrderBy(a => a.repliedDateTime).FirstOrDefault();
                var lastRecord = plantResponse.OrderBy(a => a.repliedDateTime).LastOrDefault();

                if (firstRecord == null || lastRecord == null)
                {
                    response.ErrorCode = 204;
                    response.ErrorMessage = "No hay registros válidos para el periodo establecido";
                    return response;
                }

                // la diferencia es el ultimo menos el primero, lo cual nos da lo que se genero en el intervalo de tiempo de request
                var reverActiveCap = lastRecord.metterList.Sum(a => a.dataItemMap.reverse_active_cap) - firstRecord.metterList.Sum(a => a.dataItemMap.reverse_active_cap);
                var solarConsumption = lastRecord.metterList.Sum(a => a.dataItemMap.total_apparent_power);
                var senderToCFE = lastRecord.metterList.Sum(a => a.dataItemMap.active_cap) - firstRecord.metterList.Sum(a => a.dataItemMap.active_cap);

                // calculo de total_Cap
                var dailyList = dailyPlantResponse.SelectMany(a => a.DayResume);
                var totalLast = dailyList.First(a => a.CollectTime.Day == request.StartDate.Day).PVYield;
                var totalFirst = dailyList.First(a => a.CollectTime.Day == request.EndDate.Day).PVYield;
                double? totalCap = totalLast - totalFirst;

                // calculo de campos finales
                var avoidedEmisions = (totalCap / 1000) * factorEnergia;
                var energyCoverage = totalCap / reverActiveCap ?? 0;
                var consumoSFV = totalCap - senderToCFE;
                var totalRealConsumption = consumoSFV + reverActiveCap;
                var solarcoverageOperation = (totalCap / totalRealConsumption) * 100;
                var solarcoverage = Math.Round((decimal?)solarcoverageOperation ?? 0, 2);

                // realizamos el mapeo de cada tile a devolver
                commonTiles.Add(new CommonTileResponse { Title = "Last connection timeStamp", Value = DateTime.Now.ToString() });
                commonTiles.Add(new CommonTileResponse { Title = "Life Time Energy Production", Value = Convert.ToString(totalCap) });
                commonTiles.Add(new CommonTileResponse { Title = "Life Time Energy Consumption (CFE)", Value = Convert.ToString(reverActiveCap) });
                commonTiles.Add(new CommonTileResponse { Title = "Avoided Emmisions (tCO2e)", Value = Convert.ToString(avoidedEmisions) });
                commonTiles.Add(new CommonTileResponse { Title = "Energy Coverage", Value = Convert.ToString(energyCoverage) });
                commonTiles.Add(new CommonTileResponse { Title = "Coincident Solar Consumption", Value = Convert.ToString(totalCap) });
                commonTiles.Add(new CommonTileResponse { Title = "Solar Coverage", Value = Convert.ToString(solarcoverage) });
                response.ErrorCode = 0;
                response.Success = true;
                response.Data = commonTiles;
            }
            catch (Exception ex)
            {
                response.ErrorCode = 500;
                response.Success = false;
                response.ErrorMessage = $"Error al procesar los datos: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel<List<CommonTileResponse>>> GetOverview(RequestModel request)
        {
            var response = new ResponseModel<List<CommonTileResponse>> { ErrorCode = 401, Success = false };
            var commonTiles = new List<CommonTileResponse>();
            var plantResponse = await this.GetPlantDeviceDataFromMongo(request);
            var metterList = plantResponse.metterList;
            var inverterList = plantResponse.invertersList;

            if (!metterList.Any() && !inverterList.Any())
            {
                plantResponse = await this.GetPlantdeviceData(request);
                response.ErrorMessage = "Sin resultados de mongo.";
            }

            if (!metterList.Any() && !inverterList.Any())
            {
                response.Success = false;
                response.ErrorMessage = "Sin resultados en sistema.";
                return response;
            }

            var installCapacityAC = inverterList.Sum(a => a.dataItemMap.mppt_total_cap);

            double? installCapacityDC = 0;
            foreach (var item in inverterList)
            {
                installCapacityDC = item.dataItemMap.SumMPPTCapacities();
            }

            // realizamos el mapeo de cada tile a devolver
            commonTiles.Add(new CommonTileResponse { Title = "Install capacity (AC)", Value = installCapacityAC.ToString() });
            commonTiles.Add(new CommonTileResponse { Title = "Install capacity (DC)", Value = Convert.ToString(installCapacityDC) });

            response.Data = commonTiles;
            response.Success = true;
            response.ErrorCode = 200;

            return response;
        }

        public async Task<ResponseModel<HealtCheckModel>> GetStationHealtCheck(RequestModel request)
        {
            var response = new ResponseModel<HealtCheckModel> { ErrorCode = 204, Success = false };
            // genera la instancia de la marca correspondiente
            var inverterBrand = _inverterFactory.Create(request.Brand.ToLower());
            if (inverterBrand is null)
            {
                response.ErrorMessage = "La marca indicada no existe";
                return response;
            }

            // obtiene los datos del endpoint de tiempo real
            var devicesRealTimeInfo = await _repository.GetHealtCheackAsync(request);

            if (devicesRealTimeInfo is null)
            {
                response.ErrorMessage = "El registro no existe";
                response.ErrorCode = -1;
                return response;
            }

            response.Data = devicesRealTimeInfo;
            response.Success = true;
            response.ErrorCode = 200;

            return response;
        }

        public async Task<ResponseModel<List<CommonTileResponse>>> GetStationCapacity(RequestModel request)
        {
            var response = new ResponseModel<List<CommonTileResponse>> { ErrorCode = 204, Success = false };
            // genera la instancia de la marca correspondiente
            var inverterBrand = _inverterFactory.Create(request.Brand.ToLower());
            if (inverterBrand is null)
            {
                response.ErrorMessage = "La marca indicada no existe";
                return response;
            }

            var plantResponse = await this.GetPlantDeviceDataFromMongo(request);
            var metterList = plantResponse.metterList;
            var inverterList = plantResponse.invertersList;

            if (!metterList.Any() && !inverterList.Any())
            {
                plantResponse = await this.GetPlantdeviceData(request);
                response.ErrorMessage = "Sin resultados de mongo.";
            }

            if (!metterList.Any() && !inverterList.Any())
            {
                response.Success = false;
                response.ErrorMessage = "Sin resultados en sistema.";
                return response;
            }

            var commonTiles = new List<CommonTileResponse>();

            var plantCapacity = inverterList.Sum(a => a.dataItemMap.mppt_total_cap);

            commonTiles.Add(new CommonTileResponse { Title = "Total Capacity", Value = plantCapacity.ToString() });

            response.Data = commonTiles;
            response.ErrorCode = 200;
            response.Success = true;

            return response;
        }

        public async Task<PlantDeviceResult> GetPlantDeviceDataFromMongo(RequestModel request)
        {
            return await this._repository.GetRepliedDataAsync(request);
        }

        public async Task<List<MonthProjectResume>> GetMonthResume(RequestModel? request)
        {
            var response = await this._repository.GetMonthProjectResumesAsync(request);
            foreach (var item in response)
            {
                item.Monthresume.ForEach(a => { a.InverterPower = a.InverterPower / 1000; a.InverterPower = a.DataRecovery == 0 || a.DataRecovery is null ? a.InverterPower : a.DataRecovery ?? 0; });
            }

            return response;
        }

        public async Task<bool> ReplicateToMongoDb()
        {
            // obtiene de mongo todas las plantas y sus dispositivos
            var proyects = await this._repository.GetDeviceDataAsync();
            if (proyects is null || !proyects.Any())
            {
                return false;
            }

            var plantListMerco = new List<string> { "NE=33778453", "NE=33723147", "NE=33691316", "NE=33761005", "NE=33795293", "NE=33754356" };

            // Filtrar los proyectos que tienen un NE presente en la lista plantListMerco
            var mercoProyects = proyects.Where(p => plantListMerco.Contains(p.stationCode)).ToList();

            // replica todos los datos de todos los dispositivos
            var devices = await this.ReplicateAlldeviceData(mercoProyects);

            // agrupa los dispositivos por proyecto
            foreach (var proyect in mercoProyects.GroupBy(a => a.stationCode))
            {
                var insertIntoMongo = new PlantDeviceResult();
                insertIntoMongo.invertersList = new List<DeviceDataResponse<DeviceInverterDataItem>>();
                insertIntoMongo.metterList = new List<DeviceDataResponse<DeviceMetterDataItem>>();

                // por cada proyecto filtra los dispositivos por tipo para agregarlos a la BD
                foreach (var device in proyect)
                {
                    var tosave = devices.invertersList.FirstOrDefault(a => a.devId == device.deviceId);

                    insertIntoMongo.brandName = "huawei";
                    insertIntoMongo.stationCode = device.stationCode;
                    if (tosave is not null)
                    {
                        insertIntoMongo.invertersList.Add(tosave);
                    }
                    else
                    {
                        var metter = devices.metterList.FirstOrDefault(a => a.devId == device.deviceId);

                        if (metter is not null)
                        {
                            insertIntoMongo.metterList.Add(metter);
                        }
                    }
                }

                await this._repository.InsertDeviceDataAsync(insertIntoMongo);
            }

            return true;
        }

        public async Task<bool> ReplicateMonthResumeToMongo()
        {
            var projectList = await GetProjectList();

            DateTime now = DateTime.UtcNow;

            // Calculate the Unix epoch time in milliseconds
            long collectTime = new DateTimeOffset(now).ToUnixTimeMilliseconds();

            var request = new StationAndCollectTimeRequest();

            request.stationCodes = projectList;
            request.collectTime = collectTime.ToString();

            // genera la instancia de la marca correspondiente
            var inverterBrand = _inverterFactory.Create("huawei");

            // replica los datos del endpoint de la marca correspondiente
            var response = await inverterBrand.GetMonthProjectResume(request);
            var monthResumeList = new List<DeviceDataResponse<MonthResumeResponse>>();
            try
            {
                // deserealiza de Json a objecto
                var monthResume = Newtonsoft.Json.JsonConvert.DeserializeObject<DeviceFiveMinutesResponse<MonthResumeResponse>>(response.Data);

                // los agrega a la lista que vamos a manipular
                monthResumeList.AddRange(monthResume.data);
            }
            catch (Exception ex)
            {
                return false;
            }

            // los agrupa por Codigo de planta o stationCode
            var groupbyCode = monthResumeList.GroupBy(a => a.stationCode).ToList();

            // elimina todos los registros previos de la collection en mongo
            await this._repository.DeleteManyFromCollection("RepliMonthProjectResume");
            foreach (var station in groupbyCode)
            {
                var ListResume = new List<MonthResumeResponse>();

                // genera el nuevo objeto que vamos a inyectar en la collection
                var groupResume = station.Select(a => new MonthResumeResponse
                {
                    CollectTime = DateTimeOffset.FromUnixTimeMilliseconds(a.collectTime ?? 0).DateTime,
                    BuyPower = a.dataItemMap.BuyPower,
                    InstalledCapacity = a.dataItemMap.InstalledCapacity,
                    InverterPower = a.dataItemMap.InverterPower,
                    OnGridPower = a.dataItemMap.OnGridPower,
                    PerPowerRatio = a.dataItemMap.PerPowerRatio,
                    ReductionTotalCo2 = a.dataItemMap.ReductionTotalCo2,
                    ReductionTotalCoal = a.dataItemMap.ReductionTotalCoal,
                    SelfProvide = a.dataItemMap.SelfProvide,
                    SelfUsePower = a.dataItemMap.SelfUsePower,
                    UsePower = a.dataItemMap.UsePower,
                }).ToList();

                ListResume.AddRange(groupResume);
                if (!ListResume.Any())
                {
                    continue;
                }

                // mapeo de MonthResumeResponse a MonthProjectResume
                var resumetoInsert = new MonthProjectResume
                {
                    brandName = "huawei",
                    Monthresume = ListResume,
                    stationCode = station.FirstOrDefault().stationCode
                };

                // inserta en mongo
                await this._repository.InsertMonthResumeDataAsync(resumetoInsert);
            }

            return true;
        }

        public async Task<bool> ReplicateHourlyResumeToMongo()
        {
            var projectList = await GetProjectList();

        start:
            DateTime now = DateTime.UtcNow;

            // Calculate the Unix epoch time in milliseconds
            long collectTime = new DateTimeOffset(now).ToUnixTimeMilliseconds();

            var request = new StationAndCollectTimeRequest();

            request.stationCodes = projectList;
            request.collectTime = collectTime.ToString();

            // genera la instancia de la marca correspondiente
            var inverterBrand = _inverterFactory.Create("huawei");

            // replica los datos del endpoint de la marca correspondiente
            var response = await inverterBrand.GetHourlyProjectResume(request);
            var hourresumeList = new List<DeviceDataResponse<HourResumeResponse>>();
            try
            {
                // deserealiza de Json a objecto
                var hourResume = Newtonsoft.Json.JsonConvert.DeserializeObject<DeviceFiveMinutesResponse<HourResumeResponse>>(response.Data);

                // los agrega a la lista que vamos a manipular
                hourresumeList.AddRange(hourResume.data);
            }
            catch (Exception ex)
            {
                Thread.Sleep(TimeSpan.FromMinutes(5));
                goto start;
                //return false;
            }

            // los agrupa por Codigo de planta o stationCode
            var groupbyCode = hourresumeList.GroupBy(a => a.stationCode).ToList();

            // TODO; validar si se debe de eliminar la informacion previa y en que rango de horas
            await this._repository.DeleteManyFromCollectionByDate("RepliHourProjectResume", now);
            foreach (var station in groupbyCode)
            {
                var ListResume = new List<HourResumeResponse>();

                var groupResume = station.Select(a => new HourResumeResponse
                {
                    CollectTime = DateTimeOffset.FromUnixTimeMilliseconds(a.collectTime ?? 0).DateTime,
                    DischargeCap = a.dataItemMap.DischargeCap,
                    RadiationIntensity = a.dataItemMap.RadiationIntensity,
                    InverterPower = a.dataItemMap.InverterPower,
                    InverterYield = a.dataItemMap.InverterYield,
                    PowerProfit = a.dataItemMap.PowerProfit,
                    TheoryPower = a.dataItemMap.TheoryPower,
                    PVYield = a.dataItemMap.PVYield,
                    OnGridPower = a.dataItemMap.OnGridPower,
                    ChargeCap = a.dataItemMap.ChargeCap,
                    SelfProvide = a.dataItemMap.SelfProvide
                }).ToList();

                ListResume.AddRange(groupResume);
                if (!ListResume.Any())
                {
                    continue;
                }

                // mapeo de MonthResumeResponse a MonthProjectResume
                var resumetoInsert = new HourProjectResume
                {
                    brandName = "huawei",
                    HourResume = ListResume,
                    repliedDateTime = DateTime.Now,
                    stationCode = station.FirstOrDefault().stationCode
                };

                // inserta en mongo
                await this._repository.InsertHourResumeDataAsync(resumetoInsert);
            }

            return true;
        }

        public async Task<bool> ReplicateDailyResumeToMongo()
        {
            var projectList = await GetProjectList();

        start:
            DateTime now = DateTime.UtcNow;

            // Calculate the Unix epoch time in milliseconds
            long collectTime = new DateTimeOffset(now).ToUnixTimeMilliseconds();

            var request = new StationAndCollectTimeRequest();

            request.stationCodes = projectList;
            request.collectTime = collectTime.ToString();

            // genera la instancia de la marca correspondiente
            var inverterBrand = _inverterFactory.Create("huawei");

            // replica los datos del endpoint de la marca correspondiente
            var response = await inverterBrand.GetDailyProjectResume(request);
            var dayResumeList = new List<DeviceDataResponse<DayResumeResponse>>();
            try
            {
                // deserealiza de Json a objecto
                var dayResume = Newtonsoft.Json.JsonConvert.DeserializeObject<DeviceFiveMinutesResponse<DayResumeResponse>>(response.Data);

                // los agrega a la lista que vamos a manipular
                dayResumeList.AddRange(dayResume.data);
            }
            catch (Exception ex)
            {
                Thread.Sleep(TimeSpan.FromMinutes(15));
                goto start;
            }

            // los agrupa por Codigo de planta o stationCode
            var groupbyCode = dayResumeList.GroupBy(a => a.stationCode).ToList();

            // TODO; validar si se debe de eliminar la informacion previa y en que rango de horas
            foreach (var station in groupbyCode)
            {
                var ListResume = new List<DayResumeResponse>();

                // Genera el nuevo objeto que vamos a inyectar en la colección
                var groupResume = station.Select(a => new DayResumeResponse
                {
                    CollectTime = DateTimeOffset.FromUnixTimeMilliseconds(a.collectTime ?? 0).DateTime,
                    InverterPower = a.dataItemMap.InverterPower,
                    InverterYield = a.dataItemMap.InverterYield,
                    SelfUsePower = a.dataItemMap.SelfUsePower,
                    TheoryPower = a.dataItemMap.TheoryPower,
                    PVYield = a.dataItemMap.PVYield,
                    PerPowerRatio = a.dataItemMap.PerPowerRatio,
                    ReductionTotalCo2 = a.dataItemMap.ReductionTotalCo2,
                    PerformanceRatio = a.dataItemMap.PerformanceRatio,
                    SelfProvide = a.dataItemMap.SelfProvide,
                    RadiationIntensity = a.dataItemMap.RadiationIntensity,
                    InstalledCapacity = a.dataItemMap.InstalledCapacity,
                    UsePower = a.dataItemMap.UsePower,
                    ReductionTotalCoal = a.dataItemMap.ReductionTotalCoal,
                    OnGridPower = a.dataItemMap.OnGridPower,
                    BuyPower = a.dataItemMap.BuyPower
                }).ToList();

                ListResume.AddRange(groupResume);
                if (!ListResume.Any())
                {
                    continue;
                }

                // mapeo de MonthResumeResponse a MonthProjectResume
                var resumetoInsert = new DayProjectResume
                {
                    brandName = "huawei",
                    DayResume = ListResume,
                    repliedDateTime = now,
                    stationCode = station.FirstOrDefault().stationCode
                };

                // inserta en mongo
                await this._repository.InsertDayResumeDataAsync(resumetoInsert);
            }

            return true;
        }

        public async Task<bool> ReplicateHealtCheckToMongo()
        {
            var projectList = await this.GetProjectList();
            // genera la instancia de la marca correspondiente
            var inverterBrand = _inverterFactory.Create("huawei");

            // obtiene los datos del endpoint de tiempo real
            var devicesRealTimeInfo = await inverterBrand.GetStationHealtCheck(projectList);

            if (devicesRealTimeInfo.Data is null)
            {
                return false;
            }

            // se agrega la fecha actual a cada registro
            devicesRealTimeInfo.Data.ForEach(a => { a.collectTime = DateTime.Now; });

            // guarda los datos en la bd de mongo
            await _repository.InsertHealtCheck(devicesRealTimeInfo.Data);

            return true;
        }

        private async Task<PlantDeviceResult> ReplicateAlldeviceData(List<Device> devices)
        {
            // genera la instancia de la marca correspondiente
            var inverterBrand = _inverterFactory.Create("huawei");

            // separa en lista segun el tipo de dispositivo
            var inverters = devices.Where(a => a.devTypeId == 1).ToList();
            var metters = devices.Where(a => a.devTypeId == 17).ToList();

            // lo separa en grupos de 100 ya que el endpoint solo acepta un maximo de 100
            var gruposDe100 = inverters.Batch(100).ToList();

            // instancias de respuesta
            var responseAlldevices = new PlantDeviceResult();
            responseAlldevices.invertersList = new List<DeviceDataResponse<DeviceInverterDataItem>>();
            responseAlldevices.metterList = new List<DeviceDataResponse<DeviceMetterDataItem>>();

            var inverterlist = new List<DeviceDataResponse<DeviceInverterDataItem>>();
            var metterlist = new List<DeviceDataResponse<DeviceMetterDataItem>>();

            // genera el request para replicar los inversores
            foreach (var group in gruposDe100)
            {
                var devIdsList = new List<string>();
                foreach (var item in group)
                {
                    devIdsList.Add(item.deviceId.ToString());
                }

                var devIds = string.Join(",", devIdsList);
                var realtimeRequest = new FiveMinutesRequest
                {
                    devIds = devIds,
                    devTypeId = 1
                };

                // manda el request al brand del dispositivo
                var devicesRealTimeInfo = await inverterBrand.GetRealTimeDeviceInfo(realtimeRequest);
                try
                {
                    var inverter = Newtonsoft.Json.JsonConvert.DeserializeObject<DeviceFiveMinutesResponse<DeviceInverterDataItem>>(devicesRealTimeInfo.Data);

                    inverterlist.AddRange(inverter.data);
                }
                catch (Exception ex)
                {
                    Thread.Sleep(300000);
                    continue;
                }
            }

            responseAlldevices.invertersList = inverterlist;

            // genera el request para replicar los medidores
            foreach (var device in metters.Batch(100).ToList())
            {
                var devIdsList = new List<string>();
                foreach (var item in device)
                {
                    devIdsList.Add(item.deviceId.ToString());
                }

                var devIds = string.Join(",", devIdsList);
                var realtimeRequest = new FiveMinutesRequest
                {
                    devIds = devIds,
                    devTypeId = 17
                };

                // manda el request al brand del dispositivo
                var devicesRealTimeInfo = await inverterBrand.GetRealTimeDeviceInfo(realtimeRequest);
                try
                {
                    var metter = Newtonsoft.Json.JsonConvert.DeserializeObject<DeviceFiveMinutesResponse<DeviceMetterDataItem>>(devicesRealTimeInfo.Data);

                    metterlist.AddRange(metter.data);
                }
                catch (Exception ex)
                {
                    Thread.Sleep(300000);
                    continue;
                }
            }

            responseAlldevices.metterList = metterlist;

            return responseAlldevices;
        }

        private async Task<PlantDeviceResult> GetPlantdeviceData(RequestModel request)
        {
            // obtiene los dispositivos de la planta consultada
            var device = await this._repository.GetDeviceDataAsyncByCode(request.PlantCode);

            // genera la instancia de la marca correspondiente
            var inverterBrand = _inverterFactory.Create(request.Brand.ToLower());

            // agrupa el resultado por tipo de dispositivo 1: inverter, 17: Metter, 63: logger
            var groupedDevices = device.GroupBy(a => a.devTypeId);

            string devIds = string.Empty;
            var deviceResult = new PlantDeviceResult();
            deviceResult.metterList = new List<DeviceDataResponse<DeviceMetterDataItem>>();
            deviceResult.invertersList = new List<DeviceDataResponse<DeviceInverterDataItem>>();
            // itera los tipos para extraer
            foreach (var group in groupedDevices)
            {
                var devType = group.First().devTypeId;

                //  si nos regresa el logger lo omitimos ya que no trae nada al consultar el endpoint
                if (devType == 63)
                {
                    continue;
                }

                var devIdsList = new List<string>();
                foreach (var item in group)
                {
                    devIdsList.Add(item.deviceId.ToString());
                }

                devIds = string.Join(",", devIdsList);
                var realtimeRequest = new FiveMinutesRequest
                {
                    devIds = devIds,
                    devTypeId = devType ?? 0
                };

                // obtiene los datos del endpoint de tiempo real
                var devicesRealTimeInfo = await inverterBrand.GetRealTimeDeviceInfo(realtimeRequest);

                if (devicesRealTimeInfo.Data.ToUpper().Contains("ACCESS_FREQUENCY_IS_TOO_HIGH"))
                {
                    Thread.Sleep(300000);
                    devicesRealTimeInfo = await inverterBrand.GetRealTimeDeviceInfo(realtimeRequest);
                }

                if (!devicesRealTimeInfo.Success || devicesRealTimeInfo.Data is null)
                {
                    continue;
                }

                if (devicesRealTimeInfo.ErrorMessage is not null && (devicesRealTimeInfo.Data.ToUpper().Contains("USER_MUST_RELOGIN") || devicesRealTimeInfo.Data.ToUpper().Contains("ACCESS_FREQUENCY_IS_TOO_HIGH")))
                {
                    continue;
                }

                // genera las listas por tipo para el mapeo de los datos
                if (devType == 1)
                {
                    var inverter = JsonSerializer.Deserialize<DeviceFiveMinutesResponse<DeviceInverterDataItem>>(devicesRealTimeInfo.Data);

                    if (inverter is null)
                    {
                        continue;
                    }

                    deviceResult.invertersList.AddRange(inverter.data);
                }
                else
                {
                    var metter = JsonSerializer.Deserialize<DeviceFiveMinutesResponse<DeviceMetterDataItem>>(devicesRealTimeInfo.Data);
                    if (metter is null)
                    {
                        continue;
                    }

                    deviceResult.metterList.AddRange(metter.data);
                }
            }

            return deviceResult;
        }

        private async Task<string> GetProjectList()
        {
            // obtiene de mongo todas las plantas
            var projects = await this._repository.GetPlantListAsync();
            if (projects is null || !projects.Any())
            {
                return string.Empty;
            }
            StringBuilder projectListBuilder = new StringBuilder();

            foreach (var project in projects)
            {
                projectListBuilder.Append(project.PlantCode).Append(",");
            }

            // Remove the last comma if the StringBuilder is not empty
            if (projectListBuilder.Length > 0)
            {
                projectListBuilder.Length--; // Reduce the length by one to remove the last comma
            }

            return projectListBuilder.ToString();
        }
    }
}
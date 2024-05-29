﻿using Amazon.Runtime.Internal;
using er.library.dto.Response;
using er_transformer_proxy_int.Data.Repository.Interfaces;
using er_transformer_proxy_int.Model;
using er_transformer_proxy_int.Model.Dto;
using er_transformer_proxy_int.Model.Gigawatt;
using er_transformer_proxy_int.Model.Huawei;
using er_transformer_proxy_int.Services.Interfaces;
using System.Data;
using System.Text.Json;
using MoreLinq;
using Amazon.Runtime.Internal.Transform;
using System.Text;

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
            var plantResponse = await this.GetPlantDeviceDataFromMongo(request);

            if (plantResponse is null)
            {
                plantResponse = await this.GetPlantdeviceData(request);
            }

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

            // realiza el calculo de los datos que devolvera cada tile
            var reverActiveCap = metterList.Sum(a => a.dataItemMap.reverse_active_cap);
            var totalCap = inverterList.Sum(a => a.dataItemMap.total_cap);
            var avoidedEmisions = (totalCap / 1000) * factorEnergia;
            var energyCoverage = totalCap / reverActiveCap;

            // validar este ultimo
            var solarConsumption = metterList.Sum(a => a.dataItemMap.total_apparent_power);

            // realizamos el mapeo de cada tile a devolver
            commonTiles.Add(new CommonTileResponse { Title = "Last connection timeStamp", Value = DateTime.Now.ToString() });
            commonTiles.Add(new CommonTileResponse { Title = "Life Time Energy Production", Value = Convert.ToString(totalCap) });
            commonTiles.Add(new CommonTileResponse { Title = "Life Time Energy Consumption (CFE)", Value = Convert.ToString(reverActiveCap) });
            commonTiles.Add(new CommonTileResponse { Title = "Avoided Emmisions (tCO2e)", Value = Convert.ToString(avoidedEmisions) });
            commonTiles.Add(new CommonTileResponse { Title = "Energy Coverage", Value = Convert.ToString(energyCoverage) });
            commonTiles.Add(new CommonTileResponse { Title = "Coincident Solar Consumption", Value = Convert.ToString(solarConsumption) });

            response.ErrorCode = 0;
            response.Success = true;
            response.Data = commonTiles;

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
            var devicesRealTimeInfo = await inverterBrand.GetStationHealtCheck(request.PlantCode);

            if (!devicesRealTimeInfo.Success)
            {
                response.ErrorMessage = "La marca indicada no existe => " + devicesRealTimeInfo.ErrorMessage;
                response.ErrorCode = devicesRealTimeInfo.ErrorCode;
                return response;
            }

            response.Data = devicesRealTimeInfo.Data;
            response.Success = devicesRealTimeInfo.Success;

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
            return await this._repository.GetMonthProjectResumesAsync(request);
        }

        public async Task<bool> ReplicateToMongoDb()
        {
            // obtiene de mongo todas las plantas y sus dispositivos
            var proyects = await this._repository.GetDeviceDataAsync();
            if (proyects is null || !proyects.Any())
            {
                return false;
            }

            // replica todos los datos de todos los dispositivos
            var devices = await this.ReplicateAlldeviceData(proyects);

            // agrupa los dispositivos por proyecto
            foreach (var proyect in proyects.GroupBy(a => a.stationCode))
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
            // obtiene de mongo todas las plantas
            var projects = await this._repository.GetPlantListAsync();
            if (projects is null || !projects.Any())
            {
                return false;
            }

            DateTime now = DateTime.UtcNow;

            // Calculate the Unix epoch time in milliseconds
            long collectTime = new DateTimeOffset(now).ToUnixTimeMilliseconds();

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

            var projectList = projectListBuilder.ToString();

            var request = new StationAndCollectTimeRequest();

            request.stationCodes = projectList;
            request.collectTime = collectTime.ToString();

            // genera la instancia de la marca correspondiente
            var inverterBrand = _inverterFactory.Create("huawei");

            var response = await inverterBrand.GetMonthProjectResume(request);
            var monthResumeList = new List<DeviceDataResponse<MonthResumeResponse>>();
            try
            {
                var monthResume = Newtonsoft.Json.JsonConvert.DeserializeObject<DeviceFiveMinutesResponse<MonthResumeResponse>>(response.Data);

                monthResumeList.AddRange(monthResume.data);
            }
            catch (Exception ex)
            {
                return false;
            }


            var resumeList = new List<DeviceDataResponse<MonthResumeResponse>>();
            var groupbyCode = monthResumeList.GroupBy(a => a.stationCode).ToList();

            foreach (var station in groupbyCode)
            {
                var ListResume = new List<MonthResumeResponse>();

                var groupResume = station.Select(a => a.dataItemMap).ToList();

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

                await this._repository.InsertMonthResumeDataAsync(resumetoInsert);
            }


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
    }
}
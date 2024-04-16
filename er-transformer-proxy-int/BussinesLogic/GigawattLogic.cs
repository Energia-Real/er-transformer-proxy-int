using Amazon.Runtime.Internal;
using er.library.dto.Response;
using er_transformer_proxy_int.Data.Repository.Interfaces;
using er_transformer_proxy_int.Model;
using er_transformer_proxy_int.Model.Dto;
using er_transformer_proxy_int.Model.Gigawatt;
using er_transformer_proxy_int.Model.Huawei;
using er_transformer_proxy_int.Services.Interfaces;
using System;
using System.Data;
using System.Reflection;
using System.Text.Json;

namespace er_transformer_proxy_int.BussinesLogic
{
    public class GigawattLogic : IGigawattLogic
    {
        private const double factorEnergia = .438;
        private IMongoRepository _repository;
        private readonly IBrandFactory _inverterFactory;

        public GigawattLogic(IMongoRepository repository, IBrandFactory inverterFactory)
        {
            _inverterFactory = inverterFactory;
            _repository = repository;
        }

        public async Task<ResponseModel<List<CommonTileResponse>>> GetSiteDetails(RequestModel request)
        {
            var response = new ResponseModel<List<CommonTileResponse>> { ErrorCode = 401, Success = false };
            var commonTiles = new List<CommonTileResponse>();
            var plantResponse = await this.GetPlantdeviceData(request);
            var metterList = plantResponse.metterList;
            var inverterList = plantResponse.invertersList;

            if (!metterList.Any() && !inverterList.Any())
            {
                response.ErrorMessage = "Sin resultados.";
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
            var plantResponse = await this.GetPlantdeviceData(request);
            var metterList = plantResponse.metterList;
            var inverterList = plantResponse.invertersList;

            if (!metterList.Any() && !inverterList.Any())
            {
                response.ErrorMessage = "Sin resultados.";
                return response;
            }

            var installCapacityAC = inverterList.Sum(a=>a.dataItemMap.mppt_total_cap);

            double installCapacityDC = 0; 
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
            var response = new ResponseModel<HealtCheckModel> { ErrorCode= 204, Success=false };
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

            var plantResponse = await this.GetPlantdeviceData(request);
            var metterList = plantResponse.metterList;
            var inverterList = plantResponse.invertersList;

            if (!metterList.Any() && !inverterList.Any())
            {
                response.ErrorMessage = "Sin resultados.";
                return response;
            }

            var commonTiles = new List<CommonTileResponse>();

            var plantCapacity = inverterList.Sum(a=>a.dataItemMap.mppt_total_cap);

            commonTiles.Add(new CommonTileResponse { Title = "Total Capacity", Value = plantCapacity.ToString() });

            response.Data = commonTiles;
            response.ErrorCode = 200;
            response.Success = true;

            return response;
        }

        private async Task<PlantDeviceResult> GetPlantdeviceData(RequestModel request)
        {
            // obtiene los dispositivos de la planta consultada
            var device = await this._repository.GetDeviceDataAsync(request.PlantCode);

            // genera la instancia de la marca correspondiente
            var inverterBrand = _inverterFactory.Create(request.Brand.ToLower());

            // agrupa el resultado por tipo de dispositivo 1: inverter, 17: Metter, 63: logger
            var groupedDevices = device.GroupBy(a => a.devTypeId);

            string devIds = string.Empty;
            var deviceResult = new PlantDeviceResult();
            deviceResult.metterList = new List<DeviceDataResponse<DeviceMetterDataItem>>();
            deviceResult.invertersList=new List<DeviceDataResponse<DeviceInverterDataItem>>();
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
                if (!devicesRealTimeInfo.Success || devicesRealTimeInfo.Data is null)
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

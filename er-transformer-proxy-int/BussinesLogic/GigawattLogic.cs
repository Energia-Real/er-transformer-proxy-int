using er_transformer_proxy_int.Data.Repository.Interfaces;
using er_transformer_proxy_int.Model;
using er_transformer_proxy_int.Model.Dto;
using er_transformer_proxy_int.Model.Gigawatt;
using er_transformer_proxy_int.Services.Interfaces;
using System;
using System.Data;
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
            // obtiene los dispositivos de la planta consultada
            var device = await this._repository.GetDeviceDataAsync(request.PlantCode);

            // genera la instancia de la marca correspondiente
            var inverterBrand = _inverterFactory.Create(request.Brand.ToLower());

            // agrupa el resultado por tipo de dispositivo 1: inverter, 17: Metter, 63: logger
            var groupedDevices = device.GroupBy(a => a.devTypeId);

            string devIds = string.Empty;
            var commonTiles = new List<CommonTileResponse>();
            var invertersList = new List<DeviceDataResponse<DeviceInverterDataItem>>();
            var metterList = new List<DeviceDataResponse<DeviceMetterDataItem>>();

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
                if (!devicesRealTimeInfo.Success)
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

                    invertersList.AddRange(inverter.data);
                }
                else
                {
                    var metter = JsonSerializer.Deserialize<DeviceFiveMinutesResponse<DeviceMetterDataItem>>(devicesRealTimeInfo.Data);
                    if (metter is null)
                    {
                        continue;
                    }

                    metterList.AddRange(metter.data);
                }
            }

            if (!metterList.Any() && !invertersList.Any())
            {
                response.ErrorMessage = "Sin resultados.";
            }

            var reverActiveCap = metterList.Sum(a => a.dataItemMap.reverse_active_cap);
            var totalCap = invertersList.Sum(a => a.dataItemMap.total_cap);
            var avoidedEmisions = (totalCap / 1000) * factorEnergia;
            var energyCoverage = totalCap / reverActiveCap;

            // validar este ultimo
            var solarConsumption = metterList.Sum(a => a.dataItemMap.total_apparent_power);
            // Todo: hacer calculo por cada tile
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
            // obtiene los dispositivos de la planta consultada
            var device = await this._repository.GetDeviceDataAsync(request.PlantCode);

            // genera la instancia de la marca correspondiente
            var inverterBrand = _inverterFactory.Create(request.Brand.ToLower());



            return response;
        }
    }
}

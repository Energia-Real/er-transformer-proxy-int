using er_transformer_proxy_int.Model;
using er_transformer_proxy_int.Model.Huawei;
using er_transformer_proxy_int.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace er_transformer_proxy_int.Controllers
{
    public static class IntegratorProxyEndPoints
    {
        private static IBrandFactory _inverterFactory;

        public static void SetBrandFactory(IBrandFactory inverterFactory)
        {
            _inverterFactory = inverterFactory;
        }
        public static void RegisterIntegratorEndpoints(this IEndpointRouteBuilder routes)
        {
            var routeBuilder = routes.MapGroup("/api/v1/integrators/proxy");

            GetDeviceList(routeBuilder);
            GetSiteDetailsByPlant(routeBuilder);
        }

        private static void GetDeviceList(RouteGroupBuilder rgb)
        {
            rgb.MapPost("GetDeviceList", async (HttpContext context, [FromBody] RequestModel request) =>
            {
                string brand = request.Brand;
                string plantCode = request.PlantCode;
                try
                {
                    var inverter = _inverterFactory.Create(brand.ToLower());
                    var devices = await inverter.GetDevicesAsync(plantCode);
                    if (devices is null)
                    {
                        return Results.NoContent();
                    }

                    return Results.Ok(devices);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(ex.Message);
                }
            })
                .Produces(200, typeof(DeviceData))
            .Produces(204)
            .WithTags("Proxy")
            .WithName("GetDeviceList")
            .WithOpenApi();
        }

        private static void GetSiteDetailsByPlant(RouteGroupBuilder rgb)
        {
            rgb.MapPost("GetSiteDetailsByPlant", async (HttpContext context, [FromBody] RequestModel request) =>
            {
                string brand = request.Brand;
                string plantCode = request.PlantCode;
                try
                {
                    var inverter = _inverterFactory.Create(brand.ToLower());
                    var response = await inverter.GetSiteDetailByPlantsAsync(plantCode);
                    if (response is null || response.Data is null)
                    {
                        return Results.NoContent();
                    }

                    return Results.Ok(response);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(ex.Message);
                }
            })
                .Produces(200, typeof(DeviceData))
            .Produces(204)
            .WithTags("Proxy")
            .WithName("GetSiteDetailsByPlant")
            .WithOpenApi();
        }
    }
}

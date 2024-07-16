namespace er_transformer_proxy_int.Controllers
{
    using er_transformer_proxy_int.BussinesLogic;
    using er_transformer_proxy_int.Model;
    using er_transformer_proxy_int.Model.Gigawatt;
    using er_transformer_proxy_int.Model.Huawei;
    using er_transformer_proxy_int.Services.Interfaces;
    using Microsoft.AspNetCore.Mvc;

    public static class IntegratorProxyEndPoints
    {
        private static IBrandFactory _inverterFactory;
        private static IGigawattLogic bussineslogic;
        public static void SetBrandFactory(IBrandFactory inverterFactory, IGigawattLogic gigawattLogic)
        {
            _inverterFactory = inverterFactory;
            bussineslogic = gigawattLogic;
        }
        public static void RegisterIntegratorEndpoints(this IEndpointRouteBuilder routes)
        {
            var routeBuilder = routes.MapGroup("/api/v1/integrators/proxy");

            GetDeviceList(routeBuilder);
            GetSiteDetailsByPlant(routeBuilder);
            GetOverviewByPlant(routeBuilder);
            GetStationHealtCheck(routeBuilder);
            GetInstalledCapacity(routeBuilder);
            GetMonthProyectResume(routeBuilder);
            ReplicateToMongoDb(routeBuilder);
            ReplicateMonthProjectResumeToMongoDb(routeBuilder);
            ReplicateDayProjectResumeToMongoDb(routeBuilder);
            ReplicateHourProjectResumeToMongoDb(routeBuilder);
            ReplicateHeltCheckToMongoDb(routeBuilder);
            GetGlobalSolarCoverage(routeBuilder);
        }

        private static void GetDeviceList(RouteGroupBuilder rgb)
        {
            rgb.MapPost("getDeviceList", async (HttpContext context, [FromBody] RequestModel request) =>
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
                    return Results.BadRequest(ex);
                }
            })
                .Produces(200, typeof(DeviceData))
            .Produces(204)
            .WithTags("Proxy")
            .WithName("getDeviceList")
            .WithOpenApi();
        }

        private static void GetSiteDetailsByPlant(RouteGroupBuilder rgb)
        {
            rgb.MapPost("getSiteDetailsByPlant", async (HttpContext context, [FromBody] RequestModel request) =>
            {
                string brand = request.Brand;
                string plantCode = request.PlantCode;
                try
                {
                    var siteDetail = await bussineslogic.GetSiteDetails(request);
                    if (siteDetail is null || !siteDetail.Success)
                    {
                        return Results.NoContent();
                    }

                    return Results.Ok(siteDetail);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(ex);
                }
            })
                .Produces(200, typeof(ResponseModel<List<CommonTileResponse>>))
            .Produces(204)
            .WithTags("Proxy")
            .WithName("getSiteDetailsByPlant")
            .WithOpenApi();
        }

        private static void GetOverviewByPlant(RouteGroupBuilder rgb)
        {
            rgb.MapPost("GetOverviewByPlant", async (HttpContext context, [FromBody] RequestModel request) =>
            {
                string brand = request.Brand;
                string plantCode = request.PlantCode;
                try
                {
                    var siteDetail = await bussineslogic.GetOverview(request);
                    if (siteDetail is null || !siteDetail.Success)
                    {
                        return Results.NoContent();
                    }

                    return Results.Ok(siteDetail);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(ex);
                }
            })
                .Produces(200, typeof(ResponseModel<List<CommonTileResponse>>))
            .Produces(204)
            .WithTags("Proxy")
            .WithName("GetOverviewByPlant")
            .WithOpenApi();
        }

        private static void GetStationHealtCheck(RouteGroupBuilder rgb)
        {
            rgb.MapPost("getStationHealtCheck", async (HttpContext context, [FromBody] RequestModel request) =>
            {
                try
                {
                    // obtiene el estado actual de la planta
                    var siteDetail = await bussineslogic.GetStationHealtCheck(request);

                    if (siteDetail is null || !siteDetail.Success)
                    {
                        return Results.NoContent();
                    }

                    return Results.Ok(siteDetail);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(ex);
                }
            })
                .Produces(200, typeof(ResponseModel<List<CommonTileResponse>>))
            .Produces(204)
            .WithTags("Proxy")
            .WithName("getStationHealtCheck")
            .WithOpenApi();
        }

        private static void GetInstalledCapacity(RouteGroupBuilder rgb)
        {
            rgb.MapPost("getInstalledCapacity", async (HttpContext context, [FromBody] RequestModel request) =>
            {
                try
                {
                    var siteDetail = await bussineslogic.GetStationCapacity(request);
                    if (siteDetail is null || !siteDetail.Success)
                    {
                        return Results.NoContent();
                    }

                    return Results.Ok(siteDetail);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(ex);
                }
            })
                .Produces(200, typeof(ResponseModel<List<CommonTileResponse>>))
            .Produces(204)
            .WithTags("Proxy")
            .WithName("getInstalledCapacity")
            .WithOpenApi();
        }

        private static void GetMonthProyectResume(RouteGroupBuilder rgb)
        {
            rgb.MapPost("GetMonthProyectResume", async (HttpContext context, [FromBody] RequestModel? request) =>
            {
                try
                {
                    var resumeResult = await bussineslogic.GetMonthResume(request);
                    if (!resumeResult.Any())
                    {
                        return Results.NoContent();
                    }

                    return Results.Ok(resumeResult);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(ex);
                }
            })
            .Produces(200)
            .Produces(204)
            .WithTags("Proxy")
            .WithName("GetMonthProyectResume")
            .WithOpenApi();
        }

        private static void GetGlobalSolarCoverage(RouteGroupBuilder rgb)
        {
            rgb.MapPost("GetGlobalSolarCoverage", async (HttpContext context, [FromBody] RequestModel? request) =>
            {
                try
                {
                    var resumeResult = await bussineslogic.GetGlobalSolarCoverage(request);
                    if (!resumeResult.Data.Any())
                    {
                        return Results.NoContent();
                    }

                    return Results.Ok(resumeResult);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(ex);
                }
            })
            .Produces(200)
            .Produces(204)
            .WithTags("Proxy")
            .WithName("GetGlobalSolarCoverage")
            .WithOpenApi();
        }

        private static void ReplicateToMongoDb(RouteGroupBuilder rgb)
        {
            rgb.MapPost("replicateToMongoDb", async (HttpContext context) =>
            {
                try
                {
                    var replicateResult = await bussineslogic.ReplicateToMongoDb();
                    if (!replicateResult)
                    {
                        return Results.NoContent();
                    }

                    return Results.Ok(replicateResult);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(ex);
                }
            })
                .Produces(200)
            .Produces(204)
            .WithTags("Proxy")
            .WithName("replicateToMongoDb")
            .WithOpenApi();
        }

        private static void ReplicateMonthProjectResumeToMongoDb(RouteGroupBuilder rgb)
        {
            rgb.MapPost("ReplicateMonthProjectResumeToMongoDb", async (HttpContext context) =>
            {
                try
                {
                    var replicateResult = await bussineslogic.ReplicateMonthResumeToMongo();
                    if (!replicateResult)
                    {
                        return Results.NoContent();
                    }

                    return Results.Ok(replicateResult);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(ex);
                }
            })
                .Produces(200)
            .Produces(204)
            .WithTags("Proxy")
            .WithName("ReplicateMonthProjectResumeToMongoDb")
            .WithOpenApi();
        }

        private static void ReplicateDayProjectResumeToMongoDb(RouteGroupBuilder rgb)
        {
            rgb.MapPost("ReplicateDayProjectResumeToMongoDb", async (HttpContext context) =>
            {
                try
                {
                    var replicateResult = await bussineslogic.ReplicateDailyResumeToMongo();
                    if (!replicateResult)
                    {
                        return Results.NoContent();
                    }

                    return Results.Ok(replicateResult);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(ex);
                }
            })
                .Produces(200)
            .Produces(204)
            .WithTags("Proxy")
            .WithName("ReplicateDayProjectResumeToMongoDb")
            .WithOpenApi();
        }

        private static void ReplicateHourProjectResumeToMongoDb(RouteGroupBuilder rgb)
        {
            rgb.MapPost("ReplicateHourProjectResumeToMongoDb", async (HttpContext context) =>
            {
                try
                {
                    var replicateResult = await bussineslogic.ReplicateHourlyResumeToMongo();
                    if (!replicateResult)
                    {
                        return Results.NoContent();
                    }

                    return Results.Ok(replicateResult);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(ex);
                }
            })
                .Produces(200)
            .Produces(204)
            .WithTags("Proxy")
            .WithName("ReplicateHourProjectResumeToMongoDb")
            .WithOpenApi();
        }

        private static void ReplicateHeltCheckToMongoDb(RouteGroupBuilder rgb)
        {
            rgb.MapPost("ReplicateHealtCheck", async (HttpContext context) =>
            {
                try
                {
                    var replicateResult = await bussineslogic.ReplicateHealtCheckToMongo();
                    if (!replicateResult)
                    {
                        return Results.NoContent();
                    }

                    return Results.Ok(replicateResult);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(ex);
                }
            })
                .Produces(200)
            .Produces(204)
            .WithTags("Proxy")
            .WithName("ReplicateHealtCheck")
            .WithOpenApi();
        }
    }
}

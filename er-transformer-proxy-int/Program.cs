using er_transformer_proxy_int.Configurations.Swagger;
using er_transformer_proxy_int.Configurations;

var builder = WebApplication.CreateBuilder(args);
Authentication.Config(ref builder);
Swagger.Config(ref builder);

builder.RegisterServices();

builder.Services.AddCors(options => options.AddPolicy("AllowOrigin", builder =>
{
    builder
    .AllowAnyMethod()
    .AllowAnyHeader()
    .WithOrigins("http://localhost:9000",
                 "https://er-portal.azurewebsites.net",
                 "https://portal.energiareal.mx",
                 "https://er-transformer-huawei-replicador.azurewebsites.net")
    .AllowCredentials();
}));

var app = builder.Build();
app.UseRouting();
app.UseAuthentication();
app.RegisterMiddlewares();
app.Run();
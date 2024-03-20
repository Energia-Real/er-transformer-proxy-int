using er_transformer_proxy_int.Configurations.Swagger;
using er_transformer_proxy_int.Configurations;

var builder = WebApplication.CreateBuilder(args);
Authentication.Config(ref builder);
Swagger.Config(ref builder);

builder.RegisterServices();

var app = builder.Build();
app.UseRouting();
app.UseAuthentication();
app.RegisterMiddlewares();
app.Run();

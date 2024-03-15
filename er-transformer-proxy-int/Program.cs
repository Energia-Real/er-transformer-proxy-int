using er_transformer_proxy_int.Data.Repository.Interfaces;
using er_transformer_proxy_int.Services.Interfaces;
using er_transformer_proxy_int.Services;
using er_transformer_proxy_int.Data.Repository.Adapters;
using er_transformer_proxy_int.Configurations.Swagger;
using er_transformer_proxy_int.Configurations;

var builder = WebApplication.CreateBuilder(args);
Authentication.Config(ref builder);
Swagger.Config(ref builder);
// Add services to the container.
builder.Services.AddControllers();

// Add configuration from appconfig.json
builder.Configuration.AddJsonFile("appsettings.json");

// Register IConfiguration in the service container
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddHttpClient();

// Register IHuaweiRepository in the service container
builder.Services.AddSingleton<IHuaweiRepository, HuaweiAdapter>(); // Ajusta la implementación según tu código

// Register IBrandFactory in the service container
builder.Services.AddSingleton<IBrandFactory, BrandFactory>(); // Ajusta la implementación según tu código

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

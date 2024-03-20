using er_transformer_proxy_int.Configurations.Attributes;
using er_transformer_proxy_int.Data.Repository.Adapters;
using er_transformer_proxy_int.Data.Repository.Interfaces;
using er_transformer_proxy_int.Services.Interfaces;
using er_transformer_proxy_int.Services;
using er_transformer_proxy_int.Controllers;

namespace er_transformer_proxy_int.Configurations
{
    public static class Configuration
    {
        public static void RegisterServices(this WebApplicationBuilder builder)
        {
            builder.Configuration.AddJsonFile("appsettings.json");
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddHttpClient();

            // Register IConfiguration in the service container
            builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

            builder.Services.AddScoped<ValidationFilterAttribute>();

            // Register IHuaweiRepository in the service container
            builder.Services.AddSingleton<IHuaweiRepository, HuaweiAdapter>();

            // Register IBrandFactory in the service container
            builder.Services.AddSingleton<IBrandFactory, BrandFactory>();

            // Configuración de la dependencia _inverterFactory
            IntegratorProxyEndPoints.SetBrandFactory(builder.Services.BuildServiceProvider().GetService<IBrandFactory>());
        }

        public static void RegisterMiddlewares(this WebApplication app)
        {
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();

            _ = app.UseEndpoints(endpoints =>
            {
                endpoints.RegisterIntegratorEndpoints();
            });
        }
    }
}

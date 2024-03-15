using er_transformer_proxy_int.Data.Repository.Interfaces;
using er_transformer_proxy_int.Services.Interfaces;
using System;

namespace er_transformer_proxy_int.Services
{
    public class BrandFactory : IBrandFactory
    {
        private readonly Dictionary<string, Func<IBrand>> _factories;

        public BrandFactory(IHuaweiRepository huawei)
        {
            _factories = new Dictionary<string, Func<IBrand>>();

            // Agregar aqui el resto de las marcas siempre en minusculas
            Register("huawei", () => new HuaweiService(huawei));
        }

        public void Register(string brand, Func<IBrand> factory)
        {
            _factories[brand] = factory;
        }

        public IBrand Create(string brand)
        {
            if (_factories.TryGetValue(brand, out Func<IBrand> factory))
            {
                return factory();
            }

            throw new NotSupportedException($"Brand '{brand}' is not supported");
        }
    }

}

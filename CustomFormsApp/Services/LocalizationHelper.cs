using Microsoft.Extensions.Localization;
using System.Reflection;

namespace CustomFormsApp.Services
{
    public class LocalizationHelper
    {
        private readonly IStringLocalizerFactory _factory;

        public LocalizationHelper(IStringLocalizerFactory factory)
        {
            _factory = factory;
        }

        public IStringLocalizer GetLocalizer(Type resourceType)
        {
            return _factory.Create(resourceType);
        }

        // This method doesn't use generic type argument for the factory
        public IStringLocalizer<T> GetLocalizer<T>() where T : class
        {
            return (IStringLocalizer<T>)_factory.Create(typeof(T));
        }
    }
}
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace DigiTekShop.API.Extensions.TrimmingModel
{
    public class TrimmingModelBinderProvider : IModelBinderProvider
    {
        private readonly bool _convertEmptyToNull;
        public TrimmingModelBinderProvider(bool convertEmptyToNull = true)
            => _convertEmptyToNull = convertEmptyToNull;

        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context.Metadata.ModelType == typeof(string))
            {
                var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();

                var fallback = new SimpleTypeModelBinder(typeof(string), loggerFactory);

                return new TrimmingModelBinder(fallback, _convertEmptyToNull);
            }

            return null;
        }
    }
}
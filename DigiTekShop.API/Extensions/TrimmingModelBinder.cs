namespace DigiTekShop.API.Extensions
{
    using Microsoft.AspNetCore.Mvc.ModelBinding;

    public class TrimmingModelBinder : IModelBinder
    {
        private readonly IModelBinder _fallback;
        private readonly bool _convertEmptyToNull;

        public TrimmingModelBinder(IModelBinder fallback, bool convertEmptyToNull = true)
            => (_fallback, _convertEmptyToNull) = (fallback, convertEmptyToNull);

        public async Task BindModelAsync(ModelBindingContext ctx)
        {
            if (ctx.ModelType != typeof(string))
            {
                await _fallback.BindModelAsync(ctx);
                return;
            }

            var r = ctx.ValueProvider.GetValue(ctx.ModelName);
            if (r == ValueProviderResult.None)
            {
                await _fallback.BindModelAsync(ctx);
                return;
            }

            
            var name = ctx.ModelName;
            var skipTrim = name.EndsWith("Password", StringComparison.OrdinalIgnoreCase)
                           || name.EndsWith("ConfirmPassword", StringComparison.OrdinalIgnoreCase);

            ctx.ModelState.SetModelValue(ctx.ModelName, r);
            var raw = r.FirstValue;

            if (raw is null)
            {
                ctx.Result = ModelBindingResult.Success(null);
                return;
            }

            if (skipTrim)
            {
                ctx.Result = ModelBindingResult.Success(raw); // بدون Trim
                return;
            }

            var trimmed = raw.Trim();

            if (_convertEmptyToNull && trimmed.Length == 0)
            {
                ctx.Result = ModelBindingResult.Success(null);
                return;
            }

            ctx.Result = ModelBindingResult.Success(trimmed);
        }
    }

}

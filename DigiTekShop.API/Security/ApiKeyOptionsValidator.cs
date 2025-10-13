using Microsoft.Extensions.Options;

namespace DigiTekShop.API.Security
{
    public sealed class ApiKeyOptionsValidator : IValidateOptions<ApiKeyOptions>
    {
        public ValidateOptionsResult Validate(string? name, ApiKeyOptions options)
        {
            if (!options.Enabled) return ValidateOptionsResult.Success;
            if (options.ValidKeys is null || options.ValidKeys.Length == 0)
                return ValidateOptionsResult.Fail("ApiKey:ValidKeys must have at least one key when ApiKey.Enabled=true.");
            if (string.IsNullOrWhiteSpace(options.HeaderName))
                return ValidateOptionsResult.Fail("ApiKey:HeaderName must be non-empty.");
            return ValidateOptionsResult.Success;
        }
    }
}

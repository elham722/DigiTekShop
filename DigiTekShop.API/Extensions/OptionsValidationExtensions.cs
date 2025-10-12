using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace DigiTekShop.API.Extensions;


public static class OptionsValidationExtensions
{
    public static IServiceCollection AddOptionsValidation(this IServiceCollection services, IConfiguration configuration)
    {
        // Database options
        services.Configure<DatabaseOptions>(configuration.GetSection("Database"));
        services.AddSingleton<IValidateOptions<DatabaseOptions>, DatabaseOptionsValidator>();

        // Redis options
        services.Configure<RedisOptions>(configuration.GetSection("Redis"));
        services.AddSingleton<IValidateOptions<RedisOptions>, RedisOptionsValidator>();

        // JWT options
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();

        // SMTP options
        services.Configure<SmtpOptions>(configuration.GetSection("Smtp"));
        services.AddSingleton<IValidateOptions<SmtpOptions>, SmtpOptionsValidator>();

        // API options
        services.Configure<ApiOptions>(configuration.GetSection("Api"));
        services.AddSingleton<IValidateOptions<ApiOptions>, ApiOptionsValidator>();

        return services;
    }
}


public class DatabaseOptions
{
    [Required(ErrorMessage = "ConnectionString is required")]
    public string ConnectionString { get; set; } = string.Empty;

    [Range(1, 300, ErrorMessage = "CommandTimeout must be between 1 and 300 seconds")]
    public int CommandTimeout { get; set; } = 30;

    [Range(1, 10, ErrorMessage = "MaxRetryCount must be between 1 and 10")]
    public int MaxRetryCount { get; set; } = 3;

    [Range(1, 60, ErrorMessage = "MaxRetryDelay must be between 1 and 60 seconds")]
    public int MaxRetryDelay { get; set; } = 30;
}


public class RedisOptions
{
    [Required(ErrorMessage = "ConnectionString is required")]
    public string ConnectionString { get; set; } = string.Empty;

    [Required(ErrorMessage = "InstanceName is required")]
    public string InstanceName { get; set; } = string.Empty;

    [Range(1, 3600, ErrorMessage = "DefaultDatabase must be between 1 and 3600")]
    public int DefaultDatabase { get; set; } = 0;
}


public class JwtOptions
{
    [Required(ErrorMessage = "SecretKey is required")]
    [MinLength(32, ErrorMessage = "SecretKey must be at least 32 characters")]
    public string SecretKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "Issuer is required")]
    public string Issuer { get; set; } = string.Empty;

    [Required(ErrorMessage = "Audience is required")]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 1440, ErrorMessage = "AccessTokenExpirationMinutes must be between 1 and 1440")]
    public int AccessTokenExpirationMinutes { get; set; } = 60;

    [Range(1, 10080, ErrorMessage = "RefreshTokenExpirationDays must be between 1 and 10080")]
    public int RefreshTokenExpirationDays { get; set; } = 7;
}


public class SmtpOptions
{
    [Required(ErrorMessage = "Host is required")]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
    public int Port { get; set; } = 587;

    [Required(ErrorMessage = "Username is required")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;

    public bool EnableSsl { get; set; } = true;

    [Range(1, 300, ErrorMessage = "Timeout must be between 1 and 300 seconds")]
    public int Timeout { get; set; } = 30;
}


public class ApiOptions
{
    [Required(ErrorMessage = "Title is required")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Version is required")]
    public string Version { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    public string Description { get; set; } = string.Empty;

    [Range(1, 1000, ErrorMessage = "MaxPageSize must be between 1 and 1000")]
    public int MaxPageSize { get; set; } = 100;

    [Range(1, 100, ErrorMessage = "DefaultPageSize must be between 1 and 100")]
    public int DefaultPageSize { get; set; } = 10;
}

public class DatabaseOptionsValidator : IValidateOptions<DatabaseOptions>
{
    public ValidateOptionsResult Validate(string? name, DatabaseOptions options)
    {
        var validationResults = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            validationResults.Add("Database:ConnectionString is required");

        if (options.CommandTimeout < 1 || options.CommandTimeout > 300)
            validationResults.Add("Database:CommandTimeout must be between 1 and 300 seconds");

        if (options.MaxRetryCount < 1 || options.MaxRetryCount > 10)
            validationResults.Add("Database:MaxRetryCount must be between 1 and 10");

        return validationResults.Count > 0 
            ? ValidateOptionsResult.Fail(validationResults) 
            : ValidateOptionsResult.Success;
    }
}

public class RedisOptionsValidator : IValidateOptions<RedisOptions>
{
    public ValidateOptionsResult Validate(string? name, RedisOptions options)
    {
        var validationResults = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            validationResults.Add("Redis:ConnectionString is required");

        if (string.IsNullOrWhiteSpace(options.InstanceName))
            validationResults.Add("Redis:InstanceName is required");

        return validationResults.Count > 0 
            ? ValidateOptionsResult.Fail(validationResults) 
            : ValidateOptionsResult.Success;
    }
}

public class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var validationResults = new List<string>();

        if (string.IsNullOrWhiteSpace(options.SecretKey) || options.SecretKey.Length < 32)
            validationResults.Add("Jwt:SecretKey must be at least 32 characters");

        if (string.IsNullOrWhiteSpace(options.Issuer))
            validationResults.Add("Jwt:Issuer is required");

        if (string.IsNullOrWhiteSpace(options.Audience))
            validationResults.Add("Jwt:Audience is required");

        return validationResults.Count > 0 
            ? ValidateOptionsResult.Fail(validationResults) 
            : ValidateOptionsResult.Success;
    }
}

public class SmtpOptionsValidator : IValidateOptions<SmtpOptions>
{
    public ValidateOptionsResult Validate(string? name, SmtpOptions options)
    {
        var validationResults = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Host))
            validationResults.Add("Smtp:Host is required");

        if (options.Port < 1 || options.Port > 65535)
            validationResults.Add("Smtp:Port must be between 1 and 65535");

        if (string.IsNullOrWhiteSpace(options.Username))
            validationResults.Add("Smtp:Username is required");

        if (string.IsNullOrWhiteSpace(options.Password))
            validationResults.Add("Smtp:Password is required");

        return validationResults.Count > 0 
            ? ValidateOptionsResult.Fail(validationResults) 
            : ValidateOptionsResult.Success;
    }
}

public class ApiOptionsValidator : IValidateOptions<ApiOptions>
{
    public ValidateOptionsResult Validate(string? name, ApiOptions options)
    {
        var validationResults = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Title))
            validationResults.Add("Api:Title is required");

        if (string.IsNullOrWhiteSpace(options.Version))
            validationResults.Add("Api:Version is required");

        if (string.IsNullOrWhiteSpace(options.Description))
            validationResults.Add("Api:Description is required");

        return validationResults.Count > 0 
            ? ValidateOptionsResult.Fail(validationResults) 
            : ValidateOptionsResult.Success;
    }
}

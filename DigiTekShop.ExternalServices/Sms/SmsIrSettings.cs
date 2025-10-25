namespace DigiTekShop.ExternalServices.Sms;

public class SmsIrSettings
{
    public string BaseUrl { get; set; } = "https://api.sms.ir/v1/";
    public string ApiKey { get; set; } = string.Empty;
    public string ApiKeyHeaderName { get; set; } = "x-api-key";

    public string UltraFastPath { get; set; } = "send/verify";
    public string SimpleSendPath { get; set; } = "send/bulk";

    public int TemplateId { get; set; } = 123456;      // Sandbox
    public string TemplateParamName { get; set; } = "Code";

    public string? DefaultLineNumber { get; set; } = null;
    public int TimeoutSeconds { get; set; } = 15;
    public bool LogRequestBody { get; set; } = false;
}
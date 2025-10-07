namespace DigiTekShop.Identity.Options.Security;

public class SecuritySettings
{
    public BruteForceSettings BruteForce { get; init; } = new();
    
    public StepUpSettings StepUp { get; init; } = new();
    
    public TokenSecuritySettings TokenSecurity { get; init; } = new();
}
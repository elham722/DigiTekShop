namespace DigiTekShop.SharedKernel.Enums.Auth;

public enum MfaMethod
{
    Totp = 1,    
    Sms = 2,
    Email = 3,
    BackupCode = 4
}
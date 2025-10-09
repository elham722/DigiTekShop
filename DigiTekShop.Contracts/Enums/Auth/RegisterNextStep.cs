namespace DigiTekShop.Contracts.Enums.Auth
{
    public enum RegisterNextStep
    {
        None = 0,
        ConfirmEmail = 1,
        VerifyPhone = 2,
        SetupTwoFactor = 3
    }
}

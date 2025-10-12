namespace DigiTekShop.Contracts.Abstractions.ExternalServices.EmailSender;
public interface IEmailTemplateService
{
    EmailConfirmationContent BuildEmailConfirmation(string confirmUrl, string companyName = "DigiTekShop");
}
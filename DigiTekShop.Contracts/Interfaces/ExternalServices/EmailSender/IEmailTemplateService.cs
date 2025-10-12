using DigiTekShop.Contracts.Auth.EmailConfirmation;

namespace DigiTekShop.Contracts.Interfaces.ExternalServices.EmailSender;
public interface IEmailTemplateService
{
    EmailConfirmationContent BuildEmailConfirmation(string confirmUrl, string companyName = "DigiTekShop");
}
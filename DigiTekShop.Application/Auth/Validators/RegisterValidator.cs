using System.Net;
using System.Text.RegularExpressions;
using DigiTekShop.Contracts.DTOs.Auth.Register;
using FluentValidation;

namespace DigiTekShop.Application.Auth.Validators;

public class RegisterValidator : AbstractValidator<RegisterRequestDto>
{
    private static readonly Regex DeviceIdPattern = new(@"^[A-Za-z0-9_\-\.]{1,100}$", RegexOptions.Compiled);
    private static readonly Regex E164 = new(@"^\+[1-9]\d{7,14}$", RegexOptions.Compiled);
    private static readonly Regex StrongPassword =
        new(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9])[\S]{8,128}$", RegexOptions.Compiled);

    public RegisterValidator()
    {
        // Email
        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(254) // RFC-friendly
            .EmailAddress();

        // Password / Confirm
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .Matches(StrongPassword)
            .WithMessage("Password must contain upper, lower, digit and symbol.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password)
            .WithMessage("ConfirmPassword must match Password.");

        // Terms
        RuleFor(x => x.AcceptTerms)
            .Equal(true)
            .WithMessage("You must accept terms and conditions.");

        // Phone (optional)
        When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber), () =>
        {
            RuleFor(x => x.PhoneNumber!)
                .Matches(E164).WithMessage("PhoneNumber must be in E.164 format (e.g., +98912...).")
                .MaximumLength(20);
        });

        // DeviceId (optional)
        When(x => !string.IsNullOrWhiteSpace(x.DeviceId), () =>
        {
            RuleFor(x => x.DeviceId!)
                .Must(d => DeviceIdPattern.IsMatch(d))
                .WithMessage("DeviceId contains invalid characters or is too long.");
        });

        // UserAgent (optional)
        When(x => !string.IsNullOrWhiteSpace(x.UserAgent), () =>
        {
            RuleFor(x => x.UserAgent!)
                .MaximumLength(512);
        });

        // IP (optional)
        When(x => !string.IsNullOrWhiteSpace(x.Ip), () =>
        {
            RuleFor(x => x.Ip!)
                .Must(ip => IPAddress.TryParse(ip, out _))
                .WithMessage("Invalid IP address.");
        });
    }
}

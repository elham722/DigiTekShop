using System.Net;
using System.Text.RegularExpressions;
using DigiTekShop.Contracts.DTOs.Auth.Register;
using FluentValidation;

namespace DigiTekShop.Application.Auth.Validators.Register;

public class RegisterValidator : AbstractValidator<RegisterRequestDto>
{
    private static readonly Regex DeviceIdPattern = new(@"^[A-Za-z0-9_\-\.]{1,100}$", RegexOptions.Compiled);
    private static readonly Regex E164 = new(@"^\+[1-9]\d{7,14}$", RegexOptions.Compiled);
    private static readonly Regex StrongPassword =
        new(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9])\S{8,128}$", RegexOptions.Compiled);

    public RegisterValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;


        // Email
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("{PropertyName} الزامی است.")
            .MaximumLength(254).WithMessage("{PropertyName} نباید بیش از {MaxLength} کاراکتر باشد.")
            .EmailAddress().WithMessage("{PropertyName} معتبر نیست.")
            .Must(v => v == null || v == v.Trim()).WithMessage("{PropertyName} نباید فاصله‌ی ابتدا/پایان داشته باشد.");

        // Password / Confirm
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("{PropertyName} الزامی است.")
            .MinimumLength(8).WithMessage("{PropertyName} باید حداقل {MinLength} کاراکتر باشد.")
            .MaximumLength(128).WithMessage("{PropertyName} نباید بیش از {MaxLength} کاراکتر باشد.")
            .Matches(StrongPassword).WithMessage("{PropertyName} باید شامل حروف بزرگ، حروف کوچک، عدد و نماد باشد.")
            .Must((dto, pwd) => dto.Email is null || !pwd.Contains(dto.Email, StringComparison.OrdinalIgnoreCase))
                .WithMessage("{PropertyName} نباید شامل ایمیل باشد.")
            .Must((dto, pwd) => string.IsNullOrWhiteSpace(dto.PhoneNumber) || !pwd.Contains(dto.PhoneNumber!, StringComparison.OrdinalIgnoreCase))
                .WithMessage("{PropertyName} نباید شامل شماره موبایل باشد.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("{PropertyName} باید با «رمز عبور» یکسان باشد.");

        // Terms
        RuleFor(x => x.AcceptTerms)
            .Equal(true).WithMessage("برای ادامه، {PropertyName} ضروری است.");

        When(x => x.AcceptTerms, () =>
        {
            RuleFor(x => x.AcceptTermsVersion)
                .NotEmpty().WithMessage("{PropertyName} در صورت پذیرش قوانین الزامی است.")
                .MaximumLength(32).WithMessage("{PropertyName} نباید بیش از {MaxLength} کاراکتر باشد.");

            RuleFor(x => x.AcceptedTermsAtUtc)
                .Must(dt => dt == null || dt <= DateTimeOffset.UtcNow.AddMinutes(2))
                .WithMessage("{PropertyName} نمی‌تواند در آینده باشد.");
        });

        // Phone (optional)
        When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber), () =>
        {
            RuleFor(x => x.PhoneNumber!)
                .Matches(E164).WithMessage("{PropertyName} باید در قالب E.164 باشد (مثلاً ‎+98912...).")
                .MaximumLength(20).WithMessage("{PropertyName} نباید بیش از {MaxLength} کاراکتر باشد.")
                .Must(v => v == v.Trim()).WithMessage("{PropertyName} نباید فاصله‌ی ابتدا/پایان داشته باشد.");
        });

        // DeviceId (optional)
        When(x => !string.IsNullOrWhiteSpace(x.DeviceId), () =>
        {
            RuleFor(x => x.DeviceId!)
                .Must(d => DeviceIdPattern.IsMatch(d))
                .WithMessage("{PropertyName} شامل کاراکتر نامعتبر است یا بیش از حد بلند است.");
        });

        // UserAgent (optional)
        When(x => !string.IsNullOrWhiteSpace(x.UserAgent), () =>
        {
            RuleFor(x => x.UserAgent!).MaximumLength(512)
                .WithMessage("{PropertyName} نباید بیش از {MaxLength} کاراکتر باشد.");
        });

        // IP (optional)
        When(x => !string.IsNullOrWhiteSpace(x.IpAddress), () =>
        {
            RuleFor(x => x.IpAddress!)
                .Must(ip => IPAddress.TryParse(ip, out _))
                .WithMessage("{PropertyName} معتبر نیست.");
        });
    }
}

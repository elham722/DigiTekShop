
using DigiTekShop.Application.Auth.Register.Command;
using FluentValidation;
using System.Net;
using System.Text.RegularExpressions;

namespace DigiTekShop.Application.Auth.Register.Validator;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    private static readonly Regex DeviceIdPattern = new(@"^[A-Za-z0-9_\-\.]{1,100}$", RegexOptions.Compiled);
    private static readonly Regex E164 = new(@"^\+[1-9]\d{7,14}$", RegexOptions.Compiled);
    private static readonly Regex StrongPassword =
        new(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9])\S{8,128}$", RegexOptions.Compiled);

    public RegisterUserCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        // Email
        RuleFor(x => x.Dto.Email)
            .NotEmpty().WithName("ایمیل")
            .WithMessage("{PropertyName} الزامی است.")
            .MaximumLength(254).WithMessage("{PropertyName} نباید بیش از {MaxLength} کاراکتر باشد.")
            .EmailAddress().WithMessage("{PropertyName} معتبر نیست.")
            .Must(v => v == null || v == v.Trim()).WithMessage("{PropertyName} نباید فاصله‌ی ابتدا/پایان داشته باشد.");

        // Password / Confirm
        RuleFor(x => x.Dto.Password)
            .NotEmpty().WithName("رمز عبور").
            WithMessage("{PropertyName} الزامی است.")
            .MinimumLength(8).WithMessage("{PropertyName} باید حداقل {MinLength} کاراکتر باشد.")
            .MaximumLength(128).WithMessage("{PropertyName} نباید بیش از {MaxLength} کاراکتر باشد.")
            .Matches(StrongPassword).WithMessage("{PropertyName} باید شامل حروف بزرگ، حروف کوچک، عدد و نماد باشد.")
            .Must((cmd, pwd) => cmd.Dto.Email is null || !pwd.Contains(cmd.Dto.Email, StringComparison.OrdinalIgnoreCase))
                .WithMessage("{PropertyName} نباید شامل ایمیل باشد.")
            .Must((cmd, pwd) => string.IsNullOrWhiteSpace(cmd.Dto.PhoneNumber) || !pwd.Contains(cmd.Dto.PhoneNumber!, StringComparison.OrdinalIgnoreCase))
                .WithMessage("{PropertyName} نباید شامل شماره موبایل باشد.");

        RuleFor(x => x.Dto.ConfirmPassword)
            .Equal(x => x.Dto.Password)
            .WithName("تأیید رمز عبور").WithMessage("{PropertyName} باید با «رمز عبور» یکسان باشد.");

        // Terms
        RuleFor(x => x.Dto.AcceptTerms)
            .Equal(true)
            .WithName("پذیرش قوانین")
            .WithMessage("برای ادامه، {PropertyName} ضروری است.");

        When(x => x.Dto.AcceptTerms, () =>
        {
            RuleFor(x => x.Dto.AcceptTermsVersion)
                .NotEmpty().WithName("نسخه قوانین")
                .WithMessage("{PropertyName} در صورت پذیرش قوانین الزامی است.")
                .MaximumLength(32).WithMessage("{PropertyName} نباید بیش از {MaxLength} کاراکتر باشد.");

            RuleFor(x => x.Dto.AcceptedTermsAtUtc)
                .Must(dt => dt == null || dt <= DateTimeOffset.UtcNow.AddMinutes(2))
                .WithName("زمان پذیرش قوانین")
                .WithMessage("{PropertyName} نمی‌تواند در آینده باشد.");
        });

        // Phone (optional)
        When(x => !string.IsNullOrWhiteSpace(x.Dto.PhoneNumber), () =>
        {
            RuleFor(x => x.Dto.PhoneNumber!)
                .Matches(E164).WithName("شماره موبایل")
                .WithMessage("{PropertyName} باید در قالب E.164 باشد (مثلاً ‎+98912...).")
                .MaximumLength(20).WithMessage("{PropertyName} نباید بیش از {MaxLength} کاراکتر باشد.")
                .Must(v => v == v.Trim()).WithMessage("{PropertyName} نباید فاصله‌ی ابتدا/پایان داشته باشد.");
        });

        // DeviceId (optional)
        When(x => !string.IsNullOrWhiteSpace(x.Dto.DeviceId), () =>
        {
            RuleFor(x => x.Dto.DeviceId!)
                .Must(d => DeviceIdPattern.IsMatch(d))
                .WithName("شناسه دستگاه")
                .WithMessage("{PropertyName} شامل کاراکتر نامعتبر است یا بیش از حد بلند است.");
        });

        // UserAgent (optional)
        When(x => !string.IsNullOrWhiteSpace(x.Dto.UserAgent), () =>
        {
            RuleFor(x => x.Dto.UserAgent!).MaximumLength(512)
                .WithName("مرورگر/دستگاه")
                .WithMessage("{PropertyName} نباید بیش از {MaxLength} کاراکتر باشد.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Dto.IpAddress), () =>
        {
            RuleFor(x => x.Dto.IpAddress!)
                .Must(ip =>
                {
                    var first = ip.Split(',')[0].Trim();
                    return IPAddress.TryParse(first, out _);
                })
                .WithName("آدرس IP")
                .WithMessage("{PropertyName} معتبر نیست.");
        });
    }
}

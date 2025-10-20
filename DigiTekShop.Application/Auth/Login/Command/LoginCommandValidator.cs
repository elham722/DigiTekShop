using System.Text.RegularExpressions;

namespace DigiTekShop.Application.Auth.Login.Command;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    private static readonly Regex EmailRegex =
        new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);

    public LoginCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Dto).NotNull();

        RuleFor(x => x.Dto.Login)
            .NotEmpty().WithMessage("login الزامی است.")
            .MaximumLength(256)
            .Must(IsValidLogin).WithMessage("login باید ایمیل معتبر یا نام‌کاربری 3 تا 64 کاراکتری باشد.");

        RuleFor(x => x.Dto.Password)
            .NotEmpty().WithMessage("password الزامی است.")
            .MaximumLength(256);

        RuleFor(x => x.Dto.TotpCode).MaximumLength(16);
        RuleFor(x => x.Dto.CaptchaToken).MaximumLength(2048);
    }

    private static bool IsValidLogin(string? login)
    {
        if (string.IsNullOrWhiteSpace(login)) return false;
        return login.Contains('@') ? EmailRegex.IsMatch(login) : login.Length is >= 3 and <= 64;
    }
}
using DigiTekShop.Application.Auth.LoginOrRegister.Commands;
using DigiTekShop.SharedKernel.Utilities.Text;
using System.Text.RegularExpressions;

namespace DigiTekShop.Application.Auth.LoginOrRegister.Validators;
public sealed class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Dto).NotNull();

        RuleFor(x => x.Dto.Phone)
            .NotEmpty().WithMessage("شماره موبایل الزامی است.")
            .MaximumLength(32)
            .Must(BeValidIranPhone).WithMessage("شماره موبایل معتبر ایران نیست.");

        RuleFor(x => x.Dto.Code)
            .NotEmpty().WithMessage("کد OTP الزامی است.")
            .Length(4, 8).WithMessage("طول کد OTP باید بین 4 تا 8 باشد.")
            .Matches(new Regex(@"^\d+$")).WithMessage("کد OTP باید فقط عدد باشد.");
    }

    private static bool BeValidIranPhone(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var e164 = Normalization.NormalizePhoneIranE164(raw);
        return !string.IsNullOrWhiteSpace(e164) && e164.StartsWith("+98") && e164.Length == 13;
    }
}
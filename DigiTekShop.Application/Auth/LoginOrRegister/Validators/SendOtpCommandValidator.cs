using DigiTekShop.Application.Auth.LoginOrRegister.Command;
using DigiTekShop.SharedKernel.Utilities.Text;

namespace DigiTekShop.Application.Auth.LoginOrRegister.Validators;
public sealed class SendOtpCommandValidator : AbstractValidator<SendOtpCommand>
{
    public SendOtpCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Dto).NotNull();

        RuleFor(x => x.Dto.Phone)
            .NotEmpty().WithMessage("شماره موبایل الزامی است.")
            .MaximumLength(32)
            .Must(BeValidIranPhone).WithMessage("شماره موبایل معتبر ایران نیست.");

        RuleFor(x => x.Dto.DeviceId)
            .MaximumLength(64);

    }

    private static bool BeValidIranPhone(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var e164 = Normalization.NormalizePhoneIranE164(raw);
        return !string.IsNullOrWhiteSpace(e164) && e164.StartsWith("+98") && e164.Length == 13;
    }
}

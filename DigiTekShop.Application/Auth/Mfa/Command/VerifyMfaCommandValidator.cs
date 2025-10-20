namespace DigiTekShop.Application.Auth.Mfa.Command;
public sealed class VerifyMfaCommandValidator : AbstractValidator<VerifyMfaCommand>
{
    public VerifyMfaCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Dto).NotNull();

        RuleFor(x => x.Dto.UserId).NotEmpty();

        RuleFor(x => x.Dto.Method).IsInEnum();

        RuleFor(x => x.Dto.Code)
            .NotEmpty().WithMessage("code الزامی است.")
            .MaximumLength(32);
    }
}
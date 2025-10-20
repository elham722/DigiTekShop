namespace DigiTekShop.Application.Auth.LogoutAll.Command;
public sealed class LogoutAllCommandValidator : AbstractValidator<LogoutAllCommand>
{
    public LogoutAllCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Dto).NotNull();
        RuleFor(x => x.Dto.Reason).MaximumLength(512);
    }
}
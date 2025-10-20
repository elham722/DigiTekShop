namespace DigiTekShop.Application.Auth.Logout.Command;
public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Dto).NotNull();

        RuleFor(x => x.Dto.RefreshToken).MaximumLength(4096);
    }
}
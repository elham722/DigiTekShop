namespace DigiTekShop.Application.Auth.Logout.Command;
public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Dto.UserId).NotEmpty();

        RuleFor(x => x.Dto.RefreshToken).NotEmpty().WithMessage("refresh_token الزامی است.").MaximumLength(4096);
    }
}
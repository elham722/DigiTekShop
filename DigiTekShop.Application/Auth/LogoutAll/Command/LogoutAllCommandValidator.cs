namespace DigiTekShop.Application.Auth.LogoutAll.Command;
public sealed class LogoutAllCommandValidator : AbstractValidator<LogoutAllCommand>
{
    public LogoutAllCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Dto.UserId).NotEmpty().WithMessage("userId الزامی است.");
        RuleFor(x => x.Dto.Reason).MaximumLength(512);
    }
}
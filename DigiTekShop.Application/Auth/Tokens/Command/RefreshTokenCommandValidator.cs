namespace DigiTekShop.Application.Auth.Tokens.Command;
public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Dto).NotNull();

        RuleFor(x => x.Dto.RefreshToken)
            .NotEmpty().WithMessage("refreshToken الزامی است.")
            .MaximumLength(4096);
    }
}
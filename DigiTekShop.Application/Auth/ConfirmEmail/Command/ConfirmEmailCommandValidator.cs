using System.Text.RegularExpressions;

namespace DigiTekShop.Application.Auth.ConfirmEmail.Command
{
    public sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
    {
        private static readonly Regex Base64UrlPattern = new(@"^[A-Za-z0-9\-_]+$", RegexOptions.Compiled);

        public ConfirmEmailCommandValidator()
        {
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.Dto.UserId)
                .NotEmpty().WithName("شناسهٔ کاربر")
                .WithMessage("{PropertyName} الزامی است.");

            RuleFor(x => x.Dto.Token)
                .NotEmpty().WithName("توکن")
                .WithMessage("{PropertyName} الزامی است.")
                .MaximumLength(4096).WithMessage("{PropertyName} نباید بیش از {MaxLength} کاراکتر باشد.")
                .Must(t => t == t.Trim())
                .WithMessage("{PropertyName} نباید فاصله‌ی ابتدا/پایان داشته باشد.")
                .Matches(Base64UrlPattern)
                .WithMessage("{PropertyName} معتبر نیست.");
        }
    }

}

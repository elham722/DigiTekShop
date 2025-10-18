namespace DigiTekShop.Application.Auth.ResendEmailConfirmation.Command
{
    public sealed class ResendEmailConfirmationCommandValidator : AbstractValidator<ResendEmailConfirmationCommand>
    {
        public ResendEmailConfirmationCommandValidator()
        {
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.Dto.Email)
                .NotEmpty().WithName("ایمیل")
                .WithMessage("{PropertyName} الزامی است.")
                .MaximumLength(254).WithMessage("{PropertyName} نباید بیش از {MaxLength} کاراکتر باشد.")
                .EmailAddress().WithMessage("{PropertyName} معتبر نیست.")
                .Must(v => v == v.Trim())
                .WithMessage("{PropertyName} نباید فاصله‌ی ابتدا/پایان داشته باشد.");
        }
    }

}

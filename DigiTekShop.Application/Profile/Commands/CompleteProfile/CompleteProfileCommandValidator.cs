namespace DigiTekShop.Application.Profile.Commands.CompleteProfile;

public sealed class CompleteProfileCommandValidator : AbstractValidator<CompleteProfileCommand>
{
    public CompleteProfileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("شناسه کاربر الزامی است.");

        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("نام کامل الزامی است.")
            .MinimumLength(2)
            .WithMessage("نام کامل حداقل ۲ کاراکتر باشد.")
            .MaximumLength(200)
            .WithMessage("نام کامل حداکثر ۲۰۰ کاراکتر باشد.");

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("فرمت ایمیل صحیح نیست.");
    }
}


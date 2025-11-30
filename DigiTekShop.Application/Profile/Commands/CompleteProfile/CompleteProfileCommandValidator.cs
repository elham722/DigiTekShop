using FluentValidation;

namespace DigiTekShop.Application.Profile.Commands.CompleteProfile;

public sealed class CompleteProfileCommandValidator : AbstractValidator<CompleteProfileCommand>
{
    public CompleteProfileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("شناسه کاربر الزامی است");

        RuleFor(x => x.Request)
            .NotNull()
            .WithMessage("اطلاعات پروفایل الزامی است");

        When(x => x.Request is not null, () =>
        {
            RuleFor(x => x.Request.FullName)
                .NotEmpty()
                .WithMessage("نام کامل الزامی است")
                .MinimumLength(3)
                .WithMessage("نام کامل باید حداقل ۳ کاراکتر باشد")
                .MaximumLength(200)
                .WithMessage("نام کامل نمی‌تواند بیشتر از ۲۰۰ کاراکتر باشد")
                .Matches(@"^[\u0600-\u06FFa-zA-Z\s]+$")
                .WithMessage("نام کامل فقط می‌تواند شامل حروف فارسی، انگلیسی و فاصله باشد");

            RuleFor(x => x.Request.Email)
                .EmailAddress()
                .When(x => !string.IsNullOrWhiteSpace(x.Request.Email))
                .WithMessage("فرمت ایمیل نامعتبر است");
        });
    }
}


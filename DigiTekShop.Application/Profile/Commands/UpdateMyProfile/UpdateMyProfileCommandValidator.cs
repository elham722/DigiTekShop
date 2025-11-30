using FluentValidation;

namespace DigiTekShop.Application.Profile.Commands.UpdateMyProfile;

/// <summary>
/// ولیدیتور آپدیت پروفایل
/// </summary>
public sealed class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    public UpdateMyProfileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("شناسه کاربر الزامی است.");

        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("نام کامل الزامی است.")
            .MinimumLength(2)
            .WithMessage("نام کامل حداقل 2 کاراکتر باشد.")
            .MaximumLength(200)
            .WithMessage("نام کامل حداکثر 200 کاراکتر باشد.");

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("فرمت ایمیل صحیح نیست.");

        RuleFor(x => x.Phone)
            .Matches(@"^(\+98|0)?9\d{9}$")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage("فرمت شماره موبایل صحیح نیست.");
    }
}


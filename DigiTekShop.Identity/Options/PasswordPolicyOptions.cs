using System.ComponentModel.DataAnnotations;

namespace DigiTekShop.Identity.Options;

public sealed class PasswordPolicyOptions : IValidatableObject
{
    public int MinLength { get; set; } = 12;
    public int RequiredCategoryCount { get; set; } = 3; 
    public int MaxRepeatedChar { get; set; } = 3;       
    public int HistoryDepth { get; set; } = 5;       
    public bool ForbidUserNameFragments { get; set; } = true;
    public bool ForbidEmailLocalPart { get; set; } = true;
    public bool BlacklistExactOnly { get; set; } = false; // ⬅️ اضافه شد

    public string[] Blacklist { get; set; } = Array.Empty<string>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinLength < 8)
            yield return new ValidationResult("MinLength cannot be less than 8.", new[] { nameof(MinLength) });

        if (RequiredCategoryCount < 1 || RequiredCategoryCount > 4)
            yield return new ValidationResult("RequiredCategoryCount must be between 1 and 4.", new[] { nameof(RequiredCategoryCount) });

        if (MaxRepeatedChar < 1)
            yield return new ValidationResult("MaxRepeatedChar must be >= 1.", new[] { nameof(MaxRepeatedChar) });

        if (HistoryDepth < 0)
            yield return new ValidationResult("HistoryDepth cannot be negative.", new[] { nameof(HistoryDepth) });

        if (Blacklist == null)
            yield return new ValidationResult("Blacklist cannot be null.", new[] { nameof(Blacklist) });
    }
}
namespace DigiTekShop.Contracts.DTOs.Profile;

/// <summary>
/// وضعیت تکمیل پروفایل کاربر
/// </summary>
public sealed record ProfileCompletionStatus
{
    /// <summary>
    /// آیا پروفایل کامل است؟
    /// </summary>
    public required bool IsComplete { get; init; }

    /// <summary>
    /// شناسه مشتری (اگر وجود داشته باشد)
    /// </summary>
    public Guid? CustomerId { get; init; }

    /// <summary>
    /// فیلدهای ناقص
    /// </summary>
    public IReadOnlyList<string> MissingFields { get; init; } = [];

    /// <summary>
    /// درصد تکمیل پروفایل (0-100)
    /// </summary>
    public int CompletionPercentage { get; init; }

    public static ProfileCompletionStatus Complete(Guid customerId) => new()
    {
        IsComplete = true,
        CustomerId = customerId,
        MissingFields = [],
        CompletionPercentage = 100
    };

    public static ProfileCompletionStatus Incomplete(IReadOnlyList<string> missingFields, int percentage = 0) => new()
    {
        IsComplete = false,
        CustomerId = null,
        MissingFields = missingFields,
        CompletionPercentage = percentage
    };
}


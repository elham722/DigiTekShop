#nullable enable
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using DigiTekShop.SharedKernel.Exceptions.Validation;

namespace DigiTekShop.SharedKernel.Guards;

public static class Guard
{
    private static readonly Regex PhoneE164 = new(@"^\+?[1-9]\d{9,14}$", RegexOptions.Compiled);

    #region Null / Empty / Default

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstNull<T>(T? value, string propertyName) where T : class
        => ThrowIf(value is null, $"{propertyName} cannot be null.", propertyName, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstNullOrEmpty(string? value, string propertyName)
        => ThrowIf(string.IsNullOrEmpty(value), $"{propertyName} cannot be empty.", propertyName, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstNullOrWhiteSpace(string? value, string propertyName)
        => ThrowIf(string.IsNullOrWhiteSpace(value), $"{propertyName} cannot be empty or whitespace.", propertyName, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstEmpty<T>(T value, string propertyName) where T : struct
        => ThrowIf(value.Equals(default(T)), $"{propertyName} cannot be empty.", propertyName, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstEmpty(Guid value, string propertyName)
        => ThrowIf(value == Guid.Empty, $"{propertyName} cannot be empty.", propertyName, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstNullOrEmptyCollection<T>(IEnumerable<T>? collection, string propertyName)
        => ThrowIf(collection is null || !collection.Any(), $"{propertyName} cannot be null or empty.", propertyName, collection);

    #endregion

    #region Email

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstEmail(string? email, string propertyName)
    {
        var e = email?.Trim();
        var invalid = string.IsNullOrWhiteSpace(e) || e!.Length > 256;

        if (!invalid)
        {
            try
            {
                var addr = new MailAddress(e);
                invalid = !string.Equals(addr.Address, e, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                invalid = true;
            }
        }

        ThrowIf(invalid, $"{propertyName} must be a valid email.", propertyName, email);
    }

    #endregion

    #region Numeric / Range

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstNegative<T>(T value, string propertyName) where T : IComparable<T>
        => ThrowIf(value.CompareTo(default!) < 0, $"{propertyName} cannot be negative.", propertyName, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstOutOfRange<T>(T value, T min, T max, string propertyName) where T : IComparable<T>
        => ThrowIf(value.CompareTo(min) < 0 || value.CompareTo(max) > 0,
            $"{propertyName} must be between {min} and {max}.",
            propertyName, value);

    #endregion

    #region String / Format

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstTooLong(string? value, int maxLength, string propertyName)
        => ThrowIf(value is not null && value.Length > maxLength,
            $"{propertyName} cannot exceed {maxLength} characters.",
            propertyName, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstTooShort(string? value, int minLength, string propertyName)
        => ThrowIf(value is not null && value.Length < minLength,
            $"{propertyName} must be at least {minLength} characters long.",
            propertyName, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstInvalidFormat(string value, string pattern, string propertyName, RegexOptions options = RegexOptions.None)
        => ThrowIf(!Regex.IsMatch(value, pattern, options),
            $"{propertyName} has invalid format.",
            propertyName, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstInvalidFormat(string value, Regex regex, string propertyName)
        => ThrowIf(!regex.IsMatch(value),
            $"{propertyName} has invalid format.",
            propertyName, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstInvalidPhoneNumber(string? phoneNumber, int minDigits = 10)
    {
        var s = phoneNumber?.Trim();
        var isInvalid = string.IsNullOrEmpty(s) || s!.Length < minDigits || !PhoneE164.IsMatch(s);
        ThrowIf(isInvalid, "Invalid phone number format.", "PhoneNumber", phoneNumber);
    }

    #endregion

    #region Date / Time

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstDefaultDate(DateTime date, string propertyName)
        => ThrowIf(date == default, $"{propertyName} cannot be default date.", propertyName, date);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstFutureDate(DateTime date, Func<DateTime> nowProvider, string propertyName)
        => ThrowIf(date > nowProvider(), $"{propertyName} cannot be in the future.", propertyName, date);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstPastDate(DateTime date, Func<DateTime> nowProvider, string propertyName)
        => ThrowIf(date < nowProvider(), $"{propertyName} cannot be in the past.", propertyName, date);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstDefaultDate(DateTimeOffset date, string propertyName)
        => ThrowIf(date == default, $"{propertyName} cannot be default date.", propertyName, date);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstFutureDate(DateTimeOffset date, Func<DateTimeOffset> nowProvider, string propertyName)
        => ThrowIf(date > nowProvider(), $"{propertyName} cannot be in the future.", propertyName, date);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstPastDate(DateTimeOffset date, Func<DateTimeOffset> nowProvider, string propertyName)
        => ThrowIf(date < nowProvider(), $"{propertyName} cannot be in the past.", propertyName, date);

    #endregion

    #region Business / Domain

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstInvalidOperation(bool condition, string operation, object entityId, string propertyName = "Unknown")
        => ThrowIf(condition, $"Invalid operation '{operation}' on entity {entityId}.", propertyName, entityId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstEntityNotFound(bool condition, string entityName, object key)
        => ThrowIf(condition, $"{entityName} with key '{key}' was not found.", entityName, key);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstDuplicateEntity(bool condition, string entityName, object key)
        => ThrowIf(condition, $"{entityName} with key '{key}' already exists.", entityName, key);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstForbiddenAction(bool condition, string action, string? userId = null)
    {
        var message = userId is null
            ? $"Forbidden action: {action}."
            : $"Forbidden action: {action} for user '{userId}'.";
        ThrowIf(condition, message, "Action", action);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstConcurrencyConflict(bool condition, string entityName, object key)
        => ThrowIf(condition, $"Concurrency conflict on {entityName} with key '{key}'.", entityName, key);

    #endregion

    #region Private Helper

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ThrowIf(bool condition, string message, string propertyName, object? value)
    {
        if (!condition) return;
        throw new DomainValidationException(new[] { message }, propertyName, value);
    }

    #endregion
}

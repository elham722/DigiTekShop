using DigiTekShop.SharedKernel.Exceptions.Validation;
using DigiTekShop.SharedKernel.Exceptions.Conflict;
using DigiTekShop.SharedKernel.Exceptions.Common;
using DigiTekShop.SharedKernel.Exceptions.NotFound;
using DigiTekShop.SharedKernel.Exceptions;

using System.Net.Mail;
using System.Text.RegularExpressions;

namespace DigiTekShop.SharedKernel.Guards
{
    public static class Guard
    {
        #region Null / Empty / Default

        public static void AgainstNull<T>(T? value, string propertyName) where T : class
            => ThrowIf(value is null, $"{propertyName} cannot be null.", propertyName, value);

        public static void AgainstNullOrEmpty(string? value, string propertyName)
            => ThrowIf(string.IsNullOrWhiteSpace(value), $"{propertyName} cannot be empty.", propertyName, value);

        public static void AgainstEmpty<T>(T value, string propertyName) where T : struct
            => ThrowIf(value.Equals(default(T)), $"{propertyName} cannot be empty.", propertyName, value);

        public static void AgainstEmpty(Guid value, string propertyName)
            => ThrowIf(value == Guid.Empty, $"{propertyName} cannot be empty.", propertyName, value);

        public static void AgainstNullOrEmptyCollection<T>(IEnumerable<T>? collection, string propertyName)
            => ThrowIf(collection == null || !collection.Any(), $"{propertyName} cannot be null or empty.", propertyName, collection);

        #endregion

        #region Email

        public static void AgainstEmail(string? email, string propertyName)
        {
            var invalid = string.IsNullOrWhiteSpace(email) || email!.Length > 256;
            if (!invalid)
            {
                try
                {
                    var addr = new MailAddress(email);
                    invalid = addr.Address != email;
                }
                catch { invalid = true; }
            }
            ThrowIf(invalid, $"{propertyName} must be a valid email.", propertyName, email);
        }


        #endregion

        #region Numeric / Range / TimeSpan

        public static void AgainstNegative<T>(T value, string propertyName) where T : IComparable<T>
            => ThrowIf(value.CompareTo(default) < 0, $"{propertyName} cannot be negative.", propertyName, value);

        public static void AgainstOutOfRange<T>(T value, T min, T max, string propertyName) where T : IComparable<T>
            => ThrowIf(value.CompareTo(min) < 0 || value.CompareTo(max) > 0,
                $"{propertyName} must be between {min} and {max}.",
                propertyName, value);

        #endregion

        #region String / Format

        public static void AgainstTooLong(string? value, int maxLength, string propertyName)
            => ThrowIf(value != null && value.Length > maxLength,
                $"{propertyName} cannot exceed {maxLength} characters.",
                propertyName, value);

        public static void AgainstTooShort(string? value, int minLength, string propertyName)
            => ThrowIf(value != null && value.Length < minLength,
                $"{propertyName} must be at least {minLength} characters long.",
                propertyName, value);

        public static void AgainstInvalidFormat(string value, string pattern, string propertyName)
            => ThrowIf(!Regex.IsMatch(value, pattern),
                $"{propertyName} has invalid format.",
                propertyName, value);

      
        public static void AgainstInvalidPhoneNumber(string phoneNumber, int minLength = 10)
        {
            var isInvalid = string.IsNullOrWhiteSpace(phoneNumber) ||
                           !Regex.IsMatch(phoneNumber, @"^\+?\d+$") ||
                           phoneNumber.Length < minLength;

            ThrowIf(isInvalid, "Invalid phone number format.", "PhoneNumber", phoneNumber);
        }

        #endregion

        #region Date / Time
        public static void AgainstDefaultDate(DateTime date, string propertyName)
            => ThrowIf(date == default, $"{propertyName} cannot be default date.", propertyName, date);

        public static void AgainstFutureDate(DateTime date, Func<DateTime> nowProvider, string propertyName)
            => ThrowIf(date > nowProvider(), $"{propertyName} cannot be in the future.", propertyName, date);

        public static void AgainstPastDate(DateTime date, Func<DateTime> nowProvider, string propertyName)
            => ThrowIf(date < nowProvider(), $"{propertyName} cannot be in the past.", propertyName, date);

        #endregion

        #region Business / Domain

        public static void AgainstInvalidOperation(bool condition, string operation, string entityId, string propertyName = "Unknown")
            => ThrowIf(condition, $"Invalid operation '{operation}' on entity {entityId}.", propertyName, entityId);

        public static void AgainstEntityNotFound(bool condition, string entityName, object key)
            => ThrowIf(condition, $"{entityName} with key '{key}' was not found.", entityName, key);

        public static void AgainstDuplicateEntity(bool condition, string entityName, object key)
            => ThrowIf(condition, $"{entityName} with key '{key}' already exists.", entityName, key);

        public static void AgainstForbiddenAction(bool condition, string action, string? userId = null)
        {
            var message = userId is null
                ? $"Forbidden action: {action}."
                : $"Forbidden action: {action} for user '{userId}'.";

            ThrowIf(condition, message, "Action", action);
        }

        public static void AgainstConcurrencyConflict(bool condition, string entityName, object key)
            => ThrowIf(condition, $"Concurrency conflict on {entityName} with key '{key}'.", entityName, key);

        #endregion

        #region Private Helper

        private static void ThrowIf(
            bool condition,
            string message,
            string propertyName,
            object? value)
        {
            if (!condition) return;

            throw new DomainValidationException(new[] { message }, propertyName, value);
        }

        #endregion
    }
}

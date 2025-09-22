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

        public static void AgainstInvalidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                if (addr.Address != email)
                    throwEmail();
            }
            catch
            {
                throwEmail();
            }

            void throwEmail() => ThrowIf(true, "Invalid email format.", "Email", email);
        }

        public static void AgainstInvalidPhoneNumber(string phoneNumber, int minLength = 10)
            => ThrowIf(
                string.IsNullOrWhiteSpace(phoneNumber) ||
                !Regex.IsMatch(phoneNumber, @"^\+?\d+$") ||
                phoneNumber.Length < minLength,
                "Invalid phone number format.",
                "PhoneNumber", phoneNumber);

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

        public static void AgainstInvalidOperation(string operation, string entityId, string propertyName = "Unknown")
            => ThrowIf(true, $"Invalid operation '{operation}' on entity {entityId}.", propertyName, entityId);

        public static void AgainstEntityNotFound(string entityName, object key)
            => ThrowIf(true, $"{entityName} with key '{key}' was not found.", "Entity", key);

        public static void AgainstDuplicateEntity(string entityName, object key)
            => ThrowIf(true, $"{entityName} with key '{key}' already exists.", "Entity", key);

        public static void AgainstForbiddenAction(string action, string? userId = null)
            => ThrowIf(true, $"Forbidden action: {action}.", "Action", action );

        public static void AgainstConcurrencyConflict(string entityName, object key)
            => ThrowIf(true, $"Concurrency conflict on {entityName} with key '{key}'.", "Entity", key);

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

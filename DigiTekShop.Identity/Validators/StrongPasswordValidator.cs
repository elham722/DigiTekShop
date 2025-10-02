using DigiTekShop.Identity.Options;
using Microsoft.Extensions.Options;

public sealed class StrongPasswordValidator : IPasswordValidator<User>
{
    private readonly PasswordPolicyOptions _opts;
    private readonly DigiTekShopIdentityDbContext _db;
    private readonly IPasswordHasher<User> _hasher;

    public StrongPasswordValidator(
        IOptions<PasswordPolicyOptions> opts,
        DigiTekShopIdentityDbContext db,
        IPasswordHasher<User> hasher)
    {
        _opts = opts.Value;
        _db = db;
        _hasher = hasher;
    }

    public async Task<IdentityResult> ValidateAsync(UserManager<User> manager, User user, string password)
    {
        var errors = new List<IdentityError>();

        // 1) طول پسورد
        if (password.Length < _opts.MinLength)
            errors.Add(Error("PasswordTooShort", $"Password must be at least {_opts.MinLength} characters."));

        // 2) دسته‌بندی کاراکترها
        int categories = 0;
        if (password.Any(char.IsLower)) categories++;
        if (password.Any(char.IsUpper)) categories++;
        if (password.Any(char.IsDigit)) categories++;
        if (password.Any(ch => !char.IsLetterOrDigit(ch))) categories++;
        if (categories < _opts.RequiredCategoryCount)
            errors.Add(Error("PasswordWeakComposition",
                $"Password must contain at least {_opts.RequiredCategoryCount} of: lowercase, uppercase, digit, symbol."));

        // 3) کاراکتر تکراری
        if (_opts.MaxRepeatedChar > 0 && HasRepeatedChar(password, _opts.MaxRepeatedChar + 1))
            errors.Add(Error("PasswordRepeatedChars", $"Password must not contain more than {_opts.MaxRepeatedChar} repeated characters in a row."));

        // 4) عدم شباهت به Username یا Email
        var lowerPassword = password.ToLowerInvariant();
        if (_opts.ForbidUserNameFragments && !string.IsNullOrWhiteSpace(user.UserName))
        {
            var u = user.UserName!.ToLowerInvariant();
            if (lowerPassword.Contains(u))
                errors.Add(Error("PasswordContainsUserName", "Password must not contain your username."));
        }
        if (_opts.ForbidEmailLocalPart && !string.IsNullOrWhiteSpace(user.Email))
        {
            var local = user.Email!.Split('@')[0].ToLowerInvariant();
            if (local.Length >= 3 && lowerPassword.Contains(local))
                errors.Add(Error("PasswordContainsEmail", "Password must not contain your email name."));
        }

        // 5) Blacklist
        if (_opts.Blacklist?.Length > 0)
        {
            foreach (var banned in _opts.Blacklist.Select(b => b.ToLowerInvariant()))
            {
                if (_opts.BlacklistExactOnly)
                {
                    // فقط exact match
                    if (lowerPassword.Equals(banned))
                    {
                        errors.Add(Error("PasswordBlacklisted", "Password is too common or appears in blacklist."));
                        break;
                    }
                }
                else
                {
                    // contains هم شامل میشه
                    if (lowerPassword.Contains(banned))
                    {
                        errors.Add(Error("PasswordBlacklisted", "Password is too common or appears in blacklist."));
                        break;
                    }
                }
            }
        }

        // 6) Password History
        if (_opts.HistoryDepth > 0)
        {
            var histories = await _db.PasswordHistories
                .Where(x => x.UserId == user.Id)
                .OrderByDescending(x => x.ChangedAt)
                .Take(_opts.HistoryDepth)
                .ToListAsync();

            foreach (var h in histories)
            {
                var v = _hasher.VerifyHashedPassword(user, h.PasswordHash, password);
                if (v == PasswordVerificationResult.Success || v == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    errors.Add(Error("PasswordReused", "You cannot reuse a recent password."));
                    break;
                }
            }
        }

        return errors.Count == 0 ? IdentityResult.Success : IdentityResult.Failed(errors.ToArray());
    }

    private static bool HasRepeatedChar(string s, int threshold)
    {
        int run = 1;
        for (int i = 1; i < s.Length; i++)
        {
            if (s[i] == s[i - 1])
            {
                run++;
                if (run >= threshold) return true;
            }
            else run = 1;
        }
        return false;
    }

    private static IdentityError Error(string code, string description)
        => new() { Code = code, Description = description };
}

using DigiTekShop.Identity.Models;
using DigiTekShop.SharedKernel.Enums.Verification;

namespace DigiTekShop.Identity.UnitTests.Models;

/// <summary>
/// Unit tests for PhoneVerification domain model
/// تست‌های منطق دامنه برای OTP: انقضا، تلاش‌ها، lockout
/// </summary>
public sealed class PhoneVerificationTests
{
    #region Create Tests

    [Fact]
    public void CreateForUser_ValidInputs_CreatesSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var codeHash = "hashed-code";
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);
        var phone = "+989121234567";

        // Act
        var pv = PhoneVerification.CreateForUser(
            userId: userId,
            codeHash: codeHash,
            expiresAtUtc: expiresAt,
            phoneNumber: phone,
            purpose: VerificationPurpose.Login,
            channel: VerificationChannel.Sms);

        // Assert
        pv.Should().NotBeNull();
        pv.UserId.Should().Be(userId);
        pv.CodeHash.Should().Be(codeHash);
        pv.ExpiresAtUtc.Should().Be(expiresAt);
        pv.PhoneNumber.Should().NotBeNullOrWhiteSpace();
        pv.Attempts.Should().Be(0);
        pv.IsVerified.Should().BeFalse();
        pv.VerifiedAtUtc.Should().BeNull();
        pv.LockedUntilUtc.Should().BeNull();
    }

    [Fact]
    public void CreateForUser_EmptyUserId_ThrowsException()
    {
        // Arrange
        var userId = Guid.Empty;
        var codeHash = "hashed-code";
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);

        // Act & Assert
        var action = () => PhoneVerification.CreateForUser(
            userId: userId,
            codeHash: codeHash,
            expiresAtUtc: expiresAt);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*userId*");
    }

    [Fact]
    public void CreateForUser_EmptyCodeHash_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var codeHash = "";
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);

        // Act & Assert
        var action = () => PhoneVerification.CreateForUser(
            userId: userId,
            codeHash: codeHash,
            expiresAtUtc: expiresAt);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*codeHash*");
    }

    [Fact]
    public void CreateForUser_PastExpirationDate_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var codeHash = "hashed-code";
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(-5); // در گذشته

        // Act & Assert
        var action = () => PhoneVerification.CreateForUser(
            userId: userId,
            codeHash: codeHash,
            expiresAtUtc: expiresAt);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*expiresAtUtc*");
    }

    #endregion

    #region IsExpired Tests

    [Fact]
    public void IsExpired_BeforeExpirationTime_ReturnsFalse()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(5);
        var pv = CreateTestVerification(expiresAt: expiresAt);

        // Act
        var isExpired = pv.IsExpired(now);

        // Assert
        isExpired.Should().BeFalse("OTP should not be expired before expiration time");
    }

    [Fact]
    public void IsExpired_ExactlyAtExpirationTime_ReturnsTrue()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now;
        var pv = CreateTestVerification(expiresAt: expiresAt);

        // Act
        var isExpired = pv.IsExpired(now);

        // Assert
        isExpired.Should().BeTrue("OTP should be expired at exact expiration time");
    }

    [Fact]
    public void IsExpired_AfterExpirationTime_ReturnsTrue()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(-5); // 5 دقیقه پیش منقضی شده
        var pv = CreateTestVerification(expiresAt: expiresAt);

        // Act
        var isExpired = pv.IsExpired(now);

        // Assert
        isExpired.Should().BeTrue("OTP should be expired after expiration time");
    }

    #endregion

    #region TryIncrementAttempts Tests

    [Fact]
    public void TryIncrementAttempts_BelowMaxAttempts_IncrementsSuccessfully()
    {
        // Arrange
        var pv = CreateTestVerification();
        var maxAttempts = 3;

        // Act
        var result = pv.TryIncrementAttempts(maxAttempts);

        // Assert
        result.Should().BeTrue("should allow increment when below max");
        pv.Attempts.Should().Be(1);
        pv.LockedUntilUtc.Should().BeNull("should not lock before reaching max");
    }

    [Fact]
    public void TryIncrementAttempts_ReachingMaxAttempts_ReturnsFalse()
    {
        // Arrange
        var pv = CreateTestVerification();
        var maxAttempts = 3;

        // Increment to max
        for (int i = 0; i < maxAttempts; i++)
        {
            pv.TryIncrementAttempts(maxAttempts);
        }

        // Act - تلاش برای increment بعد از رسیدن به max
        var result = pv.TryIncrementAttempts(maxAttempts);

        // Assert
        result.Should().BeFalse("should not allow increment after max attempts");
        pv.Attempts.Should().Be(maxAttempts);
    }

    [Fact]
    public void TryIncrementAttempts_WithLockDuration_LocksAfterMaxAttempts()
    {
        // Arrange
        var pv = CreateTestVerification();
        var maxAttempts = 3;
        var lockDuration = TimeSpan.FromMinutes(5);
        var now = DateTimeOffset.UtcNow;

        // Increment to max
        for (int i = 0; i < maxAttempts; i++)
        {
            pv.TryIncrementAttempts(maxAttempts);
        }

        // Act - تلاش برای increment با lockDuration
        var result = pv.TryIncrementAttempts(maxAttempts, lockDuration);

        // Assert
        result.Should().BeFalse();
        pv.LockedUntilUtc.Should().NotBeNull("should set lock time");
        pv.LockedUntilUtc.Should().BeCloseTo(now.Add(lockDuration), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void TryIncrementAttempts_MultipleAttempts_IncrementsCorrectly()
    {
        // Arrange
        var pv = CreateTestVerification();
        var maxAttempts = 5;

        // Act & Assert
        for (int i = 1; i <= maxAttempts; i++)
        {
            if (i <= maxAttempts)
            {
                var result = pv.TryIncrementAttempts(maxAttempts);
                if (i < maxAttempts)
                {
                    result.Should().BeTrue($"attempt {i} should succeed");
                    pv.Attempts.Should().Be(i);
                }
                else
                {
                    result.Should().BeFalse($"attempt {i} (at max) should fail");
                }
            }
        }

        pv.Attempts.Should().Be(maxAttempts);
    }

    #endregion

    #region IsLocked Tests

    [Fact]
    public void IsLocked_NoLockSet_ReturnsFalse()
    {
        // Arrange
        var pv = CreateTestVerification();
        var now = DateTimeOffset.UtcNow;

        // Act
        var isLocked = pv.IsLocked(now);

        // Assert
        isLocked.Should().BeFalse("should not be locked when LockedUntilUtc is null");
    }

    [Fact]
    public void IsLocked_BeforeLockExpiration_ReturnsTrue()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var pv = CreateTestVerification();
        
        // Manually set lock (در واقعیت از TryIncrementAttempts می‌آید)
        var maxAttempts = 3;
        for (int i = 0; i < maxAttempts; i++)
            pv.TryIncrementAttempts(maxAttempts);
        pv.TryIncrementAttempts(maxAttempts, TimeSpan.FromMinutes(5));

        // Act
        var isLocked = pv.IsLocked(now.AddMinutes(2)); // ۲ دقیقه بعد (هنوز lock است)

        // Assert
        isLocked.Should().BeTrue("should be locked before lock expiration");
    }

    [Fact]
    public void IsLocked_AfterLockExpiration_ReturnsFalse()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var pv = CreateTestVerification();
        
        var maxAttempts = 3;
        for (int i = 0; i < maxAttempts; i++)
            pv.TryIncrementAttempts(maxAttempts);
        pv.TryIncrementAttempts(maxAttempts, TimeSpan.FromMinutes(5));

        // Act
        var isLocked = pv.IsLocked(now.AddMinutes(6)); // ۶ دقیقه بعد (lock منقضی شده)

        // Assert
        isLocked.Should().BeFalse("should not be locked after lock expiration");
    }

    #endregion

    #region IsValid Tests

    [Fact]
    public void IsValid_AllConditionsGood_ReturnsTrue()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var pv = CreateTestVerification(expiresAt: now.AddMinutes(5));

        // Act
        var isValid = pv.IsValid(now, maxAttempts: 3);

        // Assert
        isValid.Should().BeTrue("should be valid when not expired, not locked, not verified, and attempts < max");
    }

    [Fact]
    public void IsValid_Expired_ReturnsFalse()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var pv = CreateTestVerification(expiresAt: now.AddMinutes(-1)); // منقضی شده

        // Act
        var isValid = pv.IsValid(now, maxAttempts: 3);

        // Assert
        isValid.Should().BeFalse("should not be valid when expired");
    }

    [Fact]
    public void IsValid_Locked_ReturnsFalse()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var pv = CreateTestVerification();
        
        // Lock کردن
        var maxAttempts = 3;
        for (int i = 0; i < maxAttempts; i++)
            pv.TryIncrementAttempts(maxAttempts);
        pv.TryIncrementAttempts(maxAttempts, TimeSpan.FromMinutes(5));

        // Act
        var isValid = pv.IsValid(now, maxAttempts);

        // Assert
        isValid.Should().BeFalse("should not be valid when locked");
    }

    [Fact]
    public void IsValid_AlreadyVerified_ReturnsFalse()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var pv = CreateTestVerification();
        pv.MarkAsVerified(now);

        // Act
        var isValid = pv.IsValid(now, maxAttempts: 3);

        // Assert
        isValid.Should().BeFalse("should not be valid when already verified");
    }

    [Fact]
    public void IsValid_AttemptsAtMax_ReturnsFalse()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var pv = CreateTestVerification();
        var maxAttempts = 3;
        
        // Increment تا max
        for (int i = 0; i < maxAttempts; i++)
            pv.TryIncrementAttempts(maxAttempts);

        // Act
        var isValid = pv.IsValid(now, maxAttempts);

        // Assert
        isValid.Should().BeFalse("should not be valid when attempts >= max");
    }

    #endregion

    #region ResetAttempts Tests

    [Fact]
    public void ResetAttempts_ResetsAttemptsAndLock()
    {
        // Arrange
        var pv = CreateTestVerification();
        var maxAttempts = 3;
        
        // Increment و lock
        for (int i = 0; i < maxAttempts; i++)
            pv.TryIncrementAttempts(maxAttempts);
        pv.TryIncrementAttempts(maxAttempts, TimeSpan.FromMinutes(5));

        pv.Attempts.Should().Be(maxAttempts);
        pv.LockedUntilUtc.Should().NotBeNull();

        // Act
        pv.ResetAttempts();

        // Assert
        pv.Attempts.Should().Be(0);
        pv.LockedUntilUtc.Should().BeNull();
    }

    #endregion

    #region MarkAsVerified Tests

    [Fact]
    public void MarkAsVerified_SetsVerifiedFields()
    {
        // Arrange
        var pv = CreateTestVerification();
        var now = DateTimeOffset.UtcNow;

        pv.IsVerified.Should().BeFalse();
        pv.VerifiedAtUtc.Should().BeNull();

        // Act
        pv.MarkAsVerified(now);

        // Assert
        pv.IsVerified.Should().BeTrue();
        pv.VerifiedAtUtc.Should().Be(now);
    }

    #endregion

    #region ResetCode Tests

    [Fact]
    public void ResetCode_UpdatesCodeAndResetsState()
    {
        // Arrange
        var pv = CreateTestVerification();
        pv.TryIncrementAttempts(3);
        pv.MarkAsVerified(DateTimeOffset.UtcNow);

        var newHash = "new-hashed-code";
        var newExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10);

        // Act
        pv.ResetCode(newHash, newExpiresAt);

        // Assert
        pv.CodeHash.Should().Be(newHash);
        pv.ExpiresAtUtc.Should().Be(newExpiresAt);
        pv.Attempts.Should().Be(0, "attempts should be reset");
        pv.IsVerified.Should().BeFalse("verified flag should be reset");
        pv.VerifiedAtUtc.Should().BeNull("verified time should be cleared");
        pv.LockedUntilUtc.Should().BeNull("lock should be cleared");
    }

    [Fact]
    public void ResetCode_EmptyHash_ThrowsException()
    {
        // Arrange
        var pv = CreateTestVerification();
        var newExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10);

        // Act & Assert
        var action = () => pv.ResetCode("", newExpiresAt);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*newHash*");
    }

    [Fact]
    public void ResetCode_PastExpirationDate_ThrowsException()
    {
        // Arrange
        var pv = CreateTestVerification();
        var newExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-10);

        // Act & Assert
        var action = () => pv.ResetCode("new-hash", newExpiresAt);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*newExpiresAtUtc*");
    }

    #endregion

    #region Integration Scenario Tests

    [Fact]
    public void Scenario_ThreeWrongAttempts_ThenLockout()
    {
        // Arrange
        var pv = CreateTestVerification(expiresAt: DateTimeOffset.UtcNow.AddMinutes(5));
        var maxAttempts = 3;
        var lockDuration = TimeSpan.FromMinutes(5);
        var now = DateTimeOffset.UtcNow;

        // Act: سه تلاش اشتباه
        for (int i = 0; i < maxAttempts; i++)
        {
            var canIncrement = pv.TryIncrementAttempts(maxAttempts);
            canIncrement.Should().BeTrue($"attempt {i + 1} should be allowed");
        }

        // Assert: رسیدن به max
        pv.Attempts.Should().Be(maxAttempts);
        pv.IsValid(now, maxAttempts).Should().BeFalse("should not be valid at max attempts");

        // Act: تلاش چهارم → باید lock شود
        var lockedResult = pv.TryIncrementAttempts(maxAttempts, lockDuration);

        // Assert
        lockedResult.Should().BeFalse("should not increment after max");
        pv.LockedUntilUtc.Should().NotBeNull("should be locked");
        pv.IsLocked(now).Should().BeTrue("should be locked");
    }

    [Fact]
    public void Scenario_SuccessfulVerification_AfterTwoAttempts()
    {
        // Arrange
        var pv = CreateTestVerification(expiresAt: DateTimeOffset.UtcNow.AddMinutes(5));
        var now = DateTimeOffset.UtcNow;

        // Act: دو تلاش اشتباه
        pv.TryIncrementAttempts(3);
        pv.TryIncrementAttempts(3);
        pv.Attempts.Should().Be(2);

        // Act: تلاش سوم درست → موفق
        pv.TryIncrementAttempts(3);
        pv.MarkAsVerified(now);

        // Assert
        pv.IsVerified.Should().BeTrue();
        pv.VerifiedAtUtc.Should().Be(now);
        pv.IsValid(now, 3).Should().BeFalse("verified codes are not valid anymore");
    }

    [Fact]
    public void Scenario_ResetCode_AllowsNewAttempts()
    {
        // Arrange
        var pv = CreateTestVerification();
        var maxAttempts = 3;

        // Act 1: سه تلاش اشتباه
        for (int i = 0; i < maxAttempts; i++)
            pv.TryIncrementAttempts(maxAttempts);

        pv.Attempts.Should().Be(maxAttempts);

        // Act 2: ResetCode
        var newHash = "new-hash";
        var newExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5);
        pv.ResetCode(newHash, newExpiresAt);

        // Assert
        pv.Attempts.Should().Be(0, "attempts should be reset");
        pv.CodeHash.Should().Be(newHash);
        pv.IsValid(DateTimeOffset.UtcNow, maxAttempts).Should().BeTrue(
            "should be valid again after reset");
    }

    #endregion

    #region Helper Methods

    private static PhoneVerification CreateTestVerification(
        Guid? userId = null,
        DateTimeOffset? expiresAt = null,
        string? phone = null)
    {
        return PhoneVerification.CreateForUser(
            userId: userId ?? Guid.NewGuid(),
            codeHash: "test-hash",
            expiresAtUtc: expiresAt ?? DateTimeOffset.UtcNow.AddMinutes(5),
            phoneNumber: phone ?? "+989121234567",
            purpose: VerificationPurpose.Login,
            channel: VerificationChannel.Sms);
    }

    #endregion
}


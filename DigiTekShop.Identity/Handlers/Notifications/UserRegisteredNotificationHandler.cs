using DigiTekShop.Contracts.Options.Auth;

namespace DigiTekShop.Identity.Handlers.Notifications;


public sealed class UserRegisteredNotificationHandler
    : IIntegrationEventHandler<UserRegisteredIntegrationEvent>
{
    private readonly DigiTekShopIdentityDbContext _context;
    private readonly IEmailConfirmationService _emailConfirmationService;
    private readonly IPhoneVerificationService _phoneVerificationService;
    private readonly ILogger<UserRegisteredNotificationHandler> _logger;
    private readonly EmailConfirmationOptions _emailSettings;
    private readonly PhoneVerificationOptions _phoneSettings;
    private readonly NotificationFeatureFlags _featureFlags;

    public UserRegisteredNotificationHandler(
        DigiTekShopIdentityDbContext context,
        IEmailConfirmationService emailConfirmationService,
        IPhoneVerificationService phoneVerificationService,
        ILogger<UserRegisteredNotificationHandler> logger,
        IOptions<EmailConfirmationOptions> emailSettings,
        IOptions<PhoneVerificationOptions> phoneSettings,
        IOptions<NotificationFeatureFlags> featureFlags)
    {
        _context = context;
        _emailConfirmationService = emailConfirmationService;
        _phoneVerificationService = phoneVerificationService;
        _logger = logger;
        _emailSettings = emailSettings?.Value ?? throw new ArgumentNullException(nameof(emailSettings));
        _phoneSettings = phoneSettings?.Value ?? throw new ArgumentNullException(nameof(phoneSettings));
        _featureFlags = featureFlags?.Value ?? throw new ArgumentNullException(nameof(featureFlags));
    }

    public async Task HandleAsync(UserRegisteredIntegrationEvent evt, CancellationToken ct)
    {
        _logger.LogInformation(
            "[Notification] Processing UserRegistered for UserId {UserId}, Email {Email}",
            evt.UserId, MaskEmail(evt.Email));

        // Get user to check confirmation status
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == evt.UserId, ct);

        if (user is null)
        {
            _logger.LogWarning("[Notification] User {UserId} not found, skipping notifications", evt.UserId);
            return;
        }

        // Send email confirmation
        if (_featureFlags.EnableEmailOnRegistration && _emailSettings.RequireEmailConfirmation && !user.EmailConfirmed)
        {
            await SendEmailConfirmationAsync(evt.UserId.ToString(), evt.Email, ct);
        }

        // Send phone verification
        if (_featureFlags.EnableSmsOnRegistration && 
            _phoneSettings.RequirePhoneConfirmation && 
            !string.IsNullOrWhiteSpace(evt.PhoneNumber) && 
            !user.PhoneNumberConfirmed)
        {
            await SendPhoneVerificationAsync(evt.UserId, evt.PhoneNumber, ct);
        }

        _logger.LogInformation("[Notification] Completed processing for UserId {UserId}", evt.UserId);
    }

    private async Task SendEmailConfirmationAsync(string userId, string email, CancellationToken ct)
    {
        try
        {
            var result = await _emailConfirmationService.SendAsync(userId, ct);
            if (result.IsSuccess)
            {
                _logger.LogInformation("[Notification] ✅ Email confirmation sent to {Email}", MaskEmail(email));
            }
            else
            {
                _logger.LogWarning(
                    "[Notification] ❌ Email confirmation failed for {Email}: {Error}",
                    MaskEmail(email), result.GetFirstError());
            }
        }
        catch (Exception ex)
        {
            // Don't throw - we don't want to fail the entire registration flow
            _logger.LogError(ex, "[Notification] Exception sending email confirmation to {Email}", MaskEmail(email));
        }
    }

    private async Task SendPhoneVerificationAsync(Guid userId, string phoneNumber, CancellationToken ct)
    {
        try
        {
            var result = await _phoneVerificationService.SendVerificationCodeAsync(userId, phoneNumber, ct);
            if (result.IsSuccess)
            {
                _logger.LogInformation("[Notification] ✅ Phone verification sent to {Phone}", phoneNumber);
            }
            else
            {
                _logger.LogWarning(
                    "[Notification] ❌ Phone verification failed for {Phone}: {Error}",
                    phoneNumber, result.GetFirstError());
            }
        }
        catch (Exception ex)
        {
            // Don't throw - we don't want to fail the entire registration flow
            _logger.LogError(ex, "[Notification] Exception sending phone verification to {Phone}", phoneNumber);
        }
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return email;
        var parts = email.Split('@');
        if (parts.Length != 2) return email;
        var local = parts[0];
        var domain = parts[1];
        var masked = local.Length <= 2
            ? new string('*', local.Length)
            : $"{local[0]}***{local[^1]}";
        return $"{masked}@{domain}";
    }
}


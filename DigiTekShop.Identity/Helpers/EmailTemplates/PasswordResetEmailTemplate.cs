
namespace DigiTekShop.Identity.Helpers.EmailTemplates;

/// <summary>
/// Helper for creating password reset email templates
/// </summary>
public static class PasswordResetEmailTemplateHelper
{
    /// <summary>
    /// Creates HTML email template for password reset
    /// </summary>
    /// <param name="userName">User display name</param>
    /// <param name="resetUrl">Password reset URL</param>
    /// <param name="companyName">Company name</param>
    /// <param name="supportEmail">Support email</param>
    /// <param name="webUrl">Website URL</param>
    /// <returns>HTML email content</returns>
    public static string CreatePasswordResetHtml(string userName, string resetUrl, string companyName, string? supportEmail, string? webUrl)
    {
        var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Password Reset - {companyName}</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 20px auto; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #007bff, #0056b3); color: white; padding: 30px 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 24px; }}
        .content {{ padding: 30px 20px; }}
        .content h2 {{ color: #333; margin-top: 0; }}
        .button {{ display: inline-block; background-color: #007bff; color: white; padding: 14px 28px; text-decoration: none; border-radius: 6px; font-weight: bold; margin: 20px 0; }}
        .button:hover {{ background-color: #0056b3; }}
        .security-box {{ background-color: #f8f9fa; border-left: 4px solid #007bff; padding: 15px; margin: 20px 0; }}
        .security-box h3 {{ margin-top: 0; color: #007bff; }}
        .security-box ul {{ margin: 10px 0; padding-left: 20px; }}
        .link-box {{ background-color: #f8f9fa; padding: 15px; border-radius: 4px; margin: 20px 0; word-break: break-all; }}
        .footer {{ background-color: #f8f9fa; padding: 20px; text-align: center; color: #666; font-size: 14px; border-top: 1px solid #eee; }}
        .footer a {{ color: #007bff; }}
        @media only screen and (max-width: 600px) {{ 
            .container {{ margin: 10px; }}
            .header, .content, .footer {{ padding: 20px 15px; }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔑 Password Reset Request</h1>
            <p style='margin: 10px 0 0 0; opacity: 0.9;'>Secure your account</p>
        </div>
        
        <div class='content'>
            <h2>Hello {userName}</h2>
            <p>We received a request to reset your password for your <strong>{companyName}</strong> account.</p>
            
            <p>If this was you, click the button below to reset your password:</p>
            
            <div style='text-align: center; margin: 30px 0;'>
                <a href='{resetUrl}' class='button'>Reset My Password</a>
            </div>
            
            <p>If the button doesn't work, copy and paste this link into your browser:</p>
            <div class='link-box'>{resetUrl}</div>
            
            <div class='security-box'>
                <h3>🛡️ Important Security Information:</h3>
                <ul>
                    <li><strong>Time Limited:</strong> This link will expire in 1 hour for security reasons</li>
                    <li><strong>One-Time Use:</strong> This link can only be used once</li>
                    <li><strong>Account Protection:</strong> If you didn't request this reset, please ignore this email</li>
                    <li><strong>No Changes:</strong> Your password remains unchanged until you use the link above</li>
                </ul>
            </div>
            
            {(!string.IsNullOrEmpty(supportEmail) ? $"<p>If you have any questions, please contact our support team at <a href='mailto:{supportEmail}'>{supportEmail}</a></p>" : "")}
            
            <p>Stay secure!<br>The {companyName} Team</p>
        </div>
        
        {(!string.IsNullOrEmpty(webUrl) ? $@"
        <div class='footer'>
            <p>© {DateTime.UtcNow.Year} {companyName}. All rights reserved.</p>
            <p>Visit us at <a href='{webUrl}'>{webUrl}</a></p>
        </div>" : "")}
    </div>
</body>
</html>";

        return htmlContent;
    }

    /// <summary>
    /// Creates plain text email template for password reset
    /// </summary>
    /// <param name="userName">User display name</param>
    /// <param name="resetUrl">Password reset URL</param>
    /// <param name="companyName">Company name</param>
    /// <param name="supportEmail">Support email</param>
    /// <returns>Plain text email content</returns>
    public static string CreatePasswordResetText(string userName, string resetUrl, string companyName, string? supportEmail)
    {
        var textContent = $@"
🔑 PASSWORD RESET REQUEST - {companyName}

Hello {userName},

We received a request to reset your password for your {companyName} account.

If this was you, please click the following link to reset your password:
{resetUrl}

🛡️ IMPORTANT SECURITY INFORMATION:
- TIME LIMITED: This link will expire in 1 hour for security reasons
- ONE-TIME USE: This link can only be used once  
- ACCOUNT PROTECTION: If you didn't request this reset, please ignore this email
- NO CHANGES: Your password remains unchanged until you use the link above

{(string.IsNullOrEmpty(supportEmail) ? "" : $"\nIf you have any questions, please contact our support team at {supportEmail}")}

Stay secure!
The {companyName} Team

---
© {DateTime.UtcNow.Year} {companyName}. All rights reserved.";

        return textContent;
    }
}
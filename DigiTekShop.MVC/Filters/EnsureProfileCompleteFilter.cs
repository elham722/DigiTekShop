using Microsoft.AspNetCore.Mvc.Filters;

namespace DigiTekShop.MVC.Filters;

public sealed class EnsureProfileCompleteFilter : IActionFilter
{
    private readonly ILogger<EnsureProfileCompleteFilter> _logger;

    public EnsureProfileCompleteFilter(ILogger<EnsureProfileCompleteFilter> logger)
    {
        _logger = logger;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var path = context.HttpContext.Request.Path.Value?.ToLower();
        if (path is null) return;

        if (IsWhitelisted(path))
            return;

    
        if (!context.HttpContext.Request.Cookies.TryGetValue(CookieNames.AccessToken, out var token))
            return; 

        if (string.IsNullOrWhiteSpace(token))
            return;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
                return;

            var jwt = handler.ReadJwtToken(token);
            var profileSetup = jwt.Claims.FirstOrDefault(c => c.Type == "profile_setup")?.Value;

         
            if (string.Equals(profileSetup, "pending", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "User profile incomplete, redirecting to complete page. Path={Path}", 
                    context.HttpContext.Request.Path);

                context.Result = new RedirectToActionResult(
                    actionName: "CompleteProfile",
                    controllerName: "Account",
                    routeValues: null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read JWT token for profile check");
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
       
    }

 
    private static bool IsWhitelisted(string path)
    {
        // صفحات احراز هویت
        if (path.StartsWith("/auth"))
            return true;

        // 🔥 صفحه تکمیل پروفایل (باید قابل دسترسی باشد!)
        // path با ToLower() کوچک شده، پس همه چیز lowercase چک شود
        if (path.StartsWith("/account/completeprofile"))
            return true;

        // صفحات خطا
        if (path.StartsWith("/home/error") || path.StartsWith("/error"))
            return true;

        // API calls
        if (path.StartsWith("/api"))
            return true;

        // Health checks
        if (path.StartsWith("/health"))
            return true;

        // Static files
        if (path.StartsWith("/css") || path.StartsWith("/js") || 
            path.StartsWith("/lib") || path.StartsWith("/images") ||
            path.StartsWith("/fonts") || path.StartsWith("/favicon"))
            return true;

        return false;
    }
}


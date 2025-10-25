using Microsoft.AspNetCore.Mvc;

namespace DigiTekShop.MVC.Extensions;

/// <summary>
/// Extension methods for handling notifications in controllers
/// </summary>
public static class NotificationExtensions
{
    /// <summary>
    /// Set success notification
    /// </summary>
    public static void SetSuccessNotification(this Controller controller, string message)
    {
        controller.TempData["SuccessMessage"] = message;
    }

    /// <summary>
    /// Set error notification
    /// </summary>
    public static void SetErrorNotification(this Controller controller, string message)
    {
        controller.TempData["ErrorMessage"] = message;
    }

    /// <summary>
    /// Set warning notification
    /// </summary>
    public static void SetWarningNotification(this Controller controller, string message)
    {
        controller.TempData["WarningMessage"] = message;
    }

    /// <summary>
    /// Set info notification
    /// </summary>
    public static void SetInfoNotification(this Controller controller, string message)
    {
        controller.TempData["InfoMessage"] = message;
    }

    /// <summary>
    /// Set success notification with title
    /// </summary>
    public static void SetSuccessNotification(this Controller controller, string title, string message)
    {
        controller.TempData["SuccessTitle"] = title;
        controller.TempData["SuccessMessage"] = message;
    }

    /// <summary>
    /// Set error notification with title
    /// </summary>
    public static void SetErrorNotification(this Controller controller, string title, string message)
    {
        controller.TempData["ErrorTitle"] = title;
        controller.TempData["ErrorMessage"] = message;
    }

    /// <summary>
    /// Set warning notification with title
    /// </summary>
    public static void SetWarningNotification(this Controller controller, string title, string message)
    {
        controller.TempData["WarningTitle"] = title;
        controller.TempData["WarningMessage"] = message;
    }

    /// <summary>
    /// Set info notification with title
    /// </summary>
    public static void SetInfoNotification(this Controller controller, string title, string message)
    {
        controller.TempData["InfoTitle"] = title;
        controller.TempData["InfoMessage"] = message;
    }

    /// <summary>
    /// Return JSON response with notification
    /// </summary>
    public static IActionResult JsonWithNotification(this Controller controller, bool success, string message, object? data = null)
    {
        return controller.Json(new
        {
            success = success,
            message = message,
            data = data,
            notification = new
            {
                type = success ? "success" : "error",
                title = success ? "موفقیت!" : "خطا!",
                message = message
            }
        });
    }

    /// <summary>
    /// Return JSON response with success notification
    /// </summary>
    public static IActionResult JsonSuccess(this Controller controller, string message, object? data = null)
    {
        return controller.JsonWithNotification(true, message, data);
    }

    /// <summary>
    /// Return JSON response with error notification
    /// </summary>
    public static IActionResult JsonError(this Controller controller, string message, object? data = null)
    {
        return controller.JsonWithNotification(false, message, data);
    }

    /// <summary>
    /// Return JSON response with validation errors
    /// </summary>
    public static IActionResult JsonValidationError(this Controller controller, string message, object? errors = null)
    {
        return controller.Json(new
        {
            success = false,
            message = message,
            errors = errors,
            notification = new
            {
                type = "error",
                title = "خطا در اعتبارسنجی",
                message = message
            }
        });
    }

    /// <summary>
    /// Return JSON response with warning notification
    /// </summary>
    public static IActionResult JsonWarning(this Controller controller, string message, object? data = null)
    {
        return controller.Json(new
        {
            success = true,
            message = message,
            data = data,
            notification = new
            {
                type = "warning",
                title = "هشدار!",
                message = message
            }
        });
    }

    /// <summary>
    /// Return JSON response with info notification
    /// </summary>
    public static IActionResult JsonInfo(this Controller controller, string message, object? data = null)
    {
        return controller.Json(new
        {
            success = true,
            message = message,
            data = data,
            notification = new
            {
                type = "info",
                title = "اطلاعات",
                message = message
            }
        });
    }
}

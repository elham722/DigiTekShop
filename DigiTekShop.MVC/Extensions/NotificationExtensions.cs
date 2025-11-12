namespace DigiTekShop.MVC.Extensions;

public static class NotificationExtensions
{
    public static void SetSuccessNotification(this Controller controller, string message)
    {
        controller.TempData["SuccessMessage"] = message;
    }

    public static void SetErrorNotification(this Controller controller, string message)
    {
        controller.TempData["ErrorMessage"] = message;
    }

    public static void SetWarningNotification(this Controller controller, string message)
    {
        controller.TempData["WarningMessage"] = message;
    }

    public static void SetInfoNotification(this Controller controller, string message)
    {
        controller.TempData["InfoMessage"] = message;
    }

    public static void SetSuccessNotification(this Controller controller, string title, string message)
    {
        controller.TempData["SuccessTitle"] = title;
        controller.TempData["SuccessMessage"] = message;
    }

    public static void SetErrorNotification(this Controller controller, string title, string message)
    {
        controller.TempData["ErrorTitle"] = title;
        controller.TempData["ErrorMessage"] = message;
    }

    public static void SetWarningNotification(this Controller controller, string title, string message)
    {
        controller.TempData["WarningTitle"] = title;
        controller.TempData["WarningMessage"] = message;
    }

    public static void SetInfoNotification(this Controller controller, string title, string message)
    {
        controller.TempData["InfoTitle"] = title;
        controller.TempData["InfoMessage"] = message;
    }

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

    public static IActionResult JsonSuccess(this Controller controller, string message, object? data = null)
    {
        return controller.JsonWithNotification(true, message, data);
    }

    public static IActionResult JsonError(this Controller controller, string message, object? data = null)
    {
        return controller.JsonWithNotification(false, message, data);
    }

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


namespace Bot;

/// <summary>
/// HTTP route constants for routing requests to controller methods.
/// </summary>
public static class HttpRouteConstants
{
    /// <summary>
    /// Route prefix for all incoming requests.
    /// </summary>
    public const string CallbackPrefix = "/callback";

    /// <summary>
    /// Route for incoming requests including notifications, callbacks and incoming call.
    /// </summary>
    public const string CallNotificationsRoute = CallbackPrefix + "/calling";

    /// <summary>
    /// Bot Framework route for incoming requests.
    /// </summary>
    public const string MessageNotificationsRoute = "api/messages";
}

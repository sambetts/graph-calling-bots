namespace RickrollP2PPstnBot;

/// <summary>
/// HTTP route constants for routing requests to CallController methods.
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
    public const string OnIncomingRequestRoute = CallbackPrefix + "/calling";

}

namespace PstnBot.FunctionApp;

/// <summary>
/// HTTP route constants for routing requests to controller methods.
/// </summary>
public static class HttpRouteConstants
{
    /// <summary>
    /// Route for incoming requests including notifications, callbacks and incoming call.
    /// </summary>
    public const string CallNotificationsRoute = "/api/CallNotification";


    public const string WavFileRoute = "/api/WavFile";
}

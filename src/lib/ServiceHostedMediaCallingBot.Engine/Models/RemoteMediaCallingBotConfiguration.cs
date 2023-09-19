namespace ServiceHostedMediaCallingBot.Engine.Models;

/// <summary>
/// Config needed for a remote media calling bot.
/// </summary>
public class RemoteMediaCallingBotConfiguration
{
    public string AppId { get; set; } = null!;

    public string AppSecret { get; set; } = null!;

    public string TenantId { get; set; } = null!;

    public string AppInstanceObjectId { get; set; } = null!;

    public string AppInstanceObjectName { get; set; } = null!;

    public string BotBaseUrl { get; set; } = null!;

    public string CallingEndpoint { get; set; } = null!;
}

public class SingleWavFileBotConfig : RemoteMediaCallingBotConfiguration
{
    /// <summary>
    /// A callback URL for the sound played over the phone.
    /// </summary>
    public string RelativeWavCallbackUrl { get; set; } = null!;
}

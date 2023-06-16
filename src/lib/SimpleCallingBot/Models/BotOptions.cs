namespace SimpleCallingBotEngine.Models;

public class BotOptions
{
    public string AppId { get; set; } = null!;

    public string AppSecret { get; set; } = null!;

    public string TenantId { get; set; } = null!;

    public string AppInstanceObjectId { get; set; } = null!;

    public string AppInstanceObjectName { get; set; } = null!;

    public string BotBaseUrl { get; set; } = null!;
}

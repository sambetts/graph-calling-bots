using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Graph;
using System.Threading.Tasks;

namespace GroupCallingChatBot.Web.Models;

public static class BotUserUtils
{
    public static BotUser ParseBotUserInfo(this ChannelAccount user)
    {
        return string.IsNullOrEmpty(user.AadObjectId) ? new BotUser { IsAzureAdUserId = false, UserId = user.Id } : new BotUser { IsAzureAdUserId = true, UserId = user.AadObjectId };
    }

}

public class BotUser
{
    public string UserId { get; set; } = string.Empty;
    public bool IsAzureAdUserId { get; set; } = false;
}

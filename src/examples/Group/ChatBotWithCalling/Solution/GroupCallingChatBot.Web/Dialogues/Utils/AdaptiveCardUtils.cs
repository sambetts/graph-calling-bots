using GroupCallingChatBot.Web.Models;
using Newtonsoft.Json;
using System;

namespace GroupCallingChatBot.Web.Dialogues.Utils;

public class AdaptiveCardUtils
{
    public static AdaptiveCardActionResponse? GetAdaptiveCardAction(string? submitJson)
    {
        AdaptiveCardActionResponse? r = null;
        if (string.IsNullOrEmpty(submitJson))
        {
            return r;
        }

        try
        {
            r = JsonConvert.DeserializeObject<AdaptiveCardActionResponse>(submitJson);
        }
        catch (Exception)
        {
            // Nothing
        }

        if (r != null && r.Action == CardConstants.CardActionValAddAttendee)
        {
            r = JsonConvert.DeserializeObject<AddContactAdaptiveCardActionResponse>(submitJson);
        }

        return r;
    }
}
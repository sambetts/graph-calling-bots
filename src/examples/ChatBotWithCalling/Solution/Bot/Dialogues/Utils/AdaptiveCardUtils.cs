using Newtonsoft.Json;
using System;

namespace Bot.Dialogues.Utils;


public class AdaptiveCardUtils
{
    public static ActionResponse? GetAdaptiveCardAction(string? submitJson, string fromAadObjectId)
    {
        ActionResponse? r = null;
        if (string.IsNullOrEmpty(submitJson))
        {
            return r;   
        }

        try
        {
            r = JsonConvert.DeserializeObject<ActionResponse>(submitJson);
        }
        catch (Exception)
        {
            // Nothing
        }

        return r;
    }
}
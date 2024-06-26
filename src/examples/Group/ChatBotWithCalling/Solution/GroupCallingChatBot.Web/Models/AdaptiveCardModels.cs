﻿using Newtonsoft.Json;
namespace GroupCallingChatBot.Web.Models;

public class AdaptiveCardActionResponse
{
    [JsonProperty(CardConstants.CardActionPropName)]
    public string Action { get; set; } = null!;
}

public class AddContactAdaptiveCardActionResponse : AdaptiveCardActionResponse
{
    [JsonProperty("txtContactType")]
    public string ContactType { get; set; } = null!;

    [JsonProperty("txtContactId")]
    public string ContactId { get; set; } = null!;
}

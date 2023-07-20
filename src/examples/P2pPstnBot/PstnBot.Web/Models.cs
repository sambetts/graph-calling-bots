using System.Text.Json.Serialization;

namespace PstnBot;

public abstract class ODataObject
{
    [JsonPropertyName("@odata.type")]
    public abstract string odatatype { get; }
}
public class MeetingCapability
{
    public string? odatatype { get; set; }
}

public class ResourceData
{
    [JsonPropertyName("@odata.type")]
    public string? odatatype { get; set; }

    [JsonPropertyName("state")]
    public string? state { get; set; }

    [JsonPropertyName("resultInfo")]
    public ResultInfo resultInfo { get; set; }

    [JsonPropertyName("meetingCapability")]
    public MeetingCapability meetingCapability { get; set; }

    [JsonPropertyName("coOrganizers")]
    public List<object> CoOrganizers { get; set; }

    [JsonPropertyName("callChainId")]
    public string? callChainId { get; set; }
}

public class ResultInfo
{

    [JsonPropertyName("code")]
    public int code { get; set; }

    [JsonPropertyName("subcode")]
    public int subcode { get; set; }

    [JsonPropertyName("message")]
    public string? message { get; set; }
}

public class CommsNotifications
{
    [JsonPropertyName("@odata.type")]
    public string? odatatype { get; set; }

    [JsonPropertyName("value")]
    public List<CommsNotification> value { get; set; }
}

public class CommsNotification
{
    [JsonPropertyName("@odata.type")]
    public string? odatatype { get; set; }

    [JsonPropertyName("changeType")]
    public string? changeType { get; set; }

    [JsonPropertyName("resource")]
    public string? resource { get; set; }

    [JsonPropertyName("resourceUrl")]
    public string? resourceUrl { get; set; }

    [JsonPropertyName("resourceData")]
    public ResourceData? resourceData { get; set; }
}

public class PhoneIdentitySet : ODataObject
{
    [JsonPropertyName("phone")]
    public Identity? Phone { get; set; }

    [JsonPropertyName("@odata.type")]
    public override string odatatype => "#microsoft.graph.identitySet";
}

public class AppInstanceIdentitySet : ODataObject
{
    [JsonPropertyName("applicationInstance")]
    public Identity? ApplicationInstance { get; set; }

    [JsonPropertyName("@odata.type")]
    public override string odatatype => "#microsoft.graph.identitySet";
}


public class Identity : ODataObject
{
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }


    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("@odata.type")]
    public override string odatatype => "#microsoft.graph.identity";
}

public class ParticipantInfo : ODataObject
{
    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("identity")]
    public PhoneIdentitySet Identity { get; set; }

    [JsonPropertyName("endpointType")]
    public string? EndpointType { get; set; }

    [JsonPropertyName("languageId")]
    public string? LanguageId { get; set; }

    [JsonPropertyName("region")]
    public string? Region { get; set; }

    [JsonPropertyName("participantId")]
    public string? ParticipantId { get; set; }

    [JsonPropertyName("@odata.type")]
    public override string odatatype => "#microsoft.graph.invitationParticipantInfo";
}

public class CallOptions
{
    [JsonPropertyName("@odata.type")]
    public string? OdataType { get; set; }
}

public class ContentSharingSession
{
    [JsonPropertyName("@odata.type")]
    public string? OdataType { get; set; }
}

public class MediaConfig : ODataObject
{

    [JsonPropertyName("PreFetchMedia")]
    public List<Microsoft.Graph.MediaInfo> PreFetchMedia { get; set; } = new();

    [JsonPropertyName("@odata.type")]
    public override string odatatype => "#microsoft.graph.serviceHostedMediaConfig";
}

public class MeetingInfo
{
    [JsonPropertyName("@odata.type")]
    public string? OdataType { get; set; }
}

public class Call : ODataObject
{

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("callbackUri")]
    public string? CallbackUri { get; set; }

    [JsonPropertyName("mediaConfig")]
    public MediaConfig MediaConfig { get; set; }

    [JsonPropertyName("requestedModalities")]
    public List<string> RequestedModalities { get; set; } = new();

    [JsonPropertyName("source")]
    public CallSource? Source { get; set; }

    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = null!;

    [JsonPropertyName("targets")]
    public List<ParticipantInfo>? Targets { get; set; } = new();

    [JsonPropertyName("@odata.type")]
    public override string odatatype => "#microsoft.graph.call";
}

public class CallSource : ODataObject
{
    [JsonPropertyName("@odata.type")]
    public override string odatatype => "#microsoft.graph.participantInfo";

    [JsonPropertyName("identity")]
    public AppInstanceIdentitySet Identity { get; set; }



    public string? countryCode { get; set; }
    public string? endpointType { get; set; }
    public string? region { get; set; }
    public string? languageId { get; set; }
}

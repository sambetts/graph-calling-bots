# Teams Calling Bots (with optional PSTN)
Graph calling bots built for ASP.Net 8. Simplify calling bots for Teams/Graph in C#; designed for scalable cloud. 

This is a project to demonstrate how calling bots can work in Teams, using service-hosted media (static WAV files only). It _doesn’t_ use the [Graph Communications Calling SDK](https://microsoftgraph.github.io/microsoft-graph-comms-samples/docs/client/index.html) except for some model classes and request validation checks, as I wanted a more .Net standardised app model: abstracted state persistence, standard logging libraries etc, that fit much better into things like functions apps where you don’t necessarily keep everything in memory (stateful). 

The calling logic therefore is much simplified and just uses standard .Net classes and libraries, which makes it more lightweight, but does means it can’t handle app-hosted media for now. 

## How do Bots Work in this Framework?
Here's an example:

```C#
public class CallInviteBot : AudioPlaybackAndDTMFCallingBot<GroupCallInviteActiveCallState>
{
    public const string TRANSFERING_PROMPT_ID = "transferingPrompt";

    /// <summary>
    /// Call someone and ask if they can join a group call.
    /// </summary>
    public async Task<Call?> InviteUserToGroupCall(AttendeeCallInfo initialAdd, StartGroupCallData groupMeetingRequest, Call createdGroupCall)
    {
        var callMediaPlayList = new List<MediaInfo>
        {
            // Add default media prompt. Will automatically play when call is connected.
            new MediaInfo { Uri = groupMeetingRequest.MessageInviteUrl, ResourceId = DEFAULT_PROMPT_ID },

            // Add any message transfering audio
            new MediaInfo { Uri = groupMeetingRequest.MessageTransferingUrl, ResourceId = TRANSFERING_PROMPT_ID }
        };

        // Start P2P call
        var singleAttendeeCallReq = await CreateCallRequest(new InvitationParticipantInfo { Identity = initialAdd.ToIdentity() }, callMediaPlayList, groupMeetingRequest.HasPSTN, false);
        var singleAttendeeCall = await CreateNewCall(singleAttendeeCallReq);

        // Remember initial state of the call: which group-call to transfer to and who to transfer
        await InitCallStateAndStoreMediaInfoForCreatedCall(singleAttendeeCall, callMediaPlayList,
            createdCallState =>
            {
                createdCallState.GroupCallId = createdGroupCall.Id;
                createdCallState.AtendeeIdentity = initialAdd.ToIdentity();
            });

        return singleAttendeeCall;
    }

    protected async override Task NewTonePressed(GroupCallInviteActiveCallState callState, Tone tone)
    {
        if (tone == Tone.Tone1)
        {
            // Play "transfering" WAV.
            await PlayConfiguredMediaIfNotAlreadyPlaying(callState, TRANSFERING_PROMPT_ID);

            // Transfer P2P call to group call, replacing the call used for the invite
            var transferReq = new InvitePostRequestBody
            {
                Participants = new List<InvitationParticipantInfo>
                {
                    new InvitationParticipantInfo
                    {
                        Identity = callState.AtendeeIdentity,
                        ReplacesCallId = callState.CallId
                    },

                },
            };

            await _graphServiceClient.Communications.Calls[callState.GroupCallId].Participants.Invite.PostAsync(transferReq);
        }
    }
}

```

See more on the repo home - [GitHub link](https://github.com/sambetts/graph-calling-bots/)


[Bot icons created by Freepik - Flaticon](https://www.flaticon.com/free-icons/bot)

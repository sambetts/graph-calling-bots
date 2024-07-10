﻿using Microsoft.Extensions.Logging;
using Microsoft.Graph.Communications.Calls.Item.Transfer;
using Microsoft.Graph.Models;
using ServiceHostedMediaCallingBot.Engine.CallingBots;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

namespace GroupCalls.Common;

public class CallInviteBot : PstnCallingBot<GroupCallInviteActiveCallState>
{
    public CallInviteBot(RemoteMediaCallingBotConfiguration botOptions, ICallStateManager<GroupCallInviteActiveCallState> callStateManager, ICallHistoryManager<GroupCallInviteActiveCallState, CallNotification> callHistoryManager, ILogger<GroupCallBot> logger)
        : base(botOptions, callStateManager, callHistoryManager, logger) { }

    public async Task<Call?> CallCandidateForGroupCall(AttendeeCallInfo initialAdd, StartGroupCallData groupMeetingRequest, Call createdGroupCall)
    {
        if (createdGroupCall == null || createdGroupCall.Id == null)
        {
            throw new ArgumentNullException(nameof(createdGroupCall));
        }

        // Work out what audio to play, if anything
        var mediaInfoItem = string.IsNullOrEmpty(groupMeetingRequest.MessageUrl) ? null : new MediaInfo { Uri = groupMeetingRequest.MessageUrl, ResourceId = Guid.NewGuid().ToString() };

        // Remember initial state
        await InitCallStateAndStoreMediaInfoForCreatedCall(createdGroupCall, mediaInfoItem, createdCallState => createdCallState.GroupCallId = createdGroupCall.Id);

        var newTarget = new InvitationParticipantInfo
        {
            Identity = initialAdd.ToIdentity()
        };

        var singleAttendeeCallReq = await CreateCallRequest(newTarget, mediaInfoItem, groupMeetingRequest.HasPSTN, false);

        // Start call
        var singleAttendeeCall = await CreateNewCall(singleAttendeeCallReq);

        if (singleAttendeeCall != null)
        {
            // Remember initial state of the call to transfer to and who to transfer to it
            await InitCallStateAndStoreMediaInfoForCreatedCall(singleAttendeeCall, mediaInfoItem, 
                createdCallState => 
                { 
                    createdCallState.GroupCallId = createdGroupCall.Id; 
                });
        }

        return singleAttendeeCall;
    }

    protected async override Task NewTonePressed(GroupCallInviteActiveCallState callState, Tone tone)
    {
        if (tone == Tone.Tone1)
        {
            _logger.LogInformation("Tone 1 pressed, transferring call");

            // Transfer
            var transferReq = new TransferPostRequestBody
            {
                Transferee = new ParticipantInfo
                {
                    Identity = callState.AtendeeIdentity
                }
            };
            await _graphServiceClient.Communications.Calls[callState.GroupCallId].Transfer.PostAsync(transferReq);
        }
    }

    protected async override Task CallConnectedWithP2PAudio(GroupCallInviteActiveCallState callState)
    {
        await base.CallConnectedWithP2PAudio(callState);
        await PlayConfiguredMediaIfNotAlreadyPlaying(callState);
    }
}

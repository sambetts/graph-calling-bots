
import { useContext, useState } from "react";
import { Client } from "@microsoft/microsoft-graph-client";
import { app } from "@microsoft/teams-js";

import { Button, Input, Spinner } from "@fluentui/react-components";
import { Call, ChatMessage, OnlineMeeting, TeamworkTagMember } from "@microsoft/microsoft-graph-types";
import React from "react";
import { IGraphArrayResponse } from "../../GraphResponse";
import config from "./lib/config";
import { OnlineMeetingCreator } from "./OnlineMeetingCreator";
import { BotRequest } from "../../models";
import { TabSelector } from "./TabSelector";
import { UNSELECTED_OPTION } from "./lib/controlconstants";
import { ChannelSelector } from "./ChannelSelector";
import newMeetingJson from './NewMeetingAdaptiveCard.json';
import { TeamsFxContext } from "../Context";
import { UserInfo } from "@microsoft/teamsfx";

export function CallOrchestrator(props: { graphClient: Client, team: app.TeamInfo }) {

  const [tagMembers, setTagMembers] = useState<TeamworkTagMember[] | null>(null);
  const [userInfo, setUserInfo] = useState<UserInfo | null>(null);
  const [selectedTagId, setSelectedTagId] = useState<string>(UNSELECTED_OPTION);
  const [selectedChannelId, setSelectedChannelId] = useState<string>(UNSELECTED_OPTION);
  const [loadingMembers, setLoadingMembers] = useState<boolean>(false);
  const [teamsChannelPostCreated, setTeamsChannelPostCreated] = useState<boolean>(false);
  const [defaultWavFileUrl, setDefaultWavFileUrl] = useState<string | undefined>("asdf");
  const [createdCall, setCreatedCall] = useState<Call | undefined>(undefined);
  const [createdMeeting, setCreatedMeeting] = useState<OnlineMeeting | undefined>(undefined);
  const { teamsUserCredential } = useContext(TeamsFxContext);

  const UNSELECTED = "-1";

  const unexpectedResponse = () => {
    alert("Got unexpected response creating call");
  }

  const startCall = () => {
    if (tagMembers && config.bot) {
      let req: BotRequest = { Attendees: [], MessageUrl: defaultWavFileUrl };

      // Online meeting details?
      if (createdMeeting && createdMeeting.joinWebUrl && createdMeeting.chatInfo) {
        req.JoinMeetingInfo = { JoinUrl: createdMeeting.joinWebUrl };
      }

      tagMembers.forEach(m => {
        if (m.displayName && m.userId) {
          // Add Teams users to request
          req.Attendees.push({ DisplayName: m.displayName, Id: m.userId, Type: 2 });
        }
      });

      // Send to bot
      fetch(config.bot, {
        method: "post",
        headers: {
          'Accept': 'application/json',
          'Content-Type': 'application/json'
        },

        body: JSON.stringify(req)
      })
        .then((response) => {
          if (response.ok) {
            response.text().then(responseText => {
              if (responseText) {

                // useEffect will run logic for posting call
                setCreatedCall(JSON.parse(responseText));
              }
              else unexpectedResponse();
            })
          }
          else unexpectedResponse();
        })
        .catch(err => {
          console.error(err);
          unexpectedResponse();
        });
    }
  };

  // Post call to Team channel
  React.useEffect(() => {

    if (selectedChannelId !== UNSELECTED && createdMeeting) {

      // Replace vars
      const bodyString = (JSON.stringify(newMeetingJson ?? "")
        .replace("${{MEETING_URL}}", createdMeeting?.joinWebUrl ?? "")
        .replace("${{CREATED_BY}}", userInfo?.displayName ?? "")
        .replace("${{CREATED_ON}}", new Date().toISOString().split('.')[0] + "Z")   // Remove milliseconds from ISO output as it breaks Adaptive cards DATE function

      );

      // https://learn.microsoft.com/en-us/graph/api/channel-post-messages?view=graph-rest-1.0&tabs=http
      const msg = {
        body: {
          contentType: "html",
          content: "<attachment id=\"04b151bb-2f88-4f5b-9615-30b2a59d9adf\"></attachment>"
        },
        attachments:
          [
            {
              id: "04b151bb-2f88-4f5b-9615-30b2a59d9adf",
              contentType: "application/vnd.microsoft.card.adaptive",
              content: bodyString
            }
          ]
      };

      // Post message
      props.graphClient.api(`/teams/${props.team.groupId}/channels/${selectedChannelId}/messages`).post(msg)
        .then((r: ChatMessage) => {
          setTeamsChannelPostCreated(true);
        })
        .catch(er => alert("Couldn't post call to channel: " + selectedChannelId));
    }

  }, [createdCall]);

  // Set default WAV and load user info
  React.useEffect(() => {

    // On 1st laod
    setDefaultWavFileUrl(config.defaultWavUrl);

    teamsUserCredential?.getUserInfo().then((userInfo: UserInfo) => setUserInfo(userInfo))
  }, []);

  // Load members for selected tag
  React.useEffect(() => {

    setTagMembers(null);
    if (selectedTagId && selectedTagId != UNSELECTED) {
      setLoadingMembers(true);

      // https://learn.microsoft.com/en-us/graph/api/teamworktagmember-list?view=graph-rest-1.0&tabs=http
      props.graphClient.api(`/teams/${props.team.groupId}/tags/${selectedTagId}/members`).get()
        .then((r: IGraphArrayResponse<TeamworkTagMember>) => {

          setLoadingMembers(false);
          setTagMembers(r.value);
        })
        .catch(err => alert("Couldn't load members for tag: " + selectedTagId));
    }

  }, [selectedTagId, props.graphClient, props.team.groupId]);

  return (
    <>
      <div>

        <OnlineMeetingCreator graphClient={props.graphClient} team={props.team}
          newMeeting={(m: OnlineMeeting) => setCreatedMeeting(m)} />
        {createdMeeting &&
          <ChannelSelector graphClient={props.graphClient} team={props.team}
            channelSelectedCallback={(id: string | undefined) => setSelectedChannelId(id ?? UNSELECTED_OPTION)} />
        }

        <h3>Who to Invite</h3>
        <p>Pick a tag in Team '{props.team.displayName}'. Then you can call everyone with that tag.</p>
        <TabSelector graphClient={props.graphClient} team={props.team}
          tagSelectedCallback={(id: string | undefined) => setSelectedTagId(id ?? UNSELECTED_OPTION)} />

        {loadingMembers &&
          <Spinner />
        }

        <h3>Initial Message (Optional)</h3>
        <Input onChange={v => setDefaultWavFileUrl(v.currentTarget.value)} value={defaultWavFileUrl} type="url" style={{ width: 300 }}></Input>

        {tagMembers &&
          <div>
            <label>Members:</label>

            <ul>
              {tagMembers.map(m => {
                return <li key={m.id}>{m.displayName} ({m.userId})</li>
              })}
            </ul>
          </div>
        }

        {selectedTagId !== UNSELECTED_OPTION &&
          <div>
            <Button onClick={startCall} appearance="primary">Call Everyone in Selected Tag</Button>
          </div>
        }
        {createdCall &&
          <>
            <h2>New Call Details</h2>
            <pre>
              {JSON.stringify(createdCall, null, 2)}
            </pre>
          </>
        }

        {teamsChannelPostCreated && 
          <p>Teams channel post created</p>
        }
        {teamsChannelPostCreated && createdMeeting?.joinWebUrl &&
          <p>Join meeting <a href={createdMeeting.joinWebUrl}>here</a></p>
        }
      </div >
    </>
  );
}

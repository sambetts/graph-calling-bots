
import { useId, useState } from "react";
import { Client } from "@microsoft/microsoft-graph-client";
import { app } from "@microsoft/teams-js";

import { Button, Select, Spinner } from "@fluentui/react-components";
import { Call, TeamworkTag, TeamworkTagMember } from "@microsoft/microsoft-graph-types";
import React from "react";
import { IGraphArrayResponse } from "../../GraphResponse";
import config from "./lib/config";

export function CallOrchestrator(props: { graphClient: Client, team: app.TeamInfo }) {

  const [tags, setTags] = useState<TeamworkTag[] | null>(null);
  const [tagMembers, setTagMembers] = useState<TeamworkTagMember[] | null>(null);
  const [selectedTagId, setSelectedTagId] = useState<string | undefined>(undefined);
  const [loadingMembers, setLoadingMembers] = useState<boolean>(false);
  const [createdCall, setCreatedCall] = useState<Call | undefined>(undefined);

  const selectedTagControlId = useId();

  React.useEffect(() => {
    // Load tags for current team
    props.graphClient.api(`/teams/${props.team.groupId}/tags`).get().then((r: IGraphArrayResponse<TeamworkTag>) => {

      r.value.unshift({ id: undefined, displayName: "Select" });
      setTags(r.value);
    });
  }, []);

  const unexpectedResponse = () => {
    alert("Got unexpected response creating call");
  }

  const startCall = () => {
    if (tagMembers && config.bot) {

      let req: BotRequest = { Attendees: [] };

      tagMembers.forEach(m => {
        if (m.displayName && m.userId) {
          req.Attendees.push({ DisplayId: m.displayName, Id: m.userId, Type: 2 });
        }
      });

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
            response.text().then(t => {
              if (t) {
                setCreatedCall(JSON.parse(t));
              }
              else unexpectedResponse();
            })
          }
          else unexpectedResponse();
        })
        .catch(err => {
          console.error(err);
          alert("Shit");
        });
    }
  };

  React.useEffect(() => {

    setTagMembers(null);
    if (selectedTagId) {
      setLoadingMembers(true);

      // https://learn.microsoft.com/en-us/graph/api/teamworktagmember-list?view=graph-rest-1.0&tabs=http
      props.graphClient.api(`/teams/${props.team.groupId}/tags/${selectedTagId}/members`).get()
        .then((r: IGraphArrayResponse<TeamworkTagMember>) => {

          setLoadingMembers(false);
          setTagMembers(r.value);
        });
    }

  }, [selectedTagId]);

  return (
    <>
      <div>
        <p>First, pick a tag in Team '{props.team.displayName}'. Then you can call everyone with that tag.</p>
        {tags ?
          <>
            <label htmlFor={selectedTagControlId}>Team tag:</label>
            <Select id={selectedTagControlId} {...props} onChange={c => setSelectedTagId(tags[c.target.selectedIndex].id ?? undefined)}>
              {tags.map(t => {
                return <option id={t.id} key={t.id}>{t.displayName}</option>;
              })
              }
            </Select>

            {loadingMembers &&
              <Spinner />
            }

            {tagMembers &&
              <>
                Members:
                <ul>
                  {tagMembers.map(m => {
                    return <li>{m.displayName}</li>
                  })}
                </ul>
                <Button onClick={startCall} appearance="primary">Call Everyone in Selected Tag</Button>

                {createdCall &&
                  <>
                  <h2>New Call Details</h2>
                    <pre>
                      {JSON.stringify(createdCall, null, 2)}
                    </pre>
                  </>

                }
              </>
            }
          </>
          :
          <Spinner />
        }

      </div >
    </>
  );
}

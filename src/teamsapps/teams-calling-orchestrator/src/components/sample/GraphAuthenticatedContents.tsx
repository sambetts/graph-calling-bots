
import { useContext, useId, useState } from "react";
import { Client } from "@microsoft/microsoft-graph-client";
import { app } from "@microsoft/teams-js";

import { Button, Select, Spinner } from "@fluentui/react-components";
import { TeamworkTag, TeamworkTagMember } from "@microsoft/microsoft-graph-types";
import React from "react";
import { IGraphArrayResponse } from "../../GraphResponse";

export function GraphAuthenticatedContents(props: { graphClient: Client, team: app.TeamInfo }) {

  const [tags, setTags] = useState<TeamworkTag[] | null>(null);
  const [tagMembers, setTagMembers] = useState<TeamworkTagMember[] | null>(null);
  const [selectedTagId, setSelectedTagId] = useState<string | null>(null);
  const selectedTagControlId = useId();

  React.useEffect(() => {

    props.graphClient.api(`/teams/${props.team.groupId}/tags`).get().then((r: IGraphArrayResponse<TeamworkTag>) => {
      setTags(r.value);
    });
  }, []);

  const startCall = React.useCallback(() => {
    
  }, []);

  React.useEffect(() => {

    if (selectedTagId) {

      // https://learn.microsoft.com/en-us/graph/api/teamworktagmember-list?view=graph-rest-1.0&tabs=http
      props.graphClient.api(`/teams/${props.team.groupId}/tags/${selectedTagId}/members`).get()
        .then((r: IGraphArrayResponse<TeamworkTagMember>) => {
          setTagMembers(r.value);
        });
    }

  }, [selectedTagId]);

  return (
    <>
      <main>
        <>
          <div style={{ marginLeft: 40 }}>
            {tags ?
              <>
                <label htmlFor={selectedTagControlId}>Team tag:</label>
                <Select id={selectedTagControlId} {...props} onChange={c => setSelectedTagId(tags[c.target.selectedIndex].id ?? null)}>
                  {tags.map(t => {
                    return <option id={t.id}>{t.displayName}</option>;
                  })
                  }
                </Select>

                {tagMembers &&
                  <>
                    Members:
                    <ul>
                      {tagMembers.map(m => {
                        return <li>{m.displayName}</li>
                      })}
                    </ul>
                    <Button onClick={startCall} appearance="primary">Call Everyone in Selected Tag</Button>
                    <pre>
                      {selectedTagId}
                    </pre>
                  </>
                }

              </>
              :
              <Spinner />
            }

          </div >
        </>
      </main>
    </>
  );
}

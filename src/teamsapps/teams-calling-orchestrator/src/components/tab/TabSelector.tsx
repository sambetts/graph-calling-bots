
import { useId, useState } from "react";
import { Client } from "@microsoft/microsoft-graph-client";
import { app } from "@microsoft/teams-js";

import { Select, Spinner } from "@fluentui/react-components";
import { TeamworkTag } from "@microsoft/microsoft-graph-types";
import React from "react";
import { IGraphArrayResponse } from "../../GraphResponse";
import { UNSELECTED_OPTION } from "./lib/controlconstants";

export function TabSelector(props: { graphClient: Client, team: app.TeamInfo, tagSelectedCallback : Function }) {

  const [tags, setTags] = useState<TeamworkTag[] | null>(null);
  const selectedTagControlId = useId();

  React.useEffect(() => {

    // Load tags for current team
    props.graphClient.api(`/teams/${props.team.groupId}/tags`).get().then((r: IGraphArrayResponse<TeamworkTag>) => {

      r.value.unshift({ id: UNSELECTED_OPTION, displayName: "Select" });
      setTags(r.value);
    });
  }, []);


  return (
    <>
      {tags ?
        <>
          <label htmlFor={selectedTagControlId}>Call people with tag:</label>
          <Select id={selectedTagControlId} {...props} onChange={c => props.tagSelectedCallback(tags[c.target.selectedIndex].id ?? undefined)}>
            {tags.map(t => {
              return <option id={t.id} key={t.id}>{t.displayName}</option>;
            })
            }
          </Select>
        </>
        :
        <Spinner />
      }

    </>
  );
}

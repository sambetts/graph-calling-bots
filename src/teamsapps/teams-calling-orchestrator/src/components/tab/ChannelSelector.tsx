
import { useId, useState } from "react";
import { Client } from "@microsoft/microsoft-graph-client";
import { app } from "@microsoft/teams-js";

import { Select, Spinner } from "@fluentui/react-components";
import { Channel } from "@microsoft/microsoft-graph-types";
import React from "react";
import { IGraphArrayResponse } from "../../GraphResponse";
import { UNSELECTED_OPTION } from "./lib/controlconstants";

export function ChannelSelector(props: { graphClient: Client, team: app.TeamInfo, channelSelectedCallback : Function }) {

  const [channels, setChannels] = useState<Channel[] | null>(null);
  const selectedChannelControlId = useId();
  
  React.useEffect(() => {

    // Load tags for current team
    props.graphClient.api(`/teams/${props.team.groupId}/allChannels`).get().then((r: IGraphArrayResponse<Channel>) => {

      r.value.unshift({ id: UNSELECTED_OPTION, displayName: "Select" });
      setChannels(r.value);
    });
  }, []);


  return (
    <>
      {channels ?
        <>
          <label htmlFor={selectedChannelControlId}>Post meeting to channel:</label>
          <Select id={selectedChannelControlId} {...props} onChange={c => props.channelSelectedCallback(channels[c.target.selectedIndex].id ?? undefined)}>
            {channels.map(t => {
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

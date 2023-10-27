import { useState } from "react";
import { Client } from "@microsoft/microsoft-graph-client";
import { SCOPES } from "../../constants";
import { GraphContainer } from "./lib/GraphContainer";
import { app } from "@microsoft/teams-js";
import { useData } from "@microsoft/teamsfx-react";
import {
  Image
} from "@fluentui/react-components";
import { CallOrchestrator } from "./CallOrchestrator";

export function AppPageContents() {

  const [graphClient, setGraphClient] = useState<Client | null>(null);
  const [teamInfo, setTeamInfo] = useState<app.TeamInfo | undefined | null>(undefined);

  useData(async () => {
    await app.initialize();
    const context = await app.getContext();
    setTeamInfo(context.team ?? null);
  });

  return (
    <div className="welcome page">
      <div className="narrow page-padding">
        <h1 className="center">Call Orchestrator</h1>
        <Image src="hello.png" />

        {teamInfo === undefined ?
          <div>
            Loading...
          </div>
          :
          <>
            {teamInfo === null ?
              <div>
                This app needs to be run from a Team.
              </div>
              :
              <>
                <GraphContainer scopes={SCOPES} onGraphClientValidated={(c: Client) => setGraphClient(c)}>

                  {graphClient ?
                    <CallOrchestrator graphClient={graphClient} team={teamInfo} />
                    :
                    <p>Oops. We have auth but no Graph client and/or playlists to read? Reload app maybe?</p>
                  }

                </GraphContainer>
              </>
            }

          </>
        }

      </div>
    </div>
  );
}


import { useState } from "react";
import { Client } from "@microsoft/microsoft-graph-client";
import { app } from "@microsoft/teams-js";

import { Button, Spinner } from "@fluentui/react-components";
import { OnlineMeeting } from "@microsoft/microsoft-graph-types";
import React from "react";

export function OnlineMeetingCreator(props: { graphClient: Client, team: app.TeamInfo, newMeeting: Function }) {

  const [createdMeeting, setCreatedMeeting] = useState<OnlineMeeting | undefined>(undefined);
  const [creatingMeeting, setCreatingMeeting] = useState<boolean>(false);

  const createMeeting = () => {
    setCreatingMeeting(true);
    var end = new Date();
    end.setHours(end.getHours() + 1);

    const newMeeting = {
      startDateTime: new Date().toISOString(),
      endDateTime: end.toISOString(),
      subject: "New Meeting"
    }

    props.graphClient.api(`/me/onlineMeetings`).post(newMeeting)
      .then((r: OnlineMeeting) => {
        setCreatedMeeting(r)
        setCreatingMeeting(false);
      })
      .catch(err => {
        console.error(err);
        alert('Online Meeting create failed')
        setCreatingMeeting(false);
      });
  };

  React.useEffect(() => {
    props.newMeeting(createdMeeting);
  }, [createdMeeting]);

  return (
    <>
      <div>
        <h3>Meeting for Call</h3>
        {creatingMeeting ?
          <Spinner />
          :
          <>
            {createdMeeting ?
              <>
                <pre>
                  {JSON.stringify(createdMeeting, null, 2)}
                </pre>
              </>
              :
              <Button onClick={createMeeting}>Create Meeting</Button>
            }
          </>
        }

      </div >
    </>
  );
}

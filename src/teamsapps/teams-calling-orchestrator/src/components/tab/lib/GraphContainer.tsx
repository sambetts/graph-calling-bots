import { useContext, useState } from "react";
import { useData } from "@microsoft/teamsfx-react";
import { TeamsFxContext } from "../../Context";
import { ErrorCode, ErrorWithCode } from "@microsoft/teamsfx";
import { Client, GraphError } from "@microsoft/microsoft-graph-client";
import { TokenCredentialAuthenticationProvider } from "@microsoft/microsoft-graph-client/authProviders/azureTokenCredentials";
import { Button } from "@fluentui/react-components";
import { SCOPES } from "../../../constants";

export const GraphContainer: React.FC<{ scopes: string, onGraphClientValidated: Function, children: React.ReactNode }> = (props) => {

  const { teamsUserCredential } = useContext(TeamsFxContext);
  const [graphClient, setGraphClient] = useState<Client | null>(null);
  const [graphError, setGraphError] = useState<GraphError | null>(null);
  const [errorText, setErrorText] = useState<string | null>(null);

  // Manual Login
  const authGraph = async () => {

    if (teamsUserCredential) {
      try {
        await teamsUserCredential.login(props.scopes);
        await loadTestGraph();
        setErrorText(null);
        setGraphError(null);

      } catch (err: unknown) {
        if (err instanceof ErrorWithCode && err.code !== ErrorCode.ConsentFailed) {
          throw err;
        }
        else {
          // Silently fail because user cancels the consent dialog or popup blocker is in use
          setErrorText(JSON.stringify(err))
          alert('Could not login to Graph. Check popup blocker and reload?');
          return;
        }
      }
    }
  }

  const loadTestGraph = async () =>
  {
    if (teamsUserCredential) {
      try {
        const authProvider = new TokenCredentialAuthenticationProvider(teamsUserCredential, {
          scopes: SCOPES.split(" "),
        });

        // Initialize Graph client instance with authProvider
        const c = Client.initWithMiddleware({
          authProvider: authProvider,
        });

        if (c) {

          // Test client
          await c.api("/me").get();
          setGraphClient(c);

          // Raise event
          props.onGraphClientValidated(c);
        }

      } catch (err: unknown) {
        if (err instanceof GraphError) {
          setGraphError(err);
        } else {
          console.error(err);
        }
      }
    }
  }

  // Test a Graph call
  const { loading, data, error } = useData(async () => {
    if (teamsUserCredential) {
      await loadTestGraph();
    }
    return;
  });

  return (
    <>
      {graphError ?
        <>
          {graphError.code === 'ErrorWithCode.UiRequiredError' ?
            <><div>We need your consent for Graph access. Permissions to request:</div>
              <ul>
                {SCOPES.split(" ").map(s => {
                  return <li key={s}>{s}</li>
                })
                }
              </ul>

              <div>Login below:</div>
              <Button disabled={loading} onClick={authGraph}>Authorize</Button>
            </>
            :
            <p>Unknown error: {graphError.code}</p>
          }
        </>
        :
        <>
          <div className="sections">
            {teamsUserCredential && graphClient &&
              <>
                {props.children}
              </>
            }
          </div>
        </>
      }

      {
        errorText &&
        <pre>{errorText}</pre>
      }
    </>
  );
}

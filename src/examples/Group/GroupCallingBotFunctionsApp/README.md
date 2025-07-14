# Group Calling Bots - Azure Functions App
This example calls a list of either Teams or PSTN contacts, and plays a "Hi, there has been an incident. Press 1 to join the call" message to anyone that answers. When they press "1" on the keypad, an optional 2nd audio is played and they are then transfered to the common call.

Functions apps are the more "cloud" version of the group calling bot, being that Azure Function Apps are "serverless". It does have the small downside that static content isn't so easy to host - in this example we send an embedded WAV file as an action, but usually we'd recommend hosting this on a CDN.

## Configuration 
These configuration settings are needed (in "local.settings.json" normally):

Name | Description
--------------- | -----------
MicrosoftAppId | ID of bot Azure AD application
MicrosoftAppPassword | Bot app secret
TenantId | Tenant ID of Azure AD application
Storage (optional) | Azure storage account connection string. Will use in-memory provider if not configured. Example: UseDevelopmentStorage=true
CosmosConnectionString | Cosmos DB connection string. Used for call state. Example: AccountEndpoint=https://callingbot.documents.azure.com:443/;AccountKey=xxxxxx==;
CosmosDatabaseName | Cosmos DB name if cosmos is used. Example: CallingBot
ContainerNameCallHistory (optional) | Cosmos DB container name for call history if cosmos is used. Example: CallsLogs
ContainerNameCallState | Cosmos DB container name for call state if cosmos is used. Example: CallState
SqlCallHistory (optional) | SQL Server connection string if SQL is used for storing call history. Example: Server=tcp:callingbot.database.windows.net,1433;Initial Catalog=CallingBot;Persist Security Info=False;User ID=callingbot;Password=xxxxxx;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
ServiceBusRootConnectionString | Azure Service Bus root connection string for processing call updates in a queue.
GraphMessagesServiceBusQueueCallUpdates | Value of ServiceBusRootConnectionString + service Bus queue name for call updates. Example: <your_connection_string_here>;EntityPath=callupdates
AppInstanceObjectId | For PSTN calls only: object ID of the user account used for calling
BotBaseUrl | URL root of the bot. Example: https://callingbot.eu.ngrok.io

Cosmos DB is used for storing call state and, optionally call history. If Cosmos DB is not configured, the process will throw an exception.

Note: the order of preference for call-history logger is: SQL, Cosmos, InMemory. If you have both SQL and Cosmos configured, it will use SQL. If you have neither, it will use InMemory.

For SQL logging, the account in the connection string needs owner rights to the database as it'll create the schema (only) if the database is empty. 

## Required Service Bus Queues
``callupdates`` - used for processing call updates. Needs to be created in the Service Bus namespace used by the bot. Call updates are sent to this queue by the bot and processed in order.

## Testing the Bot
Expose the bot with NGrok:
```
ngrok http http://localhost:7221 --host-header="localhost:7221"
```

POST this JSon to the bot endpoint /api/StartCall:
```json
{
    "MessageInviteUrl": "https://sambetts.eu.ngrok.io/api/WavFileInviteToCall",
    "MessageTransferingUrl": "https://sambetts.eu.ngrok.io/api/WavFileTransfering",
    "Attendees": [
        {
            "Id": "+3468279XXXX",
            "DisplayName": "Sam Phone",
            "Type": 1
        },
        {
            "Id": "3b10aa94-739a-472c-a68a-0000000",
            "Type": 2
        }
    ],
    "OrganizerUserId": "02d3a453-e241-4cf3-82b0-0000000"
}

```
For Teams users, we specify type "2" and the object ID of the user. For PSTN numbers, we specify type "1" and the PSTN number + a display name for that user. Teams users have their own names so can't be set. 

The call data can include PSTN numbers or Teams users (object IDs). The bot will call the PSTN numbers and Teams users will be called via Teams. It only works with users in the same tenant as the bot. 

## Joining an Online Meeting
You can link the call to a meeting by passing a join URL:
```json
{
    "MessageInviteUrl": "https://callingbot.eu.ngrok.io/Content/GroupCallIntro.wav",
    "Attendees": [
        {
            "Id": "3b10aa94-739a-472c-a68a-0000000",
            "Type": 2
        }
    ],
    "JoinMeetingInfo":
    {
        "JoinUrl" : "https://teams.microsoft.com/l/meetup-join/xxxxxx"
    }
}

```

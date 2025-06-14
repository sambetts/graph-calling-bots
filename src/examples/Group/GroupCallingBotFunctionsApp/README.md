# Group Calling Bots - Azure Functions App
This example calls a list of either Teams or PSTN contacts, and plays a "Hi, there has been an incident. Press 1 to join the call" message to anyone that answers. When they press "1" on the keypad, an optional 2nd audio is played and they are then transfered to the common call.

Functions apps are the more "cloud" version of the group calling bot, being that Azure Function Apps are "serverless". It does have the small downside that static content isn't so easy to host - in this example we send an embedded WAV file as an action, but usually we'd recommend hosting this on a CDN.

## Configuration 
These configuration settings are needed (in "local.settings.json" normally):

Name | Description
--------------- | -----------
MicrosoftAppId | ID of bot Azure AD application
AppInstanceObjectId | For PSTN calls only: object ID of the user account used for calling
AppInstanceObjectIdName | For PSTN calls only: object ID of the user account used for calling
TenantId | Tenant ID of Azure AD application
MicrosoftAppPassword | Bot app secret
BotBaseUrl | URL root of the bot. Example: https://callingbot.eu.ngrok.io
Storage (optional) | Azure storage account connection string. Will use in-memory provider if not configured. Example: UseDevelopmentStorage=true
CosmosDb (optional) | Cosmos DB connection string. Will use in-memory provider if not configured. Example: AccountEndpoint=https://callingbot.documents.azure.com:443/;AccountKey=xxxxxx==;
DatabaseName (optional) | Cosmos DB name if cosmos is used. Example: CallingBot
ContainerName (optional) | Cosmos DB container name if cosmos is used. Example: CallsLogs
SqlCallHistory (optional) | SQL Server connection string if SQL is used for storing call history. Example: Server=tcp:callingbot.database.windows.net,1433;Initial Catalog=CallingBot;Persist Security Info=False;User ID=callingbot;Password=xxxxxx;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;

Note: the order of preference for call-history logger is: SQL, Cosmos, InMemory. If you have both SQL and Cosmos configured, it will use SQL. If you have neither, it will use InMemory.

For SQL logging, the account in the connection string needs owner rights to the database as it'll create the schema (only) if the database is empty. 

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

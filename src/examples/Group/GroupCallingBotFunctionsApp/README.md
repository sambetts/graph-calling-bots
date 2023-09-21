# Group Calling Bots - Azure Functions App
This example calls a bunch of people and plays a "this is a group call" message to anyone that joins. 

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
Storage | Azure storage account connection string. Example: UseDevelopmentStorage=true

## Testing the Bot
Expose the bot with NGrok:
```
ngrok http http://localhost:7221
```

POST this JSon to the bot endpoint /api/StartCall:
```json
{
  "Attendees":
  [
      {
          "Id": "+3468279XXXX",
          "DisplayId" : "Sam",
          "Type": 1
      },
      {
          "Id": "3b10aa94-739a-472c-a68a-0000000",
          "DisplayId" : "sam@alfredoj.local",
          "Type": 2
      }
  ]
}

```
The call data can include PSTN numbers or Teams users (object IDs). The bot will call the PSTN numbers and Teams users will be called via Teams. It only works with users in the same tenant as the bot. 

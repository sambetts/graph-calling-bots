# Group Calling Bots - Azure Functions App
This is the more "cloud" version of the P2P bot, being that Azure Function Apps are "serverless". It does have the small downside that static content isn't so easy to host - in this example we send an embedded WAV file as an action, but usually we'd recommend hosting this on a CDN.

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
          "Id": "+34682796913",
          "DisplayId" : "Sam",
          "Type": 1
      },
      {
          "Id": "sam@alfredoj.info",
          "DisplayId" : " Teams",
          "Type": 2
      }
  ]
}

```

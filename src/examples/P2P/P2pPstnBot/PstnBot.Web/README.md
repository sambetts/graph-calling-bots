# P2P PSTN Calling Bots
This is the classic Web API/webserver implementation. We use the "www" root to serve-up the call contents: a WAV file.

## Configuration - Web API
These configuration settings are needed (via VS secrets usually):

Name | Description
--------------- | -----------
Bot:AppId | ID of bot Azure AD application
Bot:AppInstanceObjectId | For PSTN calls only: object ID of the user account used for calling
Bot:AppInstanceObjectIdName | For PSTN calls only: object ID of the user account used for calling
Bot:TenantId | Tenant ID of Azure AD application
Bot:AppSecret | Bot app secret
Bot:BotBaseUrl | URL root of the bot. Example: https://callingbot.eu.ngrok.io


## Testing the Bots
Expose the bot with NGrok:
```
ngrok http https://localhost:7028 --host-header="localhost:7028"
```
POST this JSon to the bot endpoint /StartCall:
```json
{
  "PhoneNumber": "+34682796XXX"
}
```


The result:
![alt](../../../../imgs/calling.jpg)

Rick calling!

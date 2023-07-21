# P2P PSTN Calling Bots
There are two implementations for this bot:

1. Azure Functions app with optional Azure table storage for call-state.
2. Web API implementation. 

Both share the same bot implementation of RickrollPstnBot but expose it two different ways. 

## Configuration - Web API
These configuration settings are needed:

Name | Description
--------------- | -----------
Bot:AppId | ID of bot Azure AD application
Bot:AppInstanceObjectId | For PSTN calls only: object ID of the user account used for calling
Bot:AppInstanceObjectIdName | For PSTN calls only: object ID of the user account used for calling
Bot:TenantId | Tenant ID of Azure AD application
Bot:AppSecret | Bot app secret
Bot:BotBaseUrl | URL root of the bot. Example: https://callingbot.eu.ngrok.io


## Configuration - Functions App
These configuration settings are needed:

Name | Description
--------------- | -----------
Bot:AppId | ID of bot Azure AD application
Bot:AppInstanceObjectId | For PSTN calls only: object ID of the user account used for calling
Bot:AppInstanceObjectIdName | For PSTN calls only: object ID of the user account used for calling
Bot:TenantId | Tenant ID of Azure AD application
Bot:AppSecret | Bot app secret
Bot:BotBaseUrl | URL root of the bot. Example: https://callingbot.eu.ngrok.io

## Testing the Bots
POST this JSon to the bot endpoint /StartCall:
```json
{
  "PhoneNumber": "+34682796XXX"
}
```

The result:
![alt](imgs/calling.jpg)

Rick calling!

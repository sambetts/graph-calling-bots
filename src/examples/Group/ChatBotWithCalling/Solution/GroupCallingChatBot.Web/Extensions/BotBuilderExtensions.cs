﻿using GraphCallingBots;
using GraphCallingBots.Models;
using GraphCallingBots.StateManagement;
using GroupCallingChatBot.Web.Bots;
using GroupCallingChatBot.Web.Dialogues;
using GroupCallingChatBot.Web.Models;
using GroupCalls.Common;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace GroupCallingChatBot.Web.Extensions;

public static class BotBuilderExtensions
{

    public static IServiceCollection AddCallingBot(this IServiceCollection services, TeamsChatbotBotConfig config)
    {
        services.AddSingleton<RemoteMediaCallingBotConfiguration>(config.ToRemoteMediaCallingBotConfiguration(HttpRouteConstants.CallNotificationsRoute));

        // Use in-memory storage for the call state for now
        services.AddSingleton<ICallStateManager<BaseActiveCallState>, ConcurrentInMemoryCallStateManager<BaseActiveCallState>>();
        services.AddSingleton<ICallStateManager<GroupCallInviteActiveCallState>, ConcurrentInMemoryCallStateManager<GroupCallInviteActiveCallState>>();
        services.AddSingleton<ICallHistoryManager<BaseActiveCallState>>(new ConcurrentInMemoryCallHistoryManager<BaseActiveCallState>());
        services.AddSingleton<ICallHistoryManager<GroupCallInviteActiveCallState>>(new ConcurrentInMemoryCallHistoryManager<GroupCallInviteActiveCallState>());

        services.AddSingleton<GroupCallOrchestrator>();

        services.AddSingleton<CallInviteBot>();
        return services.AddSingleton<GroupCallBot>();
    }

    public static IServiceCollection AddChatBot(this IServiceCollection services)
    {
        // Create the Bot Framework Authentication to be used with the Bot Adapter.
        services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();


        // Create the Bot Framework Adapter with error handling enabled.
        services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

        // Create the storage we'll be using for User and Conversation state. (Memory is great for testing purposes.)
        services.AddSingleton<IStorage, MemoryStorage>();

        // Create the User state. (Used in this bot's Dialog implementation.)
        services.AddSingleton<UserState>();

        // Create the Conversation state. (Used by the Dialog system itself.)
        services.AddSingleton<ConversationState>();

        // The Dialog that will be run by the bot.
        services.AddSingleton<MainDialog>();

        // Create the Bot Adapter with error handling enabled.
        services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

        // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
        return services.AddTransient<IBot, TeamsDialogueBot<MainDialog>>();
    }
}

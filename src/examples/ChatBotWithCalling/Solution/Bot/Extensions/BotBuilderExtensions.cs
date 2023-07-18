﻿using Bot.Bots;
using Bot.Dialogues;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.DependencyInjection;
using SimpleCallingBotEngine;
using Engine;

namespace Bot;

public static class BotBuilderExtensions
{

    public static IServiceCollection AddCallingBot(this IServiceCollection services, Config config) 
    {
        services.AddSingleton(config.ToRemoteMediaCallingBotConfiguration(HttpRouteConstants.CallNotificationsRoute));

        // Use in-memory storage for the call state for now
        services.AddSingleton<ICallStateManager, ConcurrentInMemoryCallStateManager>();

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

        // Bot manager
        services.AddSingleton<ITeamsChatbotManager, GraphTeamsChatbotManager>();


        // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
        return services.AddTransient<IBot, TeamsBot<MainDialog>>();
    }
}

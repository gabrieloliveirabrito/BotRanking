using System.Reflection;
using BotRanking;
using BotRanking.Services;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();

services.AddLogging(b => b.AddConsole());
services.AddSingleton<DiscordSocketClient>(services =>
{
    var config = new DiscordSocketConfig
    {
        AlwaysDownloadUsers = true,
        AlwaysResolveStickers = true,
        AlwaysDownloadDefaultStickers = true,
        APIOnRestInteractionCreation = true,
        GatewayIntents = Discord.GatewayIntents.Guilds | Discord.GatewayIntents.MessageContent | Discord.GatewayIntents.DirectMessages,
        UseSystemClock = true,
        UseInteractionSnowflakeDate = false,
        MessageCacheSize = 1000,
                
    };

    return new DiscordSocketClient(config);
});

services.AddSingleton<InteractionService>(services =>
{
    var client = services.GetRequiredService<DiscordSocketClient>();
    var config = new InteractionServiceConfig { AutoServiceScopes = true, EnableAutocompleteHandlers = true };
    var interaction = new InteractionService(client, config);

    //interaction.SlashCommandExecuted
    return interaction;
});

services.AddSingleton<CommandHandlingService>();
services.AddSingleton<Bot>();

var provider = services.BuildServiceProvider();
var bot = provider.GetRequiredService<Bot>();

bot.Start().Wait();

Console.WriteLine("End of program");
Console.ReadLine();
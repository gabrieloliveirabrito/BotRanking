namespace BotRanking.Services;

using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Microsoft.Extensions.Logging;

public class CommandHandlingService
{
    private readonly InteractionService _interaction;
    private readonly DiscordSocketClient _discord;
    private readonly IServiceProvider _services;
    private readonly ILogger<CommandHandlingService> _logger;
    private bool _registered = false;

    public CommandHandlingService(ILogger<CommandHandlingService> logger, InteractionService interaction, DiscordSocketClient discord, IServiceProvider services)
    {
        _logger = logger;
        _interaction = interaction;
        _discord = discord;
        _services = services;
    }

    public async Task InitializeAsync()
    {
        if (!_registered)
        {
            _registered = true;

            await _interaction.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            await _interaction.RegisterCommandsGloballyAsync();

            _interaction.SlashCommandExecuted += HandleSlashCommand;
            _discord.InteractionCreated += HandleInteraction;
            _logger.LogInformation("CommandHandling initialized");
        }
    }

    private async Task HandleSlashCommand(SlashCommandInfo info, IInteractionContext context, Discord.Interactions.IResult result)
    {
        if (!result.IsSuccess)
        {

            switch (result.Error)
            {
                case InteractionCommandError.UnmetPrecondition:
                    await SendResult(context, $"Unmet Precondition: {result.ErrorReason}");
                    break;
                case InteractionCommandError.UnknownCommand:
                    await SendResult(context, "Unknown command");
                    break;
                case InteractionCommandError.BadArgs:
                    await SendResult(context, "Invalid number or arguments");
                    break;
                case InteractionCommandError.Exception:
                    if (result is Discord.Interactions.ExecuteResult executeResult)
                        _logger.LogCritical(executeResult.Exception, "Slash Command Exception");
                    await SendResult(context, $"Command exception: {result.ErrorReason}");
                    break;
                case InteractionCommandError.Unsuccessful:
                    await SendResult(context, "Command could not be executed");
                    break;
                default:
                    break;
            }
        }
    }

    private async Task SendResult(IInteractionContext context, string message)
    {
        if (context.Interaction.HasResponded)
            await context.Channel.SendMessageAsync(message);
        else
            await context.Interaction.RespondAsync(message);
    }

    private IInteractionContext GetInteractionContext(SocketInteraction interaction)
    {
        switch (interaction.Type)
        {
            case InteractionType.ApplicationCommand:
                return new SocketInteractionContext<SocketSlashCommand>(_discord, (SocketSlashCommand)interaction);
            case InteractionType.ApplicationCommandAutocomplete:
                return new SocketInteractionContext<SocketAutocompleteInteraction>(_discord, (SocketAutocompleteInteraction)interaction);
            case InteractionType.MessageComponent:
                return new SocketInteractionContext<SocketMessageComponent>(_discord, (SocketMessageComponent)interaction);
            case InteractionType.ModalSubmit:
                return new SocketInteractionContext<SocketModal>(_discord, (SocketModal)interaction);
            default:
                return new SocketInteractionContext(_discord, interaction);
        }
    }

    private async Task HandleInteraction(SocketInteraction interaction)
    {
        try
        {
            var context = GetInteractionContext(interaction);
            var result = await _interaction.ExecuteCommandAsync(context, _services);

            if (!result.IsSuccess)
            {
                switch (result.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        await interaction.RespondAsync("UnmetPrecondition");
                        break;
                    default:
                        await interaction.RespondAsync(result.Error.ToString());
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            await interaction.RespondAsync(ex.Message);

            if (interaction.Type is InteractionType.ApplicationCommand)
                await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
        }
    }
}

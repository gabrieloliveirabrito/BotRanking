namespace BotRanking;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Discord;
using Discord.WebSocket;
using BotRanking.Services;

public class Bot
{
    private ILogger<Bot> _logger;
    private DiscordSocketClient _socket;
    private CommandHandlingService _commandHandling;

    public Bot(ILogger<Bot> logger, DiscordSocketClient socket, CommandHandlingService commandHandling)
    {
        _logger = logger;
        _socket = socket;
        _commandHandling = commandHandling;

        _socket.Log += DiscordSocket_Log;
        _socket.Ready += DiscordSocket_Ready;
    }

    private async Task DiscordSocket_Ready()
    {
        try
        {
            await _commandHandling.InitializeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Bot Ready Exception");
        }
    }

    private Task DiscordSocket_Log(LogMessage message)
    {
        switch (message.Severity)
        {
            case LogSeverity.Info:
                _logger.LogInformation(message.Message);
                break;
            case LogSeverity.Warning:
                _logger.LogWarning(message.Message);
                break;
            case LogSeverity.Verbose:
            case LogSeverity.Debug:
                _logger.LogDebug(message.Message);
                break;
            case LogSeverity.Error:
                _logger.LogError(message.Message);
                break;
            case LogSeverity.Critical:
                _logger.LogCritical(message.Message);
                break;
        }
        return Task.CompletedTask;
    }

    public async Task Start()
    {
        try
        {
            await _socket.LoginAsync(TokenType.Bot, "MTEzMjEwOTUyNzUyMTY4OTc4MA.Gc53J3.VFMv3gE8WETJfYZ2B36zfZ3-rynU0x1F_-1B1s");
            await _socket.StartAsync();

            await Task.Delay(-1);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Bot Start");
        }
    }
}
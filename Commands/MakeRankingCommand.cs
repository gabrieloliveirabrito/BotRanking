namespace BotRanking.Commands;

using System.Text;
using BotRanking.Data;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

public class MakeRankingCommand : InteractionModuleBase<SocketInteractionContext<SocketSlashCommand>>
{
    private ILogger<MakeRankingCommand> _logger;

    public MakeRankingCommand(ILogger<MakeRankingCommand> logger)
    {
        _logger = logger;
    }

    [EnabledInDm(false)]
    [SlashCommand("make-ranking", "Generate a top X ranking based on reaction")]
    private async Task MakeRanking(
        [Summary("Channel", "Target channel that bot retrieve messages")] ISocketMessageChannel channel,
        [Summary("Top", "Maximum users of ranking")] int top = 10
        )
    {
        await Context.Interaction.DeferAsync();
        
        var reactionEmote = Emote.Parse("<:caroline5:1129438637898076270>");
        var ranking = new List<RankingData>();

        await channel.GetMessagesAsync(1000).ForEachAwaitAsync(async messages =>
        {
            _logger.LogInformation("Parsing chunk of {0} messages", messages.Count);
            foreach (var message in messages)
            {
                _logger.LogInformation("Parsing message {0} of {1}", message.Content, message.CreatedAt);
                await message.GetReactionUsersAsync(reactionEmote, 10).ForEachAsync(reactions =>
                {
                    _logger.LogInformation("Parsing chunk of {0} reactions", reactions.Count);
                    if (reactions.Count == 0)
                        return;

                    var index = ranking.FindIndex(r => r.Member.Id == message.Author.Id);
                    if (index == -1)
                    {
                        var data = new RankingData(message.Author, message.Id);
                        data.Reactions = reactions.Count;
                        ranking.Add(data);
                    }
                    else
                    {
                        var data = ranking[index];

                        if (data.MessageID == message.Id)
                        {
                            data.Reactions += reactions.Count;

                            ranking[index] = data;
                        }
                    }
                });
            }
        });

        var builder = new EmbedBuilder()
        .WithAuthor(Context.Client.CurrentUser);

        if (ranking.Count == 0)
        {
            builder
            .WithColor(Color.DarkRed)
            .WithTitle("Ranking generation failed")
            .WithDescription("Cannot find user with vote reaction");
        }
        else
        {
            var top10 = ranking.OrderByDescending(d => d.Reactions).Take(top).Select(MakeMemberRow);

            builder.WithColor(Color.Gold)
            .WithTitle($"Top {top} of challenge")
            .WithDescription(string.Join(Environment.NewLine, top10));
        }

        var embed = builder.Build();
        await Context.Interaction.FollowupAsync("Result of challenge", embed: embed);
    }

    private string MakeMemberRow(RankingData data, int index)
    {
        var builder = new StringBuilder();
        builder.Append("- ");

        switch (index)
        {
            case 0:
                builder.Append("🥇");
                break;
            case 1:
                builder.Append("🥈");
                break;
            case 2:
                builder.Append("🥉");
                break;
        }

        builder.AppendFormat(" *{0}* - {1} reactions", data.Member, data.Reactions);
        return builder.ToString();
    }
}
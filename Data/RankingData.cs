namespace BotRanking.Data;

using Discord;

public class RankingData
{
    public RankingData(IUser member, ulong messageID)
    {
        Member = member;
        MessageID = messageID;
        Reactions = 0;
    }

    public IUser Member { get; set; }
    public ulong MessageID { get; set; }
    public int Reactions { get; set; }
}
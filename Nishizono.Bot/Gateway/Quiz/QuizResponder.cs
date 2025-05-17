using Nishizono.Database;
using Nishizono.Bot.Remora;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Extensions.Embeds;
using Remora.Discord.Gateway.Responders;
using Remora.Rest.Core;
using Remora.Results;

namespace Nishizono.Bot.Gateway.Quiz;

internal class QuizResponder : IResponder<IMessageCreate>
{
    private readonly NishizonoDbContext _database;
    private readonly QuizManager _quizManager;
    private readonly IDiscordRestGuildAPI _guildApi;
    private readonly IDiscordRestChannelAPI _channelApi;

    public QuizResponder(NishizonoDbContext database, QuizManager quizManager, IDiscordRestGuildAPI guildApi, IDiscordRestChannelAPI channelApi)
    {
        _database = database;
        _guildApi = guildApi;
        _quizManager = quizManager;
        _channelApi = channelApi;
    }
    public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = default)
    {
        if(gatewayEvent.Author.IsBot.HasValue) return Result.Success;

        if (_quizManager.Sessions.ContainsKey(gatewayEvent.ChannelID))
        {
            _quizManager.Sessions[gatewayEvent.ChannelID].AddResponse(gatewayEvent.Author.ID, gatewayEvent.Content);
        }     

        return Result.FromSuccess();
    }
}

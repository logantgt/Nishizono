using Nishizono.Database;
using Polly.Simmy.Outcomes;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Remora.Discord.API.Abstractions.Gateway.Events.IGuildCreate;

namespace Nishizono.Bot.Gateway
{
    public class GuildJoinResponder : IResponder<IGuildCreate>
    {
        private readonly NishizonoDbContext _database;
        public GuildJoinResponder(NishizonoDbContext database)
        {
            _database = database;
        }
        public async Task<Result> RespondAsync(IGuildCreate gatewayEvent, CancellationToken ct = default)
        {
            //IsUnavailable will be EMPTY when first joining

            IAvailableGuild g;
            IUnavailableGuild r;
            // TODO: error handling
            gatewayEvent.Guild.TryPickT0(out g, out r);

            if (!g.IsUnavailable.HasValue)
            {
                await _database.AddGuildConfig(g.ID.Value);
                Console.WriteLine(g.ID);
            }

            return Result.FromSuccess();
        }
    }
}

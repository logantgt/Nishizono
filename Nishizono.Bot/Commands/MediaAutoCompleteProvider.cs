using FuzzySharp;
using Humanizer;
using Nishizono.Database.Models;
using Nishizono.Providers;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Autocomplete;
using Remora.Rest.Core;
using Rollcall.Extensions.Microsoft.DependencyInjection;

namespace Nishizono.Bot.Commands
{
    internal class MediaAutoCompleteProvider : IAutocompleteProvider
    {
        private readonly IRollcallProvider<IMetadataProvider> _provider;
        public MediaAutoCompleteProvider(IRollcallProvider<IMetadataProvider> provider)
        {
            _provider = provider;
        }
        public string Identity => "autocomplete::media";

        public async ValueTask<IReadOnlyList<IApplicationCommandOptionChoice>> GetSuggestionsAsync(IReadOnlyList<IApplicationCommandInteractionDataOption> options, string userInput, CancellationToken ct = default)
        {
            options.First(_ => _.Name == "mediatype").Value.TryGet(out var option);
            option.TryPickT0(out var mediaType, out var _);

            QueryType queryType;

            if (userInput.StartsWith("\\") && userInput.Any(char.IsAsciiDigit))
            {
                queryType = QueryType.Id;
                userInput = userInput.Substring(1);
            }
            else
            {
                queryType = QueryType.Title;
            }

            var results = new List<ProviderMetadata>();

            switch (mediaType)
            {
                case "VisualNovel":
                    results = await _provider.GetService("vndb").QueryAsync(MetadataType.VisualNovel, queryType, userInput);
                    break;
                case "Anime":
                    results = await _provider.GetService("anilist").QueryAsync(MetadataType.Anime, queryType, userInput);
                    break;
                case "Manga":
                    results = await _provider.GetService("anilist").QueryAsync(MetadataType.Manga, queryType, userInput);
                    break;
                case "YouTube":
                    results = await _provider.GetService("youtube").QueryAsync(MetadataType.Youtube, QueryType.Id, userInput);
                    break;
            }

            return results
                  .OrderByDescending(n => Fuzz.Ratio(userInput, $"{n.Title} | {n.NativeTitle}"))
                  .Select(n => new ApplicationCommandOptionChoice($"{n.Title} | {n.NativeTitle}", n.ProviderId))
                  .ToList();
        }
    }
}

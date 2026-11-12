using Discord;
using Discord.Interactions;
using GuildSaber.Common.Helpers;
using GuildSaber.DiscordBot.Core.Extensions;
using Microsoft.Extensions.Caching.Hybrid;
using static GuildSaber.Api.Features.MapRequests.Http.MapRequestResponses;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    public enum ERankingMapChoice { Difficulty, PlayMode }

    [ComponentInteraction("ranking_new:*")]
    public Task ShowNewRankingFormAsync(ContextId contextId)
        => RespondWithModalAsync<RankingNewMapModal>($"ranking_new_lookup:{contextId}");

    [ModalInteraction("ranking_new_lookup:*")]
    public async Task LookupNewRankingMapAsync(ContextId contextId, RankingNewMapModal modal)
    {
        await DeferRankingInteractionAsync();
        if (!ParseDraftSong(modal.Song).TryGetValue(out var song, out var inputError))
        {
            await FollowupAsync(inputError, ephemeral: true);
            return;
        }

        if (!(await Client.Value.MapRequests.LookupSongAsync(contextId, song))
            .TryGetValue(out var lookup, out var error))
        {
            await ReportRankingErrorAsync(error);
            return;
        }

        if (lookup.Difficulties.Length == 0 || lookup.PlayModes.Length == 0)
        {
            await FollowupAsync(
                "This song version has no playable difficulties with supported characteristics or play modes.",
                ephemeral: true);
            return;
        }

        var sessionId = Guid.NewGuid().ToString("N");
        var selection = new RankingMapSelection(Context.User.DiscordId, contextId,
            lookup, Math.Max(0, Array.FindIndex(lookup.Difficulties, x => x.Characteristic == "Standard")),
            Math.Max(0, Array.FindIndex(lookup.PlayModes, x => x == "Standard")));
        await StoreRankingMapSelectionAsync(sessionId, selection);
        await ShowRankingMapSelectionAsync(sessionId, selection.DifficultyIndex / 25 + 1,
            selection.PlayModeIndex / 25 + 1);
    }

    [ComponentInteraction("ranking_new_map:*,*,*")]
    public async Task ShowRankingMapSelectionAsync(string sessionId, int difficultyPage, int playModePage)
    {
        await DeferRankingInteractionAsync();
        if (await GetRankingMapSelectionAsync(sessionId) is not { } selection) return;

        var component = RankingMapSelectionView.BuildComponent(sessionId, selection,
            Math.Clamp(difficultyPage, 1, (selection.Lookup.Difficulties.Length + 24) / 25),
            Math.Clamp(playModePage, 1, (selection.Lookup.PlayModes.Length + 24) / 25)).Build();
        await ModifyOriginalResponseAsync(x =>
        {
            x.Flags = MessageFlags.ComponentsV2;
            x.Components = component;
            x.AllowedMentions = AllowedMentions.None;
        });
    }

    [ComponentInteraction("ranking_new_choose:*,*,*,*")]
    public async Task ChooseNewRankingMapAsync(
        string sessionId, ERankingMapChoice choice, int difficultyPage, int playModePage, string[] values)
    {
        await DeferRankingInteractionAsync();
        if (await GetRankingMapSelectionAsync(sessionId) is not { } selection) return;

        if (!int.TryParse(values[0], out var index) || index < 0 || index >= (choice == ERankingMapChoice.Difficulty
                ? selection.Lookup.Difficulties.Length
                : selection.Lookup.PlayModes.Length))
        {
            await FollowupAsync("This option is no longer available. Choose another map or refresh the selection.",
                ephemeral: true);
            return;
        }

        await StoreRankingMapSelectionAsync(sessionId, choice switch
        {
            ERankingMapChoice.Difficulty => selection with
            {
                DifficultyIndex = index, EditorId = index == selection.DifficultyIndex ? selection.EditorId : null
            },
            ERankingMapChoice.PlayMode => selection with
            {
                PlayModeIndex = index, EditorId = index == selection.PlayModeIndex ? selection.EditorId : null
            },
            _ => throw new ArgumentOutOfRangeException(nameof(choice))
        });
        await ShowRankingMapSelectionAsync(sessionId, difficultyPage, playModePage);
    }

    private async ValueTask<RankingMapSelection?> GetRankingMapSelectionAsync(string sessionId)
    {
        var selection = await Cache.GetOrCreateAsync<RankingMapSelection?>($"RankingMapSelection_{sessionId}",
            static _ => ValueTask.FromResult<RankingMapSelection?>(null));
        if (selection is not null && selection.OwnerId == Context.User.DiscordId) return selection;

        await FollowupAsync(
            "This map selection has expired or belongs to another player. Start a new request from /ranking.",
            ephemeral: true);
        return null;
    }

    private ValueTask StoreRankingMapSelectionAsync(string sessionId, RankingMapSelection selection)
        => Cache.SetAsync($"RankingMapSelection_{sessionId}", selection,
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(20), LocalCacheExpiration = TimeSpan.FromMinutes(20)
            });
}

public class RankingNewMapModal : IModal
{
    public string Title => "New ranking request";

    [InputLabel("BeatSaver map", "Paste a BeatSaver map link, a key or an exact song hash.")]
    [ModalTextInput("song", placeholder: "https://beatsaver.com/maps/1bd7, !bsr 1bd7, or an exact hash",
        maxLength: 512)]
    public string Song { get; set; } = "";
}

public sealed record RankingMapSelection(
    DiscordId OwnerId,
    ContextId ContextId,
    SongLookup Lookup,
    int DifficultyIndex,
    int PlayModeIndex,
    string? EditorId = null);

file static class RankingMapSelectionView
{
    public static ComponentBuilderV2 BuildComponent(
        string sessionId, RankingMapSelection selection, int difficultyPage, int playModePage)
    {
        var song = selection.Lookup.Song;
        var difficulty = selection.Lookup.Difficulties[selection.DifficultyIndex];
        var builder = new ComponentBuilderV2().WithContainer(container => container.WithAccentColor(Color.Orange)
                .WithSection(section => section
                    .WithTextDisplay(
                        $"### New ranking request\n**[{Format.Sanitize(song.Info.BeatSaverName)}](https://beatsaver.com/maps/{song.Key})** ({song.Key.ToBsrKey()})\n" +
                        $"Mapper(s): {Format.Sanitize(song.Info.MapperName)}\n" +
                        $"**{difficulty.Difficulty}, {difficulty.Characteristic}** · {selection.Lookup.PlayModes[selection.PlayModeIndex]}\n" +
                        $"NJS: {difficulty.Stats.NJS:0.##} | Length: " +
                        TimeSpan.FromSeconds(difficulty.Stats.Duration) switch
                        {
                            { Hours: > 0 } duration => duration.ToString(@"hh\:mm\:ss"),
                            var duration => duration.ToString(@"mm\:ss")
                        } + $" | BPM: {song.Stats.BPM:0.##}")
                    .WithAccessory(new ThumbnailBuilder().WithMedia($"https://cfcdn.beatsaver.com/{song.Hash}.jpg")))
                .WithTextDisplay("Choose the difficulty and play mode, then check existing threads. " +
                                 "The version shown here is kept when you save a private draft from the editor." +
                                 (selection.EditorId is null
                                     ? ""
                                     : " Your unsaved definition is kept. Changing difficulty or play mode starts a new definition.")))
            .WithActionRow(row => row.WithSelectMenu(new SelectMenuBuilder()
                .WithCustomId($"ranking_new_choose:{sessionId},Difficulty,{difficultyPage},{playModePage}")
                .WithPlaceholder("Choose a difficulty")
                .WithOptions(selection.Lookup.Difficulties.Index().Skip((difficultyPage - 1) * 25).Take(25)
                    .Select(x => new SelectMenuOptionBuilder(
                        $"{x.Item.Difficulty}, {x.Item.Characteristic}".Truncate(100), x.Index.ToString(),
                        $"NJS: {x.Item.Stats.NJS:0.##} · {x.Item.Stats.NotesPerSecond:0.##} notes/sec",
                        isDefault: selection.DifficultyIndex == x.Index)).ToList())));

        if (selection.Lookup.Difficulties.Length > 25)
            builder.WithActionRow(row => row
                .WithButton("Prev difficulties", $"ranking_new_map:{sessionId},{difficultyPage - 1},{playModePage}",
                    ButtonStyle.Secondary, disabled: difficultyPage == 1)
                .WithButton("Next difficulties", $"ranking_new_map:{sessionId},{difficultyPage + 1},{playModePage}",
                    ButtonStyle.Secondary, disabled: difficultyPage * 25 >= selection.Lookup.Difficulties.Length));

        if (selection.Lookup.PlayModes.Length > 1)
            builder.WithActionRow(row => row.WithSelectMenu(new SelectMenuBuilder()
                .WithCustomId($"ranking_new_choose:{sessionId},PlayMode,{difficultyPage},{playModePage}")
                .WithPlaceholder("Choose a play mode")
                .WithOptions(selection.Lookup.PlayModes.Index().Skip((playModePage - 1) * 25).Take(25)
                    .Select(x => new SelectMenuOptionBuilder(x.Item.Truncate(100), x.Index.ToString(),
                        isDefault: selection.PlayModeIndex == x.Index)).ToList())));

        if (selection.Lookup.PlayModes.Length > 25)
            builder.WithActionRow(row => row
                .WithButton("Prev play modes", $"ranking_new_map:{sessionId},{difficultyPage},{playModePage - 1}",
                    ButtonStyle.Secondary, disabled: playModePage == 1)
                .WithButton("Next play modes", $"ranking_new_map:{sessionId},{difficultyPage},{playModePage + 1}",
                    ButtonStyle.Secondary, disabled: playModePage * 25 >= selection.Lookup.PlayModes.Length));

        return builder.WithActionRow(row => row
            .WithButton("Check existing threads", $"ranking_new_check:{sessionId}")
            .WithButton("Change map", $"ranking_new:{selection.ContextId}", ButtonStyle.Secondary)
            .WithButton("Cancel", $"ranking:{selection.ContextId}", ButtonStyle.Secondary));
    }
}
using System.Diagnostics.CodeAnalysis;
using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.Guilds.Members.LevelStats;
using GuildSaber.Common.Result;
using GuildSaber.CSharpClient;
using GuildSaber.DiscordBot.AutocompleteHandlers;
using GuildSaber.DiscordBot.Core.Extensions;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = SixLabors.ImageSharp.Color;
using Image = SixLabors.ImageSharp.Image;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [SlashCommand("me", "Get information about your account")]
    public async Task Me(
        [Autocomplete<ContextAutocompleteHandler>] int contextId,
        [Summary("VisibleToOther")] EDisplayChoice displayChoice = EDisplayChoice.Invisible)
    {
        await DeferAsync(ephemeral: displayChoice.ToEphemeral());
        var stream = await MeCommand.GeneratePlayerCardAsync(
            (await Cache.FindGuildIdFromDiscordGuildIdAsync(Context.Guild.DiscordId, Client.Value))
            .ValueOrGuildMissingException(),
            contextId,
            Client.Value
        );
        if (stream is null)
        {
            await FollowupAsync(embed: new EmbedBuilder
            {
                Title = "Whoops!",
                Color = Discord.Color.DarkOrange,
                Description = "Your discord account doesn't seem to be linked to any GuildSaber account." +
                              "\nPlease visit GuildSaber and link your account to use this command."
            }.Build());

            return;
        }

        await FollowupWithFileAsync(stream, "PlayerCard.png", "[Profile Link](<https://beatleader.com/u/kuurama>)");
    }
}

/// <summary>
/// Static class used to hold functions for the current command.
/// </summary>
file static class MeCommand
{
    public readonly record struct TrophiesCount(
        int Plastic,
        int Silver,
        int Gold,
        int Diamond,
        int Ruby
    );

    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public static async Task<Stream?> GeneratePlayerCardAsync(GuildId guildId, int contextId,
                                                              GuildSaberClient client)
    {
        var atMe = await client.Players.GetExtendedAtMeAsync(CancellationToken.None).Unwrap();
        if (atMe is not { Player: var player })
            return null;

        // Avatar
        await using var avatarStream = await client.HttpClient.GetStreamAsync(player.PlayerInfo.AvatarUrl);
        using var avatarImage = Image.Load<Rgba32>(avatarStream);
        avatarImage.Mutate(a => a.Resize(216, 216));

        // Guild Logo
        await using var guildLogoStream = await client.HttpClient
            .GetStreamAsync($"https://cdn.guildsaber.com/Guild/{guildId}/Logo.jpg");
        using var guildLogoImage = Image.Load<Rgba32>(guildLogoStream);
        guildLogoImage.Mutate(a => a.Resize(80, 80));

        var levelStats = await client.LevelStats.GetAtMeAsync(contextId, CancellationToken.None).Unwrap();
        var currentLevel = levelStats.LastOrDefault(x => x is { IsCompleted: true, Level.CategoryId: null })
            as LevelStatResponses.MemberLevelStat?;

        var categories = await client.Categories.GetAllByGuildIdAsync(guildId, CancellationToken.None).Unwrap();
        var contextStats = (await client.ContextStats.GetAtMeAsync(contextId, CancellationToken.None)).Unwrap();
        if (!contextStats.HasValue)
            throw new InvalidOperationException("Failed to retrieve context stats for player.");

        const int width = 902;
        var height = 340 + (int)Math.Ceiling(categories.Length / 2.0) * 43;
        const string fontFamily = "JetBrainsMono NF";

        var fontAwesome = SystemFonts.Get("Font Awesome 6 Free Solid");
        var regularTextFont = SystemFonts.CreateFont(fontFamily, 26, FontStyle.Regular);
        var boldTextFont = SystemFonts.CreateFont(fontFamily, 26, FontStyle.Bold);
        var globalLevelFont = SystemFonts.CreateFont(fontFamily + " ExtraBold", 48, FontStyle.BoldItalic);

        var primaryColor = Color.FromRgb(26, 28, 30);
        var secondaryColor = currentLevel is not null
            ? Color.FromArgb(currentLevel.Value.Level.Info.Color)
            : Color.Black;

        using var image = new Image<Rgba32>(width, height);
        image.Mutate(ctx =>
        {
            // Background
            ctx.Fill(primaryColor, new RectangleF(0, 0, width, height)
                .ToRoundedRectangle(10));

            // Avatar
            ctx.DrawImage(avatarImage, new Point(20, 20), 1f);

            // Player name
            ctx.DrawText(player.PlayerInfo.Username switch
            {
                { Length: > 14 } name => name[..12] + "..",
                var name => name
            }, SystemFonts.CreateFont(fontFamily, 64, FontStyle.Regular), Color.White, new PointF(256, 17));

            // Guild logo
            ctx.DrawImage(guildLogoImage, new Point(width - 100, 20), 1f);

            // Point stats
            var simplePointsWithRank = contextStats.Value.SimplePointsWithRank
                .Where(x => x.CategoryId is null)
                .ToArray();
            switch (simplePointsWithRank.Length)
            {
                case 1:
                {
                    var pointWithRank = simplePointsWithRank[0];
                    ctx.DrawText(new RichTextOptions(regularTextFont)
                        {
                            Origin = new PointF(256, 126),
                            FallbackFontFamilies = [fontAwesome]
                        }, $"🏅 {pointWithRank.Points:00} {pointWithRank.Name} (#{pointWithRank.Rank})",
                        new SolidBrush(Color.Gold), null);
                    break;
                }
                case 2:
                    ctx.DrawText(new RichTextOptions(regularTextFont)
                        {
                            Origin = new PointF(256, 106),
                            FallbackFontFamilies = [fontAwesome]
                        },
                        $"🏅 {simplePointsWithRank[0].Points:00} {simplePointsWithRank[0].Name} (#{simplePointsWithRank[0].Rank})",
                        new SolidBrush(Color.Gold), null);

                    ctx.DrawText(new RichTextOptions(regularTextFont)
                        {
                            Origin = new PointF(256, 142),
                            FallbackFontFamilies = [fontAwesome]
                        },
                        $"🏅 {simplePointsWithRank[1].Points:00} {simplePointsWithRank[1].Name} (#{simplePointsWithRank[1].Rank})",
                        new SolidBrush(Color.Gold), null);
                    break;
                default:
                    throw new InvalidOperationException("Unexpected number of simple points with rank.");
            }

            // Passes stats
            ctx.DrawText(new RichTextOptions(regularTextFont)
                {
                    Origin = new PointF(256 + 325, 126),
                    FallbackFontFamilies = [fontAwesome]
                },
                $"⭐ {contextStats.Value.PassCountWithRank.PassCount} passes (#{contextStats.Value.PassCountWithRank.Rank})",
                new SolidBrush(Color.Gold), null);

            // Global level
            ctx.DrawText(new RichTextOptions(globalLevelFont)
            {
                Origin = new PointF(614, 180),
                FallbackFontFamilies = [fontAwesome]
            }, currentLevel?.Level.Info.Name ?? "", new SolidBrush(secondaryColor), null);

            // Equilibrium label
            ctx.DrawText("", boldTextFont, Color.FromRgb(217, 217, 217), new PointF(256, 190));

            // Equilibrium dots
            var dotStartX = 256 + 55;
            var equilibriumLevel = 3; // Example: 3 out of 5
            for (var i = 0; i < 5; i++)
            {
                var dotColor = i < equilibriumLevel ? secondaryColor : Color.FromRgb(217, 217, 217);
                ctx.Fill(dotColor, new EllipsePolygon(dotStartX + i * 37, 205, 10));
            }

            // Equilibrium percentage
            ctx.DrawText("(69%)", regularTextFont, Color.FromRgb(217, 217, 217), new PointF(256 + 226, 190));

            // Separator line
            ctx.DrawLine(secondaryColor, thickness: 6, new PointF(0, 256), new PointF(width, 256));

            // Trophies
            const int trophyStartY = 275;
            const int trophyStartX = 130;
            const int trophySpacing = 140;
            const int trophySize = 35;

            var trophiesCount = CalculateTrophies(levelStats);
            var trophies = new[]
            {
                ("Resources/Trophies/Plastic.webp", trophiesCount.Plastic),
                ("Resources/Trophies/Silver.webp", trophiesCount.Silver),
                ("Resources/Trophies/Gold.webp", trophiesCount.Gold),
                ("Resources/Trophies/Diamond.webp", trophiesCount.Diamond),
                ("Resources/Trophies/Ruby.webp", trophiesCount.Ruby)
            };

            for (var i = 0; i < trophies.Length; i++)
            {
                var (trophyFile, count) = trophies[i];
                var xPosition = trophyStartX + i * trophySpacing;

                using var trophyStream = File.OpenRead(trophyFile);
                using var trophyImage = Image.Load<Rgba32>(trophyStream);
                trophyImage.Mutate(a => a.Resize(trophySize, trophySize));
                ctx.DrawImage(trophyImage, new Point(xPosition, trophyStartY), 1f);

                var countText = count.ToString();
                var countSize = TextMeasurer.MeasureSize(countText, new TextOptions(regularTextFont));
                ctx.DrawText(countText, regularTextFont, Color.White,
                    new PointF(xPosition + trophySize + 4, trophyStartY + (trophySize - countSize.Height) / 2));
            }

            // Category levels
            const int categoryStartY = 340;
            const int categoryLeftX = 160;
            const int categoryRightX = 480;
            const int categoryRowSpacing = 43;
            var categoryIndex = 0;

            foreach (var category in categories)
            {
                LevelStatResponses.MemberLevelStat? categoryLevelStat = levelStats
                    .LastOrDefault(x => x.Level.CategoryId == category.Id && x.IsCompleted);
                if (categoryLevelStat is null)
                    continue;

                var categoryColor = Color.FromArgb(categoryLevelStat.Value.Level.Info.Color);
                var isLeftColumn = categoryIndex % 2 == 0;
                var xPosition = isLeftColumn ? categoryLeftX : categoryRightX;
                var yPosition = categoryStartY + categoryIndex / 2 * categoryRowSpacing;

                ctx.DrawText($"{category.Info.Name}: ", boldTextFont, Color.White, new PointF(xPosition, yPosition));

                var categoryNameSize = TextMeasurer
                    .MeasureSize($"{category.Info.Name}: ", new TextOptions(boldTextFont));

                ctx.DrawText(categoryLevelStat.Value.Level.Info.Name, boldTextFont, categoryColor,
                    new PointF(xPosition + categoryNameSize.Width + 10, yPosition));

                categoryIndex++;
            }
        });

        var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        stream.Position = 0;
        return stream;
    }

    public static TrophiesCount CalculateTrophies(LevelStatResponses.MemberLevelStat[] levelStats)
    {
        var plastic = 0;
        var silver = 0;
        var gold = 0;
        var diamond = 0;
        var ruby = 0;

        foreach (var stat in levelStats)
        {
            if (!stat.IsCompleted)
                continue;

            var completionPercent = stat.Level switch
            {
                LevelStatResponses.Level.RankedMapListLevel listLevel =>
                    stat.PassCount.HasValue && listLevel.RankedMapCount > 0
                        ? stat.PassCount.Value / (float)listLevel.RankedMapCount
                        : 0f,
                _ => 0f
            };

            switch (completionPercent)
            {
                case <= 0.25f:
                    plastic++;
                    break;
                case <= 0.5f:
                    silver++;
                    break;
                case <= 0.75f:
                    gold++;
                    break;
                case < 1.0f:
                    diamond++;
                    break;
                default:
                    ruby++;
                    break;
            }
        }

        return new TrophiesCount(plastic, silver, gold, diamond, ruby);
    }
}
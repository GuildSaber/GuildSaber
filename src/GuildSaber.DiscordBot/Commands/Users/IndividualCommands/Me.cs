using System.Diagnostics;
using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.Guilds.Categories;
using GuildSaber.Api.Features.Guilds.Members.ContextStats;
using GuildSaber.Api.Features.Guilds.Members.LevelStats;
using GuildSaber.Api.Features.Players;
using GuildSaber.Common.Result;
using GuildSaber.Common.Settings;
using GuildSaber.Common.StrongTypes;
using GuildSaber.CSharpClient;
using GuildSaber.DiscordBot.AutocompleteHandlers;
using GuildSaber.DiscordBot.Core.Extensions;
using GuildSaber.DiscordBot.Core.Handlers;
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
        [Summary("User", "The user to get the player card for (you if empty)")] IUser? user = null,
        [Summary("Visibility")] EDisplayChoice displayChoice = EDisplayChoice.Visible)
    {
        await DeferAsync(ephemeral: displayChoice.ToEphemeral());

        var guildIdTask = GetGuildIdAsync().AsTask();
        var playerTask = user is null
            ? GetPlayerAtMeAsync().AsTask()
            : GetPlayerAsync(user.DiscordId).AsTask();
        await Task.WhenAll(guildIdTask, playerTask);

        var (guildId, player, playerId) = (guildIdTask.Result, playerTask.Result, playerTask.Result.Id);
        var client = Client.Value;

        var (levelStatsTask, categoriesTask, contextStatsTask, resourcesTask) = (
            client.LevelStats.GetByPlayerIdAsync(playerId, contextId),
            client.Categories.GetAllByGuildIdAsync(guildId),
            client.ContextStats.GetByPlayerIdAsync(playerId, contextId),
            CardResources.LoadAsync(client, guildId, player, LinkSettings.Value));
        await Task.WhenAll(levelStatsTask, categoriesTask, contextStatsTask, resourcesTask);

        var cardResources = resourcesTask.Result;
        var cardData = CardData.Build(
            player,
            categoriesTask.Result.Unwrap(),
            levelStatsTask.Result.Unwrap(),
            contextStatsTask.Result.Unwrap() ?? (user is null
                ? throw new InteractionHandler.CurrentPlayerDidNotJoinGuildContextException()
                : throw new InteractionHandler.PlayerIsNotInGuildContextException())
        );

        using var stream = new MemoryStream();
        await stream.WritePlayerCardToStreamAsync(cardData, cardResources);
        await FollowupWithFileAsync(stream,
            fileName: "PlayerCard.png",
            text: $"[Profile Link](<https://beatleader.com/u/{player.PlayerLinkedAccounts.BeatLeaderId}>)");
    }
}

/// <summary>
/// Static class used to hold functions for the current command.
/// </summary>
file static class MeCommand
{
    public static FontCollection FontCollection => field ??= LoadFonts();

    private static FontCollection LoadFonts()
    {
        var fontCollection = new FontCollection();
        fontCollection.Add("Resources/Fonts/JetBrainsMonoNF/JetBrainsMonoNLNerdFont-Regular.ttf");
        fontCollection.Add("Resources/Fonts/JetBrainsMonoNF/JetBrainsMonoNLNerdFont-Bold.ttf");

        return fontCollection;
    }

    public static async Task WritePlayerCardToStreamAsync(
        this MemoryStream stream, CardData data, CardResources resources)
    {
        var dimensions = new CardDimensions(902, 340 + (int)Math.Ceiling(data.Categories.Length / 2.0) * 43);
        using var image = new Image<Rgba32>(dimensions.Width, dimensions.Height);

        image.Mutate(ctx => ctx.RenderPlayerCard(resources, data, dimensions, data.Player.PlayerInfo.Username));

        await image.SaveAsPngAsync(stream);
        stream.Position = 0;
    }

    internal static double StandardDeviation(IReadOnlyCollection<int> sequence)
    {
        if (sequence.Count == 0)
            return 0;

        var average = sequence.Average();
        var sum = sequence.Sum(x => Math.Pow(x - average, 2));

        return Math.Sqrt(sum / (sequence.Count - 1));
    }
}

file static class CardRenderingExtensions
{
    private static readonly Color _whiteColor = Color.White;
    private static readonly Color _goldColor = Color.Gold;
    private static readonly Color _grayColor = Color.FromRgb(217, 217, 217);

    extension(IImageProcessingContext ctx)
    {
        public void RenderPlayerCard(
            CardResources resources, CardData data, CardDimensions dimensions, string playerName) => ctx
            .DrawBackground(data.PrimaryColor, dimensions)
            .DrawAvatar(resources.Avatar)
            .DrawPlayerName(playerName, resources.Fonts.Large)
            .DrawGuildLogo(resources.GuildLogo, dimensions.Width)
            .DrawPointStats(data.PointStats, resources.Fonts)
            .DrawPassStats(data.PassCount, data.PassRank, resources.Fonts)
            .DrawEquilibrium(data.EquilibriumPercentage, data.SecondaryColor, resources.Fonts)
            .DrawGlobalLevel(data.GlobalLevelName, data.SecondaryColor, resources.Fonts.GlobalLevel, dimensions.Width)
            .DrawSeparator(data.SecondaryColor, dimensions.Width)
            .DrawTrophies(data.Trophies, resources.Fonts.Regular)
            .DrawCategoryLevels(data.Categories, resources.Fonts.Bold);

        private IImageProcessingContext DrawBackground(Color color, CardDimensions dimensions)
            => ctx.Fill(color, new RectangleF(0, 0, dimensions.Width, dimensions.Height).ToRoundedRectangle(10));

        private IImageProcessingContext DrawAvatar(Image<Rgba32> avatar)
            => ctx.DrawImage(avatar, new Point(20, 20), 1f);

        private IImageProcessingContext DrawPlayerName(string name, Font font)
            => ctx.DrawText(name.Length > 14 ? name[..12] + ".." : name, font, _whiteColor, new PointF(256, 17));

        private IImageProcessingContext DrawGuildLogo(Image<Rgba32> logo, int width)
            => ctx.DrawImage(logo, new Point(width - 100, 20), 1f);

        private IImageProcessingContext DrawPointStats(PointStatData[] stats, CardFonts fonts)
        {
            var options = new RichTextOptions(fonts.Regular);

            switch (stats.Length)
            {
                case 1:
                    options.Origin = new PointF(256, 126);
                    ctx.DrawPointStat(stats[0], options);
                    break;
                case 2:
                    options.Origin = new PointF(256, 106);
                    ctx.DrawPointStat(stats[0], options);
                    options.Origin = new PointF(256, 142);
                    ctx.DrawPointStat(stats[1], options);
                    break;
                default:
                    throw new InvalidOperationException("Unexpected number of simple points with rank.");
            }

            return ctx;
        }

        private void DrawPointStat(PointStatData stat, RichTextOptions options)
            => ctx.DrawText(options, $" {stat.Points:0.##} {stat.Name} (#{stat.Rank})",
                new SolidBrush(_goldColor), null);

        private IImageProcessingContext DrawPassStats(int passCount, int passRank, CardFonts fonts)
            => ctx.DrawText(new RichTextOptions(fonts.Regular)
                {
                    Origin = new PointF(256 + 325, 126)
                },
                $" {passCount} passes (#{passRank})",
                new SolidBrush(_goldColor), null);

        private IImageProcessingContext DrawGlobalLevel(string levelName, Color color, Font font, int width)
        {
            const int startX = 601;
            var textSize = TextMeasurer.MeasureSize(levelName, new TextOptions(font));
            var x = startX + (width - startX - 20 - textSize.Width) / 2;

            ctx.DrawText(new RichTextOptions(font) { Origin = new PointF(x, 185) },
                levelName, new SolidBrush(color), null);

            return ctx;
        }

        private IImageProcessingContext DrawSeparator(Color color, int width)
            => ctx.DrawLine(color, 6, new PointF(0, 256), new PointF(width, 256));

        private IImageProcessingContext DrawTrophies(TrophiesData trophies, Font font)
        {
            const int startY = 275;
            const int startX = 130;
            const int spacing = 140;
            const int size = 35;

            var trophyList = new (string Path, int Count)[]
            {
                ("Resources/Trophies/Plastic.webp", trophies.Plastic),
                ("Resources/Trophies/Silver.webp", trophies.Silver),
                ("Resources/Trophies/Gold.webp", trophies.Gold),
                ("Resources/Trophies/Diamond.webp", trophies.Diamond),
                ("Resources/Trophies/Ruby.webp", trophies.Ruby)
            };

            for (var i = 0; i < trophyList.Length; i++)
                ctx.DrawTrophy(trophyList[i], startX + i * spacing, startY, size, font);

            return ctx;
        }

        private void DrawTrophy((string Path, int Count) trophy, int x, int y, int size, Font font)
        {
            using var stream = File.OpenRead(trophy.Path);
            using var image = Image.Load<Rgba32>(stream);
            image.Mutate(a => a.Resize(size, size));
            ctx.DrawImage(image, new Point(x, y), 1f);

            var countText = trophy.Count.ToString();
            var countSize = TextMeasurer.MeasureSize(countText, new TextOptions(font));
            ctx.DrawText(countText, font, _whiteColor, new PointF(x + size + 4, y + (size - countSize.Height) / 2));
        }

        private IImageProcessingContext DrawCategoryLevels(CategoryLevelData[] categories, Font font)
        {
            if (categories.Length == 0)
                return ctx;

            const int startY = 340;
            const int rowSpacing = 43;

            var maxLabelWidth = categories
                .Select(c => TextMeasurer.MeasureSize($"{c.CategoryName}: ", new TextOptions(font)).Width)
                .Max();

            for (var i = 0; i < categories.Length; i++)
            {
                var category = categories[i];
                var isLeftColumn = i % 2 == 0;
                var baseX = isLeftColumn ? 160 : 480;
                var y = startY + i / 2 * rowSpacing;

                ctx.DrawCategoryLevel(category, baseX, y, maxLabelWidth, font);
            }

            return ctx;
        }

        private void DrawCategoryLevel(
            CategoryLevelData category, float baseX, float y, float maxLabelWidth, Font font)
        {
            var label = $"{category.CategoryName}: ";
            var labelSize = TextMeasurer.MeasureSize(label, new TextOptions(font));

            ctx.DrawText(label, font, _whiteColor, new PointF(baseX + maxLabelWidth - labelSize.Width, y));
            ctx.DrawText(category.LevelName, font, category.Color, new PointF(baseX + maxLabelWidth + 10, y));
        }

        private IImageProcessingContext DrawEquilibrium(float percentage, Color secondaryColor, CardFonts fonts)
        {
            ctx.DrawText("", fonts.Bold, _grayColor, new PointF(256, 190));

            const int dotStartX = 256 + 55;
            var filledDots = (int)Math.Round(percentage / 20.0);

            for (var i = 0; i < 5; i++)
            {
                var dotColor = i < filledDots ? secondaryColor : _grayColor;
                ctx.Fill(dotColor, new EllipsePolygon(dotStartX + i * 37, 205, 10));
            }

            ctx.DrawText($"({percentage:0.##}%)", fonts.Regular, _grayColor, new PointF(256 + 226, 190));

            return ctx;
        }
    }
}

file record struct CardDimensions(int Width, int Height);
file record struct CardFonts(Font Regular, Font Bold, Font Large, Font GlobalLevel);
file record struct PointStatData(float Points, string Name, int Rank);
file record struct CategoryLevelData(string CategoryName, string LevelName, Color Color);

file record struct CardResources(Image<Rgba32> Avatar, Image<Rgba32> GuildLogo, CardFonts Fonts)
{
    public static async Task<CardResources> LoadAsync(
        GuildSaberClient client, GuildId guildId, PlayerResponses.Player player, LinkSettings linkSettings)
    {
        var fontFamily = MeCommand.FontCollection.Get("JetBrainsMonoNL NF");
        var fonts = new CardFonts(
            fontFamily.CreateFont(26, FontStyle.Regular),
            fontFamily.CreateFont(26, FontStyle.Bold),
            fontFamily.CreateFont(64, FontStyle.Regular),
            fontFamily.CreateFont(48, FontStyle.BoldItalic)
        );

        var avatarTask = LoadAvatarAsync(client, player.PlayerInfo.AvatarUrl);
        var guildLogoTask = LoadGuildLogoAsync(client, guildId, fontFamily, linkSettings);

        await Task.WhenAll(avatarTask, guildLogoTask);

        return new CardResources(avatarTask.Result, guildLogoTask.Result, fonts);
    }

    private static async Task<Image<Rgba32>> LoadAvatarAsync(GuildSaberClient client, string avatarUrl)
    {
        await using var avatarStream = await client.HttpClient.GetStreamAsync(avatarUrl);
        var avatarImage = Image.Load<Rgba32>(avatarStream);
        avatarImage.Mutate(a => a.Resize(216, 216));
        return avatarImage;
    }

    private static async Task<Image<Rgba32>> LoadGuildLogoAsync(
        GuildSaberClient client, GuildId guildId, FontFamily fonts, LinkSettings linkSettings)
    {
        try
        {
            await using var guildLogoStream = await client.HttpClient
                .GetStreamAsync($"{linkSettings.CdnBaseUri}guilds/{guildId}/logo.jpg");
            var guildLogoImage = Image.Load<Rgba32>(guildLogoStream);
            guildLogoImage.Mutate(a => a.Resize(80, 80));
            return guildLogoImage;
        }
        catch // Create a fallback image with "?"
        {
            var guildLogoImage = new Image<Rgba32>(80, 80);
            var font = fonts.CreateFont(48, FontStyle.Bold);
            guildLogoImage.Mutate(ctx => ctx
                .Fill(Color.FromRgb(50, 50, 50))
                .DrawText("?", font, Color.White, new PointF(25, 10)));
            return guildLogoImage;
        }
    }
}

file record struct TrophiesData(int Plastic, int Silver, int Gold, int Diamond, int Ruby)
{
    public static TrophiesData Calculate(LevelStatResponses.MemberLevelStat[] levelStats)
    {
        Span<int> counts = stackalloc int[5];
        foreach (var stat in levelStats.Where(s => s.IsCompleted))
        {
            var completionPercent = stat.Level switch
            {
                LevelStatResponses.Level.RankedMapListLevel { RankedMapCount: > 0 } listLevel => stat.PassCount.HasValue
                    ? stat.PassCount.Value / (float)listLevel.RankedMapCount
                    : 0f,
                _ => 0f
            };

            Trace.Assert((int)Trophy.Ruby == 4, "Trophy enum values code assumption changed.");
            var trophy = Trophy.GetFromPercentage(completionPercent);
            if (trophy is null) continue;

            // Use the int value of the trophy enum to index into the counts array
            counts[(int)trophy]++;
        }

        return new TrophiesData(counts[0], counts[1], counts[2], counts[3], counts[4]);
    }
}

file record struct CardData(
    PlayerResponses.Player Player,
    Color PrimaryColor,
    Color SecondaryColor,
    PointStatData[] PointStats,
    int PassCount,
    int PassRank,
    string GlobalLevelName,
    TrophiesData Trophies,
    CategoryLevelData[] Categories,
    float EquilibriumPercentage)
{
    public static CardData Build(
        PlayerResponses.Player player,
        CategoryResponses.Category[] categories,
        LevelStatResponses.MemberLevelStat[] levelStats,
        ContextStatResponses.MemberContextStat contextStats)
    {
        var currentLevel = levelStats
            .Where(x => x.Level.CategoryId is null)
            .TakeWhile(x => !x.IsLocked || x.IsCompleted)
            .LastOrDefault(x => x.IsCompleted);

        var primaryColor = Color.FromRgb(26, 28, 30);
        var secondaryColor = currentLevel is not null
            ? Color.FromArgb(currentLevel.Level.Info.Color)
            : Color.Black;

        var pointStats = contextStats.SimplePointsWithRank
            .Where(x => x.CategoryId is null)
            .Select(p => new PointStatData(p.Points, p.Name, p.Rank))
            .ToArray();

        var trophies = TrophiesData.Calculate(levelStats);
        var categoryLevels = new List<CategoryLevelData>();
        var categoryLevelOrders = new List<int>();

        foreach (var category in categories)
        {
            var categoryLevelStat = levelStats
                .Where(x => x.Level.CategoryId == category.Id)
                .TakeWhile(x => !x.IsLocked || x.IsCompleted)
                .LastOrDefault(x => x.IsCompleted);
            if (categoryLevelStat is null)
                continue;

            categoryLevels.Add(new CategoryLevelData(
                category.Info.Name,
                categoryLevelStat.Level.Info.Name,
                Color.FromArgb(categoryLevelStat.Level.Info.Color)
            ));
            categoryLevelOrders.Add(categoryLevelStat.Level.Order);
        }

        var equilibriumPercentage = categoryLevelOrders.Count > 1
            ? Math.Max(0f, 100f - MeCommand
                .StandardDeviation(categoryLevelOrders) * 100f / categoryLevelOrders.Average())
            : 100f;

        return new CardData(
            player,
            primaryColor,
            secondaryColor,
            pointStats,
            contextStats.PassCountWithRank.PassCount,
            contextStats.PassCountWithRank.Rank,
            currentLevel?.Level.Info.Name ?? "",
            trophies,
            [.. categoryLevels],
            (float)equilibriumPercentage
        );
    }
}
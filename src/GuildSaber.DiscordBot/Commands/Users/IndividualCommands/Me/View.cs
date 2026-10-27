using GuildSaber.Api.Features.Guilds.Http;
using GuildSaber.Api.Features.Guilds.Members.AchievementStats.Http;
using GuildSaber.Api.Features.Guilds.Members.ContextStats.Http;
using GuildSaber.Api.Features.Players.Http;
using GuildSaber.Common.Helpers;
using GuildSaber.CSharpClient.Routes.Guilds.Members.AchievementStats;
using GuildSaber.DiscordBot.Commands.Users.Me.Components;
using GuildSaber.DiscordBot.Core.Extensions;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GuildSaber.DiscordBot.Commands.Users.Me;

public class PlayerCardView(
    PlayerResponses.Player player,
    GuildResponses.GuildExtended guildExtended,
    AchievementStatResponses.MemberAchievementStat[] achievementStats,
    ContextStatResponses.MemberContextStat contextStat,
    byte[] avatarBytes,
    byte[] guildLogoBytes) : IDocument
{
    static PlayerCardView()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        // Embedded the fonts directly in the Resources directory as their license allowed it.
        FontManager.RegisterFontFromStream(File
            .OpenRead("Resources/Fonts/JetBrainsMonoNF/JetBrainsMonoNLNerdFont-Regular.ttf"));
        FontManager.RegisterFontFromStream(File
            .OpenRead("Resources/Fonts/JetBrainsMonoNF/JetBrainsMonoNLNerdFont-Bold.ttf"));
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer document)
    {
        var achievement = achievementStats.GetGlobalAchievement();
        var accent = Color.FromArgb(achievement?.Info.Color ?? 0xFFFFFF);
        var extraHeight = Math.Max(0, (guildExtended.Categories.Length + 1) / 2 - 3) * 45;

        document.Page(page =>
        {
            page.Size(880, guildExtended.Categories.Length == 0 ? 380 : 470 + extraHeight);
            page.PageColor(Color.FromRGB(26, 28, 30));
            page.DefaultTextStyle(x => x.FontFamily("JetBrainsMonoNL NF").FontColor(Colors.White));

            page.Content().Padding(10).Column(column =>
            {
                column.Item().Height(220).Element(container
                    => Header(container, achievement?.Info.Name ?? "No Level", accent));
                column.Item().LineHorizontal(3).LineColor(accent);
                column.Item().Height(70).PaddingTop(20).AlignMiddle()
                    .Component(new TrophyRow(achievementStats.CalculateTrophiesData()));

                if (guildExtended.Categories.Length > 0)
                    column.Item().Height(150 + extraHeight)
                        .Component(new CategoryGrid(guildExtended.Categories, achievementStats));
            });
        });
    }

    private void Header(IContainer container, string achievementName, Color accent) => container.Row(row =>
    {
        var avatar = row.ConstantItem(210).AlignMiddle().Width(200).Height(200).CornerRadius(16);
        if (avatarBytes.Length > 0)
            avatar.Image(avatarBytes).FitArea();
        else
            avatar.Background(Colors.Grey.Darken3);

        row.RelativeItem().PaddingLeft(20).Column(info =>
        {
            info.Item().Height(90).Row(name =>
            {
                name.RelativeItem().AlignMiddle().ScaleToFit()
                    .Text(player.PlayerInfo.Username.Truncate(20)).FontSize(52).Bold().FontColor(Colors.White);

                var logo = name.ConstantItem(90).PaddingLeft(10).Width(80).Height(80).CornerRadius(8);
                if (guildLogoBytes.Length > 0)
                    logo.Image(guildLogoBytes).FitArea();
                else
                    logo.Background(Colors.Grey.Darken3).AlignCenter().AlignMiddle()
                        .Text("?").FontSize(40).Bold();
            });

            info.Item().Height(110).Row(stats =>
            {
                stats.RelativeItem(5).AlignMiddle().Column(points =>
                {
                    points.Item().Height(42).ScaleToFit().Row(values =>
                    {
                        values.Spacing(10);
                        foreach (var point in contextStat.SimplePointsWithRank.Where(x => x.CategoryId is null))
                            values.AutoItem().Text($" {point.Points:0.##} {point.Name} (#{point.Rank})")
                                .FontSize(30).FontColor(Color.FromRGB(255, 215, 0));
                    });

                    var passes = contextStat.PassCountsWithRank.FirstOrDefault(x => x.CategoryId is null);
                    points.Item().Height(42).ScaleToFit()
                        .Text($" {passes.PassCount} passes (#{passes.Rank})")
                        .FontSize(30).FontColor(Color.FromRGB(255, 215, 0));
                });

                stats.RelativeItem(4).AlignMiddle().ScaleToFit()
                    .Text(achievementName).AlignCenter().FontSize(58).Bold().Italic().FontColor(accent);
            });
        });
    });
}
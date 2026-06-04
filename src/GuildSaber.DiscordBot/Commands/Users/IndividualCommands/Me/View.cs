using GuildSaber.Api.Features.Guilds.Http;
using GuildSaber.Api.Features.Guilds.Members.ContextStats.Http;
using GuildSaber.Api.Features.Guilds.Members.LevelStats.Http;
using GuildSaber.Api.Features.Players.Http;
using GuildSaber.Common.Helpers;
using GuildSaber.CSharpClient.Routes.Guilds.Members.LevelStats;
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
    LevelStatResponses.MemberLevelStat[] levelStats,
    ContextStatResponses.MemberContextStat contextStat,
    byte[] avatarBytes,
    byte[] guildLogoBytes) : IDocument
{
    static PlayerCardView()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        FontManager.RegisterFont(File
            .OpenRead("Resources/Fonts/JetBrainsMonoNF/JetBrainsMonoNLNerdFont-Regular.ttf"));
        FontManager.RegisterFont(File
            .OpenRead("Resources/Fonts/JetBrainsMonoNF/JetBrainsMonoNLNerdFont-Bold.ttf"));
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer document)
    {
        var currentLevel = levelStats.GetGlobalLevel();
        var primaryColor = Color.FromRGB(26, 28, 30);
        var secondaryColor = currentLevel is not null
            ? Color.FromArgb(currentLevel.Info.Color)
            : Color.FromRGB(0, 0, 0);

        document.Page(page =>
        {
            page.Size(902, 340 + (int)Math.Ceiling(guildExtended.Categories.Length / 2.0) * 43);
            page.PageColor(primaryColor);
            page.DefaultTextStyle(x => x.FontFamily("JetBrainsMonoNL NF"));

            page.Content().Column(col =>
            {
                // Top row for Avatar + Info + Logo
                col.Item()
                    .PaddingTop(20)
                    .PaddingHorizontal(20)
                    .Row(row =>
                    {
                        // Avatar
                        var avatarContainer = row.ConstantItem(216).Height(216);
                        if (avatarBytes.Length > 0)
                            avatarContainer.Image(avatarBytes).FitArea();
                        else
                            avatarContainer.Background(Colors.Grey.Darken3);

                        // Middle Info
                        row.RelativeItem()
                            .PaddingLeft(20)
                            .Column(infoCol =>
                            {
                                infoCol.Item()
                                    .PaddingTop(-5)
                                    .Row(nameRow =>
                                    {
                                        nameRow.RelativeItem()
                                            .Text(player.PlayerInfo.Username.Truncate(16))
                                            .FontColor(Colors.White)
                                            .FontSize(56)
                                            .LineHeight(1f);

                                        var logoContainer = nameRow.ConstantItem(80)
                                            .AlignRight()
                                            .AlignTop()
                                            .Width(80)
                                            .Height(80);

                                        if (guildLogoBytes.Length > 0)
                                            logoContainer.Image(guildLogoBytes).FitArea();
                                        else
                                            logoContainer
                                                .Background(Colors.Grey.Darken3)
                                                .AlignCenter()
                                                .AlignMiddle()
                                                .Text("?")
                                                .FontColor(Colors.White)
                                                .FontSize(40)
                                                .Bold();
                                    });

                                // Points and Passes
                                infoCol.Item()
                                    .PaddingTop(25)
                                    .Row(statRow =>
                                    {
                                        var gold = Color.FromARGB(255, 255, 215, 0);
                                        if (contextStat.SimplePointsWithRank.Length > 0)
                                        {
                                            var pointStat = contextStat.SimplePointsWithRank[0];
                                            statRow.AutoItem()
                                                .Text($" {pointStat.Points:0.##} {pointStat.Name} (#{pointStat.Rank})")
                                                .FontColor(gold)
                                                .FontSize(24);
                                        }

                                        if (contextStat.PassCountsWithRank.Length <= 0)
                                            return;

                                        var passStat = contextStat.PassCountsWithRank
                                            .FirstOrDefault(x => x.CategoryId == null);

                                        // Pass count wrapping to relative if it overlaps, else auto
                                        statRow.RelativeItem()
                                            .PaddingLeft(40)
                                            .AlignLeft()
                                            .Text($" {passStat.PassCount} passes (#{passStat.Rank})")
                                            .FontColor(gold)
                                            .FontSize(24);
                                    });

                                // Equilibrium
                                infoCol.Item()
                                    .Row(row =>
                                    {
                                        row.AutoItem()
                                            .PaddingTop(25)
                                            .Row(eqRow =>
                                            {
                                                var grayColor = Color.FromHex("#d9d9d9");
                                                eqRow.AutoItem()
                                                    .Text("")
                                                    .FontColor(grayColor)
                                                    .FontSize(24)
                                                    .Bold();

                                                var equilibriumPercentage = levelStats.CalculateSkillEquilibrium(
                                                    guildExtended.Categories.Select(x => x.Id)) ?? 0f;

                                                eqRow.AutoItem()
                                                    .PaddingLeft(25)
                                                    .PaddingTop(-10)
                                                    .Row(dotsRow =>
                                                    {
                                                        var filledDots = (int)Math.Round(equilibriumPercentage / 20.0);
                                                        for (var i = 0; i < 5; i++)
                                                        {
                                                            var dotColor = i < filledDots ? secondaryColor : grayColor;
                                                            dotsRow.AutoItem()
                                                                .PaddingHorizontal(6)
                                                                .Text("●")
                                                                .FontColor(dotColor)
                                                                .FontSize(40);
                                                        }
                                                    });

                                                eqRow.AutoItem()
                                                    .PaddingLeft(15)
                                                    .Text($"({equilibriumPercentage:0.##}%)")
                                                    .FontColor(grayColor)
                                                    .FontSize(24);
                                            });

                                        row.RelativeItem()
                                            .AlignMiddle()
                                            .AlignCenter()
                                            .PaddingTop(5)
                                            .Text(currentLevel?.Info.Name ?? "No Level")
                                            .FontColor(secondaryColor)
                                            .FontSize(48)
                                            .Bold()
                                            .Italic();
                                    });
                            });
                    });

                col.Item()
                    .PaddingTop(10)
                    .LineHorizontal(6)
                    .LineColor(secondaryColor);

                col.Item()
                    .PaddingHorizontal(20)
                    .Column(bottomCol =>
                    {
                        // Trophies
                        bottomCol.Item()
                            .PaddingVertical(20)
                            .Component(new TrophyRow(levelStats.CalculateTrophiesData()));

                        // Categories
                        bottomCol.Item()
                            .Component(new CategoryGrid(guildExtended.Categories, levelStats));
                    });
            });
        });
    }
}
using GuildSaber.Common.Extra;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GuildSaber.DiscordBot.Commands.Users.Me.Components;

public sealed class TrophyRow(TrophiesData trophies) : IComponent
{
    private readonly (string Name, int Count)[] _trophies =
    [
        ("Plastic", trophies.Plastic),
        ("Silver", trophies.Silver),
        ("Gold", trophies.Gold),
        ("Diamond", trophies.Diamond),
        ("Ruby", trophies.Ruby)
    ];

    public void Compose(IContainer container) => container.Row(row =>
    {
        foreach (var trophy in _trophies)
            row.RelativeItem().AlignCenter().ScaleToFit().Row(item =>
            {
                item.ConstantItem(36).Height(36)
                    .Image($"Resources/Trophies/{trophy.Name}.webp").FitArea();
                item.AutoItem().PaddingLeft(6).AlignMiddle()
                    .Text(trophy.Count.ToString()).FontColor(Colors.White).FontSize(28);
            });
    });
}
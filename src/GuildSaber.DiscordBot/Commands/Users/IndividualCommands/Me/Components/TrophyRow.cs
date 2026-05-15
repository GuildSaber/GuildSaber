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

    public void Compose(IContainer container) =>
        container.Row(tRow => tRow
            .RelativeItem()
            .AlignCenter()
            .Row(tc =>
            {
                foreach (var t in _trophies)
                    tc.AutoItem()
                        .PaddingHorizontal(45)
                        .Row(r =>
                        {
                            r.AutoItem()
                                .Width(35)
                                .Height(35)
                                .Image($"Resources/Trophies/{t.Name}.webp")
                                .FitArea();

                            r.AutoItem()
                                .PaddingLeft(5)
                                .AlignMiddle()
                                .Text(t.Count.ToString())
                                .FontColor(Colors.White)
                                .FontSize(24);
                        });
            }));
}
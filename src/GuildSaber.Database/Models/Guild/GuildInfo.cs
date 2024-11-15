using System.Drawing;

namespace GuildSaber.Database.Models.Guild;

public readonly record struct GuildInfo(
    string Description,
    Color Color,
    DateTimeOffset CreationDate
);
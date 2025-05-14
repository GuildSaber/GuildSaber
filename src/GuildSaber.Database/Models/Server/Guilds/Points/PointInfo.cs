namespace GuildSaber.Database.Models.Server.Guilds.Points;

public readonly record struct PointInfo
{
    private PointInfo(string name, string description)
        => (Name, Description) = (name, description);

    public string Name { get; init; }
    public string Description { get; init; }
}
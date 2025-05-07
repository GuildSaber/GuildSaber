namespace GuildSaber.Database.Models.Guilds.Points;

public readonly record struct PointInfo
{
    public string Name { get; init; }
    public string Description { get; init; }

    private PointInfo(string name, string description)
        => (Name, Description) = (name, description);
};
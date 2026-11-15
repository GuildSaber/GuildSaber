using System.ComponentModel.DataAnnotations;

namespace GuildSaber.Common.Settings;

public class LinkSettings
{
    public const string LinkSettingsSectionsKey = "LinkSettings";

    [Required]
    public required Uri ApiBaseUri { get; init; }

    [Required]
    public required Uri WebsiteBaseUri { get; init; }

    [Required]
    public required Uri CdnBaseUri { get; init; }
}
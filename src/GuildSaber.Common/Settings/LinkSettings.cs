#if NET10_0_OR_GREATER
using System.ComponentModel.DataAnnotations;
#endif

namespace GuildSaber.Common.Settings;

public class LinkSettings
{
    public const string LinkSettingsSectionsKey = "LinkSettings";

#if NET10_0_OR_GREATER
    [Required]
#endif
    public required Uri ApiBaseUri { get; init; }
#if NET10_0_OR_GREATER
    [Required]
#endif
    public required Uri WebsiteBaseUri { get; init; }
#if NET10_0_OR_GREATER
    [Required]
#endif
    public required Uri CdnBaseUri { get; init; }
}
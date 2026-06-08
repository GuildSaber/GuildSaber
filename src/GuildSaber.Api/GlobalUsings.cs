global using LinqKit;
global using static CSharpFunctionalExtensions.Maybe;
global using static CSharpFunctionalExtensions.Result;
global using static CSharpFunctionalExtensions.UnitResult;
global using static GuildSaber.Common.Result.RustExtensions;
global using FromQueryAttribute = Microsoft.AspNetCore.Mvc.FromQueryAttribute;
global using FromRouteAttribute = Microsoft.AspNetCore.Mvc.FromRouteAttribute;
global using FromFormAttribute = Microsoft.AspNetCore.Mvc.FromFormAttribute;
global using FromBodyAttribute = Microsoft.AspNetCore.Mvc.FromBodyAttribute;
global using FromHeaderAttribute = Microsoft.AspNetCore.Mvc.FromHeaderAttribute;
global using FromServicesAttribute = Microsoft.AspNetCore.Mvc.FromServicesAttribute;
global using EPermission = GuildSaber.Database.Models.Server.Guilds.Members.Member.EPermission;
global using SongDifficultyId =
    GuildSaber.Database.Models.Server.Songs.SongDifficulties.SongDifficulty.SongDifficultyId;
global using GuildSaber.Common.StrongTypes;
global using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
global using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
global using GuildSaber.Common.Services.ScoreSaber.Models.StrongTypes;
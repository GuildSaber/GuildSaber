using System;
using static GuildSaber.Api.Features.RankedMaps.Http.RankedMapRequests;
using static GameplayModifiers;

namespace GuildSaber.Mod.Helpers;

public static class GameplayModifiersExtensions
{
    extension(GameplayModifiers self)
    {
        /// <summary>Converts the `GameplayModifiers` to an `EModifier` bitmask.</summary>
        /// <remarks>The `NoFail` modifier won't be included in the mapping as it's conditional to the player failing the map.</remarks>
        public EModifiers ToEModifier()
        {
            var result = EModifiers.None;

            if (self.instaFail) result |= EModifiers.InstaFail;
            if (self.noFailOn0Energy) result |= EModifiers.BatteryEnergy;

            if (self.noBombs) result |= EModifiers.NoBombs;
            if (self.enabledObstacleType != EnabledObstacleType.All) result |= EModifiers.NoObstacles;
            if (self.noArrows) result |= EModifiers.NoArrows;

            if (self.ghostNotes) result |= EModifiers.GhostNotes;
            if (self.disappearingArrows) result |= EModifiers.DisappearingArrows;
            if (self.smallCubes) result |= EModifiers.SmallNotes;

            if (self.proMode) result |= EModifiers.ProMode;
            if (self.strictAngles) result |= EModifiers.StrictAngles;
            if (self.zenMode) result |= EModifiers.Unk;

            result |= self.songSpeed switch
            {
                SongSpeed.Normal => EModifiers.None,
                SongSpeed.Slower => EModifiers.SlowerSong,
                SongSpeed.Faster => EModifiers.FasterSong,
                SongSpeed.SuperFast => EModifiers.SuperFastSong,
                _ => throw new ArgumentOutOfRangeException()
            };

            return result;
        }
    }
}
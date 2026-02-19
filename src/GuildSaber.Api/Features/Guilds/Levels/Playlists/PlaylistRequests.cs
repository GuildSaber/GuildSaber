namespace GuildSaber.Api.Features.Guilds.Levels.Playlists;

public static class PlaylistRequests
{
    public enum PlaylistFilter
    {
        /// <summary>
        /// No filter applied.
        /// </summary>
        None = 0,

        /// <summary>
        /// Remove songs with passed scores.
        /// </summary>
        NoneWithPassedScores = 1,

        /// <summary>
        /// Remove songs with passed or pending scores.
        /// </summary>
        NoneWithPassedNorPendingScores
    }
}
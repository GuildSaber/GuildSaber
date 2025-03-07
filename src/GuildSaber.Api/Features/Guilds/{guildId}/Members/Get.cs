using GuildSaber.Database;
using GuildSaber.Database.Contexts;
using GuildSaber.Database.Models.Guilds;
using GuildSaber.Database.Models.Guilds.Navigation;
using GuildSaber.Database.Models.Players;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.Guilds._guildId_.Members;

/// <summary>
/// Get all members of the guild.
/// </summary>
public class Get(GSDbContext dbContext, ILogger logger)
{
    public async Task<Results<Ok<Member>, NotFound, InternalServerError<Exception>>> GetMember
        (Guild.GuildId guildId, Player.PlayerId playerId) => 
        await Try(dbContext.Members.FirstOrDefaultAsync(x => x.GuildId == guildId && x.PlayerId == playerId), e => e)
            switch
            {
                { IsFailure: true, Error: var exception } => TypedResults.InternalServerError(exception),
                { Value: var member } => member switch
                {
                    null => TypedResults.NotFound(),
                    _ => TypedResults.Ok(member)
                }
            };
    
    /* Theory:
    // Assuming generic in using alias gets added alongside unions
    using HttpResult<T> = Microsoft.AspNetCore.Http.HttpResults<Ok<T>, NotFound, InternalServerError<Exception>>>
    
    public async Task<HttpResult<Member>> GetMember
        (Guild.GuildId guildId, Player.PlayerId playerId) => 
        await Try(dbContext.Members.FirstOrDefaultAsync(x => x.GuildId == guildId && x.PlayerId == playerId), e => e)
            switch
            {
                Failure(var error) => TypedResults.InternalServerError(error),
                Success(var value) => value switch
                {
                    Some(var x) => TypedResults.Ok(x),
                    None => TypedResults.NotFound()
                }
            };*/
}
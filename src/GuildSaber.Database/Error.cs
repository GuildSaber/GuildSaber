using System.Runtime.CompilerServices;

namespace GuildSaber.Database;

public abstract record Error
{
    public record ValidationError(
        string Name,
        string Description,
        Exception? Exception,
        [CallerMemberName] string MemberName = "",
        [CallerFilePath] string FilePath = ""
    ) : Error;

    public record ExceptionalError(
        Exception Exception,
        [CallerMemberName] string MemberName = "",
        [CallerFilePath] string FilePath = ""
    ) : Error;

    public record NotFoundError(
        Exception? Exception,
        [CallerMemberName] string MemberName = "",
        [CallerFilePath] string FilePath = ""
    ) : Error;

    public record InsertError(
        Exception? Exception,
        [CallerMemberName] string MemberName = "",
        [CallerFilePath] string FilePath = ""
    ) : Error;

    public record UpdateError(
        Exception? Exception,
        [CallerMemberName] string MemberName = "",
        [CallerFilePath] string FilePath = ""
    ) : Error;
}
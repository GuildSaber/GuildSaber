using System.Runtime.CompilerServices;

// ReSharper disable NotAccessedPositionalProperty.Global
namespace GuildSaber.Core.Errors;

public abstract partial record Error
{
    public record ValidationError(
        string Name,
        string Description,
        Exception? Exception,
        [CallerMemberName] string MemberName = "",
        [CallerFilePath] string FilePath = ""
    ) : Error(Exception is null);
}
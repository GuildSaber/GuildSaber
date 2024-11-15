using System.Runtime.CompilerServices;

// ReSharper disable NotAccessedPositionalProperty.Global
namespace GuildSaber.Database.Errors;

public record EFExceptionalError(
    Exception Exception,
    [CallerMemberName] string MemberName = "",
    [CallerFilePath] string FilePath = ""
) : DbError.ExceptionalError;

public record EFNotFoundError(
    Exception? Exception,
    [CallerMemberName] string MemberName = "",
    [CallerFilePath] string FilePath = ""
) : DbError.NotFoundError(IsExceptional: Exception is null);

public record EFInsertError(
    Exception? Exception,
    [CallerMemberName] string MemberName = "",
    [CallerFilePath] string FilePath = ""
) : DbError.InsertError(IsExceptional: Exception is not null);

public record EFUpdateError(
    Exception? Exception,
    [CallerMemberName] string MemberName = "",
    [CallerFilePath] string FilePath = ""
) : DbError.UpdateError(IsExceptional: Exception is not null);
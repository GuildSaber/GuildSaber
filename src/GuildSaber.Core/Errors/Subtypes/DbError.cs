namespace GuildSaber.Core.Errors;

public abstract partial record Error
{
    public abstract record DbError(bool IsExceptional) : Error(IsExceptional)
    {
        public abstract record ExceptionalError() : DbError(IsExceptional: true);
        public abstract record NotFoundError(bool IsExceptional) : DbError(IsExceptional);
        public abstract record InsertError(bool IsExceptional) : DbError(IsExceptional);
        public abstract record UpdateError(bool IsExceptional) : DbError(IsExceptional);
    }
}
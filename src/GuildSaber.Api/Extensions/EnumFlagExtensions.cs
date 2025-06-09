namespace GuildSaber.Api.Extensions;

public static class EnumFlagExtensions
{
    public static IEnumerable<T> GetFlags<T>(this T value) where T : struct
    {
        CheckIsEnum<T>(true);

        foreach (var flag in Enum.GetValues(typeof(T)).Cast<T>())
            if (value.HasAnyFlag(flag))
                yield return flag;
    }

    private static void CheckIsEnum<T>(bool withFlags)
    {
        if (!typeof(T).IsEnum) throw new ArgumentException($"Type '{typeof(T).FullName}' is not an enum");
        if (withFlags && !Attribute.IsDefined(typeof(T), typeof(FlagsAttribute)))
            throw new ArgumentException($"Type '{typeof(T).FullName}' doesn't have the 'Flags' attribute");
    }

    public static bool HasAnyFlag<T>(this T value, T flag) where T : struct
    {
        CheckIsEnum<T>(true);
        return (Convert.ToInt64(value) & Convert.ToInt64(flag)) != 0;
    }
}
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GuildSaber.Core.Helpers;

internal static class Check<A>
{
    private static bool IsNullable => Nullable.GetUnderlyingType(typeof(A)) != null;
    private static bool IsReferenceType => !typeof(A).GetTypeInfo().IsValueType;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsNullOrDefault(A value) =>
        IsReferenceType && ReferenceEquals(value, null)
        || IsNullable && value!.Equals(null)
        || value!.Equals(default(A));
}
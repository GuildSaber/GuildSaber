using System.Diagnostics.CodeAnalysis;

namespace GuildSaber.CSharpClient
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    internal static class PolyFill
    {
        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
        internal class FromQueryAttribute : Attribute
        {
            public string? Name { get; set; }
        }
    }
}

#if NETSTANDARD2_1
namespace System.Runtime.CompilerServices
{
    // Compiler metadata for shared closed response/request hierarchies on the mod's older runtime.
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    internal sealed class IsClosedTypeAttribute : Attribute
    {
        public Type[] DerivedTypes { get; set; } = Type.EmptyTypes;
    }
}
#endif
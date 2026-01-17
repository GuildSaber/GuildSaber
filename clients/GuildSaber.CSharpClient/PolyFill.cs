using System.Diagnostics.CodeAnalysis;

namespace GuildSaber.CSharpClient;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal static class PolyFill
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    internal class FromQueryAttribute : Attribute
    {
        public string? Name { get; set; }
    }
}
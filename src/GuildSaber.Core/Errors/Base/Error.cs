using System.Diagnostics.CodeAnalysis;

namespace GuildSaber.Core.Errors;

[ExcludeFromCodeCoverage]
[SuppressMessage("ReSharper", "RedundantExtendsListEntry")]
public abstract partial record Error(bool IsExceptional);

using System.Diagnostics.CodeAnalysis;

namespace GuildSaber.Core.Errors;

[ExcludeFromCodeCoverage]
public class ErrorException(string message) : Exception(message);
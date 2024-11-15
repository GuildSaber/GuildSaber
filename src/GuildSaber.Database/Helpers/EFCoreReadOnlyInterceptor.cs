using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GuildSaber.Database.Helpers;

public class EFCoreReadOnlyInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
        => throw new InvalidOperationException("This DbContext is read-only.");

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("This DbContext is read-only.");
}

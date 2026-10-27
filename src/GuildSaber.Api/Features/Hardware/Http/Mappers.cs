using System.Linq.Expressions;
using GuildSaber.Database.Models.Server.Hardware;

namespace GuildSaber.Api.Features.Hardware.Http;

public static class HardwareMappers
{
    private static Func<Hmd?, HardwareResponses.Hmd?>? _mapHmdImpl;
    private static Func<Controller?, HardwareResponses.Controller?>? _mapControllerImpl;

    [Expandable(nameof(MapHmdExpression))]
    public static HardwareResponses.Hmd? Map(this Hmd? self)
        => (_mapHmdImpl ??= MapHmdExpression().Compile())(self);

    [Expandable(nameof(MapControllerExpression))]
    public static HardwareResponses.Controller? Map(this Controller? self)
        => (_mapControllerImpl ??= MapControllerExpression().Compile())(self);

    private static Expression<Func<Hmd?, HardwareResponses.Hmd?>> MapHmdExpression()
        => self => self == null ? null : new HardwareResponses.Hmd(self.Id, self.Name);

    private static Expression<Func<Controller?, HardwareResponses.Controller?>> MapControllerExpression()
        => self => self == null ? null : new HardwareResponses.Controller(self.Id, self.Name);
}
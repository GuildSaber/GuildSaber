using Hangfire.Dashboard;

namespace GuildSaber.Api.Hangfire;

public class HangFireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    //TODO: Implement the dashboard authorization (see: https://docs.hangfire.io/en/latest/configuration/using-dashboard.html#id2)
    public bool Authorize(DashboardContext context)
        => true;
}
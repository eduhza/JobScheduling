using Hangfire.Annotations;
using Hangfire.Dashboard;

namespace JobScheduling.API.Filters;

public class HangfireAuthorizationFilter : IDashboardAsyncAuthorizationFilter
{
    public Task<bool> AuthorizeAsync([NotNull] DashboardContext context)
    {
        // Em produção verificar se o usuário está autenticado e autorizado a acessar o dashboard
        return Task.FromResult(true);
    }
}

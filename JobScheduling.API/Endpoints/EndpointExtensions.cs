namespace JobScheduling.API.Endpoints;

public static class EndpointExtensions
{
    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        app.MapTickerQEndpoints();
        app.MapHangfireEndpoints();

        return app;
    }
}

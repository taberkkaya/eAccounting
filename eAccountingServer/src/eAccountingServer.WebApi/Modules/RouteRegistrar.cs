namespace eAccountingServer.WebApi.Modules
{
    public static class RouteRegistrar
    {
        public static void RegisterRoutes(this IEndpointRouteBuilder app)
        {

            app.RegisterAuthRoutes();
        }
    }
}

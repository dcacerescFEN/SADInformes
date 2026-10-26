using System.Reflection;

namespace WebApi.Extension;

public static class EndpointExtensions
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        var endpoints = assembly.DefinedTypes.Where(type =>
            !type.IsAbstract && !type.IsInterface && type.IsAssignableTo(typeof(IEndpoint)));
        foreach (var type in endpoints)
            services.AddSingleton((IEndpoint)Activator.CreateInstance(type.AsType())!);
        return services;
    }

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        foreach (var endpoint in app.Services.GetRequiredService<IEnumerable<IEndpoint>>())
            endpoint.MapEndpoint(app);
        return app;
    }
}

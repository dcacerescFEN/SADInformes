namespace WebApi.Spa;

/// <summary>
/// Entrega index.html solo para rutas del cliente Angular. Las rutas de API,
/// documentación y archivos inexistentes no se convierten en respuestas SPA.
/// </summary>
public static class SpaHosting
{
    private static readonly string[] ReservedPrefixes = ["/api", "/scalar", "/openapi"];

    public static IResult ServeIndex(HttpContext context, IWebHostEnvironment environment)
    {
        var path = context.Request.Path.Value ?? "/";
        if (IsReserved(path) || Path.HasExtension(path)) return Results.NotFound();

        var index = Path.Combine(environment.WebRootPath ?? string.Empty, "index.html");
        return File.Exists(index) ? Results.File(index, "text/html") : Results.NotFound();
    }

    public static bool IsReserved(string path) =>
        ReservedPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && (path.Length == prefix.Length || path[prefix.Length] == '/'));
}

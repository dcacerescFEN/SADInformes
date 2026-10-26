using System.Security.Claims;
using WebApi.Extension;

namespace WebApi.Endpoints.Auth;

public sealed class AuthEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGroup("/api/auth").RequireAuthorization().WithTags("Auth")
            .MapGet("/me", (ClaimsPrincipal user) => Results.Ok(new { username = user.Identity?.Name }));
}

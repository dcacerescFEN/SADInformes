using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using WebApi.Authentication;

namespace WebApi.OpenApi;

public static class ApiDocumentation
{
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi("v1", options =>
        {
            options.AddDocumentTransformer((document, context, _) =>
            {
                var config = context.ApplicationServices.GetRequiredService<PublicKeycloakConfiguration>();
                var authority = $"{config.Url}/realms/{config.Realm}";
                document.Info = new OpenApiInfo { Title = "SADInformes API", Version = "1.0" };
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                {
                    ["Keycloak"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.OAuth2,
                        Flows = new OpenApiOAuthFlows
                        {
                            AuthorizationCode = new OpenApiOAuthFlow
                            {
                                AuthorizationUrl = new Uri($"{authority}/protocol/openid-connect/auth"),
                                TokenUrl = new Uri($"{authority}/protocol/openid-connect/token"),
                                Scopes = new Dictionary<string, string>
                                {
                                    ["openid"] = "Identidad del usuario",
                                    ["profile"] = "Perfil del usuario",
                                },
                            },
                        },
                    },
                };
                return Task.CompletedTask;
            });
            options.AddOperationTransformer((operation, context, _) =>
            {
                var metadata = context.Description.ActionDescriptor.EndpointMetadata;
                if (metadata.OfType<IAuthorizeData>().Any() && !metadata.OfType<IAllowAnonymous>().Any())
                {
                    operation.Security = [new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference("Keycloak", context.Document)] = ["openid", "profile"],
                    }];
                }
                return Task.CompletedTask;
            });
        });
        return services;
    }

    public static void MapDevelopmentDocumentation(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return;
        var config = app.Services.GetRequiredService<PublicKeycloakConfiguration>();
        app.MapOpenApi("/openapi/v1.json");
        app.MapScalarApiReference(options => options
            .WithTitle("SADInformes API")
            .AddPreferredSecuritySchemes("Keycloak")
            .AddAuthorizationCodeFlow("Keycloak", flow =>
            {
                flow.ClientId = config.ClientId;
                flow.Pkce = Pkce.Sha256;
                flow.SelectedScopes = ["openid", "profile"];
            }));
    }
}

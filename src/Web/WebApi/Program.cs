using Application;
using Infrastructure;
using Infrastructure.Configuration;
using WebApi;
using WebApi.OpenApi;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using WebApi.Extension;
using WebApi.Spa;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationServices();
builder.Services.AddWebApiServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddEndpoints(typeof(Program).Assembly);

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
await app.InitialiseConfigurationAsync(builder.Configuration);
app.UseAuthentication();
app.UseAuthorization();
app.MapEndpoints();
app.MapDevelopmentDocumentation();
// Solo GET/HEAD llegan al fallback: las rutas reservadas y los métodos no soportados conservan 404/405.
app.MapFallback(SpaHosting.ServeIndex)
    .WithMetadata(new HttpMethodMetadata([HttpMethods.Get, HttpMethods.Head]));
app.Run();

public partial class Program;

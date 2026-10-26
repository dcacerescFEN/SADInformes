using Infrastructure;
using Infrastructure.Configuration;
using WebApi.Authentication;
using WebApi.Extension;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddEndpoints(typeof(Program).Assembly);

var app = builder.Build();
await app.InitialiseConfigurationAsync(builder.Configuration);
app.UseAuthentication();
app.UseAuthorization();
app.MapEndpoints();
app.Run();

public partial class Program;

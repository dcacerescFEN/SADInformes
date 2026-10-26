using Application.Configuration;
using Application.Navigation;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Configuration;

public sealed class AppConfigConfigurationReader(Func<AppConfigContext> createContext) : IAppConfigurationReader
{
    public async Task<IReadOnlyDictionary<string, string?>> LoadAsync(
        string applicationName, string environmentName, CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var keys = SiteMenuFields.Names.Append("StatusSite").Select(name => name.ToLowerInvariant()).ToArray();
        var rows = await context.Configurations.AsNoTracking()
            .Where(row => row.IsActive && row.Application.IsActive && row.Environment.IsActive
                && row.Application.ApplicationName == applicationName
                && row.Environment.EnvironmentName == environmentName
                && keys.Contains(row.ConfigurationName.ToLower()))
            .Select(row => new { row.ConfigurationName, row.ConfigurationValue })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(row => row.ConfigurationName,
            row => (string?)row.ConfigurationValue, StringComparer.OrdinalIgnoreCase);
    }
}

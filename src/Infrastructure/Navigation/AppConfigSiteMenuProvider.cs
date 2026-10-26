using Application.Navigation;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Navigation;

public sealed class AppConfigSiteMenuProvider(
    Func<AppConfigContext> createContext, string applicationName, string environmentName) : ISiteMenuProvider
{
    public async Task<IReadOnlyDictionary<string, string?>> GetMenuAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(applicationName) || string.IsNullOrWhiteSpace(environmentName))
            throw new InvalidOperationException("Los selectores de AppConfig son obligatorios.");

        await using var context = createContext();
        var keys = SiteMenuFields.Names.Select(name => name.ToLowerInvariant()).ToArray();
        var rows = await context.Configurations.AsNoTracking()
            .Where(row => row.IsActive && row.Application.IsActive && row.Environment.IsActive
                && row.Application.ApplicationName == applicationName
                && row.Environment.EnvironmentName == environmentName
                && keys.Contains(row.ConfigurationName.ToLower()))
            .Select(row => new { row.ConfigurationName, row.ConfigurationValue })
            .ToListAsync(cancellationToken);
        var values = rows.ToDictionary(row => row.ConfigurationName,
            row => row.ConfigurationValue, StringComparer.OrdinalIgnoreCase);
        return SiteMenuFields.Names.ToDictionary(
            name => char.ToLowerInvariant(name[0]) + name[1..],
            name => values.TryGetValue(name, out var value) ? value : (string?)null);
    }
}

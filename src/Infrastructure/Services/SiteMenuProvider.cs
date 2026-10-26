using Application.Navigation;
using Infrastructure.Configuration;

namespace Infrastructure.Services;

public sealed class SiteMenuProvider(SiteSettings settings) : ISiteMenuProvider
{
    public Task<IReadOnlyDictionary<string, string?>> GetMenuAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(settings.Menu);
    }
}

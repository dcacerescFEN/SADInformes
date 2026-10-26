namespace Application.Configuration;

public interface IAppConfigurationReader
{
    Task<IReadOnlyDictionary<string, string?>> LoadAsync(
        string applicationName, string environmentName, CancellationToken cancellationToken);
}

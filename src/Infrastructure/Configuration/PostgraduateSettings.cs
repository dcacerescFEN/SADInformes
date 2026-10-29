namespace Infrastructure.Configuration;

public sealed class PostgraduateSettings : Application.Common.Interfaces.IPostgraduateSettings
{
    public const string UserConfigurationKey = "PostgraduateUser";

    private string? _user;

    public string User => _user
        ?? throw new InvalidOperationException("El usuario de Postgrado no está configurado.");

    internal void Initialize(string? user)
    {
        var value = (user ?? "mvenegas").Trim();
        // An empty SAD user would bypass department restrictions in the legacy procedure.
        if (string.IsNullOrWhiteSpace(value) || value.Length > 20)
            return;

        _user = value;
    }
}

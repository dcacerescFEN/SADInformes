namespace Application.Common.Interfaces;

/// <summary>Usuario SAD de Postgrado configurado en AppConfig, nunca provisto por el cliente.</summary>
public interface IPostgraduateSettings
{
    string User { get; }
}

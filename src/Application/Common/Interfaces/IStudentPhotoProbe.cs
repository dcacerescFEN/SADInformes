namespace Application.Common.Interfaces;

/// <summary>Comprueba si la fotografía institucional de una persona está disponible.</summary>
public interface IStudentPhotoProbe
{
    /// <summary>Devuelve <c>false</c> solo cuando el servidor confirma que la foto no existe.</summary>
    Task<bool> HasPhotoAsync(string personCode, CancellationToken cancellationToken);
}

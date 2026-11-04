namespace Application.Common.Interfaces;

/// <summary>URL base de fotos de alumnos configurada en AppConfig.</summary>
public interface IStudentPhotoSettings
{
    string BaseUrl { get; }
}

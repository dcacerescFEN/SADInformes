namespace Domain.Entities.Sad.Tables.dbo;

/// <summary>Read projection of the existing table; canonical mapping: MappingDB/Models/Curso.cs.</summary>
public sealed class Curso
{
    public string Periodo { get; set; } = null!;
    public string CodCatedra { get; set; } = null!;
    public string CodSeccion { get; set; } = null!;
    public bool Eliminado { get; set; }
    public string? TipoAlumnoPostgrado { get; set; }
    public string? ModalidadPostgrado { get; set; }
    public string? DecretoPostgrado { get; set; }
}

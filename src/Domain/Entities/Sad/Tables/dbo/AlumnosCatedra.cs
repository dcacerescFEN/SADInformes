namespace Domain.Entities.Sad.Tables.dbo;

/// <summary>Read projection of the existing table; canonical mapping: MappingDB/Models/AlumnosCatedra.cs.</summary>
public sealed class AlumnosCatedra
{
    public string CodAlumno { get; set; } = null!;
    public string Periodo { get; set; } = null!;
    public string CodCatedra { get; set; } = null!;
    public string CodSeccion { get; set; } = null!;
    public string Categoria { get; set; } = null!;
}

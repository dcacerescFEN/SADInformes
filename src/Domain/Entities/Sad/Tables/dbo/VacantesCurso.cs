namespace Domain.Entities.Sad.Tables.dbo;

/// <summary>Read projection of the existing table; canonical mapping: MappingDB/Models/VacantesCurso.cs.</summary>
public sealed class VacantesCurso
{
    public string Periodo { get; set; } = null!;
    public string CodCatedra { get; set; } = null!;
    public string CodSeccion { get; set; } = null!;
    public string TipoAlumno { get; set; } = null!;
    public short VacOfrecidas { get; set; }
}

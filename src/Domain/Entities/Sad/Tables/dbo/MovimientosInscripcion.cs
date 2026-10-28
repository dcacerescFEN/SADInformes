namespace Domain.Entities.Sad.Tables.dbo;

/// <summary>Read projection of the existing table; canonical mapping: MappingDB/Models/MovimientosInscripcion.cs.</summary>
public sealed class MovimientosInscripcion
{
    public int Identificacion { get; set; }
    public string CodAlumno { get; set; } = null!;
    public string Periodo { get; set; } = null!;
    public string CodCatedra { get; set; } = null!;
    public string CodSeccion { get; set; } = null!;
    public string? TipoMovimiento { get; set; }
    public string? Estado { get; set; }
}

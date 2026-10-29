using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Sad.Sp;

// MappingDB's generated result is empty; these columns follow the canonical procedure's SELECT.
public sealed class SpRecAlumnosCuadroDeHonorPosgradoResult
{
    public int Posicion { get; set; }
    public string? Nombre { get; set; }
    public string? Rut { get; set; }
    [Column("Código")]
    public string? Codigo { get; set; }
    public string? Ingreso { get; set; }
    [Column("Situación")]
    public string? Situacion { get; set; }
    public decimal? Promedio { get; set; }
    public decimal? Percentil { get; set; }
}

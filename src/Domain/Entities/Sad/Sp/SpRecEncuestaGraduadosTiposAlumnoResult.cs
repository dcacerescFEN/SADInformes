using System.ComponentModel.DataAnnotations.Schema;
namespace Domain.Entities.Sad.Sp;
public sealed class SpRecEncuestaGraduadosTiposAlumnoResult
{
    [Column("Tipo")] public string? Tipo { get; set; }
    [Column("Nombre")] public string? Nombre { get; set; }
    [Column("Orden")] public int? Orden { get; set; }
}

using System.ComponentModel.DataAnnotations.Schema;
namespace Domain.Entities.Sad.Sp;
public sealed class SpRecEncuestaGraduadosEstadisticasResult
{
    [Column("Tipo")] public string? Tipo { get; set; }
    [Column("Modalidad")] public string? Modalidad { get; set; }
    [Column("Muestra")] public int? Muestra { get; set; }
    [Column("EnProceso")] public int? EnProceso { get; set; }
    [Column("Contestadas")] public int? Contestadas { get; set; }
}

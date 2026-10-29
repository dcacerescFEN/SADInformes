using System.ComponentModel.DataAnnotations.Schema;
namespace Domain.Entities.Sad.Sp;
public sealed class SpRecEncuestaGraduadosPeriodosResult
{
    [Column("Periodo")] public string? Periodo { get; set; }
    [Column("Descripcion")] public string? Descripcion { get; set; }
}

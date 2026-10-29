using System.ComponentModel.DataAnnotations.Schema;
namespace Domain.Entities.Sad.Sp;
public sealed class SpRecEncuestaGraduadosDetalleProfResult
{
    [Column("Motivos")] public string? Motivos { get; set; }
}

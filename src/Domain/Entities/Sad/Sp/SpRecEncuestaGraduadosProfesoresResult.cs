using System.ComponentModel.DataAnnotations.Schema;
namespace Domain.Entities.Sad.Sp;
public sealed class SpRecEncuestaGraduadosProfesoresResult
{
    [Column("Profesor")] public string? Profesor { get; set; }
    [Column("Frecuencia")] public int? Frecuencia { get; set; }
}

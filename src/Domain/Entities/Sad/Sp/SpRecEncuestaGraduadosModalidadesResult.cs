using System.ComponentModel.DataAnnotations.Schema;
namespace Domain.Entities.Sad.Sp;
public sealed class SpRecEncuestaGraduadosModalidadesResult
{
    [Column("Tipo")] public string? Tipo { get; set; }
    [Column("Modalidad")] public string? Modalidad { get; set; }
}

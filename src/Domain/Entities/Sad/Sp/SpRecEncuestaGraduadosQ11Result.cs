using System.ComponentModel.DataAnnotations.Schema;
namespace Domain.Entities.Sad.Sp;
public sealed class SpRecEncuestaGraduadosQ11Result
{
    [Column("Pregunta")] public string? Pregunta { get; set; }
    [Column("Descripcion")] public string? Descripcion { get; set; }
    [Column("Alternativa1")] public int? Alternativa1 { get; set; }
    [Column("Alternativa2")] public int? Alternativa2 { get; set; }
    [Column("Alternativa3")] public int? Alternativa3 { get; set; }
    [Column("Alternativa4")] public int? Alternativa4 { get; set; }
    [Column("Alternativa5")] public int? Alternativa5 { get; set; }
    [Column("Alternativa6")] public int? Alternativa6 { get; set; }
    [Column("Alternativa7")] public int? Alternativa7 { get; set; }
    [Column("Promedio")] public double? Promedio { get; set; }
    [Column("Total")] public int? Total { get; set; }
}

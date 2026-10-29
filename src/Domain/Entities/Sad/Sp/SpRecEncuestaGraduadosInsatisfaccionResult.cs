using System.ComponentModel.DataAnnotations.Schema;
namespace Domain.Entities.Sad.Sp;
public sealed class SpRecEncuestaGraduadosInsatisfaccionResult
{
    [Column("Profesor")] public string? Profesor { get; set; }
    [Column("Cantidad")] public int? Cantidad { get; set; }
    [Column("Cod_Persona")] public string? CodPersona { get; set; }
    [Column("Periodo")] public string? Periodo { get; set; }
    [Column("Tipo_Alumno")] public string? TipoAlumno { get; set; }
    [Column("Modalidad")] public string? Modalidad { get; set; }
}

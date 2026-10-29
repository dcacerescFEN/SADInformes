using System.ComponentModel.DataAnnotations.Schema;
namespace Domain.Entities.Sad.Sp;
public sealed class SpRecEncuestaGraduadosAlumnosResult
{
    [Column("Nombre")] public string? Nombre { get; set; }
    [Column("Rut")] public string? Rut { get; set; }
    [Column("Cod_Alumno")] public string? CodAlumno { get; set; }
    [Column("Cod_Persona")] public string? CodPersona { get; set; }
    [Column("Sem_Ingreso")] public string? SemIngreso { get; set; }
    [Column("Tipo_Ingreso")] public string? TipoIngreso { get; set; }
    [Column("Tipo_Alumno")] public string? TipoAlumno { get; set; }
    [Column("Fecha_Egreso")] public DateTime? FechaEgreso { get; set; }
    [Column("Fecha_Titulacion")] public DateTime? FechaTitulacion { get; set; }
    [Column("Inscrito")] public string? Inscrito { get; set; }
    [Column("Porcentaje")] public string? Porcentaje { get; set; }
    [Column("Modalidad")] public string? Modalidad { get; set; }
}

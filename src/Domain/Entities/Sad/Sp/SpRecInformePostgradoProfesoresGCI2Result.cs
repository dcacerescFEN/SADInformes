using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Sad.Sp;

public sealed class SpRecInformePostgradoProfesoresGCI2Result
{
    public string Periodo { get; set; } = null!;
    public string Cod_Catedra { get; set; } = null!;
    public string Cod_Seccion { get; set; } = null!;
    public string Cod_Profesor { get; set; } = null!;
    [Column("Nombre Profesor")]
    public string? NombreProfesor { get; set; }
    public string? Tipo { get; set; }
    public string Cod_Alumno { get; set; } = null!;
    public string Nota { get; set; } = null!;
    public string? Estudiante { get; set; }
    public string Programa { get; set; } = null!;
    public string? Modalidad { get; set; }
    [Column("Año Ingreso")]
    public string? AñoIngreso { get; set; }
    [Column("Tema/Nombre Tésis")]
    public string TemaNombreTesis { get; set; } = null!;
    public DateTime? Fecha_Examen { get; set; }
    public string? Estado_Examen { get; set; }
    [Column("Centro Costo")]
    public string CentroCosto { get; set; } = null!;
    [Column("Fecha Resolución")]
    public string? FechaResolucion { get; set; }
}

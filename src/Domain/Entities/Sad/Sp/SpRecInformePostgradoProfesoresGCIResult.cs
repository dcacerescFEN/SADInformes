using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Sad.Sp;

public sealed class SpRecInformePostgradoProfesoresGCIResult
{
    public string Cod_Alumno { get; set; } = null!;
    public string? Nombre { get; set; }
    public string Programa { get; set; } = null!;
    public string? Modalidad { get; set; }
    [Column("Año Ingreso")]
    public string? AñoIngreso { get; set; }
    [Column("Fecha Examen")]
    public string? FechaExamen { get; set; }
    public string Periodo { get; set; } = null!;
    public string Cod_Catedra { get; set; } = null!;
    public string Cod_Seccion { get; set; } = null!;
    public string Nota { get; set; } = null!;
    public string? Estado_Examen { get; set; }
    [Column("Tema/Nombre Tésis")]
    public string TemaNombreTesis { get; set; } = null!;
    public string? GUIA { get; set; }
    public string? COGUIA { get; set; }
    public string? INFORMANTE1 { get; set; }
    public string? INFORMANTE2 { get; set; }
    public string? INFORMANTE3 { get; set; }
    [Column("Centro Costo")]
    public string CentroCosto { get; set; } = null!;
    [Column("Año Defensa")]
    public string? AñoDefensa { get; set; }
    [Column("Fecha Resolución")]
    public string? FechaResolucion { get; set; }
}

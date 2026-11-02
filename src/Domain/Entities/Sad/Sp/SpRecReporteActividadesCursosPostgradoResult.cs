namespace Domain.Entities.Sad.Sp;

public sealed class SpRecReporteActividadesCursosPostgradoResult
{
    public int Id { get; set; }
    public string Tipo { get; set; } = null!;
    public string Catedra { get; set; } = null!;
    public string? Desde { get; set; }
    public string? Hasta { get; set; }
    public string? Periodo { get; set; }
    public string? Cod_Catedra { get; set; }
    public string? Cod_Seccion { get; set; }
    public string? Programa { get; set; }
    public string? Modalidad { get; set; }
    public string? Promocion { get; set; }
    public string Sala { get; set; } = null!;
    public int? Capacidad_Sala { get; set; }
    public int? Estudiantes_Inscritos { get; set; }
    public string? Tipo_Sala { get; set; }
    public string Es_Sala_Hibrida { get; set; } = null!;
    public string? Fecha_Ingreso { get; set; }
    public string? UsuarioIngreso { get; set; }
    public string Suspendida { get; set; } = null!;
    public DateTime? Fecha_Suspención { get; set; }
    public string? Usuario_Suspención { get; set; }
    public string? Observaciones { get; set; }
}

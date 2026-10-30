namespace Domain.Entities.Sad.Sp;

public sealed class SpRecRegistroTesisPostgradoResult
{
    public string Cod_Alumno { get; set; } = null!;
    public string Matricula { get; set; } = null!;
    public string Sem_IngresoDecreto { get; set; } = null!;
    public string Decreto { get; set; } = null!;
    public string Cod_Persona { get; set; } = null!;
    public string Tipo_Alumno { get; set; } = null!;
    public string? Modalidad { get; set; }
    public string? Apellidos { get; set; }
    public string? Nombres { get; set; }
    public string? CatedraAFE { get; set; }
    public string? PeriodoAFE { get; set; }
    public string? ProfesorAFE { get; set; }
    public string? NotaAFE { get; set; }
    public string Cod_Catedra { get; set; } = null!;
    public string Periodo { get; set; } = null!;
    public string Cod_SitAcademica { get; set; } = null!;
    public string? Motivo { get; set; }
    public string Nombre { get; set; } = null!;
    public decimal? Nota { get; set; }
    public string? Profesores { get; set; }
    public DateTime? Fecha_Examen { get; set; }
}

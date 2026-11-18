namespace Domain.Entities.Sad.Sp;

public sealed class SpRecAlumnosConSolicitudesPermanenciaNoResueltasResult
{
    public string? Nombre { get; set; }
    public string? Cod_Persona { get; set; }
    public string? Cod_Alumno { get; set; }
    public string? Region { get; set; }
    public string? Tipo_Ingreso { get; set; }
    public string? TipoColegio { get; set; }
    public decimal? PorcentajeCredito { get; set; }
    public string? PeriodoCredito { get; set; }
    public int? UDA { get; set; }
    public int? UDR { get; set; }
    public double? PorcentajeUDR { get; set; }
    public decimal? PromedioAproREP { get; set; }
    public string? PeriodoCausal { get; set; }
    public string? PeriodoCausalLargo { get; set; }
    public int? UdInscritasPeriodoCausal { get; set; }
    public int? UdInscritas { get; set; }
    public string? PeriodoProceso { get; set; }
    public string? Situacion { get; set; }
    public int? SemestresOrdinariosCursados { get; set; }
    public int? SemestresExtraordinariosCursados { get; set; }
    public int? SemestresOrdinariosNoCursados { get; set; }
    public string? BajoUdSemestre { get; set; }
    public string? BajoUdSemestreNuevas { get; set; }
    public string? BajoUdPrimerAño { get; set; }
    public string? Reprobadas2a { get; set; }
    public string? Reprobadas3a { get; set; }
    public string? Mas100UdReprobadas { get; set; }
    public string? MasDe16Semestres { get; set; }
}

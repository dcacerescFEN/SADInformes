namespace Domain.Entities.Sad.Tables.dbo;

public sealed class PeriodoAcademico
{
    public string Periodo { get; set; } = "";
    public string Tipo { get; set; } = "";
    public DateTime? InicioPeriodo { get; set; }
    public DateTime? FinPeriodo { get; set; }
}

namespace Domain.Entities.Sad.Sp;

/// <summary>Resultado de <c>dbo.spRec_PeriodosAcademicosProgramacion</c>.</summary>
public sealed class SpRecPeriodosAcademicosProgramacionResult
{
    public string Periodo { get; set; } = null!;
    public string? PeriodoLargo { get; set; }
    public string? Inscripcion { get; set; }
}

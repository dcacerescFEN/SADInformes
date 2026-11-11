namespace Domain.Entities.Sad.Sp;

public sealed class SpRecProcesosPeriodosResult
{
    public int IdProcesoPeriodo { get; set; }
    public string Nombre { get; set; } = null!;
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaTermino { get; set; }
}

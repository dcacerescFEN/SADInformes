namespace Domain.Entities.DocenciaWeb;

public sealed class TaskGrupo
{
    public long Id { get; set; }
    public long TaskId { get; set; }
    public int? Orden { get; set; }
    public DateTime? FechaElimina { get; set; }
}

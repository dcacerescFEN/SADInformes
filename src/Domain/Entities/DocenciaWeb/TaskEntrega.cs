namespace Domain.Entities.DocenciaWeb;

public sealed class TaskEntrega
{
    public long Id { get; set; }
    public long TaskId { get; set; }
    public string UnameAlumno { get; set; } = null!;
    public DateTime? FechaEntrega { get; set; }
    public string? Nota { get; set; }
    public DateTime? FechaEvaluacion { get; set; }
    public string? ObservacionProfesor { get; set; }
    public DateTime? FechaElimina { get; set; }
}

namespace Domain.Entities.DocenciaWeb;

public sealed class TaskM
{
    public long Id { get; set; }
    public string CodCatedra { get; set; } = null!;
    public string CodSeccion { get; set; } = null!;
    public string Periodo { get; set; } = null!;
    public string? Titulo { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public DateTime? FechaEntrega { get; set; }
    public int TipoTaskId { get; set; }
    public DateTime? FechaElimina { get; set; }
}

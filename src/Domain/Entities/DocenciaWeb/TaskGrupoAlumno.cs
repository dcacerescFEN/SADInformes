namespace Domain.Entities.DocenciaWeb;

public sealed class TaskGrupoAlumno
{
    public long GrupoId { get; set; }
    public string UnameAlumno { get; set; } = null!;
    public DateTime? FechaElimina { get; set; }
}

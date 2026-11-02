namespace Domain.Entities.Sad.Tables.dbo;

/// <summary>Tabla <c>dbo.Evaluacion_ISS_PostgradoCategorias</c>.</summary>
public sealed class EvaluacionIssPostgradoCategoria
{
    public int IdCategoria { get; set; }
    public string Descripcion { get; set; } = null!;
    public int IdEvaluacion { get; set; }
}

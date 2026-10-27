namespace Domain.Entities.Sad.Sp;

/// <summary>Resultado de <c>dbo.spRec_TiposAlumno</c>.</summary>
public sealed class SpRecTiposAlumnoResult
{
    public string Tipo { get; set; } = null!;
    public string? Nombre { get; set; }
    public int? Orden { get; set; }
}

namespace Domain.Entities.Sad.Tables.dbo;

/// <summary>Vista <c>dbo.POSTGRADO_Ayudantes_Sigma_SinHistorial</c>; solo columnas consumidas.</summary>
public sealed class PostgradoAyudanteSigmaSinHistorial
{
    public string COD { get; set; } = "";
    public string? Nombre1 { get; set; }
    public string? Nombre2 { get; set; }
    public string? Apellido1 { get; set; }
    public string? Apellido2 { get; set; }
    public string? CodPersona { get; set; }
    public string? Email { get; set; }
    public string? CodCatedra { get; set; }
    public string? NomCatedra { get; set; }
    public string? CodSeccion { get; set; }
    public DateOnly? Inicio { get; set; }
    public DateOnly? Termino { get; set; }
    public string? AñoCatedra { get; set; }
    public string? Periodo { get; set; }
    public string? TipoAlumnoPostgrado { get; set; }
    public string? ModalidadPostgrado { get; set; }
    public string? Promocion { get; set; }
    public string? Ccosto { get; set; }
    public string? TipoPago { get; set; }
    public int? ValorCursoReal { get; set; }
}

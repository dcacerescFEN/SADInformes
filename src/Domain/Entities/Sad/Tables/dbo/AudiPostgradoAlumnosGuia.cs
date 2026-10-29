namespace Domain.Entities.Sad.Tables.dbo;

public sealed class AudiPostgradoAlumnosGuia
{
    public int Id { get; set; }
    public string Estado { get; set; } = "";
    public string CodPersona { get; set; } = "";
    public string CodAlumnoAntiguo { get; set; } = "";
    public string? CodAlumnoNuevo { get; set; }
    public string? CodSitAcademica { get; set; }
    public string? Matricula { get; set; }
    public DateTime FechaRegistro { get; set; }
    public string? MensajeError { get; set; }
}

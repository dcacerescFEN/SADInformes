namespace Domain.Entities.Sad.Sp;

public sealed class SpRecObtenerTareasAfeReporteResult
{
    public long Id { get; set; }
    public string Cod_Catedra { get; set; } = null!;
    public string Cod_Seccion { get; set; } = null!;
    public string Periodo { get; set; } = null!;
    public string? Titulo { get; set; }
    public DateTime? Fecha_creacion { get; set; }
    public DateTime? Fecha_Entrega { get; set; }
    public string? Nombre1 { get; set; }
    public string? Apellido1 { get; set; }
    public string? Apellido2 { get; set; }
    public string? Uname { get; set; }
    public string? EnviarMail { get; set; }
    public string? CursoTipo { get; set; }
    public string? Tipo_AlumnoPostgrado { get; set; }
    public string? ModalidadPostgrado { get; set; }
    public string? Nom_Catedra { get; set; }
}

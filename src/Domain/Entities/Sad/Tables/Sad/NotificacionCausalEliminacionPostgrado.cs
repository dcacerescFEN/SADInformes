namespace Domain.Entities.Sad.Tables.Sad;

public sealed class NotificacionCausalEliminacionPostgrado
{
    public int IdNotificacionCausalEliminacionPostgrado { get; set; }
    public Guid IdPublico { get; set; }
    public string CodigoAlumno { get; set; } = null!;
    public string Notificacion { get; set; } = null!;
    public string CasillaCordinadora { get; set; } = null!;
    public bool OmitirProximaNotificacion { get; set; }
    public DateTime Fecha { get; set; }
    public bool Estado { get; set; }
}

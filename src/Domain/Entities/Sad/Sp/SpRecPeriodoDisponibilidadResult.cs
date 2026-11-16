namespace Domain.Entities.Sad.Sp;

public sealed class SpRecPeriodoDisponibilidadResult
{
    public string? Periodo { get; set; }
    public string? NombrePeriodo { get; set; }
    public int? Habilitado { get; set; }
    public int? EsProfesor { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaTermino { get; set; }
    public string? HoraInicio { get; set; }
    public string? HoraTermino { get; set; }
    public string? TextoFechaCorreoDisponibilidad { get; set; }
    public string? TextoFechaRezagados { get; set; }
}

namespace Domain.Entities.DocenciaWeb.Tables;

public sealed class FeModuloCascadaFinal
{
    public string Periodo { get; set; } = "";
    public string CodPersona { get; set; } = "";
    public int TipoModulo { get; set; }
    public string ModuloCascada { get; set; } = "";
    public DateTime? FechaCreacion { get; set; }
    public bool? Eliminada { get; set; }
    public DateTime? FechaEliminado { get; set; }
    public int Id { get; set; }
    public string? Ingreso { get; set; }
    public string? Modulo { get; set; }
    public string? Orden { get; set; }
    public string? Marca { get; set; }
}

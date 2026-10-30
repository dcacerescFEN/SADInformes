namespace Domain.Entities.SigmaRemuneraciones;

/// <summary>Tabla <c>prestacion</c> de Sigma Remuneraciones.</summary>
public sealed class Prestacion
{
    public int Id { get; set; }
    public int? RemuneracionId { get; set; }
    public string? CodImp { get; set; }
}

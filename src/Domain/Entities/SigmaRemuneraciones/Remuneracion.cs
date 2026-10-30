namespace Domain.Entities.SigmaRemuneraciones;

/// <summary>Tabla <c>remuneracion</c> de Sigma Remuneraciones.</summary>
public sealed class Remuneracion
{
    public int Id { get; set; }
    public DateTime? CreatedAt { get; set; }
    public float EstadoId { get; set; }
    public bool BanderaId { get; set; }
}

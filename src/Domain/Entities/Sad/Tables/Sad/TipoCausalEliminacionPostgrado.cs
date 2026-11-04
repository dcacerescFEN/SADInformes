namespace Domain.Entities.Sad.Tables.Sad;

public sealed class TipoCausalEliminacionPostgrado
{
    public int IdTipoCausalEliminacionPostgrado { get; set; }
    public Guid IdPublico { get; set; }
    public string Descripcion { get; set; } = null!;
    public int Orden { get; set; }
    public bool Estado { get; set; }
}

namespace Domain.Entities.Sad.Sp;

public sealed class SpRecEncuestaIssEstadisticasMuestraResult
{
    public string Cod_Catedra { get; set; } = null!;
    public string Cod_Seccion { get; set; } = null!;
    public string Periodo { get; set; } = null!;
    public int? Inscritos { get; set; }
    public int? Contestadas { get; set; }
    public double? Porcentaje { get; set; }
    public int? Faltantes { get; set; }
}

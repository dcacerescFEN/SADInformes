namespace Domain.Entities.Sad.Sp;

public sealed class SpRecEvaluacionIssEstadisticasResult
{
    public int Id_Pregunta { get; set; }
    public string? Pregunta_descripcion { get; set; }
    public int Alternativa1 { get; set; }
    public int Alternativa2 { get; set; }
    public int Alternativa3 { get; set; }
    public int Alternativa4 { get; set; }
    public int Alternativa5 { get; set; }
    public double? Promedio { get; set; }
    public int Total { get; set; }
}

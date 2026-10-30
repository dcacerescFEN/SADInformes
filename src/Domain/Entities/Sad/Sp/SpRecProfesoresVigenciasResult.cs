namespace Domain.Entities.Sad.Sp;

/// <summary>Resultado de <c>dbo.spRec_ProfesoresVigencias</c>.</summary>
public sealed class SpRecProfesoresVigenciasResult
{
    public long? ID { get; set; }
    public string? Año { get; set; }
    public string Periodo { get; set; } = null!;
    public string Cod_Catedra { get; set; } = null!;
    public string? Nom_Catedra { get; set; }
    public string Cod_Seccion { get; set; } = null!;
    public string? Tipo_Alumno { get; set; }
    public string? Modalidad { get; set; }
    public string? Promocion { get; set; }
    public string Cod_Persona { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? Mail1 { get; set; }
    public double? HorasDedicadas { get; set; }
    public bool Vigencia { get; set; }
}

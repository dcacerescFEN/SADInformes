namespace Domain.Entities.Sad.Sp;

/// <summary>Resultado de <c>dbo.spRec_CatedrasAreas_ReportePostgrado</c>.</summary>
public sealed class SpRecCatedrasAreasReportePostgradoResult
{
    public long? ID { get; set; }
    public string? descripcion { get; set; }
    public string? area { get; set; }
    public string? periodo { get; set; }
    public string? cod_catedra { get; set; }
    public string? cod_seccion { get; set; }
    public int? Creditos { get; set; }
    public string? Nombre_Profesor { get; set; }
    public string? Cod_Persona { get; set; }
    public string? AQPQ { get; set; }
    public double? HorasDedicadas { get; set; }
    public string? Inicio { get; set; }
    public string? Termino { get; set; }
    public string? Promocion { get; set; }
    public string? Tipo_Profesor { get; set; }
    public string? nom_catedra { get; set; }
    public string? TipoCatedra { get; set; }
    public string? Programa { get; set; }
    public string? Modalidad { get; set; }
    public string? Confirmado { get; set; }
    public string? Tipo_Horario { get; set; }
    public int? Alumnos_Totales { get; set; }
    public int? Alumnos_Postgrado { get; set; }
    public int? Alumnos_Pregrado { get; set; }
    public string? Promedio_Ev_Docente { get; set; }
    public string? FacultyQualificationSA_PA_SP_IP { get; set; }
    public string? Observaciones_Curso { get; set; }
    public string? Usuario_Observ { get; set; }
}

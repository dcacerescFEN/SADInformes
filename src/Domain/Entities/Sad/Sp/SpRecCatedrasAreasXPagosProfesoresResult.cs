namespace Domain.Entities.Sad.Sp;

/// <summary>Resultado de <c>dbo.spRec_CatedrasAreasXPagosProfesores</c>.</summary>
public sealed class SpRecCatedrasAreasXPagosProfesoresResult
{
    public long? ID { get; set; }
    public string? COD { get; set; }
    public string? descripcion { get; set; }
    public string? area { get; set; }
    public string? periodo { get; set; }
    public string? cod_catedra { get; set; }
    public string? cod_seccion { get; set; }
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
    public string? Existe_En_Vista { get; set; }
    public decimal? Escenario1Proyectado { get; set; }
    public decimal? Escenario1Real { get; set; }
    public decimal? Escenario2Proyectado { get; set; }
    public decimal? Escenario2Real { get; set; }
    public decimal? Escenario3Proyectado { get; set; }
    public decimal? Escenario3Real { get; set; }
    public string? Existe_En_Sigma { get; set; }
    public DateTime? Fecha_Confirmacion { get; set; }
    public string? Usuario_Confirmacion { get; set; }
    public string? Observaciones_Curso { get; set; }
    public string? Usuario_Observ { get; set; }
}

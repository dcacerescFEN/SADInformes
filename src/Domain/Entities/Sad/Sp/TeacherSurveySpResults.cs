namespace Domain.Entities.Sad.Sp;

/// <summary>
/// Resultado de <c>dbo.spRec_Encuesta_DocenteXCurso</c>. Los tipos replican las
/// columnas reales: P1 del curso son float y P1 del profesor son int.
/// </summary>
public sealed class SpRecEncuestaDocenteXCursoResult
{
    public string Periodo { get; set; } = null!;
    public string Curso { get; set; } = null!;
    public string? Catedra { get; set; }
    public string? Cod_Profesor { get; set; }
    public string? Nombre_Profesor { get; set; }
    public string? Ayudantes { get; set; }
    public double? A1 { get; set; }
    public double? A2 { get; set; }
    public double? A3 { get; set; }
    public double? A4 { get; set; }
    public string? AObservaciones { get; set; }
    public int? B1 { get; set; }
    public int? B2 { get; set; }
    public int? B3 { get; set; }
    public int? B4 { get; set; }
    public int? B5 { get; set; }
    public int? B6 { get; set; }
    public int? B7 { get; set; }
    public int? B8 { get; set; }
    public int? B9 { get; set; }
    public int? B10 { get; set; }
    public int? B11 { get; set; }
    public int? B12 { get; set; }
    public int? B13 { get; set; }
    public double? BNota { get; set; }
    public string? BObservaciones { get; set; }
}

/// <summary>Resultado de <c>dbo.spRec_PeriodosAcademicosProgramacion</c>.</summary>
public sealed class SpRecPeriodosAcademicosProgramacionResult
{
    public string Periodo { get; set; } = null!;
    public string? PeriodoLargo { get; set; }
    public string? Inscripcion { get; set; }
}

/// <summary>Resultado de <c>dbo.spRec_TiposAlumno</c>.</summary>
public sealed class SpRecTiposAlumnoResult
{
    public string Tipo { get; set; } = null!;
    public string? Nombre { get; set; }
    public int? Orden { get; set; }
}

/// <summary>Resultado de <c>dbo.spRec_TiposAlumnosModalidades</c>.</summary>
public sealed class SpRecTiposAlumnosModalidadesResult
{
    public string Tipo { get; set; } = null!;
    public string Modalidad { get; set; } = null!;
}

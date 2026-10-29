namespace Domain.Entities.Sad.Sp;

/// <summary>Resultado de <c>dbo.spRec_AlumnosMatriculadosPostgrado</c>.</summary>
public sealed class SpRecAlumnosMatriculadosPostgradoResult
{
    public string Cod_Alumno { get; set; } = null!;
    public string Sem_IngresoDecreto { get; set; } = null!;
    public string? Rut { get; set; }
    public string Tipo_Alumno { get; set; } = null!;
    public string? Modalidad { get; set; }
    public string Cod_SitAcademica { get; set; } = null!;
    public string? Nombres { get; set; }
    public string? Apellidos { get; set; }
    public string? Email_Personal { get; set; }
    public string? Email_Fen { get; set; }
    public string? Email_laboral { get; set; }
    public string? Celular { get; set; }
    public string? Tel_laboral { get; set; }
    public string? NomEmpresa { get; set; }
    public string? CargoEmpresa { get; set; }
}

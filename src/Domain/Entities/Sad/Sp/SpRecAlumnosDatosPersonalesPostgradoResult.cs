namespace Domain.Entities.Sad.Sp;

public sealed class SpRecAlumnosDatosPersonalesPostgradoResult
{
    public int ID_Alumno { get; set; }
    public string Cod_Alumno { get; set; } = null!;
    public string Sem_IngresoDecreto { get; set; } = null!;
    public string Cod_Persona { get; set; } = null!;
    public string Tipo_Alumno { get; set; } = null!;
    public string? Modalidad { get; set; }
    public string Cod_SitAcademica { get; set; } = null!;
    public string? Motivo { get; set; }
    public string? Nombres { get; set; }
    public string? Apellidos { get; set; }
    public string? Email_Personal { get; set; }
    public string? Email_Fen { get; set; }
    public string? Email_laboral { get; set; }
    public string? Celular { get; set; }
    public string? Tel_laboral { get; set; }
    public string? NomEmpresa { get; set; }
    public string? CargoEmpresa { get; set; }
    public DateTime? Fec_Nacimiento { get; set; }
    public string Nacionalidad { get; set; } = null!;
    public string? Sexo { get; set; }
    public string? CarreraAnterior { get; set; }
    public string Universidad_de_Procedencia { get; set; } = null!;
    public string? FechaResolucion { get; set; }
    public string Fecha_Examen { get; set; } = null!;
    public string Ultimo_Periodo_Inscripcion_Curso { get; set; } = null!;
    public string Periodo_Examen { get; set; } = null!;
}

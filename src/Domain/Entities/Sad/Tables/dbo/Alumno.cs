namespace Domain.Entities.Sad.Tables.dbo;

public sealed class Alumno
{
    public string Matricula { get; set; } = "";
    public string SemIngresoDecreto { get; set; } = "";
    public string CodAlumno { get; set; } = "";
    public string CodPersona { get; set; } = "";
    public string TipoAlumno { get; set; } = "";
    public string? Modalidad { get; set; }
}

namespace Application.Reports.Postgraduate.SadVtiUpdates.Queries.GetSadVtiUpdates;
public sealed record SadVtiUpdateDTO(string Estado, string Descripcion, string CodPersona,
    string CodAlumnoAnterior, string CodAlumno, string? NombreCompleto, string TipoAlumno,
    string? Modalidad, string? CodSitAcademica, string? Matricula, DateTime FechaRegistro, string? MensajeError);

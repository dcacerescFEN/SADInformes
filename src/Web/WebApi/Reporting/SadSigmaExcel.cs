using Application.Reports.Postgraduate.SadSigma.Queries.GetSadSigma;

namespace WebApi.Reporting;

public static class SadSigmaExcel
{
    private static readonly string[] Headers =
    [
        "ID", "COD", "descripcion", "area", "periodo", "cod_catedra", "cod_seccion",
        "Nombre_Profesor", "Cod_Persona", "AQPQ", "HorasDedicadas", "Inicio", "Termino",
        "Promocion", "Tipo_Profesor", "nom_catedra", "TipoCatedra", "Programa", "Modalidad",
        "Confirmado", "Tipo_Horario", "Alumnos_Totales", "Alumnos_Postgrado", "Alumnos_Pregrado",
        "Promedio_Ev_Docente", "Existe_En_Vista", "Escenario1Proyectado", "Escenario1Real",
        "Escenario2Proyectado", "Escenario2Real", "Escenario3Proyectado", "Escenario3Real",
        "Existe_En_Sigma", "Fecha_Confirmacion", "Usuario_Confirmacion", "Observaciones_Curso",
        "Usuario_Observ", "Creado_En_Sigma",
    ];

    public static byte[] Create(IReadOnlyList<SadSigmaDTO> rows) =>
        TextWorkbook.Create("SAD Sigma", Headers, rows.Select(row => new string?[]
        {
            row.Id, row.Cod, row.Description, row.Area, row.Period, row.CourseCode, row.Section,
            row.TeacherName, row.PersonCode, row.Aqpq, row.DedicatedHours, row.Start, row.End,
            row.Promotion, row.TeacherType, row.CourseName, row.CourseType, row.Program, row.Modality,
            row.Confirmed, row.ScheduleType, row.TotalStudents, row.PostgraduateStudents,
            row.UndergraduateStudents, row.TeachingEvaluationAverage, row.ExistsInVista,
            row.Scenario1Projected, row.Scenario1Actual, row.Scenario2Projected, row.Scenario2Actual,
            row.Scenario3Projected, row.Scenario3Actual, row.ExistsInSigma,
            row.ConfirmationDate, row.ConfirmationUser, row.CourseObservations, row.ObservationUser,
            row.CreatedInSigma,
        }));
}

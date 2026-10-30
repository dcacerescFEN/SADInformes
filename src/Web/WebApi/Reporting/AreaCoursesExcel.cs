using Application.Reports.Postgraduate.AreaCourses.Queries.GetAreaCourses;

namespace WebApi.Reporting;

public static class AreaCoursesExcel
{
    private static readonly string[] Headers =
    [
        "ID", "Area_Catedra", "Area", "Periodo", "Cod_Catedra", "Seccion", "Creditos",
        "Nombre_Profesor", "Cod_Persona", "Tipo_Profesor", "Inicio_Catedra", "Termino_Catedra",
        "Tipo_Alumno", "Modalidad", "Promocion", "Nombre_Catedra", "TipoCatedra", "Tipo_Horario",
        "AQPQ", "Categorización_Docente", "Confirmado", "HorasDedicadas", "Alumnos_Totales",
        "Alumnos_Postgrado", "Alumnos_Pregrado", "Promedio_Ev_Docente", "Observaciones Curso",
        "Usuario_Observ",
    ];

    public static byte[] Create(IReadOnlyList<AreaCoursesDTO> rows) =>
        TextWorkbook.Create("Areas y catedras", Headers, rows.Select(row => new string?[]
        {
            row.Id, row.Description, row.Area, row.Period, row.CourseCode, row.Section,
            row.Credits, row.TeacherName, row.PersonCode, row.TeacherType, row.Start, row.End,
            row.Program, row.Modality, row.Promotion, row.CourseName, row.CourseType,
            row.ScheduleType, row.Aqpq, row.TeachingCategory, row.Confirmed, row.DedicatedHours,
            row.TotalStudents, row.PostgraduateStudents, row.UndergraduateStudents,
            row.TeachingEvaluationAverage, row.CourseObservations, row.ObservationUser,
        }));
}

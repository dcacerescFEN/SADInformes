using Application.Reports.AcademicEnrollment.EnrollmentTeachingSchedule.Queries.GetEnrollmentTeachingSchedule;

namespace WebApi.Reporting;

public static class EnrollmentTeachingScheduleExcel
{
    private static readonly string[] Headers =
    [
        "Curso", "Cátedra", "Profesor(es)", "Clases", "Ayudantías", "Sala",
        "Sala Ayudantías", "Vacantes", "Inscritos", "Área", "Rut Profesor",
    ];

    public static byte[] Create(IReadOnlyList<EnrollmentTeachingScheduleRowDTO> rows) =>
        TextWorkbook.Create("Programación Docente", Headers, rows.Select(row => new string?[]
        {
            row.Course, row.CourseName, row.Teachers, row.Classes, row.Assistantships,
            row.Room, row.AssistantRoom, row.Vacancies, row.Enrolled, row.Area, row.TeacherId,
        }));
}

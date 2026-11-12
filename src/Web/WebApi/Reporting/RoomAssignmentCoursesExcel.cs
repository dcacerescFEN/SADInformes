using Application.Reports.AcademicEnrollment.RoomAssignmentCourses.Queries.GetRoomAssignmentCourses;

namespace WebApi.Reporting;

public static class RoomAssignmentCoursesExcel
{
    private static readonly string[] Headers =
    [
        "Período", "Cátedra", "Sección", "Eliminado", "Clases", "Tipo",
        "Vacantes Ofrecidas", "Vacantes Ocupadas",
        "Profesor 1", "Profesor 2", "Profesor 3", "Profesor 4", "Profesor 5",
        "Profesor 6", "Profesor 7", "Profesor 8", "Profesor 9", "Profesor 10",
    ];

    public static byte[] Create(IReadOnlyList<RoomAssignmentCourseRowDTO> rows) =>
        TextWorkbook.Create("Cursos Asignación Salas", Headers, rows.Select(row => new string?[]
        {
            row.Period, row.CourseCode, row.Section, row.Deleted, row.Classes, row.Type,
            row.OfferedVacancies, row.OccupiedVacancies,
            row.Teacher1, row.Teacher2, row.Teacher3, row.Teacher4, row.Teacher5,
            row.Teacher6, row.Teacher7, row.Teacher8, row.Teacher9, row.Teacher10,
        }));
}

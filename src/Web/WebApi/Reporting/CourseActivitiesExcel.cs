using Application.Reports.Postgraduate.CourseActivities.Queries.GetActivities;

namespace WebApi.Reporting;

public static class CourseActivitiesExcel
{
    private static readonly string[] Headers =
    [
        "Id", "Tipo", "Catedra", "Desde", "Hasta", "Periodo", "Cod_Catedra", "Cod_Seccion",
        "Programa", "Modalidad", "Promocion", "Sala", "Capacidad_Sala", "Estudiantes_Inscritos",
        "Tipo_Sala", "Es_Sala_Hibrida", "Fecha_Ingreso", "UsuarioIngreso", "Suspendida",
        "Fecha_Suspención", "Usuario_Suspención", "Observaciones",
    ];

    public static byte[] Create(IReadOnlyList<ActivityRowDTO> rows) =>
        TextWorkbook.Create("Actividades Cursos", Headers, rows.Select(row => new string?[]
        {
            row.Id, row.Type, row.Course, row.From, row.To, row.Period, row.CourseCode, row.Section,
            row.Program, row.Modality, row.Promotion, row.Room, row.RoomCapacity, row.EnrolledStudents,
            row.RoomType, row.HybridRoom, row.EntryDate, row.EntryUser, row.Suspended,
            row.SuspensionDate, row.SuspensionUser, row.Observations,
        }));
}

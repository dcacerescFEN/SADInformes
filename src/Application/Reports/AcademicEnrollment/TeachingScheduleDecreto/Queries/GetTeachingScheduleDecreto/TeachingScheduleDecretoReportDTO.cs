namespace Application.Reports.AcademicEnrollment.TeachingScheduleDecreto.Queries.GetTeachingScheduleDecreto;

public sealed record TeachingScheduleDecretoReportDTO(
    string Title,
    string Slug,
    string Period,
    TeachingScheduleDecretoRowDTO[] Rows,
    int Total);

namespace Application.Reports.Postgraduate.TeachingSchedule.Queries.GetTeachingSchedule;

public sealed record TeachingScheduleReportDTO(IReadOnlyList<TeachingScheduleDTO> Rows, int Total);

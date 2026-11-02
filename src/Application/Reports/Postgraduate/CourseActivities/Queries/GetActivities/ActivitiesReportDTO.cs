namespace Application.Reports.Postgraduate.CourseActivities.Queries.GetActivities;

public sealed record ActivitiesReportDTO(string Year, ActivityRowDTO[] Rows, int Total);

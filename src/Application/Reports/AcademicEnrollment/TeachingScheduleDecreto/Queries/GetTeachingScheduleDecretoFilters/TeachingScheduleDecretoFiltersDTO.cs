namespace Application.Reports.AcademicEnrollment.TeachingScheduleDecreto.Queries.GetTeachingScheduleDecretoFilters;

public sealed record TeachingScheduleDecretoFiltersDTO(
    string Title,
    TeachingScheduleDecretoOptionDTO[] Periods);

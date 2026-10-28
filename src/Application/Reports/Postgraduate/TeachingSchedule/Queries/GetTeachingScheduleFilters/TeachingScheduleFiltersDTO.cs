namespace Application.Reports.Postgraduate.TeachingSchedule.Queries.GetTeachingScheduleFilters;

public sealed record TeachingScheduleFiltersDTO(
    IReadOnlyList<TeachingScheduleOptionDTO> PeriodTypes,
    IReadOnlyList<TeachingScheduleOptionDTO> Periods,
    IReadOnlyList<TeachingScheduleOptionDTO> Programs,
    IReadOnlyList<string> Modalities,
    IReadOnlyList<TeachingScheduleOptionDTO> Promotions);

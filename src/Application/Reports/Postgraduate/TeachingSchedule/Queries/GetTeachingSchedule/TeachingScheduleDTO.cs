namespace Application.Reports.Postgraduate.TeachingSchedule.Queries.GetTeachingSchedule;

public sealed record TeachingScheduleDTO(
    string Period, string PeriodLabel, string Course, string Name, string Type, int? Units, string Teachers, string Classes);

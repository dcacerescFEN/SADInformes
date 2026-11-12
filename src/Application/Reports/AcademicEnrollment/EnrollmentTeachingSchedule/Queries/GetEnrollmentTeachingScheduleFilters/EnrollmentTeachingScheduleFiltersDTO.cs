namespace Application.Reports.AcademicEnrollment.EnrollmentTeachingSchedule.Queries.GetEnrollmentTeachingScheduleFilters;

public sealed record EnrollmentTeachingScheduleFiltersDTO(
    EnrollmentTeachingScheduleOptionDTO[] Periods,
    EnrollmentTeachingScheduleOptionDTO[] Areas);

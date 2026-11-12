namespace Application.Reports.AcademicEnrollment.EnrollmentTeachingSchedule.Queries.GetEnrollmentTeachingSchedule;

public sealed record EnrollmentTeachingScheduleReportDTO(
    string Period,
    string School,
    string Area,
    EnrollmentTeachingScheduleRowDTO[] Rows,
    int Total);

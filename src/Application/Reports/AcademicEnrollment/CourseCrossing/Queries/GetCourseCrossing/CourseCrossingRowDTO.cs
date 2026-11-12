namespace Application.Reports.AcademicEnrollment.CourseCrossing.Queries.GetCourseCrossing;

public sealed record CourseCrossingRowDTO(
    string Course1,
    string CourseName1,
    string Course2,
    string CourseName2,
    string IcLimit,
    string IicgLimit,
    string OtherLimit,
    string TotalLimit);

namespace Application.Reports.AcademicEnrollment.PostulationResults.Queries.GetPostulationResultsByCourse;

public sealed record PostulationResultRowDTO(
    string Course,
    string CourseName,
    string StudentType,
    int OriginalVacancies,
    int OtherAccepted,
    int ProcessVacancies,
    int Postulations,
    int Accepted,
    int AcceptedProcess,
    int TotalOccupied,
    int AvailableVacancies,
    int VacancyRejections,
    int RealVacancyRejections,
    int ScheduleCapRejections,
    int LoadRejections,
    int RequirementRejections,
    int OtherRejections);

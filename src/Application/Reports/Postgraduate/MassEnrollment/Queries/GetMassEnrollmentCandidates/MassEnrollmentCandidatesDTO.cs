namespace Application.Reports.Postgraduate.MassEnrollment.Queries.GetMassEnrollmentCandidates;

public sealed record MassEnrollmentCandidatesDTO(
    IReadOnlyList<MassEnrollmentCandidateDTO> Rows, int Total, bool CanEnroll = true)
{
    public const string NoVacanciesMessage = "El curso ya no tiene vacantes.";
    public string? AvailabilityMessage => CanEnroll ? null : NoVacanciesMessage;
}

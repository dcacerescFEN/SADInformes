namespace Application.Reports.Requests.UnresolvedPermanence.Queries.GetUnresolvedPermanence;

public sealed record UnresolvedPermanenceReportDTO(
    string Period,
    string Career,
    IReadOnlyList<UnresolvedPermanenceRowDTO> Rows);

public sealed record UnresolvedPermanenceRowDTO(
    string PersonCode,
    string StudentCode,
    string Name,
    string Situation,
    string EntryData,
    string Credit,
    string Performance,
    string Semesters,
    string FailedCore,
    string EnrolledUnits,
    IReadOnlyList<PreviousRequestDTO> PreviousRequests,
    IReadOnlyList<CurrentRequestDTO> CurrentRequests,
    IReadOnlyList<EliminationCauseDTO> EliminationCauses);

public sealed record PreviousRequestDTO(
    string Period, string Type, string Resolution, string Authority, bool Approved);

public sealed record CurrentRequestDTO(string Period, string Type);

public sealed record EliminationCauseDTO(string Text, string Severity);

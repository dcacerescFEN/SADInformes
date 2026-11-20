namespace Application.Reports.Regulation.ExpedientesXPeriodo.Queries.GetOpenedFilesByPeriod;

public sealed record OpenedFilesByPeriodReportDTO(
    string From,
    string To,
    IReadOnlyList<OpenedFilesByPeriodRowDTO> Rows);

public sealed record OpenedFilesByPeriodRowDTO(
    string StudentCode,
    string TitleCode,
    string PersonCode,
    string Name,
    string DegreeTitle,
    string Career,
    string Situation,
    string Promotion,
    string OpeningDate,
    string Phone,
    string FenEmail,
    string PersonalEmail);

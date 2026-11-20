namespace Application.Reports.Regulation.ExpedientesNoAbiertos.Queries.GetUnopenedFiles;

public sealed record UnopenedFilesReportDTO(
    IReadOnlyList<UnopenedFilesRowDTO> Rows);

public sealed record UnopenedFilesRowDTO(
    string StudentCode,
    string TitleCode,
    string PersonCode,
    string Name,
    string DegreeTitle,
    string Career,
    string Situation,
    string Promotion,
    string Phone,
    string FenEmail,
    string PersonalEmail);

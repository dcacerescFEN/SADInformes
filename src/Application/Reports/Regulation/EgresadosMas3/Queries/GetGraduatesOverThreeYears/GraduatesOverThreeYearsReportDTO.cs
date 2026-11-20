namespace Application.Reports.Regulation.EgresadosMas3.Queries.GetGraduatesOverThreeYears;

public sealed record GraduatesOverThreeYearsReportDTO(
    string Period,
    IReadOnlyList<GraduatesOverThreeYearsRowDTO> Rows);

public sealed record GraduatesOverThreeYearsRowDTO(
    string StudentCode,
    string Name,
    string StudentType,
    string Decree,
    string DecreeDescription,
    string Mention,
    string GraduatedFrom,
    string Situation,
    string EntrySemester,
    string Promotion);

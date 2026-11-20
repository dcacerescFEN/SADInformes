namespace Application.Reports.Regulation.CuadroHonor.Queries.GetHonorRoll;

public sealed record HonorRollReportDTO(
    string Career,
    string EntryYear,
    string HonorYear,
    IReadOnlyList<HonorRollRowDTO> Rows);

public sealed record HonorRollRowDTO(
    int Position,
    string Name,
    string Rut,
    string StudentCode,
    string EntrySemester,
    string Situation,
    string Average,
    string Percentile);

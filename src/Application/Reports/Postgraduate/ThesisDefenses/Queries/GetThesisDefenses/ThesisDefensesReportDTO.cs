namespace Application.Reports.Postgraduate.ThesisDefenses.Queries.GetThesisDefenses;

public sealed record ThesisDefensesReportDTO(
    CoordinationDefenseDTO[] Coordination,
    TreasuryDefenseDTO[] Treasury,
    int CoordinationTotal,
    int TreasuryTotal);

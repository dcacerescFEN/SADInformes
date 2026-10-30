namespace Application.Reports.Postgraduate.ThesisDefenses.Queries.GetThesisDefenses;

public sealed record TreasuryDefenseDTO(
    string Period, string CourseCode, string Section, string ProfessorCode, string ProfessorName,
    string Type, string StudentCode, string Grade, string StudentName, string Program,
    string Modality, string EntryYear, string ThesisTopic, string ExamDate, string ExamStatus,
    string CostCenter, string ResolutionDate);

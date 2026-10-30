namespace Application.Reports.Postgraduate.ThesisDefenses.Queries.GetThesisDefenses;

public sealed record CoordinationDefenseDTO(
    string StudentCode, string StudentName, string Program, string Modality, string EntryYear,
    string ExamDate, string Period, string CourseCode, string Section, string Grade,
    string ExamStatus, string ThesisTopic, string Guide, string CoGuide, string Informant1,
    string Informant2, string Informant3, string CostCenter, string DefenseYear,
    string ResolutionDate);

namespace Application.Reports.Postgraduate.EntrySurveyStatistics.Queries.GetEntrySurveyStatistics;

public sealed record EntrySurveyDTO(
    string CodAlumno, string StudentType, string Modality, string Q1,
    string Q2_1, string Q2_2, string Q2_3, string Q2_4, string Q2_5, string Q2_6, string Q2_7, string Q2_8,
    string Q3_1, string Q3_2, string Q3_3, string Q3_4, string Q3_5, string Q3_6, string Q3_7,
    string Q4_1, string Q4_2, string Q4_3, string Q4_4, string Q4_5, string Q4_6, string Q4_7, string Q4_8,
    string Q5, string Q5Otro, string Q6, string Q7, string Q8, string Q9, string Q9Otro, string Q10,
    string Q11, string Q11Otro, string Q12, string Q13, string Q14, string Q15, string Q16, string Q17,
    string Fecha);

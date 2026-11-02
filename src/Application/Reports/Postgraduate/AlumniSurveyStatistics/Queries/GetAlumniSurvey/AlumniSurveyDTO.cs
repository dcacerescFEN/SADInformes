namespace Application.Reports.Postgraduate.AlumniSurveyStatistics.Queries.GetAlumniSurvey;

public sealed record AlumniSurveyDTO(
    string CodAlumno, string P1, string P1_1, string P1_2, string P1_3, string P1_4,
    string P2, string P2_1, string P2_2, string P2_3, string P2_4,
    string P3_1, string P3_2, string P3_3, string P3_4, string P4, string P5, string P6, string P7,
    string P8_1, string P8_2, string P9, string P10, string P10_1, string P10_2, string P10_3,
    string P10_4, string P11, string P12, string P13, string P14, string Fecha);

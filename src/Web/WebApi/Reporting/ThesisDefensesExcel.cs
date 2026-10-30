using Application.Reports.Postgraduate.ThesisDefenses.Queries.GetThesisDefenses;

namespace WebApi.Reporting;

public static class ThesisDefensesExcel
{
    private static readonly string[] CoordinationHeaders =
    [
        "Cod_Alumno", "Nombre", "Programa", "Modalidad", "Año Ingreso", "Fecha Examen",
        "Periodo", "Cod_Catedra", "Cod_Seccion", "Nota", "Estado_Examen",
        "Tema/Nombre Tésis", "GUIA", "COGUIA", "INFORMANTE1", "INFORMANTE2", "INFORMANTE3",
        "Centro Costo", "Año Defensa", "Fecha Resolución",
    ];

    private static readonly string[] TreasuryHeaders =
    [
        "Periodo", "Cod_Catedra", "Cod_Seccion", "Cod_Profesor", "Nombre Profesor", "Tipo",
        "Cod_Alumno", "Nota", "Estudiante", "Programa", "Modalidad", "Año Ingreso",
        "Tema/Nombre Tésis", "Fecha_Examen", "Estado_Examen", "Centro Costo", "Fecha Resolución",
    ];

    public static byte[] Coordination(IReadOnlyList<CoordinationDefenseDTO> rows) =>
        TextWorkbook.Create("InformeComision", CoordinationHeaders, rows.Select(row => new string?[]
        {
            row.StudentCode, row.StudentName, row.Program, row.Modality, row.EntryYear,
            row.ExamDate, row.Period, row.CourseCode, row.Section, row.Grade, row.ExamStatus,
            row.ThesisTopic, row.Guide, row.CoGuide, row.Informant1, row.Informant2,
            row.Informant3, row.CostCenter, row.DefenseYear, row.ResolutionDate,
        }));

    public static byte[] Treasury(IReadOnlyList<TreasuryDefenseDTO> rows) =>
        TextWorkbook.Create("InformeTesorería", TreasuryHeaders, rows.Select(row => new string?[]
        {
            row.Period, row.CourseCode, row.Section, row.ProfessorCode, row.ProfessorName,
            row.Type, row.StudentCode, row.Grade, row.StudentName, row.Program, row.Modality,
            row.EntryYear, row.ThesisTopic, row.ExamDate, row.ExamStatus, row.CostCenter,
            row.ResolutionDate,
        }));
}

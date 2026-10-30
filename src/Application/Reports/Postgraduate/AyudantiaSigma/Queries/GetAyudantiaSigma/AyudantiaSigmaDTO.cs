namespace Application.Reports.Postgraduate.AyudantiaSigma.Queries.GetAyudantiaSigma;

public sealed record AyudantiaSigmaDTO(
    string Cod, string Nombre1, string Nombre2, string Apellido1, string Apellido2, string Rut,
    string Email, string CourseCode, string CourseName, string Section, string Start, string End,
    string Year, string Period, string StudentType, string Modality, string Promotion,
    string CostCenter, string PaymentType, string Amount, string ExistsInSigma, string SigmaDate,
    string Step, string Flag, string RemunerationId);

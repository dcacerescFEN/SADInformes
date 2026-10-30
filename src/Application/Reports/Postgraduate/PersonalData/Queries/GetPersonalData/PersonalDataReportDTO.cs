namespace Application.Reports.Postgraduate.PersonalData.Queries.GetPersonalData;

public sealed record PersonalDataReportDTO(PersonalDataDTO[] Rows, int Total);

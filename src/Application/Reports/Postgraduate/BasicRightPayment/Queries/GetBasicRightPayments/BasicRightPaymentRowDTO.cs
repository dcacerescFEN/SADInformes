namespace Application.Reports.Postgraduate.BasicRightPayment.Queries.GetBasicRightPayments;

public sealed record BasicRightPaymentRowDTO(
    int Position, string Name, string Rut, string StudentCode, string Entry, string Situation,
    decimal? Average, bool Paid, string PhotoUrl);

namespace Application.Reports.Postgraduate.BasicRightPayment.Queries.GetBasicRightPayments;

public sealed record BasicRightPaymentsDTO(
    int? Amount, BasicRightPaymentRowDTO[] Rows, int Total, bool CanManage);

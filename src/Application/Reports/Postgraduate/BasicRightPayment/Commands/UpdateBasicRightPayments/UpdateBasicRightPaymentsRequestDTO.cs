namespace Application.Reports.Postgraduate.BasicRightPayment.Commands.UpdateBasicRightPayments;

public sealed record UpdateBasicRightPaymentsRequestDTO(
    string PaymentYear, BasicRightPaymentChangeDTO[] Rows);

namespace Application.Reports.Postgraduate.BasicRightPayment.Queries.GetBasicRightPaymentFilters;

public sealed record BasicRightPaymentFiltersDTO(
    BasicRightOptionDTO[] StudentTypes, BasicRightOptionDTO[] Modalities,
    BasicRightOptionDTO[] Years, BasicRightOptionDTO[] PaymentStatuses);

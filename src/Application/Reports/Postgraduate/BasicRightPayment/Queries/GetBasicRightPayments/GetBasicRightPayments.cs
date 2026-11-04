using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Reports.Postgraduate.BasicRightPayment.Queries.GetBasicRightPayments;

public sealed record GetBasicRightPayments(
    string? StudentType, string? Modality, string? EntryYear, string? PaymentYear, string? PaymentStatus,
    bool CanManage)
    : IRequest<ApiResponse<BasicRightPaymentsDTO>>;

public sealed class GetBasicRightPaymentsHandler(
    ISadContextProcedures procedures, ISadContext context, IStudentPhotoSettings photoSettings,
    IApiResponseService responses)
    : IRequestHandler<GetBasicRightPayments, ApiResponse<BasicRightPaymentsDTO>>
{
    public async Task<ApiResponse<BasicRightPaymentsDTO>> Handle(
        GetBasicRightPayments request, CancellationToken ct)
    {
        var filters = await BasicRightPaymentCatalog.ReadAsync(procedures, request.StudentType, ct);
        var type = BasicRightPaymentCatalog.Value(request.StudentType, "Seleccione un tipo de alumno.");
        var modality = BasicRightPaymentCatalog.Value(request.Modality, "Seleccione una modalidad.");
        var entryYear = BasicRightPaymentCatalog.Year(request.EntryYear, "Seleccione un año de ingreso.");
        var paymentYear = BasicRightPaymentCatalog.Year(request.PaymentYear, "Seleccione un año de pago.");
        var status = BasicRightPaymentCatalog.Value(request.PaymentStatus, "Seleccione una situación de pago.");
        if (!filters.StudentTypes.Any(row => row.Value == type)
            || !filters.Modalities.Any(row => row.Value == modality)
            || !filters.Years.Any(row => row.Value == entryYear)
            || !filters.Years.Any(row => row.Value == paymentYear)
            || !filters.PaymentStatuses.Any(row => row.Value == status))
            throw new ArgumentException("Los filtros seleccionados no son válidos.");
        var amount = await context.PagosDerechoBasicoAnualMontos.AsNoTracking()
            .Where(row => row.Año == paymentYear).Select(row => (int?)row.Monto).FirstOrDefaultAsync(ct);
        var data = await procedures.SpRec_AlumnosXPago_Derecho_BasicoAsync(
            type, modality, entryYear, paymentYear, status, cancellationToken: ct) ?? [];
        var codes = data.Select(row => Text(row.Código)).Where(code => code.Length > 0).Distinct().ToArray();
        var personCodes = codes.Length == 0
            ? new Dictionary<string, string>()
            : await context.Alumnos.AsNoTracking()
                .Where(row => codes.Contains(row.CodAlumno))
                .Select(row => new { row.CodAlumno, row.CodPersona })
                .ToDictionaryAsync(row => row.CodAlumno, row => row.CodPersona, ct);
        var rows = data.Select(row => new BasicRightPaymentRowDTO(
            row.Posicion, Text(row.Nombre), Text(row.Rut), Text(row.Código), Text(row.Ingreso),
            Text(row.Situación), row.Promedio, row.Pago,
            Photo(photoSettings.BaseUrl, personCodes.GetValueOrDefault(Text(row.Código))))).ToArray();
        return responses.Success(new BasicRightPaymentsDTO(amount, rows, rows.Length, request.CanManage));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
    private static string Photo(string baseUrl, string? personCode) => string.IsNullOrWhiteSpace(personCode)
        ? baseUrl + "perfilNN.jpg"
        : $"{baseUrl}{personCode.Trim()}.jpg";
}

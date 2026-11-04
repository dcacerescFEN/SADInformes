using System.Data;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Reports.Postgraduate.BasicRightPayment.Commands.UpdateBasicRightPayments;

public sealed record UpdateBasicRightPayments(UpdateBasicRightPaymentsRequestDTO Request, string User)
    : IRequest<ApiResponse<BasicRightPaymentUpdateResultDTO>>;

public sealed class UpdateBasicRightPaymentsHandler(
    ISadContext context, ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<UpdateBasicRightPayments, ApiResponse<BasicRightPaymentUpdateResultDTO>>
{
    public async Task<ApiResponse<BasicRightPaymentUpdateResultDTO>> Handle(
        UpdateBasicRightPayments request, CancellationToken ct)
    {
        var user = request.User?.Trim() ?? "";
        if (user.Length is < 1 or > 20) throw new ArgumentException("No se pudo identificar al usuario.");
        var year = request.Request.PaymentYear?.Trim() ?? "";
        if (year.Length != 4 || !year.All(char.IsAsciiDigit)) throw new ArgumentException("El año de pago no es válido.");
        var rows = request.Request.Rows ?? [];
        if (rows.Length == 0) throw new ArgumentException("No hay cambios de pago para guardar.");
        if (rows.Length > 5000) throw new ArgumentException("El lote de pagos es demasiado grande.");
        var codes = rows.Select(row => row.StudentCode?.Trim() ?? "").ToArray();
        if (codes.Any(code => code.Length is < 1 or > 20) || codes.Distinct(StringComparer.Ordinal).Count() != codes.Length)
            throw new ArgumentException("Los alumnos del lote no son válidos.");
        var amountExists = await context.PagosDerechoBasicoAnualMontos.AsNoTracking()
            .AnyAsync(row => row.Año == year, ct);
        if (!amountExists) throw new ArgumentException("No existe un monto configurado para el año de pago.");
        var existing = await context.Alumnos.AsNoTracking()
            .Where(row => codes.Contains(row.CodAlumno)).Select(row => row.CodAlumno).ToListAsync(ct);
        if (existing.Count != codes.Length) throw new ArgumentException("El lote contiene alumnos inexistentes.");

        await using var transaction = await context.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        foreach (var row in rows)
            await procedures.SpCre_AlumnosXPago_Derecho_BasicoAsync(
                row.StudentCode.Trim(), year, row.Paid, user, ct);
        await transaction.CommitAsync(ct);
        return responses.Success(new BasicRightPaymentUpdateResultDTO(year, rows.Length));
    }
}

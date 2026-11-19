using System.Data;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Requests.RequestsByStatus.Commands.ConfirmRequestsPrinted;

public sealed record ConfirmRequestsPrinted(IReadOnlyList<string>? RequestCodes)
    : IRequest<ApiResponse<int>>;

public sealed class ConfirmRequestsPrintedHandler(
    ISadContext sad, ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<ConfirmRequestsPrinted, ApiResponse<int>>
{
    private const int MaxBatch = 1000;

    public async Task<ApiResponse<int>> Handle(
        ConfirmRequestsPrinted request, CancellationToken cancellationToken)
    {
        var codes = (request.RequestCodes ?? [])
            .Select(code => code?.Trim() ?? "")
            .Where(code => code.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (codes.Length == 0)
            throw new ArgumentException("Seleccione al menos una solicitud.");
        if (codes.Length > MaxBatch)
            throw new ArgumentException("La cantidad de solicitudes supera el máximo permitido.");
        if (codes.Any(code => code.Length > 20))
            throw new ArgumentException("Una solicitud no es válida.");

        await using var transaction = await sad.BeginTransactionAsync(
            IsolationLevel.ReadCommitted, cancellationToken);
        foreach (var code in codes)
        {
            await procedures.SpMod_Solicitud_ImpresaAsync(
                code, cancellationToken: cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return responses.Success(codes.Length);
    }
}

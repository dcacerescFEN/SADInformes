using System.Globalization;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.PermutationResults.Common;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.CourseCrossing.Queries.GetCourseCrossing;

public sealed record GetCourseCrossing(string? Period)
    : IRequest<ApiResponse<CourseCrossingReportDTO>>;

public sealed class GetCourseCrossingHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetCourseCrossing, ApiResponse<CourseCrossingReportDTO>>
{
    public async Task<ApiResponse<CourseCrossingReportDTO>> Handle(
        GetCourseCrossing request, CancellationToken cancellationToken)
    {
        var period = PermutationRules.Period(request.Period);
        var data = await procedures.SpRec_CruceCursosAsync(
            period, cancellationToken: cancellationToken) ?? [];

        var rows = data.Select(row => new CourseCrossingRowDTO(
            Text(row.Curso1),
            Text(row.Catedra1),
            Text(row.Curso2),
            Text(row.Catedra2),
            Number(row.TopesIC),
            Number(row.TopesIICG),
            Number(row.TopesOtros),
            Number(row.TopesTotal))).ToArray();

        return responses.Success(new CourseCrossingReportDTO(period, rows, rows.Length));
    }

    private static string Text(string? value) => value?.Trim() ?? "";

    private static string Number(int? value) =>
        value?.ToString(CultureInfo.InvariantCulture) ?? "";
}

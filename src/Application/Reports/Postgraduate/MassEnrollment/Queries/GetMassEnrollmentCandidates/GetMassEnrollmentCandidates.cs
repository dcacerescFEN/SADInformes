using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.Postgraduate.MassEnrollment.Common;
using Domain.Common;

namespace Application.Reports.Postgraduate.MassEnrollment.Queries.GetMassEnrollmentCandidates;

public sealed record GetMassEnrollmentCandidates(EnrollmentSelection Selection, string User)
    : IRequest<ApiResponse<MassEnrollmentReport>>;

public sealed class GetMassEnrollmentCandidatesHandler(IMassEnrollmentData data, IApiResponseService responses)
    : IRequestHandler<GetMassEnrollmentCandidates, ApiResponse<MassEnrollmentReport>>
{
    public async Task<ApiResponse<MassEnrollmentReport>> Handle(GetMassEnrollmentCandidates request, CancellationToken ct)
    {
        await MassEnrollmentRules.ValidateAvailableAsync(data, request.Selection, request.User, ct);
        var rows = await data.GetCandidatesAsync(request.Selection, ct);
        return responses.Success(new MassEnrollmentReport(rows, rows.Count));
    }
}

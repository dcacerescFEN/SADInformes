using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.Postgraduate.MassEnrollment.Common;
using Domain.Common;

namespace Application.Reports.Postgraduate.MassEnrollment.Queries.GetMassEnrollmentCandidates;

public sealed record GetMassEnrollmentCandidates(MassEnrollmentSelection Selection, string User)
    : IRequest<ApiResponse<MassEnrollmentCandidatesDTO>>;

public sealed class GetMassEnrollmentCandidatesHandler(IMassEnrollmentData data, IApiResponseService responses)
    : IRequestHandler<GetMassEnrollmentCandidates, ApiResponse<MassEnrollmentCandidatesDTO>>
{
    public async Task<ApiResponse<MassEnrollmentCandidatesDTO>> Handle(GetMassEnrollmentCandidates request, CancellationToken ct)
    {
        var canEnroll = await MassEnrollmentRules.ValidateAvailableAsync(data, request.Selection, request.User, ct,
            requireVacancies: false);
        var rows = await data.GetCandidatesAsync(request.Selection, ct);
        return responses.Success(new MassEnrollmentCandidatesDTO(rows, rows.Count, canEnroll));
    }
}

using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.Postgraduate.MassEnrollment.Common;
using Domain.Common;

namespace Application.Reports.Postgraduate.MassEnrollment.Queries.GetMassEnrollmentFilters;

public sealed record GetMassEnrollmentFilters(MassEnrollmentSelection Selection, string User)
    : IRequest<ApiResponse<MassEnrollmentFiltersDTO>>;

public sealed class GetMassEnrollmentFiltersHandler(IMassEnrollmentData data, IApiResponseService responses)
    : IRequestHandler<GetMassEnrollmentFilters, ApiResponse<MassEnrollmentFiltersDTO>>
{
    public async Task<ApiResponse<MassEnrollmentFiltersDTO>> Handle(GetMassEnrollmentFilters request, CancellationToken ct)
    {
        MassEnrollmentRules.ValidateIdentity(request.User);
        MassEnrollmentRules.Validate(request.Selection, required: false);
        return responses.Success(await data.GetFiltersAsync(request.Selection, request.User, ct));
    }
}

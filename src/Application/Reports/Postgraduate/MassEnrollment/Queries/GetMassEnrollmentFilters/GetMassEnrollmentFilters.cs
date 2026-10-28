using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.Postgraduate.MassEnrollment.Common;
using Domain.Common;

namespace Application.Reports.Postgraduate.MassEnrollment.Queries.GetMassEnrollmentFilters;

public sealed record GetMassEnrollmentFilters(EnrollmentSelection Selection, string User)
    : IRequest<ApiResponse<MassEnrollmentFilters>>;

public sealed class GetMassEnrollmentFiltersHandler(IMassEnrollmentData data, IApiResponseService responses)
    : IRequestHandler<GetMassEnrollmentFilters, ApiResponse<MassEnrollmentFilters>>
{
    public async Task<ApiResponse<MassEnrollmentFilters>> Handle(GetMassEnrollmentFilters request, CancellationToken ct)
    {
        MassEnrollmentRules.ValidateIdentity(request.User);
        MassEnrollmentRules.Validate(request.Selection, required: false);
        return responses.Success(await data.GetFiltersAsync(request.Selection, request.User, ct));
    }
}

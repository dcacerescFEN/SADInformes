using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Postgraduate.EnrolledStudents.Queries.GetEnrolledStudentsFilters;

public sealed record GetEnrolledStudentsFilters : IRequest<ApiResponse<EnrolledStudentsFiltersDTO>>;

public sealed class GetEnrolledStudentsFiltersHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetEnrolledStudentsFilters, ApiResponse<EnrolledStudentsFiltersDTO>>
{
    public async Task<ApiResponse<EnrolledStudentsFiltersDTO>> Handle(
        GetEnrolledStudentsFilters request, CancellationToken ct) =>
        responses.Success(await EnrolledStudentsCatalog.ReadAsync(procedures, ct));
}

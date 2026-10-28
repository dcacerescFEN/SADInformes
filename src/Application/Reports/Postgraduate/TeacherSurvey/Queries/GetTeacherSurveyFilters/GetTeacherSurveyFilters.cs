using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Common.Validation;
using Domain.Common;

namespace Application.Reports.Postgraduate.TeacherSurvey.Queries.GetTeacherSurveyFilters;

public sealed record GetTeacherSurveyFilters(string? StudentType)
    : IRequest<ApiResponse<TeacherSurveyFiltersDTO>>;

public sealed class GetTeacherSurveyFiltersHandler(ITeacherSurveyReader reader, IApiResponseService responses)
    : IRequestHandler<GetTeacherSurveyFilters, ApiResponse<TeacherSurveyFiltersDTO>>
{
    public async Task<ApiResponse<TeacherSurveyFiltersDTO>> Handle(
        GetTeacherSurveyFilters request, CancellationToken cancellationToken)
    {
        var filters = await reader.GetFiltersAsync(request.StudentType?.Trim(), cancellationToken);
        return responses.Success(new TeacherSurveyFiltersDTO(filters.Periods, filters.StudentTypes, filters.Modalities));
    }
}

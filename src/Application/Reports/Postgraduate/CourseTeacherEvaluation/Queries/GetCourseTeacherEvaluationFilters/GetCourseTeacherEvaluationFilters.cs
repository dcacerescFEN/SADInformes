using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Postgraduate.CourseTeacherEvaluation.Queries.GetCourseTeacherEvaluationFilters;

public sealed record GetCourseTeacherEvaluationFilters : IRequest<ApiResponse<CourseTeacherEvaluationFiltersDTO>>;

public sealed class GetCourseTeacherEvaluationFiltersHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetCourseTeacherEvaluationFilters, ApiResponse<CourseTeacherEvaluationFiltersDTO>>
{
    public async Task<ApiResponse<CourseTeacherEvaluationFiltersDTO>> Handle(
        GetCourseTeacherEvaluationFilters request, CancellationToken ct)
    {
        var years = (await procedures.SpRec_AnosAsync("NO", "Todos", cancellationToken: ct) ?? [])
            .Select(row => row.Año?.Trim()).Where(value => value is { Length: 4 }
                && value.All(char.IsAsciiDigit) && value != "0000").Cast<string>()
            .Distinct().ToArray();
        return responses.Success(new CourseTeacherEvaluationFiltersDTO(years, ["EX", "FT"]));
    }
}

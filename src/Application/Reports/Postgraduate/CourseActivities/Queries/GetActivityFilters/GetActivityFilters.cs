using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Postgraduate.CourseActivities.Queries.GetActivityFilters;

public sealed record GetActivityFilters : IRequest<ApiResponse<ActivityFiltersDTO>>;

public sealed class GetActivityFiltersHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetActivityFilters, ApiResponse<ActivityFiltersDTO>>
{
    public async Task<ApiResponse<ActivityFiltersDTO>> Handle(
        GetActivityFilters request, CancellationToken ct)
    {
        var rows = await procedures.SpRec_AnosAsync("NO", "Todos", cancellationToken: ct) ?? [];
        var years = rows.Select(row => row.Año?.Trim())
            .Where(value => value is { Length: 4 } && value.All(char.IsAsciiDigit))
            .Cast<string>().Distinct()
            .Select(value => new ActivityOptionDTO(value, value)).ToArray();
        return responses.Success(new ActivityFiltersDTO(years));
    }
}

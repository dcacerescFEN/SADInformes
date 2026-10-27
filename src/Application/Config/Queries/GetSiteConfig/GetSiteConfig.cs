using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Common.Models;
using Application.Navigation;
using Domain.Common;

namespace Application.Config.Queries.GetSiteConfig;

public sealed record GetSiteConfig : IRequest<ApiResponse<SiteConfigDto>>;

public sealed class GetSiteConfigHandler(ISiteMenuProvider provider, IApiResponseService responses)
    : IRequestHandler<GetSiteConfig, ApiResponse<SiteConfigDto>>
{
    public async Task<ApiResponse<SiteConfigDto>> Handle(GetSiteConfig request, CancellationToken cancellationToken)
    {
        var menu = await provider.GetMenuAsync(cancellationToken);
        return responses.Success(new SiteConfigDto
        {
            StudentsMenuUrl = menu["studentsMenuUrl"]!,
            TeachersMenuUrl = menu["teachersMenuUrl"]!,
            CoursesMenuUrl = menu["coursesMenuUrl"]!,
            ExamsMenuUrl = menu["examsMenuUrl"]!,
            ActivitiesMenuUrl = menu["activitiesMenuUrl"]!,
            AssistantsMenuUrl = menu["assistantsMenuUrl"]!,
            ProcessesMenuUrl = menu["processesMenuUrl"]!,
            AdministrationMenuUrl = menu["administrationMenuUrl"]!,
            ReportsMenuUrl = menu["reportsMenuUrl"]!,
            StudentsMenuClientId = menu["studentsMenuClientId"],
            TeachersMenuClientId = menu["teachersMenuClientId"],
            CoursesMenuClientId = menu["coursesMenuClientId"],
            ExamsMenuClientId = menu["examsMenuClientId"],
            ActivitiesMenuClientId = menu["activitiesMenuClientId"],
            AssistantsMenuClientId = menu["assistantsMenuClientId"],
            ProcessesMenuClientId = menu["processesMenuClientId"],
            AdministrationMenuClientId = menu["administrationMenuClientId"],
            ReportsMenuClientId = menu["reportsMenuClientId"],
        });
    }
}

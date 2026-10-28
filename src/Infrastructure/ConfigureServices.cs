using Application.Navigation;
using Application.Reports.Postgraduate.MassEnrollment.Common;
using Application.Common.Interfaces;
using Application.Reports.Postgraduate.TeacherSurvey;
using Application.Reports.Postgraduate.CoursesByStudents;
using Infrastructure.Data;
using Infrastructure.Reports.Postgraduate;
using Infrastructure.Configuration;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppConfigContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("AppConfig"),
                sql => sql.CommandTimeout(10)));
        services.AddScoped<IApiResponseService, ApiResponseService>();
        services.AddDbContext<SadContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Sad"),
                sql => sql.CommandTimeout(60)));
        services.AddScoped<ISadContext>(provider => provider.GetRequiredService<SadContext>());
        services.AddScoped<ISadContextProcedures, SadContextProcedures>();
        services.AddScoped<IMassEnrollmentData, MassEnrollmentData>();
        services.AddScoped<ITeacherSurveyReader, TeacherSurveyReader>();
        services.AddScoped<ICoursesByStudentsReader, CoursesByStudentsReader>();
        services.AddScoped<ConfigurationInitialiser>();
        services.AddSingleton<SiteSettings>();
        services.AddSingleton<ISiteMenuProvider, SiteMenuProvider>();
        return services;
    }
}

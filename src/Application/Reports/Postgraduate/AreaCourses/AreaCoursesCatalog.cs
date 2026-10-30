using Application.Common.Interfaces;
using Application.Common.Validation;
using Application.Reports.Postgraduate.AreaCourses.Queries.GetAreaCoursesFilters;

namespace Application.Reports.Postgraduate.AreaCourses;

internal static class AreaCoursesCatalog
{
    internal static bool IsYear(string? value) =>
        value is { Length: 4 } && value.All(char.IsAsciiDigit) && value != "0000";

    internal static string RequiredYear(string? value, string message)
    {
        var text = ConcreteFilter.Required(message, value);
        if (text.Length > 4 || text.Any(char.IsControl) || !IsYear(text))
            throw new ArgumentException("Los años deben tener cuatro dígitos.");
        return text;
    }

    internal static async Task<AreaCoursesFiltersDTO> ReadAsync(
        ISadContextProcedures procedures, CancellationToken ct)
    {
        var years = (await procedures.SpRec_AnosAsync("NO", "Todos", cancellationToken: ct) ?? [])
            .Select(row => row.Año?.Trim()).Where(IsYear).Cast<string>().Distinct().ToArray();
        return new(years);
    }
}

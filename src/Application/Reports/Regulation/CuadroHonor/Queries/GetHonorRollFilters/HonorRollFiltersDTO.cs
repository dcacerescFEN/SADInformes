namespace Application.Reports.Regulation.CuadroHonor.Queries.GetHonorRollFilters;

public sealed record HonorRollFiltersDTO(
    IReadOnlyList<HonorRollCareerDTO> Careers,
    IReadOnlyList<HonorRollYearDTO> Years);

public sealed record HonorRollCareerDTO(string Value, string Text);

public sealed record HonorRollYearDTO(string Value, string Text);

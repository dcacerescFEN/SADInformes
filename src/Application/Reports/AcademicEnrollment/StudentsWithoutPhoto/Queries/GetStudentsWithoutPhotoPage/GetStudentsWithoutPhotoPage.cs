using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;
using Domain.Entities.Sad.Sp;

namespace Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Queries.GetStudentsWithoutPhotoPage;

public sealed record GetStudentsWithoutPhotoPage(int Page, int PageSize, string? Search)
    : IRequest<ApiResponse<StudentsWithoutPhotoPageDTO>>;

public sealed class GetStudentsWithoutPhotoPageHandler(
    ISadContextProcedures procedures, IStudentPhotoProbe photoProbe, IApiResponseService responses)
    : IRequestHandler<GetStudentsWithoutPhotoPage, ApiResponse<StudentsWithoutPhotoPageDTO>>
{
    private const int ProbeBatch = 8;

    public async Task<ApiResponse<StudentsWithoutPhotoPageDTO>> Handle(
        GetStudentsWithoutPhotoPage request, CancellationToken cancellationToken)
    {
        if (request.Page < 1) throw new ArgumentException("La página no es válida.");
        if (request.PageSize is < 1 or > 50) throw new ArgumentException("El tamaño de página no es válido.");
        var search = request.Search?.Trim() ?? "";
        if (search.Length > 100) throw new ArgumentException("La búsqueda no es válida.");

        var data = await procedures.SpRec_SAD_SAD_PersonasSinFotosAsync(
            cancellationToken: cancellationToken) ?? [];
        var candidates = data.Where(row => Matches(row, search)).ToArray();
        var skip = (request.Page - 1) * request.PageSize;

        var rows = new List<StudentWithoutPhotoPageRowDTO>(request.PageSize);
        var found = 0;
        var index = 0;
        while (index < candidates.Length && rows.Count < request.PageSize)
        {
            var batchSize = Math.Min(ProbeBatch, candidates.Length - index);
            var batch = candidates.Skip(index).Take(batchSize).ToArray();
            var checks = await Task.WhenAll(batch.Select(async row =>
            {
                var personCode = row.Cod_Persona?.Trim() ?? "";
                var hasPhoto = personCode.Length > 0
                    && await photoProbe.HasPhotoAsync(personCode, cancellationToken);
                return (Row: row, HasPhoto: hasPhoto);
            }));

            foreach (var check in checks)
            {
                if (check.HasPhoto) continue;
                if (found >= skip && rows.Count < request.PageSize)
                    rows.Add(new StudentWithoutPhotoPageRowDTO(
                        Text(check.Row.Nombre),
                        Text(check.Row.Cod_Persona),
                        Text(check.Row.Cod_Alumno),
                        Text(check.Row.Tipo_Alumno),
                        Text(check.Row.Celular),
                        Text(check.Row.MailFen),
                        Text(check.Row.E_Mail)));
                found++;
            }

            index += batch.Length;
        }

        var hasMore = index < candidates.Length || found > skip + rows.Count;
        return responses.Success(new StudentsWithoutPhotoPageDTO(
            rows.ToArray(), request.Page, hasMore, index));
    }

    private static bool Matches(SpRecPersonasSinFotosResult row, string search) =>
        search.Length == 0 || new[]
        {
            row.Nombre, row.Cod_Persona, row.Cod_Alumno, row.Tipo_Alumno,
            row.Celular, row.MailFen, row.E_Mail,
        }.Any(value => value?.Contains(search, StringComparison.OrdinalIgnoreCase) == true);

    private static string Text(string? value) => value?.Trim() ?? "";
}

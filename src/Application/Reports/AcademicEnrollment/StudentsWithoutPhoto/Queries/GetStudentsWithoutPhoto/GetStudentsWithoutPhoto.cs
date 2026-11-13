using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Queries.GetStudentsWithoutPhoto;

public sealed record GetStudentsWithoutPhoto : IRequest<ApiResponse<StudentsWithoutPhotoReportDTO>>;

public sealed class GetStudentsWithoutPhotoHandler(
    ISadContextProcedures procedures, IStudentPhotoProbe photoProbe, IApiResponseService responses)
    : IRequestHandler<GetStudentsWithoutPhoto, ApiResponse<StudentsWithoutPhotoReportDTO>>
{
    private const int ProbeConcurrency = 8;

    public async Task<ApiResponse<StudentsWithoutPhotoReportDTO>> Handle(
        GetStudentsWithoutPhoto request, CancellationToken cancellationToken)
    {
        var data = await procedures.SpRec_SAD_SAD_PersonasSinFotosAsync(
            cancellationToken: cancellationToken) ?? [];

        using var gate = new SemaphoreSlim(ProbeConcurrency);
        var checks = data.Select(async row =>
        {
            var personCode = row.Cod_Persona?.Trim() ?? "";
            var hasPhoto = false;
            if (personCode.Length > 0)
            {
                await gate.WaitAsync(cancellationToken);
                try { hasPhoto = await photoProbe.HasPhotoAsync(personCode, cancellationToken); }
                finally { gate.Release(); }
            }
            return (Row: row, HasPhoto: hasPhoto);
        });

        var rows = (await Task.WhenAll(checks))
            .Where(item => !item.HasPhoto)
            .Select(item => new StudentWithoutPhotoRowDTO(
                Text(item.Row.Nombre),
                Text(item.Row.Cod_Persona),
                Text(item.Row.Cod_Alumno),
                Text(item.Row.Tipo_Alumno),
                Text(item.Row.Celular),
                Text(item.Row.MailFen),
                Text(item.Row.E_Mail)))
            .ToArray();

        return responses.Success(new StudentsWithoutPhotoReportDTO(rows, rows.Length));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
}

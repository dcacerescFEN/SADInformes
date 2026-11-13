namespace Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Common.Email;

public static class StudentsWithoutPhotoEmailTemplate
{
    public const string Subject = "Fotografía Web de Docencia";
    public const string Bcc = "secretariaestudios@fen.uchile.cl";

    public const string Body = """
        <h2>Estimados Estudiantes,</h2>

        <p>
          Informamos a ustedes que a la brevedad deberán pasar por los módulos de
          Secretaría de Estudios, entre las 9.30 hrs y las 17.30 hrs., a tomarse la
          fotografía para ser incorporada al Web de Docencia. De no estar registrada su
          fotografía al 13 de junio, serán suspendidos todos los servicios otorgados por
          Secretaría de Estudios.
        </p>

        <p><strong>SECRETARIA DE ESTUDIOS</strong></p>
        """;
}

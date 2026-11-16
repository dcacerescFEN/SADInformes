using System.Net;

namespace Application.Reports.AcademicEnrollment.TeacherAvailability.Common;

public static class TeacherAvailabilityEmailTemplate
{
    public const string Subject = "Solicitud Disponibilidad Horaria";
    public const string Cc = "secretariaestudios@fen.uchile.cl";

    public static string Welcome(string name, string courses, string periodName, string pendingDate) =>
        Body(name, courses,
            $"Las Escuelas de Pregrado en conjunto con la Secretaría de Estudios y los coordinadores de área han estado trabajando en la programación de cursos para el semestre {periodName}. Usted ha sido elegido para dictar el o los siguientes cursos:",
            "Junto con lo anterior, podrá registrar información adicional relevante, para el mejor desarrollo de sus cátedras como lo son el tipo de sala requerida o si necesita que su curso tenga programada evaluación durante la semana de solemnes y/o exámenes.",
            $"Para lo anterior, debe ingresar al nuevo DocenciaWeb y registrar sus opciones en el menú disponibilidades horarias a más tardar el día <strong>{Encode(pendingDate)}</strong>.");

    public static string Pending(string name, string courses, string laggedDate) =>
        Body(name, courses,
            "De acuerdo a nuestros registros usted aún no ha ingresado su disponibilidad horaria para el o los siguientes cursos:",
            null,
            $"Para lo anterior, debe ingresar al nuevo DocenciaWeb y registrar sus opciones en el menú disponibilidades horarias a más tardar el día <strong>{Encode(laggedDate)}</strong>.");

    private static string Body(string name, string courses, string intro, string? extra, string deadline) =>
        $"""
        <!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
        <html xmlns="http://www.w3.org/1999/xhtml">
        <head><meta http-equiv="Content-Type" content="text/html; charset=iso-8859-1" /><title>Documento sin título</title></head>
        <body>
        <table><tr><td>
        <p>Estimado Profesor:</p>
        <p>{Encode(name)}<br></p>
        <p align="justify" style="color:#000000">{Encode(intro)}</p>
        <div align="justify"><ul><strong>{Encode(courses).Replace("\n", "<br>")}</strong></ul></div>
        {(extra is null ? "" : $"<p align=\"justify\">{Encode(extra)}</p>")}
        <p align="justify">{deadline}</p>
        <p align="justify">Ante cualquier duda o consulta favor comunicarse con Daniela Silva en el correo <u>dsilva@fen.uchile.cl</u>.</p>
        <p align="justify">Atte:<br/>Secretaría de Estudios</p>
        </td></tr></table>
        </body></html>
        """;

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}

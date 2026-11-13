using System.Net.Mail;
using Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Queries.GetStudentsWithoutPhoto;

namespace Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Common.Email;

public sealed record StudentsWithoutPhotoEmailRecipient(string StudentCode, string To, string[] Cc);

public sealed record StudentsWithoutPhotoEmailRecipientPlan(
    StudentsWithoutPhotoEmailRecipient[] Recipients, int Omitted);

public static class StudentsWithoutPhotoEmailRecipients
{
    public static StudentsWithoutPhotoEmailRecipientPlan Build(
        IEnumerable<StudentWithoutPhotoRowDTO> rows)
    {
        var recipients = new List<StudentsWithoutPhotoEmailRecipient>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var omitted = 0;

        foreach (var row in rows)
        {
            if (!seen.Add(row.StudentCode)) continue;
            var personal = Valid(row.PersonalMail) ? row.PersonalMail.Trim() : "";
            var fen = Valid(row.MailFen) ? row.MailFen.Trim() : "";

            if (fen.Length > 0)
            {
                var cc = personal.Length > 0
                    && !string.Equals(personal, fen, StringComparison.OrdinalIgnoreCase)
                    ? new[] { personal } : [];
                recipients.Add(new(row.StudentCode, fen, cc));
                continue;
            }

            if (personal.Length > 0)
            {
                recipients.Add(new(row.StudentCode, personal, []));
                continue;
            }

            omitted++;
        }

        return new(recipients.ToArray(), omitted);
    }

    private static bool Valid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && MailAddress.TryCreate(value.Trim(), out _);
}

namespace Application.Common.Interfaces;

/// <summary>Envía mensajes de correo con adjuntos.</summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

/// <summary>Representa un correo saliente sin datos de infraestructura.</summary>
public sealed record EmailMessage(
    string To,
    IReadOnlyList<string> Cc,
    IReadOnlyList<string> Bcc,
    string Subject,
    string Body,
    IReadOnlyList<EmailAttachment> Attachments);

/// <summary>Representa un adjunto de un correo saliente.</summary>
public sealed record EmailAttachment(string FileName, string ContentType, byte[] Content);

/// <summary>Estados seguros de un intento de entrega de correo.</summary>
public enum EmailDeliveryStatus
{
    Sent,
    Omitted,
    Failed,
}

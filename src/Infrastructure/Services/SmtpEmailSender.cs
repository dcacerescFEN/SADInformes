using System.Net.Mail;
using Application.Common.Interfaces;
using Application.Common.Settings;
using Infrastructure.Configuration;

namespace Infrastructure.Services;

/// <summary>Envía correo por SMTP usando la configuración general cargada desde AppConfig.</summary>
public sealed class SmtpEmailSender : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        if (!EmailSettings.SendEmail || !EmailDeliverySettings.IsSmtpConfigured)
            throw new InvalidOperationException("La configuración SMTP no está disponible.");

        using var client = new SmtpClient(EmailSettings.Server, EmailSettings.Port);
        using var mail = new MailMessage
        {
            From = new MailAddress(EmailSettings.EmailFrom),
            Subject = message.Subject,
            Body = message.Body,
            IsBodyHtml = true,
        };
        mail.To.Add(message.To);
        foreach (var address in message.Cc) mail.CC.Add(address);
        foreach (var address in message.Bcc) mail.Bcc.Add(address);
        foreach (var attachment in message.Attachments)
            mail.Attachments.Add(new Attachment(
                new MemoryStream(attachment.Content), attachment.FileName, attachment.ContentType));

        await client.SendMailAsync(mail, cancellationToken);
    }
}

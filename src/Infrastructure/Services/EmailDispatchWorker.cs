using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Common.Email;
using Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Queries.GetStudentsWithoutPhoto;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>Procesa la cola de correos en segundo plano, sin bloquear la request.</summary>
public sealed class EmailDispatchWorker(
    EmailDispatchQueue queue, EmailDispatchRegistry registry,
    IServiceScopeFactory scopeFactory, ILogger<EmailDispatchWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var jobId in queue.ReadAllAsync(stoppingToken))
        {
            if (!registry.TryGet(jobId, out var job)) continue;
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                stoppingToken, job.Cancellation.Token);
            var jobToken = linked.Token;
            registry.MarkRunning(jobId);
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<IRequestDispatcher>();
                jobToken.ThrowIfCancellationRequested();
                var report = await dispatcher.Send(new GetStudentsWithoutPhoto(), jobToken);
                var plan = StudentsWithoutPhotoEmailRecipients.Build(report.Data.Rows);
                registry.SetTotal(jobId, plan.Recipients.Length);
                registry.AddOmitted(jobId, plan.Omitted);
                registry.SetPhase(jobId, "SENDING");

                var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                foreach (var recipient in plan.Recipients)
                {
                    try
                    {
                        jobToken.ThrowIfCancellationRequested();
                        await sender.SendAsync(new EmailMessage(
                            recipient.To,
                            recipient.Cc,
                            new[] { StudentsWithoutPhotoEmailTemplate.Bcc },
                            StudentsWithoutPhotoEmailTemplate.Subject,
                            StudentsWithoutPhotoEmailTemplate.Body,
                            []), jobToken);
                        registry.IncrementSent(jobId);
                    }
                    catch (OperationCanceledException) when (jobToken.IsCancellationRequested)
                    {
                        registry.Cancel(jobId);
                        return;
                    }
                    catch (InvalidOperationException error)
                    {
                        // Un fallo de configuración SMTP afecta a todos los destinatarios:
                        // se corta el job para que el modal muestre el fallo, en vez de
                        // repetir el mismo error por cada alumno.
                        registry.IncrementFailed(jobId);
                        registry.Fail(jobId);
                        logger.LogWarning("Falló el job de correo {JobId}: {ErrorType}: {ErrorMessage}", jobId, error.GetType().Name, error.Message);
                        return;
                    }
                    catch (Exception error)
                    {
                        registry.IncrementFailed(jobId);
                        logger.LogWarning("Falló un correo del job {JobId}: {ErrorType}", jobId, error.GetType().Name);
                    }
                }

                registry.Finish(jobId);
            }
            catch (OperationCanceledException) when (jobToken.IsCancellationRequested)
            {
                registry.Cancel(jobId);
                return;
            }
            catch (Exception error)
            {
                registry.Fail(jobId);
                logger.LogWarning("Falló el job de correo {JobId}: {ErrorType}: {ErrorMessage}", jobId, error.GetType().Name, error.Message);
            }
        }
    }
}

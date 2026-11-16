using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Common.Settings;
using Application.Reports.AcademicEnrollment.TeacherAvailability.Common;
using Application.Reports.AcademicEnrollment.TeacherAvailability.Common.Email;
using Application.Reports.AcademicEnrollment.TeacherAvailability.Queries.GetTeacherAvailability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>Procesa la cola de correos de disponibilidad sin bloquear la request.</summary>
public sealed class TeacherAvailabilityEmailWorker(
    TeacherAvailabilityEmailQueue queue, TeacherAvailabilityEmailRegistry registry,
    IServiceScopeFactory scopeFactory, ILogger<TeacherAvailabilityEmailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var jobId in queue.ReadAllAsync(stoppingToken))
        {
            if (!registry.TryGet(jobId, out var job)) continue;
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                stoppingToken, job.Cancellation.Token);
            var token = linked.Token;
            registry.MarkRunning(jobId);
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<IRequestDispatcher>();
                var report = await dispatcher.Send(new GetTeacherAvailability(job.Period, job.User), token);
                var selected = job.Mode switch
                {
                    "single" => report.Data.Rows.Where(row => string.Equals(row.TeacherCode,
                        job.TeacherCode?.Trim(), StringComparison.OrdinalIgnoreCase)),
                    "all" => report.Data.Rows,
                    "welcome-pending" => report.Data.Rows.Where(row => row.WelcomeEmail == "0"),
                    "pending" => report.Data.Rows.Where(row => string.Equals(row.State, "PENDIENTE",
                        StringComparison.OrdinalIgnoreCase)),
                    _ => [],
                };
                var recipients = selected.Where(row => !string.IsNullOrWhiteSpace(row.Mail)).ToArray();
                registry.SetTotal(jobId, recipients.Length);
                registry.AddOmitted(jobId, selected.Count() - recipients.Length);
                registry.SetPhase(jobId, "SENDING");

                var procedures = scope.ServiceProvider.GetRequiredService<ISadContextProcedures>();
                var dates = await procedures.SpRec_PeriodoDisponibilidad_DatosXPeriodoAsync(
                    report.Data.Period, cancellationToken: token);
                var date = dates?.FirstOrDefault();
                var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                var welcome = job.Mode is "all" or "welcome-pending";
                foreach (var row in recipients)
                {
                    try
                    {
                        token.ThrowIfCancellationRequested();
                        if (welcome)
                            await procedures.SpMOD_FE_BienvenidaAsync(
                                report.Data.Period, row.TeacherCode, cancellationToken: token);
                        var body = welcome
                            ? TeacherAvailabilityEmailTemplate.Welcome(row.Teacher, row.Course,
                                report.Data.PeriodName, date?.TextoFechaCorreoDisponibilidad ?? "")
                            : TeacherAvailabilityEmailTemplate.Pending(row.Teacher, row.Course,
                                date?.TextoFechaRezagados ?? "");
                        await sender.SendAsync(new EmailMessage(
                            row.Mail,
                            [TeacherAvailabilityEmailTemplate.Cc],
                            string.IsNullOrWhiteSpace(TeacherAvailabilityEmailSettings.Bcc)
                                ? [] : [TeacherAvailabilityEmailSettings.Bcc],
                            TeacherAvailabilityEmailTemplate.Subject,
                            body,
                            []), token);
                        registry.IncrementSent(jobId);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        registry.Cancel(jobId);
                        return;
                    }
                    catch (InvalidOperationException error)
                    {
                        registry.IncrementFailed(jobId);
                        registry.Fail(jobId);
                        logger.LogWarning(
                            "Falló el job de disponibilidad {JobId}: {ErrorType}: {ErrorMessage}",
                            jobId, error.GetType().Name, error.Message);
                        return;
                    }
                    catch (Exception error)
                    {
                        registry.IncrementFailed(jobId);
                        logger.LogWarning("Falló un correo de disponibilidad {JobId}: {ErrorType}",
                            jobId, error.GetType().Name);
                    }
                }

                registry.Finish(jobId);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                registry.Cancel(jobId);
                return;
            }
            catch (Exception error)
            {
                registry.Fail(jobId);
                logger.LogWarning(
                    "Falló el job de disponibilidad {JobId}: {ErrorType}: {ErrorMessage}",
                    jobId, error.GetType().Name, error.Message);
            }
        }
    }
}

using Application.Common.Mediator;
using Application.Reports.Postgraduate.BasicRightPayment.Commands.UpdateBasicRightPayments;
using Application.Reports.Postgraduate.BasicRightPayment.Queries.GetBasicRightPaymentFilters;
using Application.Reports.Postgraduate.BasicRightPayment.Queries.GetBasicRightPayments;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using WebApi.Extension;
using WebApi.Reporting;

namespace WebApi.Endpoints.Reports.Postgraduate;

public sealed class BasicRightPaymentEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/postgraduate/basic-right-payment")
            .RequireAuthorization(Permission.REPORT_VIEW_POSTGRADUATE_BASIC_RIGHT_PAYMENT)
            .WithTags("Reports/Postgraduate");
        group.MapGet("/filters", Filters).WithSummary("Filtros de pago derecho básico");
        group.MapGet("/report", Report).WithSummary("Alumnos y estado de pago");
        group.MapGet("/download", Download).WithSummary("Pago derecho básico en Excel");
        group.MapPut("/payments", Save)
            .RequireAuthorization(Permission.REPORT_MANAGE_POSTGRADUATE_BASIC_RIGHT_PAYMENT)
            .WithSummary("Guarda cambios de pago");
    }

    private static Task<IResult> Filters(HttpContext context, IRequestDispatcher sender,
        string? studentType, CancellationToken ct) =>
        Execute(false, async () => Results.Ok(await sender.Send(
            new GetBasicRightPaymentFilters(studentType), ct)));

    private static Task<IResult> Report(HttpContext context, IAuthorizationService authorization,
        IRequestDispatcher sender, string? studentType, string? modality, string? entryYear,
        string? paymentYear, string? paymentStatus, CancellationToken ct) =>
        Execute(false, async () => Results.Ok(await sender.Send(new GetBasicRightPayments(
            studentType, modality, entryYear, paymentYear, paymentStatus,
            (await authorization.AuthorizeAsync(context.User,
                Permission.REPORT_MANAGE_POSTGRADUATE_BASIC_RIGHT_PAYMENT)).Succeeded), ct)));

    private static Task<IResult> Download(HttpContext context, IRequestDispatcher sender,
        string? studentType, string? modality, string? entryYear, string? paymentYear, string? paymentStatus,
        CancellationToken ct) =>
        Execute(false, async () =>
        {
            var report = await sender.Send(new GetBasicRightPayments(
                studentType, modality, entryYear, paymentYear, paymentStatus, false), ct);
            if (report.Data.Rows.Length == 0) return Results.Problem(statusCode: 400, title: "Sin Datos");
            context.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(BasicRightPaymentExcel.Create(report.Data.Rows),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "PagoDerechoBasicoAnual.xlsx");
        });

    private static Task<IResult> Save(HttpContext context, IRequestDispatcher sender,
        UpdateBasicRightPaymentsRequestDTO request, CancellationToken ct) =>
        Execute(true, async () => Results.Ok(await sender.Send(
            new UpdateBasicRightPayments(request, User(context)), ct)));

    private static async Task<IResult> Execute(bool write, Func<Task<IResult>> operation)
    {
        try { return await operation(); }
        catch (ArgumentException error) { return Results.Problem(statusCode: 400, title: error.Message); }
        catch (Exception error) when (error is SqlException or InvalidOperationException or TimeoutException)
        {
            return Results.Problem(statusCode: 503, title: write
                ? "No se pudo guardar el pago. Actualice el listado antes de reintentar."
                : "El informe no está disponible.");
        }
    }

    private static string User(HttpContext context) => context.User.Identity?.Name ?? "";
}

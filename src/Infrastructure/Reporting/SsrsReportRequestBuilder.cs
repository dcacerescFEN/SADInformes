using Application.Common.Reporting;

namespace Infrastructure.Reporting;

internal static class SsrsReportRequestBuilder
{
    internal static (string Ssrs, string ContentType, string Extension) GetFormat(ReportFormat format) => format switch
    {
        ReportFormat.PDF => ("PDF", "application/pdf", "pdf"),
        ReportFormat.EXCEL => ("EXCELOPENXML", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
        ReportFormat.WORD => ("WORDOPENXML", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "docx"),
        ReportFormat.CSV => ("CSV", "text/csv", "csv"),
        _ => throw new ArgumentOutOfRangeException(nameof(format), "Formato de reporte no admitido."),
    };

    internal static void Validate(ReportRenderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateSegment(request.ReportName);
        ArgumentNullException.ThrowIfNull(request.Parameters);
        if (request.OperationTimeout <= TimeSpan.Zero || request.OperationTimeout.TotalMilliseconds > uint.MaxValue - 1)
            throw new ArgumentOutOfRangeException(nameof(request), "El plazo debe ser positivo y finito.");
        _ = GetFormat(request.Format);
        foreach (var (key, value) in request.Parameters)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Any(char.IsControl)
                || key.TrimStart().StartsWith("rs:", StringComparison.OrdinalIgnoreCase)
                || key.TrimStart().StartsWith("rc:", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Nombre de parámetro no admitido.", nameof(request));
            ArgumentNullException.ThrowIfNull(value);
        }
        if (request.PageSize is { } pageSize)
        {
            ValidateLength(pageSize.Width);
            ValidateLength(pageSize.Height);
        }
    }

    internal static Uri Build(ReportRenderRequest request, string baseUrl, string path)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var server)
            || (server.Scheme != Uri.UriSchemeHttp && server.Scheme != Uri.UriSchemeHttps)
            || server.Query.Length != 0 || server.Fragment.Length != 0 || server.UserInfo.Length != 0)
            throw new InvalidOperationException("La dirección del servicio de reportes no es válida.");

        var folder = path.Trim().Trim('/');
        var segments = folder.Length == 0 ? [] : folder.Split('/');
        foreach (var segment in segments) ValidateSegment(segment);
        var reportPath = string.Join('/', segments.Append(request.ReportName).Select(Uri.EscapeDataString));
        var parameters = string.Concat(request.Parameters.Select(parameter =>
            $"&{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));
        var deviceInfo = request.PageSize is { } pageSize
            ? $"&rc:PageWidth={Uri.EscapeDataString(pageSize.Width)}&rc:PageHeight={Uri.EscapeDataString(pageSize.Height)}"
            : string.Empty;
        return new Uri($"{server.AbsoluteUri.TrimEnd('/')}?/{reportPath}&rs:Command=Render&rs:Format={GetFormat(request.Format).Ssrs}{deviceInfo}{parameters}");
    }

    private static void ValidateSegment(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment) || segment != segment.Trim() || segment is "." or ".."
            || segment.Any(character => char.IsControl(character) || "/\\%?#&=:".Contains(character)))
            throw new ArgumentException("La ruta del reporte no es válida.");
    }

    private static void ValidateLength(string value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value != value.Trim()
            || value.Length > 20
            || !EndsWithSupportedUnit(value)
            || !value[..^2].All(character => char.IsAsciiDigit(character) || character == '.')
            || !decimal.TryParse(value[..^2], System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out var amount)
            || amount <= 0)
            throw new ArgumentException("El tamaño de hoja del reporte no es válido.");
    }

    private static bool EndsWithSupportedUnit(string value) => value.EndsWith("cm", StringComparison.Ordinal)
        || value.EndsWith("in", StringComparison.Ordinal);
}

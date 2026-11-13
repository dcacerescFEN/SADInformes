namespace Domain.Constants;

public static class Permission
{
    public const string REPORT_ACCESS = "REPORT_ACCESS_REPORT_MODULE";

    /// <summary>
    /// Acceso único a los informes de Postgrado. Reemplaza los permisos por informe
    /// y se evalúa en Keycloak como PERMISSION_REPORT_VIEW_POSTGRADUATE.
    /// </summary>
    public const string REPORT_VIEW_POSTGRADUATE = "REPORT_VIEW_POSTGRADUATE";

    public const string REPORT_VIEW_AVAILABILITY = "REPORT_VIEW_AVAILABILITY";
    public const string REPORT_SEND_AVAILABILITY_EMAIL = "REPORT_SEND_AVAILABILITY_EMAIL";
    public const string REPORT_EDIT_AVAILABILITY = "REPORT_EDIT_AVAILABILITY";
    public const string REPORT_DELETE_AVAILABILITY = "REPORT_DELETE_AVAILABILITY";
}

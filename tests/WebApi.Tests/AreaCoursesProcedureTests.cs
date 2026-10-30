using System.Data;
using System.Data.Common;
using Domain.Common;
using Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace WebApi.Tests;

public sealed class AreaCoursesProcedureTests
{
    [Fact]
    public async Task Wrapper_materializes_integer_Creditos_and_keeps_canonical_parameters()
    {
        var capture = new ReaderCapture();
        await using var context = new SadContext(new DbContextOptionsBuilder<SadContext>()
            .UseSqlServer("Server=unused.invalid;Database=isolated;Integrated Security=true")
            .AddInterceptors(new NoNetwork(), capture).Options);
        var output = new OutputParameter<int>();
        var procedures = new SadContextProcedures(context);

        var rows = await procedures.SpRec_CatedrasAreas_ReportePostgradoAsync(
            "2024", "2026", output, CancellationToken.None);

        Assert.Equal(9, output.Value);
        Assert.StartsWith(
            "EXEC @returnValue = [dbo].[spRec_CatedrasAreas_ReportePostgrado]",
            capture.Text);
        var parameters = capture.Parameters.ToDictionary(parameter => parameter.ParameterName);
        Assert.Equal((10, SqlDbType.VarChar, (object)"2024"),
            (parameters["Año1"].Size, parameters["Año1"].SqlDbType, parameters["Año1"].Value));
        Assert.Equal((10, SqlDbType.VarChar, (object)"2026"),
            (parameters["Año2"].Size, parameters["Año2"].SqlDbType, parameters["Año2"].Value));
        Assert.Equal(ParameterDirection.Output, parameters["returnValue"].Direction);

        var row = Assert.Single(rows!);
        Assert.Equal(4, row.Creditos);
        Assert.Equal(2, row.ID);
        Assert.Equal(4.5d, row.HorasDedicadas);
        Assert.Equal("Gestión", row.descripcion);
    }

    private sealed class NoNetwork : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,
            ConnectionEventData eventData, InterceptionResult result,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(InterceptionResult.Suppress());
    }

    private sealed class ReaderCapture : DbCommandInterceptor
    {
        public string Text { get; private set; } = "";
        public SqlParameter[] Parameters { get; private set; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Text = command.CommandText;
            Parameters = command.Parameters.Cast<SqlParameter>().ToArray();
            Parameters.Single(parameter => parameter.Direction == ParameterDirection.Output).Value = 9;

            var table = new DataTable();
            table.Columns.Add("ID", typeof(long));
            table.Columns.Add("descripcion", typeof(string));
            table.Columns.Add("area", typeof(string));
            table.Columns.Add("periodo", typeof(string));
            table.Columns.Add("cod_catedra", typeof(string));
            table.Columns.Add("cod_seccion", typeof(string));
            table.Columns.Add("Creditos", typeof(int));
            table.Columns.Add("Nombre_Profesor", typeof(string));
            table.Columns.Add("Cod_Persona", typeof(string));
            table.Columns.Add("AQPQ", typeof(string));
            table.Columns.Add("HorasDedicadas", typeof(double));
            table.Columns.Add("Inicio", typeof(string));
            table.Columns.Add("Termino", typeof(string));
            table.Columns.Add("Promocion", typeof(string));
            table.Columns.Add("Tipo_Profesor", typeof(string));
            table.Columns.Add("nom_catedra", typeof(string));
            table.Columns.Add("TipoCatedra", typeof(string));
            table.Columns.Add("Programa", typeof(string));
            table.Columns.Add("Modalidad", typeof(string));
            table.Columns.Add("Confirmado", typeof(string));
            table.Columns.Add("Tipo_Horario", typeof(string));
            table.Columns.Add("Alumnos_Totales", typeof(int));
            table.Columns.Add("Alumnos_Postgrado", typeof(int));
            table.Columns.Add("Alumnos_Pregrado", typeof(int));
            table.Columns.Add("Promedio_Ev_Docente", typeof(string));
            table.Columns.Add("FacultyQualificationSA_PA_SP_IP", typeof(string));
            table.Columns.Add("Observaciones_Curso", typeof(string));
            table.Columns.Add("Usuario_Observ", typeof(string));
            table.Rows.Add(2, "Gestión", "ADM", "2024F", "MBA600", "01", 4, "Ana Pérez",
                "P/1", "AQ", 4.5d, "01/03/2024", "30/06/2024", "2024", "FULL TIME",
                "Gestión Estratégica", "Cátedra", "MBA", "SUNRISE", "Confirmado", "Diurno",
                20, 15, 5, "6,25", "SA", "Observación", "usuario 01-03-2024 10:00");
            return ValueTask.FromResult(
                InterceptionResult<DbDataReader>.SuppressWithResult(table.CreateDataReader()));
        }
    }
}

using System.Data;
using System.Data.Common;
using Domain.Common;
using Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace WebApi.Tests;

public sealed class ExitSurveyStatisticsProcedureTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Likert_wrappers_preserve_canonical_procedure_parameters_output_and_shape(bool q7)
    {
        var capture = new ReaderCapture();
        await using var context = new SadContext(new DbContextOptionsBuilder<SadContext>()
            .UseSqlServer("Server=unused.invalid;Database=isolated;Integrated Security=true")
            .AddInterceptors(new NoNetwork(), capture).Options);
        var output = new OutputParameter<int>();
        using var cancellation = new CancellationTokenSource();
        var procedures = new SadContextProcedures(context);
        var rows = q7
            ? await procedures.spRec_EncuestaGraduadosEstadisticasQ7Async("2024A", "MBA", "PT", output, cancellation.Token)
            : await procedures.spRec_EncuestaGraduadosEstadisticasQ1Async("2024A", "MBA", "PT", output, cancellation.Token);
        Assert.Equal(9, output.Value);
        Assert.Equal(cancellation.Token, capture.Token);
        Assert.StartsWith($"EXEC @returnValue = [dbo].[spRec_EncuestaGraduadosEstadisticas{(q7 ? "Q7" : "Q1")}]", capture.Text);
        var parameters = capture.Parameters.ToDictionary(parameter => parameter.ParameterName);
        Assert.Equal((5, SqlDbType.VarChar, (object)"2024A"), (parameters["Periodo"].Size, parameters["Periodo"].SqlDbType, parameters["Periodo"].Value));
        Assert.Equal((30, SqlDbType.VarChar, (object)"MBA"), (parameters["Tipo_Alumno"].Size, parameters["Tipo_Alumno"].SqlDbType, parameters["Tipo_Alumno"].Value));
        Assert.Equal((10, SqlDbType.VarChar, (object)"PT"), (parameters["Modalidad"].Size, parameters["Modalidad"].SqlDbType, parameters["Modalidad"].Value));
        Assert.Equal(ParameterDirection.Output, parameters["returnValue"].Direction);
        var row = Assert.Single(rows!);
        Assert.Equal(("1", "Pregunta A", 3, 4, 5, 6, 7, 4.5d, 25),
            (row.Pregunta, row.Descripcion, row.Alternativa1, row.Alternativa2, row.Alternativa3, row.Alternativa4, row.Alternativa5, row.Promedio, row.Total));
    }

    private sealed class NoNetwork : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,
            ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(InterceptionResult.Suppress());
    }
    private sealed class ReaderCapture : DbCommandInterceptor
    {
        public string Text { get; private set; } = "";
        public SqlParameter[] Parameters { get; private set; } = [];
        public CancellationToken Token { get; private set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Text = command.CommandText;
            Parameters = command.Parameters.Cast<SqlParameter>().ToArray();
            Token = cancellationToken;
            Parameters.Single(parameter => parameter.Direction == ParameterDirection.Output).Value = 9;
            var table = new DataTable();
            table.Columns.Add("Pregunta", typeof(string)); table.Columns.Add("Descripcion", typeof(string));
            for (var i = 1; i <= 5; i++) table.Columns.Add("Alternativa" + i, typeof(int));
            table.Columns.Add("Promedio", typeof(double)); table.Columns.Add("Total", typeof(int));
            table.Rows.Add("1", "Pregunta A", 3, 4, 5, 6, 7, 4.5d, 25);
            return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(table.CreateDataReader()));
        }
    }
}

using System.Data;
using System.Data.Common;
using Domain.Common;
using Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace WebApi.Tests;

public sealed class TeacherCoursesProcedureTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Report_wrapper_keeps_canonical_parameter_sizes_and_legacy_columns(bool nullInputs)
    {
        var capture = new ReaderCapture("report");
        await using var context = Context(capture);
        using var cancellation = new CancellationTokenSource();
        var output = new OutputParameter<int>();
        var rows = await new SadContextProcedures(context).SpRec_EvaPostgradoCursosDocentes2Async(
            nullInputs ? null : "2024", nullInputs ? null : "Ingeniería", output, cancellation.Token);
        Assert.Equal(9, output.Value);
        Assert.Equal(cancellation.Token, capture.Token);
        Assert.StartsWith("EXEC @returnValue = [dbo].[spRec_EvaPostgradoCursosDocentes2]", capture.Text);
        var parameters = capture.Parameters.ToDictionary(parameter => parameter.ParameterName);
        Assert.Equal(3, parameters.Count);
        foreach (var (name, size, value) in new[] { ("Año", 4, "2024"), ("Departamento", 70, "Ingeniería") })
        {
            Assert.Equal(size, parameters[name].Size);
            Assert.Equal(SqlDbType.VarChar, parameters[name].SqlDbType);
            Assert.Equal(nullInputs ? DBNull.Value : (object)value, parameters[name].Value);
        }
        var row = Assert.Single(rows!);
        Assert.Equal(("PROF001", "ANA PEREZ", "AQ", 3), (row.Cod_Profesor, row.Nombre, row.AQPQ, row.Catedras));
        Assert.Equal(5.25m, row.Promedio);
        Assert.Equal(1.15m, row.P1_13);
        Assert.Null(row.Nota);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Department_catalogue_wrapper_forwards_the_configured_user_with_canonical_sizes(bool nullInputs)
    {
        var capture = new ReaderCapture("departments");
        await using var context = Context(capture);
        using var cancellation = new CancellationTokenSource();
        var output = new OutputParameter<int>();
        var rows = await new SadContextProcedures(context).SpRec_DocentesDepartamentosAsync(
            nullInputs ? null : "NO", nullInputs ? null : "mvenegas", output, cancellation.Token);
        Assert.Equal(9, output.Value);
        Assert.Equal(cancellation.Token, capture.Token);
        Assert.StartsWith("EXEC @returnValue = [dbo].[spRec_DocentesDepartamentos]", capture.Text);
        var parameters = capture.Parameters.ToDictionary(parameter => parameter.ParameterName);
        Assert.Equal(3, parameters.Count);
        foreach (var (name, size, value) in new[] { ("IncluirTodos", 2, "NO"), ("Usuario", 20, "mvenegas") })
        {
            Assert.Equal(size, parameters[name].Size);
            Assert.Equal(SqlDbType.VarChar, parameters[name].SqlDbType);
            Assert.Equal(nullInputs ? DBNull.Value : (object)value, parameters[name].Value);
        }
        Assert.Equal("Ingeniería", Assert.Single(rows!).Departamento);
    }

    private static SadContext Context(ReaderCapture capture) => new(new DbContextOptionsBuilder<SadContext>()
        .UseSqlServer("Server=unused.invalid;Database=isolated;Integrated Security=true")
        .AddInterceptors(new NoNetwork(), capture).Options);

    private sealed class NoNetwork : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,
            ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(InterceptionResult.Suppress());
    }

    private sealed class ReaderCapture(string catalogue) : DbCommandInterceptor
    {
        private static readonly string[] ReportColumns =
        [
            "Cod_Profesor", "Nombre", "P1_1", "P1_2", "P1_3", "P1_4", "P1_5", "P1_6", "P1_7", "P1_8", "P1_9",
            "P1_10", "P1_11", "P1_12", "P1_13", "Nota", "Promedio", "AQPQ", "#Catedras",
        ];

        public string Text { get; private set; } = "";
        public SqlParameter[] Parameters { get; private set; } = [];
        public CancellationToken Token { get; private set; }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Text = command.CommandText;
            Parameters = command.Parameters.Cast<SqlParameter>().ToArray();
            Token = cancellationToken;
            Parameters.Single(p => p.Direction == ParameterDirection.Output).Value = 9;
            var table = new DataTable();
            if (catalogue == "departments")
            {
                table.Columns.Add("Departamento", typeof(string));
                table.Rows.Add("Ingeniería");
            }
            else
            {
                foreach (var column in ReportColumns)
                    table.Columns.Add(column, column == "#Catedras" ? typeof(int)
                        : (column is "Promedio" or "Nota") || column.StartsWith("P1_") ? typeof(decimal) : typeof(string));
                table.Rows.Add("PROF001", "ANA PEREZ", 4.1m, 4.2m, 4.3m, 4.4m, 4.1m, 4.2m, 4.3m, 4.4m, 4.1m,
                    4.2m, 4.3m, 4.4m, 1.15m, DBNull.Value, 5.25m, "AQ", 3);
            }
            return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(table.CreateDataReader()));
        }
    }
}

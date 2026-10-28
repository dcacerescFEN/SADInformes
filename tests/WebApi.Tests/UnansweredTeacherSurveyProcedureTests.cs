using System.Data;
using System.Data.Common;
using Domain.Common;
using Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace WebApi.Tests;

public sealed class UnansweredTeacherSurveyProcedureTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Canonical_read_preserves_parameter_metadata_output_cancellation_and_result_shape(bool nullInputs)
    {
        var capture = new ReaderCapture(nullInputs);
        await using var context = new SadContext(new DbContextOptionsBuilder<SadContext>()
            .UseSqlServer("Server=unused.invalid;Database=isolated;Integrated Security=true")
            .AddInterceptors(new NoNetwork(), capture).Options);
        var output = new OutputParameter<int>();
        using var cancellation = new CancellationTokenSource();
        var rows = await new SadContextProcedures(context).SpRec_Alumnos_Sin_Evaluacion_DocenteAsync(
            nullInputs ? null : "2024A", nullInputs ? null : "MBA", nullInputs ? null : "PT", output, cancellation.Token);
        Assert.Equal(9, output.Value);
        Assert.Equal(cancellation.Token, capture.Token);
        Assert.StartsWith("EXEC @returnValue = [dbo].[spRec_Alumnos_Sin_Evaluacion_Docente]", capture.Text);
        var parameters = capture.Parameters.ToDictionary(parameter => parameter.ParameterName);
        Assert.Equal(4, parameters.Count);
        foreach (var (name, size, value) in new[] { ("Periodo", 20, "2024A"), ("Tipo_Alumno", 30, "MBA"), ("Modalidad", 30, "PT") })
        {
            Assert.Equal(size, parameters[name].Size);
            Assert.Equal(SqlDbType.VarChar, parameters[name].SqlDbType);
            Assert.Equal(nullInputs ? DBNull.Value : (object)value, parameters[name].Value);
        }
        Assert.Equal(ParameterDirection.Output, parameters["returnValue"].Direction);
        Assert.Equal(SqlDbType.Int, parameters["returnValue"].SqlDbType);
        if (nullInputs) Assert.Empty(rows!);
        else
        {
            var row = Assert.Single(rows!);
            Assert.Equal(("2024A", "A001", "MBA101", "1", "Gestión", "Alumno", "MBA101/1"),
                (row.Periodo, row.Cod_Alumno, row.Cod_Catedra, row.Cod_Seccion, row.Catedra, row.Nombre, row.Curso));
        }
    }

    private sealed class NoNetwork : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,
            ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(InterceptionResult.Suppress());
    }

    private sealed class ReaderCapture(bool empty) : DbCommandInterceptor
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
            foreach (var column in new[] { "Periodo", "Cod_Alumno", "Cod_Catedra", "Cod_Seccion", "Catedra", "Nombre", "Curso" })
                table.Columns.Add(column, typeof(string));
            if (!empty) table.Rows.Add("2024A", "A001", "MBA101", "1", "Gestión", "Alumno", "MBA101/1");
            return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(table.CreateDataReader()));
        }
    }
}

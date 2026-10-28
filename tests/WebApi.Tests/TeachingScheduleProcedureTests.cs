using System.Data;
using System.Data.Common;
using Domain.Common;
using Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace WebApi.Tests;

public sealed class TeachingScheduleProcedureTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Canonical_report_keeps_description_value_varchar_seven_output_and_nullable_units(bool nullInputs)
    {
        var capture = new ReaderCapture("report");
        await using var context = Context(capture);
        using var cancellation = new CancellationTokenSource();
        var output = new OutputParameter<int>();
        const string description = "Descripcion de promocion mayor a siete";
        var rows = await new SadContextProcedures(context).SpRec_ProgramacionDocenteXProgramaModalidadAsync(
            nullInputs ? null : "2024B", nullInputs ? null : "PERÍODO", nullInputs ? null : "MBA",
            nullInputs ? null : "Todas", nullInputs ? null : description, output, cancellation.Token);
        Assert.Equal(9, output.Value);
        Assert.Equal(cancellation.Token, capture.Token);
        Assert.StartsWith("EXEC @returnValue = [dbo].[spRec_ProgramacionDocenteXProgramaModalidad]", capture.Text);
        var parameters = capture.Parameters.ToDictionary(parameter => parameter.ParameterName);
        Assert.Equal(6, parameters.Count);
        foreach (var (name, size, value) in new[] { ("Periodo", 20, "2024B"), ("TipoPeriodo", 20, "PERÍODO"),
            ("Tipo_Alumno", 30, "MBA"), ("Modalidad", 30, "Todas"), ("Promocion", 7, description) })
        {
            Assert.Equal(size, parameters[name].Size);
            Assert.Equal(SqlDbType.VarChar, parameters[name].SqlDbType);
            Assert.Equal(nullInputs ? DBNull.Value : (object)value, parameters[name].Value);
        }
        AssertOutput(parameters["returnValue"]);
        var row = Assert.Single(rows!);
        Assert.Equal(("2024B", "2024 - Bimestre", "MBA101/1", "HIJA", "Cátedra", "Obligatoria", "Requisito", "Profesor", "Lunes"),
            (row.Periodo, row.PeriodoLargo, row.Curso, row.Cod_CatedraHija, row.Nom_CatedraHija, row.Tipo, row.Requisitos, row.Profesores, row.Clases));
        Assert.Null(row.Ud);
    }

    [Theory]
    [InlineData("periods")]
    [InlineData("programs")]
    [InlineData("promotions")]
    public async Task Canonical_catalogues_keep_metadata_and_result_shapes(string catalogue)
    {
        var capture = new ReaderCapture(catalogue);
        await using var context = Context(capture);
        var procedures = new SadContextProcedures(context);
        var output = new OutputParameter<int>();
        using var cancellation = new CancellationTokenSource();
        if (catalogue == "periods")
        {
            var row = Assert.Single((await procedures.SpRec_PeriodosAcademicosXTipoAsync("AÑO", "POSTGRADO", "19000", output, cancellation.Token))!);
            Assert.Equal(("2024", "2024"), (row.Periodo, row.PeriodoLargo));
            Assert.Equal(new[] { "Tipo", "Escuela", "Limite" }, capture.Parameters.Where(p => p.Direction == ParameterDirection.Input).Select(p => p.ParameterName));
            Assert.All(capture.Parameters.Where(p => p.Direction == ParameterDirection.Input), p => Assert.Equal(20, p.Size));
        }
        else if (catalogue == "programs")
        {
            var row = Assert.Single((await procedures.SpRec_TiposAlumnoXEscuelaAsync("POSTGRADO", "NO", output, cancellation.Token))!);
            Assert.Equal(("MBA", "Programa", 1), (row.Tipo, row.Nombre, row.ORDEN));
            Assert.Equal(new[] { "Escuela", "IncluirTodos" }, capture.Parameters.Where(p => p.Direction == ParameterDirection.Input).Select(p => p.ParameterName));
            Assert.Equal(new[] { 20, 2 }, capture.Parameters.Where(p => p.Direction == ParameterDirection.Input).Select(p => p.Size));
        }
        else
        {
            var row = Assert.Single((await procedures.SpRec_PromocionesPostgradoTodosAsync(output, cancellation.Token))!);
            Assert.Equal(("P2024", "Descripcion distinta"), (row.Promocion, row.Descripcion));
            Assert.Single(capture.Parameters);
        }
        Assert.Equal(cancellation.Token, capture.Token);
        Assert.Equal(9, output.Value);
        AssertOutput(capture.Parameters.Single(p => p.Direction == ParameterDirection.Output));
    }

    private static SadContext Context(ReaderCapture capture) => new(new DbContextOptionsBuilder<SadContext>()
        .UseSqlServer("Server=unused.invalid;Database=isolated;Integrated Security=true")
        .AddInterceptors(new NoNetwork(), capture).Options);
    private static void AssertOutput(SqlParameter parameter)
    {
        Assert.Equal(ParameterDirection.Output, parameter.Direction);
        Assert.Equal(SqlDbType.Int, parameter.SqlDbType);
    }
    private sealed class NoNetwork : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,
            ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(InterceptionResult.Suppress());
    }
    private sealed class ReaderCapture(string catalogue) : DbCommandInterceptor
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
            Parameters.Single(p => p.Direction == ParameterDirection.Output).Value = 9;
            var table = new DataTable();
            var columns = catalogue switch {
                "report" => new[] { "Periodo", "PeriodoLargo", "Curso", "Cod_CatedraHija", "Nom_CatedraHija", "Tipo", "Ud", "Requisitos", "Profesores", "Clases" },
                "periods" => ["Periodo", "PeriodoLargo"],
                "programs" => ["Tipo", "Nombre", "ORDEN"],
                _ => ["Promocion", "Descripcion"],
            };
            foreach (var column in columns) table.Columns.Add(column, column is "Ud" or "ORDEN" ? typeof(int) : typeof(string));
            object[] values = catalogue switch {
                "report" => ["2024B", "2024 - Bimestre", "MBA101/1", "HIJA", "Cátedra", "Obligatoria", DBNull.Value, "Requisito", "Profesor", "Lunes"],
                "periods" => ["2024", "2024"],
                "programs" => ["MBA", "Programa", 1],
                _ => ["P2024", "Descripcion distinta"],
            };
            table.Rows.Add(values);
            return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(table.CreateDataReader()));
        }
    }
}

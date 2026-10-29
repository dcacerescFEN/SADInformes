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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Q8_and_Q11_wrappers_preserve_distinct_shapes_and_parameters(bool q8)
    {
        var capture = new RatingReaderCapture(q8);
        await using var context = new SadContext(new DbContextOptionsBuilder<SadContext>()
            .UseSqlServer("Server=unused.invalid;Database=isolated;Integrated Security=true")
            .AddInterceptors(new NoNetwork(), capture).Options);
        var output = new OutputParameter<int>();
        var procedures = new SadContextProcedures(context);
        var token = CancellationToken.None;
        if (q8)
        {
            var rows = await procedures.spRec_EncuestaGraduadosEstadisticasQ8Async("2024A", "MBA", "PT", output, token);
            Assert.Equal(9, output.Value);
            Assert.StartsWith("EXEC @returnValue = [dbo].[spRec_EncuestaGraduadosEstadisticasQ8]", capture.Text);
            var row = Assert.Single(rows!);
            Assert.Equal(("C1", "Cátedra Uno", 1, 2, 3, 4, 5, 3.0d, 15),
                (row.CodCatedra, row.Descripcion, row.Alternativa1, row.Alternativa2, row.Alternativa3, row.Alternativa4, row.Alternativa5, row.Promedio, row.Total));
        }
        else
        {
            var rows = await procedures.spRec_EncuestaGraduadosEstadisticasQ11Async("2024A", "MBA", "PT", output, token);
            Assert.Equal(9, output.Value);
            Assert.StartsWith("EXEC @returnValue = [dbo].[spRec_EncuestaGraduadosEstadisticasQ11]", capture.Text);
            var row = Assert.Single(rows!);
            Assert.Equal(("Q11", "Habilidad", 1, 2, 3, 4, 5, 6, 7, 4.0d, 28),
                (row.Pregunta, row.Descripcion, row.Alternativa1, row.Alternativa2, row.Alternativa3, row.Alternativa4, row.Alternativa5, row.Alternativa6, row.Alternativa7, row.Promedio, row.Total));
        }
        var parameters = capture.Parameters.ToDictionary(parameter => parameter.ParameterName);
        Assert.Equal((5, 30, 10), (parameters["Periodo"].Size, parameters["Tipo_Alumno"].Size, parameters["Modalidad"].Size));
        Assert.Equal(ParameterDirection.Output, parameters["returnValue"].Direction);
    }

    [Theory]
    [InlineData("Q9I")]
    [InlineData("Q9II")]
    [InlineData("Q9III")]
    [InlineData("Q10")]
    [InlineData("DETAIL")]
    public async Task Teacher_wrappers_preserve_canonical_names_parameters_and_shapes(string kind)
    {
        var capture = new TeacherReaderCapture(kind);
        await using var context = new SadContext(new DbContextOptionsBuilder<SadContext>()
            .UseSqlServer("Server=unused.invalid;Database=isolated;Integrated Security=true")
            .AddInterceptors(new NoNetwork(), capture).Options);
        var output = new OutputParameter<int>();
        var procedures = new SadContextProcedures(context);
        switch (kind)
        {
            case "Q9I":
            {
                var rows = await procedures.spRec_EncuestaGraduadosEstadisticasProfesoresIAsync("2024A", "MBA", "PT", output);
                Assert.StartsWith("EXEC @returnValue = [dbo].[spRec_EncuestaGraduados_Estadisticas_ProfesoresI]", capture.Text);
                Assert.Equal(("Profesor I", 5), (Assert.Single(rows!).Profesor, Assert.Single(rows!).Frecuencia));
                break;
            }
            case "Q9II":
            {
                var row = Assert.Single(await procedures.spRec_EncuestaGraduadosEstadisticasProfesoresIIAsync("2024A", "MBA", "PT", output))!;
                Assert.StartsWith("EXEC @returnValue = [dbo].[spRec_EncuestaGraduados_Estadisticas_ProfesoresII]", capture.Text);
                Assert.Equal("Profesor II", row.Profesor);
                break;
            }
            case "Q9III":
            {
                var row = Assert.Single(await procedures.spRec_EncuestaGraduadosEstadisticasProfesoresIIIAsync("2024A", "MBA", "PT", output))!;
                Assert.StartsWith("EXEC @returnValue = [dbo].[spRec_EncuestaGraduados_Estadisticas_ProfesoresIII]", capture.Text);
                Assert.Equal("Profesor III", row.Profesor);
                break;
            }
            case "Q10":
            {
                var rows = await procedures.spRec_EncuestaGraduadosEstadisticasInsatisfaccionAsync("2024A", "MBA", "PT", output);
                Assert.StartsWith("EXEC @returnValue = [dbo].[spRec_EncuestaGraduados_EstadisticasInsatisfaccion]", capture.Text);
                var row = Assert.Single(rows!);
                Assert.Equal(("Profesor Insatisfecho", 3, "P9", "2024A", "MBA", "PT"),
                    (row.Profesor, row.Cantidad, row.CodPersona, row.Periodo, row.TipoAlumno, row.Modalidad));
                break;
            }
            default:
            {
                var rows = await procedures.spRec_EncuestaGraduadosEstadisticasDetalleProfAsync("2024A", "MBA", "PT", "P9", output);
                Assert.StartsWith("EXEC @returnValue = [dbo].[spRec_EncuestaGraduados_Estadisticas_DetallesProf]", capture.Text);
                Assert.Equal("Motivo", Assert.Single(rows!).Motivos);
                Assert.Equal(20, capture.Parameters.Single(parameter => parameter.ParameterName == "Profesor").Size);
                break;
            }
        }
        Assert.Equal(9, output.Value);
        var parameters = capture.Parameters.ToDictionary(parameter => parameter.ParameterName);
        Assert.Equal((5, 30, 10), (parameters["Periodo"].Size, parameters["Tipo_Alumno"].Size, parameters["Modalidad"].Size));
        Assert.Equal(ParameterDirection.Output, parameters["returnValue"].Direction);
    }

    [Fact]
    public async Task Q17_wrapper_uses_general_evaluation_procedure()
    {
        var capture = new ReaderCapture();
        await using var context = new SadContext(new DbContextOptionsBuilder<SadContext>()
            .UseSqlServer("Server=unused.invalid;Database=isolated;Integrated Security=true")
            .AddInterceptors(new NoNetwork(), capture).Options);
        var output = new OutputParameter<int>();
        var rows = await new SadContextProcedures(context).spRec_EncuestaGraduadosEstadisticasQ17Async("2024A", "MBA", "PT", output);
        Assert.StartsWith("EXEC @returnValue = [dbo].[spRec_EncuestaGraduadosEstadisticasQ17]", capture.Text);
        Assert.Equal(9, output.Value);
        Assert.Equal("Pregunta A", Assert.Single(rows!).Descripcion);
    }

    private sealed class NoNetwork : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,
            ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(InterceptionResult.Suppress());
    }
    private sealed class TeacherReaderCapture(string kind) : DbCommandInterceptor
    {
        public string Text { get; private set; } = "";
        public SqlParameter[] Parameters { get; private set; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Text = command.CommandText;
            Parameters = command.Parameters.Cast<SqlParameter>().ToArray();
            Parameters.Single(parameter => parameter.Direction == ParameterDirection.Output).Value = 9;
            var table = new DataTable();
            if (kind.StartsWith("Q9", StringComparison.Ordinal))
            {
                table.Columns.Add("Profesor", typeof(string)); table.Columns.Add("Frecuencia", typeof(int));
                table.Rows.Add("Profesor " + kind[2..], 5);
            }
            else if (kind == "Q10")
            {
                foreach (var column in new[] { "Profesor", "Cod_Persona", "Periodo", "Tipo_Alumno", "Modalidad" }) table.Columns.Add(column, typeof(string));
                table.Columns.Add("Cantidad", typeof(int));
                table.Rows.Add("Profesor Insatisfecho", "P9", "2024A", "MBA", "PT", 3);
            }
            else
            {
                table.Columns.Add("Motivos", typeof(string)); table.Rows.Add("Motivo");
            }
            return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(table.CreateDataReader()));
        }
    }

    private sealed class RatingReaderCapture(bool q8) : DbCommandInterceptor
    {
        public string Text { get; private set; } = "";
        public SqlParameter[] Parameters { get; private set; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Text = command.CommandText;
            Parameters = command.Parameters.Cast<SqlParameter>().ToArray();
            Parameters.Single(parameter => parameter.Direction == ParameterDirection.Output).Value = 9;
            var table = new DataTable();
            if (q8)
            {
                table.Columns.Add("Cod_Catedra", typeof(string)); table.Columns.Add("Descripcion", typeof(string));
                for (var i = 1; i <= 5; i++) table.Columns.Add("Alternativa" + i, typeof(int));
                table.Columns.Add("Promedio", typeof(double)); table.Columns.Add("Total", typeof(int));
                table.Rows.Add("C1", "Cátedra Uno", 1, 2, 3, 4, 5, 3.0d, 15);
            }
            else
            {
                table.Columns.Add("Pregunta", typeof(string)); table.Columns.Add("Descripcion", typeof(string));
                for (var i = 1; i <= 7; i++) table.Columns.Add("Alternativa" + i, typeof(int));
                table.Columns.Add("Promedio", typeof(double)); table.Columns.Add("Total", typeof(int));
                table.Rows.Add("Q11", "Habilidad", 1, 2, 3, 4, 5, 6, 7, 4.0d, 28);
            }
            return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(table.CreateDataReader()));
        }
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

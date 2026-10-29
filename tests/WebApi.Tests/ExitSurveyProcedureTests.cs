using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Domain.Common;
using Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WebApi.Tests;

public sealed class ExitSurveyProcedureTests
{
    [Fact]
    public async Task Catalogues_and_listing_keep_canonical_parameters_and_accented_columns()
    {
        using var connection = new RecordingConnection();
        await using var context = new SadContext(new DbContextOptionsBuilder<SadContext>().UseSqlServer(connection).Options);
        var procedures = new SadContextProcedures(context);
        var types = await procedures.spRec_EncuestaGraduados_TiposAlumnoAsync("SI", "user", cancellationToken: CancellationToken.None);
        Assert.Equal(("IncluirTodos", "Usuario"), (connection.LastParameters[0].ParameterName, connection.LastParameters[1].ParameterName));
        Assert.Equal((2, 20), (connection.LastParameters[0].Size, connection.LastParameters[1].Size));
        Assert.Equal("user", connection.LastParameters[1].Value);
        var list = await procedures.spRec_EncuestaGraduados_AlumnosAsync("MBA", "PT", "2024", cancellationToken: CancellationToken.None);
        Assert.Equal("100-20241", list![0].CodAlumno);
        Assert.Equal("P1", list[0].CodPersona);
        Assert.Equal("15-12-2023", list[0].FechaEgreso?.ToString("dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task Enrollment_wrapper_uses_typed_parameters_output_and_sql_raw_write()
    {
        using var connection = new RecordingConnection();
        await using var context = new SadContext(new DbContextOptionsBuilder<SadContext>().UseSqlServer(connection).Options);
        var output = new OutputParameter<int>();
        using var cancellation = new CancellationTokenSource();
        var affected = await new SadContextProcedures(context).spCre_EncuestaGraduados_Ins_AlumnoAsync(
            "100-20241", "20241", "15/12/2023", output, cancellation.Token);
        Assert.Contains("[dbo].[spCre_EncuestaGraduados_Ins_Alumno]", connection.LastText);
        Assert.Equal(new[] { "Cod_Alumno", "Periodo", "Fecha_Egreso", "returnValue" }, connection.LastParameters.Select(parameter => parameter.ParameterName));
        Assert.Equal(new[] { 30, 5, 40, 0 }, connection.LastParameters.Select(parameter => parameter.Size));
        Assert.Equal(SqlDbType.VarChar, connection.LastParameters[0].SqlDbType);
        Assert.Equal(7, output.Value);
        Assert.Equal(1, affected);
    }

    private sealed class RecordingConnection : DbConnection
    {
        private ConnectionState state;
        [AllowNull] public override string ConnectionString { get; set; } = "";
        public override string Database => "isolated";
        public override string DataSource => "isolated";
        public override string ServerVersion => "16.0";
        public override ConnectionState State => state;
        public SqlParameter[] LastParameters { get; internal set; } = [];
        public string LastText { get; internal set; } = "";
        public CancellationToken LastToken { get; internal set; }
        public override void Open() => state = ConnectionState.Open;
        public override void Close() => state = ConnectionState.Closed;
        public override void ChangeDatabase(string name) => throw new NotSupportedException();
        protected override DbTransaction BeginDbTransaction(IsolationLevel level) => throw new NotSupportedException();
        protected override DbCommand CreateDbCommand() => new RecordingCommand(this);
    }
    private sealed class RecordingCommand(RecordingConnection connection) : DbCommand
    {
        private readonly SqlCommand parameters = new();
        [AllowNull] public override string CommandText { get; set; } = "";
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; } = connection;
        protected override DbTransaction? DbTransaction { get; set; }
        protected override DbParameterCollection DbParameterCollection => parameters.Parameters;
        protected override DbParameter CreateDbParameter() => new SqlParameter();
        public override void Cancel() { }
        public override void Prepare() { }
        public override object? ExecuteScalar() => throw new NotSupportedException();
        public override int ExecuteNonQuery() => Record(1);
        public override Task<int> ExecuteNonQueryAsync(CancellationToken ct) => Task.FromResult(Record(1));
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => Read(default);
        protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken ct) => Task.FromResult(Read(ct));
        private int Record(int affected)
        {
            Capture(); foreach (var output in connection.LastParameters.Where(parameter => parameter.Direction == ParameterDirection.Output)) output.Value = 7;
            return affected;
        }
        private DbDataReader Read(CancellationToken ct)
        {
            Capture(ct);
            var table = new DataTable();
            if (CommandText.Contains("spRec_EncuestaGraduados_TiposAlumno", StringComparison.Ordinal))
            {
                table.Columns.Add("Tipo", typeof(string)); table.Columns.Add("Nombre", typeof(string)); table.Columns.Add("Orden", typeof(int));
                table.Rows.Add("Todos", "Todos", 0);
                return table.CreateDataReader();
            }
            table.Columns.Add("Nombre", typeof(string)); table.Columns.Add("Rut", typeof(string)); table.Columns.Add("Cod_Alumno", typeof(string));
            table.Columns.Add("Cod_Persona", typeof(string)); table.Columns.Add("Sem_Ingreso", typeof(string)); table.Columns.Add("Tipo_Ingreso", typeof(string));
            table.Columns.Add("Tipo_Alumno", typeof(string)); table.Columns.Add("Fecha_Egreso", typeof(DateTime)); table.Columns.Add("Fecha_Titulacion", typeof(DateTime));
            table.Columns.Add("Inscrito", typeof(string)); table.Columns.Add("Porcentaje", typeof(string)); table.Columns.Add("Modalidad", typeof(string));
            table.Rows.Add("ANA", "1-K", "100-20241", "P1", "2024A", "REGULAR", "MBA", new DateTime(2023, 12, 15), new DateTime(2024, 1, 10), "SI", "40%", "PT");
            return table.CreateDataReader();
        }
        private void Capture(CancellationToken ct = default)
        {
            connection.LastText = CommandText; connection.LastToken = ct;
            connection.LastParameters = parameters.Parameters.Cast<SqlParameter>().ToArray();
        }
        protected override void Dispose(bool disposing) { if (disposing) parameters.Dispose(); base.Dispose(disposing); }
    }
}

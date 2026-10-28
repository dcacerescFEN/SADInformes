using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Application.Common.Interfaces;
using Domain.Common;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebApi.Tests;

public sealed class SadPersistenceTests
{
    [Fact]
    public void Context_and_procedures_resolve_the_same_scoped_context()
    {
        var services = new ServiceCollection();
        services.AddInfrastructureServices(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:Sad"] = "Server=unused;Database=unused;Integrated Security=true" }).Build());
        using var provider = services.BuildServiceProvider();
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();
        Assert.Same(first.ServiceProvider.GetRequiredService<SadContext>(), first.ServiceProvider.GetRequiredService<ISadContext>());
        Assert.NotSame(first.ServiceProvider.GetRequiredService<ISadContext>(), second.ServiceProvider.GetRequiredService<ISadContext>());
        Assert.IsType<SadContextProcedures>(first.ServiceProvider.GetRequiredService<ISadContextProcedures>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Generic_transaction_preserves_isolation_enlists_procedures_and_disposes(bool commit)
    {
        using var connection = new RecordingConnection();
        await using var context = new SadContext(new DbContextOptionsBuilder<SadContext>().UseSqlServer(connection).Options);
        ISadContext contract = context;
        var output = new OutputParameter<int>();
        await using (var transaction = await contract.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            Assert.Equal(IsolationLevel.Serializable, connection.Transaction!.IsolationLevel);
            Assert.Equal("SET XACT_ABORT ON;", Assert.Single(connection.Commands).Text);
            var affected = await Write(new SadContextProcedures(context), output);
            Assert.Equal(-1, affected);
            Assert.Equal(7, output.Value);
            Assert.All(connection.Commands, command => Assert.Same(connection.Transaction, command.Transaction));
            if (commit) await transaction.CommitAsync();
            else await transaction.RollbackAsync();
        }
        Assert.True(connection.Transaction!.Disposed);
        Assert.Equal(commit, connection.Transaction.Committed);
        Assert.Equal(!commit, connection.Transaction.RolledBack);
        Assert.Null(context.Database.CurrentTransaction);
    }

    [Fact]
    public async Task Disposing_uncommitted_transaction_rolls_back_and_setup_failure_releases_it()
    {
        using var connection = new RecordingConnection();
        await using var context = new SadContext(new DbContextOptionsBuilder<SadContext>().UseSqlServer(connection).Options);
        await using (await context.BeginTransactionAsync(IsolationLevel.Serializable)) { }
        Assert.True(connection.Transaction!.RolledBack);
        connection.FailSetup = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.BeginTransactionAsync(IsolationLevel.ReadCommitted));
        Assert.True(connection.Transaction.Disposed);
        Assert.True(connection.Transaction.RolledBack);
        Assert.Null(context.Database.CurrentTransaction);
    }

    [Fact]
    public async Task Enrollment_procedure_preserves_canonical_parameters_output_and_cancellation()
    {
        using var connection = new RecordingConnection();
        await using var context = new SadContext(new DbContextOptionsBuilder<SadContext>().UseSqlServer(connection).Options);
        var output = new OutputParameter<int>();
        using var cancellation = new CancellationTokenSource();
        Assert.Equal(-1, await Write(new SadContextProcedures(context), output, cancellation.Token));
        var command = Assert.Single(connection.Commands);
        Assert.StartsWith("EXEC @returnValue = [dbo].[spCre_MovInscAdministrativo_Postgrado]", command.Text);
        Assert.Equal(cancellation.Token, command.Token);
        Assert.Equal(7, output.Value);
        var parameters = command.Parameters.ToDictionary(parameter => parameter.ParameterName);
        Assert.Equal(20, parameters.Count);
        foreach (var (name, value, size) in new (string, object, int)[] {
            ("Cod_Alumno", "A1", 20), ("Periodo", "2024A", 20), ("Cod_Catedra", "MBA00101", 30), ("Cod_Seccion", "1", 2),
            ("Accion", "AGREGA", 10), ("Pc", "127.0.0.1", 100), ("Extracurricular", "NO", 2), ("MencionCurso", "NA", 2),
            ("Tipo_Catedra", "0", 2), ("Nota", "", 5), ("Situacion", "PENDIENTE", 10), ("ObservacionesNota", "", 1000),
            ("Usuario", "usuario-prueba", 30), ("CalificadorElectivo", DBNull.Value, 10),
            ("TipoCatedraSeleccionable", DBNull.Value, 1), ("SistemaProcesa", "SADInformes", 15) })
        {
            Assert.Equal(value, parameters[name].Value);
            Assert.Equal(size, parameters[name].Size);
            Assert.Equal(SqlDbType.VarChar, parameters[name].SqlDbType);
        }
        foreach (var name in new[] { "EliminaAcademico", "AutorizadoPorAlumno", "RegistroEspecial" })
        {
            Assert.Equal(0, parameters[name].Value);
            Assert.Equal(SqlDbType.Int, parameters[name].SqlDbType);
        }
        Assert.Equal(ParameterDirection.Output, parameters["returnValue"].Direction);
        Assert.Equal(SqlDbType.Int, parameters["returnValue"].SqlDbType);
    }

    [Fact]
    public async Task Enrollment_procedure_accepts_canonical_null_inputs_without_fabricating_results()
    {
        using var connection = new RecordingConnection();
        await using var context = new SadContext(new DbContextOptionsBuilder<SadContext>().UseSqlServer(connection).Options);
        Assert.Equal(-1, await new SadContextProcedures(context).SpCre_MovInscAdministrativo_PostgradoAsync(
            null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null));
        Assert.All(Assert.Single(connection.Commands).Parameters.Where(parameter => parameter.Direction == ParameterDirection.Input),
            parameter => Assert.Equal(DBNull.Value, parameter.Value));
    }

    private static Task<int> Write(ISadContextProcedures procedures, OutputParameter<int> output, CancellationToken ct = default) =>
        procedures.SpCre_MovInscAdministrativo_PostgradoAsync("A1", "2024A", "MBA00101", "1", "AGREGA", "127.0.0.1",
            "NO", "NA", "0", 0, 0, "", "PENDIENTE", "", 0, "usuario-prueba", null, null, "SADInformes", output, ct);

    // A recording ADO.NET boundary exercises real EF relational transaction/command dispatch, not a live database.
    private sealed class RecordingConnection : DbConnection
    {
        private ConnectionState state;
        [AllowNull] public override string ConnectionString { get; set; } = "";
        public override string Database => "isolated";
        public override string DataSource => "isolated";
        public override string ServerVersion => "16.0";
        public override ConnectionState State => state;
        public RecordingTransaction? Transaction { get; private set; }
        public bool FailSetup { get; set; }
        public List<(string Text, DbTransaction? Transaction, SqlParameter[] Parameters, CancellationToken Token)> Commands { get; } = [];
        public override void Open() => state = ConnectionState.Open;
        public override void Close() => state = ConnectionState.Closed;
        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => Transaction = new(this, isolationLevel);
        protected override DbCommand CreateDbCommand() => new RecordingCommand(this);
    }

    private sealed class RecordingTransaction(RecordingConnection connection, IsolationLevel isolation) : DbTransaction
    {
        public bool Committed { get; private set; }
        public bool RolledBack { get; private set; }
        public bool Disposed { get; private set; }
        public override IsolationLevel IsolationLevel => isolation;
        protected override DbConnection DbConnection => connection;
        public override void Commit() => Committed = true;
        public override void Rollback() => RolledBack = true;
        protected override void Dispose(bool disposing)
        {
            if (!Committed) Rollback();
            Disposed = true;
            base.Dispose(disposing);
        }
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
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();
        public override int ExecuteNonQuery() => Execute(default);
        public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) => Task.FromResult(Execute(cancellationToken));
        private int Execute(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (connection.FailSetup && CommandText == "SET XACT_ABORT ON;") throw new InvalidOperationException("setup failed");
            var values = parameters.Parameters.Cast<SqlParameter>().ToArray();
            connection.Commands.Add((CommandText, DbTransaction, values, token));
            foreach (var parameter in values.Where(parameter => parameter.Direction == ParameterDirection.Output)) parameter.Value = 7;
            return -1;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) parameters.Dispose();
            base.Dispose(disposing);
        }
    }
}

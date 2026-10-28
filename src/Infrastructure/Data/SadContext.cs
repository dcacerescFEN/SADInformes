using System.Data;
using Application.Reports.Postgraduate.MassEnrollment.Common;
using Domain.Entities.Sad.Sp;
using Microsoft.Data.SqlClient;
using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Data;

/// <summary>
/// Contexto del SAD: consultas de informes y transacciones de inscripción administrativa.
/// La cadena de conexión se toma de
/// <c>ConnectionStrings:Sad</c> y nunca se versiona.
/// </summary>
public partial class SadContext(DbContextOptions<SadContext> options)
    : DbContext(options), ISadContext
{
    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
        => Database.BeginTransactionAsync(cancellationToken);

    public DatabaseFacade DatabaseFacade => Database;

    public virtual Task<int> ExecuteSqlRawAsync(string sql, object[] parameters, CancellationToken ct) =>
        Database.ExecuteSqlRawAsync(sql, parameters, ct);

    public virtual async Task<IEnrollmentTransaction> BeginEnrollmentTransactionAsync(CancellationToken ct)
    {
        var transaction = await Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            await ExecuteSqlRawAsync("SET XACT_ABORT ON;", [], ct);
            return new EnrollmentTransaction(transaction);
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

    public virtual async Task<bool> IsEnrollmentConfirmedAsync(
        string student, string period, string course, string section, CancellationToken ct)
    {
        var rows = await this.SqlQueryAsync<SpRecEnrollmentStateResult>(
            """
            SELECT CAST(CASE WHEN EXISTS (
                SELECT 1 FROM dbo.Alumnos_Catedras WITH (UPDLOCK, HOLDLOCK)
                WHERE Cod_Alumno = @student AND Periodo = @period AND Cod_Catedra = @course
                  AND Cod_Seccion = @section AND Categoria = 'CURSADA'
            ) AND dbo.CatedraYaInscrita(@student, @course, @period) <> 0
            THEN 1 ELSE 0 END AS bit) AS Enrolled
            """,
            [Text("student", student, 20), Text("period", period, 20),
             Text("course", course, 30), Text("section", section, 2)], ct);
        return rows.Single().Enrolled;
    }

    private static SqlParameter Text(string name, string value, int size) =>
        new("@" + name, SqlDbType.VarChar, size) { Value = value };

    private sealed class EnrollmentTransaction(IDbContextTransaction transaction) : IEnrollmentTransaction
    {
        public Task CommitAsync(CancellationToken ct) => transaction.CommitAsync(ct);
        // Dispose rolls back an uncommitted outer transaction, including nested SP writes.
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}

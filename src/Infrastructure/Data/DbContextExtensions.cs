using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

/// <summary>
/// Consulta SQL cruda que materializa tipos sin clave, como el contexto generado
/// de las aplicaciones de referencia.
/// </summary>
public static class DbContextExtensions
{
    public static async Task<List<T>> SqlQueryAsync<T>(
        this DbContext db, string sql, object[]? parameters = null, CancellationToken cancellationToken = default)
        where T : class
    {
        parameters ??= [];
        return await db.Database.SqlQueryRaw<T>(sql, parameters).ToListAsync(cancellationToken);
    }
}

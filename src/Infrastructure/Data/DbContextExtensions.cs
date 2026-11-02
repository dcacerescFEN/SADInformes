using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

/// <summary>
/// Consulta SQL cruda que materializa tipos sin clave, como el contexto generado
/// de las aplicaciones de referencia.
/// </summary>
public static class DbContextExtensions
{
    public static async Task<List<T>> SqlQueryAsync<T>(
        this DbContext db, string sql, object[]? parameters = null,
        CancellationToken cancellationToken = default, int? commandTimeoutSeconds = null)
        where T : class
    {
        parameters ??= [];
        var previousTimeout = db.Database.GetCommandTimeout();
        if (commandTimeoutSeconds is int seconds) db.Database.SetCommandTimeout(seconds);
        try
        {
            return await db.Database.SqlQueryRaw<T>(sql, parameters).ToListAsync(cancellationToken);
        }
        finally
        {
            if (commandTimeoutSeconds is not null) db.Database.SetCommandTimeout(previousTimeout);
        }
    }
}

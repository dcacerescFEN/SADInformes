using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Data;

/// <summary>
/// Contexto de solo lectura del SAD. La cadena de conexión se toma de
/// <c>ConnectionStrings:Sad</c> y nunca se versiona.
/// </summary>
public partial class SadContext(DbContextOptions<SadContext> options)
    : DbContext(options), ISadContext
{
    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
        => Database.BeginTransactionAsync(cancellationToken);

    public DatabaseFacade DatabaseFacade => Database;
}

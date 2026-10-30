using Application.Common.Interfaces;
using Domain.Entities.SigmaRemuneraciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Data;

/// <summary>Minimal read-only mapping of the external Sigma remuneration database.</summary>
public sealed class SigmaRemuneracionesContext(
    DbContextOptions<SigmaRemuneracionesContext> options, IConfiguration? configuration = null)
    : DbContext(options), ISigmaRemuneracionesContext
{
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(configuration?.GetConnectionString("SigmaRemuneraciones"));

    public DbSet<Remuneracion> Remuneraciones => Set<Remuneracion>();
    public DbSet<Prestacion> Prestaciones => Set<Prestacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Remuneracion>(entity =>
        {
            entity.ToTable("remuneracion");
            entity.HasKey(row => row.Id).HasName("PRIMARY");
            entity.Property(row => row.Id).HasColumnName("id");
            entity.Property(row => row.CreatedAt).HasColumnName("created_at").HasColumnType("datetime");
            entity.Property(row => row.EstadoId).HasColumnName("estado_id");
            entity.Property(row => row.BanderaId).HasColumnName("bandera_id");
        });
        modelBuilder.Entity<Prestacion>(entity =>
        {
            entity.ToTable("prestacion");
            entity.HasKey(row => row.Id).HasName("PRIMARY");
            entity.Property(row => row.Id).HasColumnName("id");
            entity.Property(row => row.RemuneracionId).HasColumnName("remuneracion_id");
            entity.Property(row => row.CodImp).HasColumnName("cod_imp").HasMaxLength(255);
        });
    }
}

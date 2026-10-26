using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public sealed class AppConfigContext(DbContextOptions<AppConfigContext> options) : DbContext(options)
{
    public DbSet<Domain.Entities.AppConfig.Configuration> Configurations => Set<Domain.Entities.AppConfig.Configuration>();
    public DbSet<Domain.Entities.AppConfig.Application> Applications => Set<Domain.Entities.AppConfig.Application>();
    public DbSet<Domain.Entities.AppConfig.Environment> Environments => Set<Domain.Entities.AppConfig.Environment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Domain.Entities.AppConfig.Application>().Property(row => row.ApplicationName).HasColumnType("varchar(30)");
        builder.Entity<Domain.Entities.AppConfig.Environment>().Property(row => row.EnvironmentName).HasColumnType("varchar(30)");
        builder.Entity<Domain.Entities.AppConfig.Configuration>().Property(row => row.ConfigurationName).HasColumnType("varchar(100)");
        builder.Entity<Domain.Entities.AppConfig.Configuration>().Property(row => row.ConfigurationValue).HasColumnType("varchar(4000)");
    }
}

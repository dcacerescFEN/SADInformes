using Domain.Entities.Sad.Tables.dbo;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

/// <summary>
/// Definition of the SAD database: mapped tables and their keys/columns.
/// Queries and commands belong to data gateways such as MassEnrollmentData;
/// stored-procedure execution belongs to SadContextProcedures.
/// </summary>
public partial class SadContext(DbContextOptions<SadContext> options)
    : DbContext(options), Application.Common.Interfaces.ISadContext
{
    public DbSet<Catedra> Catedras => Set<Catedra>();
    public DbSet<Curso> Cursos => Set<Curso>();
    public DbSet<PlanesFormacionCatedra> PlanesFormacionCatedras => Set<PlanesFormacionCatedra>();
    public DbSet<VacantesCurso> VacantesCursos => Set<VacantesCurso>();
    public DbSet<AlumnosCatedra> AlumnosCatedras => Set<AlumnosCatedra>();
    public DbSet<MovimientosInscripcion> MovimientosInscripcions => Set<MovimientosInscripcion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Keys, column names, lengths and collations follow MappingDB/Models/SadContext.cs.
        modelBuilder.Entity<Catedra>(entity =>
        {
            entity.ToTable("Catedras", "dbo");
            entity.HasKey(row => row.CodCatedra);
            entity.Property(row => row.CodCatedra)
                .HasColumnName("Cod_Catedra")
                .HasMaxLength(30)
                .IsUnicode(false)
                .UseCollation("Compatibility_42_409_30003");
            entity.Property(row => row.NomCatedra)
                .HasColumnName("Nom_Catedra")
                .HasMaxLength(300)
                .UseCollation("Compatibility_42_409_30003");
        });
        modelBuilder.Entity<Curso>(entity =>
        {
            entity.ToTable("Cursos", "dbo");
            entity.HasKey(row => new { row.Periodo, row.CodCatedra, row.CodSeccion });
            entity.Property(row => row.Periodo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .UseCollation("Compatibility_42_409_30003");
            entity.Property(row => row.CodCatedra)
                .HasColumnName("Cod_Catedra")
                .HasMaxLength(30)
                .IsUnicode(false)
                .UseCollation("Compatibility_42_409_30003");
            entity.Property(row => row.CodSeccion)
                .HasColumnName("Cod_Seccion")
                .HasMaxLength(2)
                .IsUnicode(false)
                .UseCollation("Compatibility_42_409_30003");
            entity.Property(row => row.TipoAlumnoPostgrado)
                .HasColumnName("Tipo_AlumnoPostgrado")
                .HasMaxLength(30)
                .IsUnicode(false)
                .UseCollation("Compatibility_42_409_30003");
            entity.Property(row => row.ModalidadPostgrado)
                .HasMaxLength(30)
                .IsUnicode(false)
                .UseCollation("Compatibility_42_409_30003");
            entity.Property(row => row.DecretoPostgrado)
                .HasMaxLength(30)
                .IsUnicode(false)
                .UseCollation("Compatibility_42_409_30003");
        });
        modelBuilder.Entity<PlanesFormacionCatedra>(entity =>
        {
            entity.ToTable("PlanesFormacion_Catedras", "dbo");
            entity.HasKey(row => new { row.Decreto, row.CodPlanFormacion, row.CodCatedra });
            entity.Property(row => row.Decreto)
                .HasMaxLength(30)
                .IsUnicode(false)
                .UseCollation("Compatibility_42_409_30003");
            entity.Property(row => row.CodPlanFormacion)
                .HasColumnName("Cod_PlanFormacion")
                .HasMaxLength(50)
                .IsUnicode(false)
                .UseCollation("Compatibility_42_409_30003");
            entity.Property(row => row.CodCatedra)
                .HasColumnName("Cod_Catedra")
                .HasMaxLength(30)
                .IsUnicode(false)
                .UseCollation("Compatibility_42_409_30003");
            entity.Property(row => row.TipoCatedra)
                .HasColumnName("Tipo_Catedra")
                .HasMaxLength(2)
                .IsUnicode(false)
                .UseCollation("Compatibility_42_409_30003");
        });
        modelBuilder.Entity<VacantesCurso>(entity =>
        {
            entity.ToTable("Vacantes_Cursos", "dbo");
            entity.HasKey(row => new { row.Periodo, row.CodCatedra, row.CodSeccion, row.TipoAlumno });
            entity.Property(row => row.Periodo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .UseCollation("Compatibility_42_409_30003");
            entity.Property(row => row.CodCatedra)
                .HasColumnName("Cod_Catedra")
                .HasMaxLength(30)
                .IsUnicode(false)
                .UseCollation("Compatibility_42_409_30003");
            entity.Property(row => row.CodSeccion)
                .HasColumnName("Cod_Seccion")
                .HasMaxLength(2)
                .IsUnicode(false)
                .UseCollation("Compatibility_42_409_30003");
            entity.Property(row => row.TipoAlumno)
                .HasColumnName("Tipo_Alumno")
                .HasMaxLength(30)
                .IsUnicode(false)
                .UseCollation("Compatibility_42_409_30003");
            entity.Property(row => row.VacOfrecidas)
                .HasColumnName("Vac_Ofrecidas");
        });
        modelBuilder.Entity<AlumnosCatedra>(entity =>
        {
            entity.ToTable("Alumnos_Catedras", "dbo", table => table.HasTrigger("Audit_AlumnosCatedras"));
            entity.HasKey(row => new { row.CodAlumno, row.Periodo, row.CodCatedra, row.CodSeccion });
            entity.Property(row => row.CodAlumno)
                .HasColumnName("Cod_Alumno")
                .HasMaxLength(20)
                .IsUnicode(false)
                .UseCollation("Compatibility_42_409_30003");
            entity.Property(row => row.Periodo)
                .HasMaxLength(5)
                .IsUnicode(false)
                .UseCollation("Compatibility_42_409_30003");
            entity.Property(row => row.CodCatedra)
                .HasColumnName("Cod_Catedra")
                .HasMaxLength(30)
                .IsUnicode(false)
                .UseCollation("Compatibility_42_409_30003");
            entity.Property(row => row.CodSeccion)
                .HasColumnName("Cod_Seccion")
                .HasMaxLength(2)
                .IsUnicode(false)
                .UseCollation("Compatibility_42_409_30003");
            entity.Property(row => row.Categoria)
                .HasMaxLength(20)
                .IsUnicode(false)
                .UseCollation("Compatibility_42_409_30003");
        });
        modelBuilder.Entity<MovimientosInscripcion>(entity =>
        {
            entity.ToTable("Movimientos_Inscripcion", "dbo");
            entity.HasKey(row => row.Identificacion);
            entity.Property(row => row.CodAlumno)
                .HasColumnName("Cod_Alumno")
                .HasMaxLength(20)
                .IsUnicode(false)
                .UseCollation("Compatibility_42_409_30003");
            entity.Property(row => row.Periodo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .UseCollation("Compatibility_42_409_30003");
            entity.Property(row => row.CodCatedra)
                .HasColumnName("Cod_Catedra")
                .HasMaxLength(30)
                .IsUnicode(false)
                .UseCollation("Compatibility_42_409_30003");
            entity.Property(row => row.CodSeccion)
                .HasColumnName("Cod_Seccion")
                .HasMaxLength(2)
                .IsUnicode(false)
                .UseCollation("Compatibility_42_409_30003");
            entity.Property(row => row.TipoMovimiento)
                .HasMaxLength(10)
                .IsUnicode(false)
                .UseCollation("Compatibility_42_409_30003");
            entity.Property(row => row.Estado)
                .HasMaxLength(10)
                .IsUnicode(false)
                .UseCollation("Compatibility_42_409_30003");
        });
    }
}

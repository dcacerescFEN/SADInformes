using System.Data;
using Application.Reports.Postgraduate.MassEnrollment.Common;
using Domain.Entities.Sad.Sp;
using Domain.Entities.Sad.Tables.dbo;
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

    public virtual Task<bool> IsEnrollmentConfirmedAsync(
        string student, string period, string course, string section, CancellationToken ct) =>
        AlumnosCatedras.AsNoTracking().AnyAsync(row =>
            row.CodAlumno == student && row.Periodo == period && row.CodCatedra == course
            && row.CodSeccion == section && row.Categoria == "CURSADA"
            && MovimientosInscripcions.Any(movement =>
                movement.CodAlumno == student && movement.Periodo == period
                && movement.CodCatedra == course && movement.TipoMovimiento == "AGREGA"
                && movement.Estado == "ACEPTADA"), ct);

    public virtual Task<SpRecEnrollmentCoursesResult?> FindMassEnrollmentCourseAsync(
        string period, string course, string section, string courseType,
        string studentType, CancellationToken ct) =>
        (from subject in Catedras.AsNoTracking()
         join offeredCourse in Cursos.AsNoTracking() on subject.CodCatedra equals offeredCourse.CodCatedra
         join plan in PlanesFormacionCatedras.AsNoTracking()
             on new { offeredCourse.CodCatedra, Decreto = offeredCourse.DecretoPostgrado }
             equals new { plan.CodCatedra, plan.Decreto }
         join vacancies in VacantesCursos.AsNoTracking()
             on new { offeredCourse.Periodo, offeredCourse.CodCatedra, offeredCourse.CodSeccion }
             equals new { vacancies.Periodo, vacancies.CodCatedra, vacancies.CodSeccion }
         where offeredCourse.Periodo == period && subject.CodCatedra == course
             && offeredCourse.CodSeccion == section && !offeredCourse.Eliminado
             // The legacy available-course SP leaves the course type/modality unfiltered;
             // capacity is keyed by the selected student type in Vacantes_Cursos.
             && vacancies.TipoAlumno == studentType && vacancies.VacOfrecidas > 0
             && plan.TipoCatedra == courseType && plan.CodPlanFormacion == "GRAL"
             && (plan.TipoCatedra == "0" || plan.TipoCatedra == "1" || plan.TipoCatedra == "2" || plan.TipoCatedra == "3")
         select new SpRecEnrollmentCoursesResult
         {
             Cod_Catedra = subject.CodCatedra + "/" + offeredCourse.CodSeccion + "/" + plan.TipoCatedra,
             NombreCatedra = subject.NomCatedra == null ? null
                 : subject.CodCatedra + "/" + offeredCourse.CodSeccion + "/" + subject.NomCatedra,
         }).Distinct().FirstOrDefaultAsync(ct);

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

    private sealed class EnrollmentTransaction(IDbContextTransaction transaction) : IEnrollmentTransaction
    {
        public Task CommitAsync(CancellationToken ct) => transaction.CommitAsync(ct);
        // Dispose rolls back an uncommitted outer transaction, including nested SP writes.
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}

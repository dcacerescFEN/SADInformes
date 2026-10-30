using System.Data;
using Microsoft.EntityFrameworkCore.Storage;
using Domain.Entities.Sad.Tables.dbo;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

/// <summary>
/// Definition of SAD tables and generic transaction support.
/// Module queries and commands belong to Application;
/// stored-procedure execution belongs to SadContextProcedures.
/// </summary>
public partial class SadContext(DbContextOptions<SadContext> options)
    : DbContext(options), Application.Common.Interfaces.ISadContext
{
    public DbSet<AudiPostgradoAlumnosGuia> AudiPostgradoAlumnosGuia => Set<AudiPostgradoAlumnosGuia>();
    public DbSet<AudiEstadoPostgradoAlumnosGuia> AudiEstadoPostgradoAlumnosGuia => Set<AudiEstadoPostgradoAlumnosGuia>();
    public DbSet<Alumno> Alumnos => Set<Alumno>();
    public DbSet<Persona> Personas => Set<Persona>();
    public DbSet<EncuestaSalidaGraduado> EncuestaSalidaGraduados => Set<EncuestaSalidaGraduado>();
    public DbSet<Catedra> Catedras => Set<Catedra>();
    public DbSet<Curso> Cursos => Set<Curso>();
    public DbSet<PlanesFormacionCatedra> PlanesFormacionCatedras => Set<PlanesFormacionCatedra>();
    public DbSet<VacantesCurso> VacantesCursos => Set<VacantesCurso>();
    public DbSet<AlumnosCatedra> AlumnosCatedras => Set<AlumnosCatedra>();
    public DbSet<MovimientosInscripcion> MovimientosInscripcions => Set<MovimientosInscripcion>();
    public DbSet<PostgradoAyudanteSigmaSinHistorial> PostgradoAyudantesSigmaSinHistorial => Set<PostgradoAyudanteSigmaSinHistorial>();

    /// <summary>Starts a transaction on the connection also used by typed procedures.</summary>
    public virtual async Task<IDbContextTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
    {
        var transaction = await Database.BeginTransactionAsync(isolationLevel, cancellationToken);
        try
        {
            await Database.ExecuteSqlRawAsync("SET XACT_ABORT ON;", cancellationToken);
            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<PostgradoAyudanteSigmaSinHistorial>(entity =>
        {
            entity.ToView("POSTGRADO_Ayudantes_Sigma_SinHistorial", "dbo");
            entity.HasKey(row => new { row.CodCatedra, row.CodSeccion, row.Periodo, row.CodPersona, row.Ccosto });
            entity.Property(row => row.CodPersona).HasColumnName("Cod_Persona");
            entity.Property(row => row.CodCatedra).HasColumnName("Cod_Catedra");
            entity.Property(row => row.NomCatedra).HasColumnName("Nom_Catedra");
            entity.Property(row => row.CodSeccion).HasColumnName("Cod_Seccion");
            entity.Property(row => row.AñoCatedra).HasColumnName("Año_Catedra");
            entity.Property(row => row.TipoAlumnoPostgrado).HasColumnName("Tipo_AlumnoPostgrado");
            entity.Property(row => row.ModalidadPostgrado).HasColumnName("ModalidadPostgrado");
            entity.Property(row => row.TipoPago).HasColumnName("tipoPago");
            entity.Property(row => row.Inicio).HasColumnType("date");
            entity.Property(row => row.Termino).HasColumnType("date");
        });
        modelBuilder.Entity<EncuestaSalidaGraduado>(entity =>
        {
            entity.ToTable("Encuesta_Salida_Graduados", "dbo");
            entity.HasKey(row => new { row.Folio, row.CodAlumno });
            entity.Property(row => row.Folio).HasColumnName("Folio").ValueGeneratedOnAdd();
            entity.Property(row => row.CodAlumno).HasColumnName("Cod_Alumno").HasMaxLength(20).IsUnicode(false)
                .UseCollation("Modern_Spanish_CI_AS");
        });
        modelBuilder.Entity<AudiPostgradoAlumnosGuia>(entity =>
        {
            entity.ToTable("Audi_PostgradoAlumnosGuia", "dbo");
            entity.HasKey(row => row.Id);
            entity.Property(row => row.Estado).HasMaxLength(5).IsUnicode(false);
            entity.Property(row => row.CodPersona).HasColumnName("Cod_Persona").HasMaxLength(20).IsUnicode(false);
            entity.Property(row => row.CodAlumnoAntiguo).HasColumnName("Cod_Alumno_Antiguo").HasMaxLength(20).IsUnicode(false);
            entity.Property(row => row.CodAlumnoNuevo).HasColumnName("Cod_Alumno_Nuevo").HasMaxLength(20).IsUnicode(false);
            entity.Property(row => row.CodSitAcademica).HasColumnName("Cod_SitAcademica").HasMaxLength(25).IsUnicode(false);
            entity.Property(row => row.Matricula).HasMaxLength(10).IsUnicode(false);
            entity.Property(row => row.FechaRegistro).HasColumnName("Fecha_Registro").HasColumnType("datetime");
            entity.Property(row => row.MensajeError).HasColumnName("Mensaje_Error").HasMaxLength(500).IsUnicode(false);
        });
        modelBuilder.Entity<AudiEstadoPostgradoAlumnosGuia>(entity =>
        {
            entity.ToTable("Audi_EstadoPostgradoAlumnosGuia", "dbo");
            entity.HasKey(row => row.Id);
            entity.HasIndex(row => row.Estado).IsUnique();
            entity.Property(row => row.Estado).HasMaxLength(5).IsUnicode(false);
            entity.Property(row => row.Descripcion).HasMaxLength(100).IsUnicode(false);
        });
        modelBuilder.Entity<Alumno>(entity =>
        {
            entity.ToTable("Alumnos", "dbo");
            entity.HasKey(row => new { row.Matricula, row.SemIngresoDecreto });
            entity.HasIndex(row => row.CodAlumno).IsUnique();
            entity.Property(row => row.Matricula).HasMaxLength(10).IsUnicode(false).UseCollation("Compatibility_42_409_30003");
            entity.Property(row => row.SemIngresoDecreto).HasColumnName("Sem_IngresoDecreto").HasMaxLength(5).IsUnicode(false).UseCollation("Compatibility_42_409_30003");
            entity.Property(row => row.CodAlumno).HasColumnName("Cod_Alumno").HasMaxLength(20).IsUnicode(false).UseCollation("Compatibility_42_409_30003");
            entity.Property(row => row.CodPersona).HasColumnName("Cod_Persona").HasMaxLength(20).IsUnicode(false).UseCollation("Compatibility_42_409_30003");
            entity.Property(row => row.TipoAlumno).HasColumnName("Tipo_Alumno").HasMaxLength(30).IsUnicode(false).UseCollation("Compatibility_42_409_30003");
            entity.Property(row => row.Modalidad).HasMaxLength(30).IsUnicode(false).UseCollation("Compatibility_42_409_30003");
        });
        modelBuilder.Entity<Persona>(entity =>
        {
            entity.ToTable("Personas", "dbo");
            entity.HasKey(row => row.CodPersona);
            entity.Property(row => row.CodPersona).HasColumnName("Cod_Persona").HasMaxLength(20).IsUnicode(false).UseCollation("Compatibility_42_409_30003");
            entity.Property(row => row.Nombre1).HasMaxLength(100).IsUnicode(false).UseCollation("Compatibility_42_409_30003");
            entity.Property(row => row.Apellido1).HasMaxLength(100).IsUnicode(false).UseCollation("Compatibility_42_409_30003");
            entity.Property(row => row.Apellido2).HasMaxLength(100).IsUnicode(false).UseCollation("Compatibility_42_409_30003");
        });
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

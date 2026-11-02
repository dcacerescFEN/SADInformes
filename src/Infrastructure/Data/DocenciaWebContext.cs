using Application.Common.Interfaces;
using Domain.Entities.DocenciaWeb;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

/// <summary>Contexto de solo lectura para la base externa DocenciaWeb.</summary>
public sealed class DocenciaWebContext(DbContextOptions<DocenciaWebContext> options)
    : DbContext(options), IDocenciaWebContext
{
    public DbSet<TaskM> TaskMs => Set<TaskM>();
    public DbSet<TaskTipo> TaskTipos => Set<TaskTipo>();
    public DbSet<TaskEntrega> TaskEntregas => Set<TaskEntrega>();
    public DbSet<TaskGrupo> TaskGrupos => Set<TaskGrupo>();
    public DbSet<TaskGrupoAlumno> TaskGrupoAlumnos => Set<TaskGrupoAlumno>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskM>(entity =>
        {
            entity.ToTable("TaskM", "dbo");
            entity.HasKey(row => row.Id);
            entity.Property(row => row.CodCatedra).HasColumnName("Cod_Catedra").HasMaxLength(30).IsUnicode(false);
            entity.Property(row => row.CodSeccion).HasColumnName("Cod_Seccion").HasMaxLength(2).IsUnicode(false);
            entity.Property(row => row.Periodo).HasMaxLength(20).IsUnicode(false);
            entity.Property(row => row.Titulo).HasMaxLength(500).IsUnicode(false);
            entity.Property(row => row.FechaCreacion).HasColumnName("Fecha_creacion");
            entity.Property(row => row.FechaEntrega).HasColumnName("Fecha_Entrega");
            entity.Property(row => row.TipoTaskId).HasColumnName("Tipo_task_id");
            entity.Property(row => row.FechaElimina).HasColumnName("Fecha_elimina");
        });
        modelBuilder.Entity<TaskTipo>(entity =>
        {
            entity.ToTable("TaskTipo", "dbo");
            entity.HasKey(row => row.Id);
            entity.Property(row => row.TipoTask).HasColumnName("Tipo_task").HasMaxLength(50).IsUnicode(false);
        });
        modelBuilder.Entity<TaskEntrega>(entity =>
        {
            entity.ToTable("TaskEntregas", "dbo");
            entity.HasKey(row => row.Id);
            entity.Property(row => row.TaskId).HasColumnName("Task_id");
            entity.Property(row => row.UnameAlumno).HasColumnName("Uname_Alumno").HasMaxLength(50).IsUnicode(false);
            entity.Property(row => row.FechaEntrega).HasColumnName("Fecha_entrega");
            entity.Property(row => row.Nota).HasMaxLength(20).IsUnicode(false);
            entity.Property(row => row.FechaEvaluacion).HasColumnName("Fecha_evaluacion");
            entity.Property(row => row.ObservacionProfesor).HasColumnName("Observacion_Profesor").HasMaxLength(1000).IsUnicode(false);
            entity.Property(row => row.FechaElimina).HasColumnName("Fecha_elimina");
        });
        modelBuilder.Entity<TaskGrupo>(entity =>
        {
            entity.ToTable("TaskGrupo", "dbo");
            entity.HasKey(row => row.Id);
            entity.Property(row => row.TaskId).HasColumnName("Task_id");
            entity.Property(row => row.FechaElimina).HasColumnName("Fecha_elimina");
        });
        modelBuilder.Entity<TaskGrupoAlumno>(entity =>
        {
            entity.ToTable("TaskGrupoAlumno", "dbo");
            entity.HasKey(row => new { row.GrupoId, row.UnameAlumno });
            entity.Property(row => row.GrupoId).HasColumnName("Grupo_id");
            entity.Property(row => row.UnameAlumno).HasColumnName("Uname_Alumno").HasMaxLength(50).IsUnicode(false);
            entity.Property(row => row.FechaElimina).HasColumnName("Fecha_elimina");
        });
    }
}

using Domain.Entities.DocenciaWeb;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Interfaces;

public interface IDocenciaWebContext
{
    DbSet<TaskM> TaskMs { get; }
    DbSet<TaskTipo> TaskTipos { get; }
    DbSet<TaskEntrega> TaskEntregas { get; }
    DbSet<TaskGrupo> TaskGrupos { get; }
    DbSet<TaskGrupoAlumno> TaskGrupoAlumnos { get; }
}

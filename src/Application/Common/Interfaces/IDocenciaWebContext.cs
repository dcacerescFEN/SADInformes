using Domain.Entities.DocenciaWeb;
using Domain.Entities.DocenciaWeb.Tables;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Interfaces;

public interface IDocenciaWebContext
{
    DbSet<TaskM> TaskMs { get; }
    DbSet<TaskTipo> TaskTipos { get; }
    DbSet<TaskEntrega> TaskEntregas { get; }
    DbSet<TaskGrupo> TaskGrupos { get; }
    DbSet<TaskGrupoAlumno> TaskGrupoAlumnos { get; }
    DbSet<FeTipoModulo> FeTipoModulos { get; }
    DbSet<FeModuloCascadaFinal> FeModulosCascadaFinal { get; }
    DbSet<FeCeldaModulo> FeCeldasModulos { get; }
}

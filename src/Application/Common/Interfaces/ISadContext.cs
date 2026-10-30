using System.Data;
using Microsoft.EntityFrameworkCore.Storage;
using Domain.Entities.Sad.Tables.dbo;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Interfaces;

public partial interface ISadContext
{
    DbSet<AudiPostgradoAlumnosGuia> AudiPostgradoAlumnosGuia { get; }
    DbSet<AudiEstadoPostgradoAlumnosGuia> AudiEstadoPostgradoAlumnosGuia { get; }
    DbSet<Alumno> Alumnos { get; }
    DbSet<Persona> Personas { get; }
    DbSet<EncuestaSalidaGraduado> EncuestaSalidaGraduados { get; }
    DbSet<Catedra> Catedras { get; }
    DbSet<Curso> Cursos { get; }
    DbSet<PlanesFormacionCatedra> PlanesFormacionCatedras { get; }
    DbSet<VacantesCurso> VacantesCursos { get; }
    DbSet<AlumnosCatedra> AlumnosCatedras { get; }
    DbSet<MovimientosInscripcion> MovimientosInscripcions { get; }
    DbSet<PostgradoAyudanteSigmaSinHistorial> PostgradoAyudantesSigmaSinHistorial { get; }

    Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default);
}

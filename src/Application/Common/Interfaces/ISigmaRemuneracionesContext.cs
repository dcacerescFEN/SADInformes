using Domain.Entities.SigmaRemuneraciones;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Interfaces;

public interface ISigmaRemuneracionesContext
{
    bool IsConfigured { get; }
    DbSet<Remuneracion> Remuneraciones { get; }
    DbSet<Prestacion> Prestaciones { get; }
}

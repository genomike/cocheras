using Cochera.Domain.Entities;
using Cochera.Domain.Interfaces;
using Cochera.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Cochera.Infrastructure.Repositories;

public class EventoSensorRepository : Repository<EventoSensor>, IEventoSensorRepository
{
    public EventoSensorRepository(CocheraDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<EventoSensor>> GetUltimosEventosAsync(int cantidad, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .OrderByDescending(e => e.FechaCreacion)
            .Take(cantidad)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<EventoSensor>> GetEventosByFechaAsync(DateTime desde, DateTime hasta, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.FechaCreacion >= desde && e.FechaCreacion <= hasta)
            .OrderByDescending(e => e.FechaCreacion)
            .ToListAsync(cancellationToken);
    }

    public async Task<EventoSensor?> GetUltimoEventoAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .OrderByDescending(e => e.FechaCreacion)
            .FirstOrDefaultAsync(cancellationToken);
    }
}

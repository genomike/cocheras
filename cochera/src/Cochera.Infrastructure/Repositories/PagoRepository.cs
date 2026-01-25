using Cochera.Domain.Entities;
using Cochera.Domain.Interfaces;
using Cochera.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Cochera.Infrastructure.Repositories;

public class PagoRepository : Repository<Pago>, IPagoRepository
{
    public PagoRepository(CocheraDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Pago>> GetPagosByFechaAsync(DateTime desde, DateTime hasta, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Sesion)
            .ThenInclude(s => s.Usuario)
            .Where(p => p.FechaPago >= desde && p.FechaPago <= hasta)
            .OrderByDescending(p => p.FechaPago)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalRecaudadoAsync(DateTime desde, DateTime hasta, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.FechaPago >= desde && p.FechaPago <= hasta)
            .SumAsync(p => p.Monto, cancellationToken);
    }
}

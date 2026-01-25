using Cochera.Domain.Entities;
using Cochera.Domain.Interfaces;
using Cochera.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Cochera.Infrastructure.Repositories;

public class EstadoCocheraRepository : Repository<EstadoCochera>, IEstadoCocheraRepository
{
    public EstadoCocheraRepository(CocheraDbContext context) : base(context)
    {
    }

    public async Task<EstadoCochera?> GetEstadoActualAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.OrderByDescending(e => e.UltimaActualizacion).FirstOrDefaultAsync(cancellationToken);
    }
}

using Cochera.Domain.Entities;
using Cochera.Domain.Enums;
using Cochera.Domain.Interfaces;
using Cochera.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Cochera.Infrastructure.Repositories;

public class CajonRepository : Repository<Cajon>, ICajonRepository
{
    public CajonRepository(CocheraDbContext context) : base(context)
    {
    }

    public async Task<Cajon?> GetByNumeroAsync(int numero, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(c => c.Numero == numero, cancellationToken);
    }

    public async Task<IEnumerable<Cajon>> GetCajonesLibresAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(c => c.Estado == EstadoCajon.Libre).ToListAsync(cancellationToken);
    }
}

using Cochera.Domain.Entities;
using Cochera.Domain.Interfaces;
using Cochera.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Cochera.Infrastructure.Repositories;

public class TarifaRepository : Repository<Tarifa>, ITarifaRepository
{
    public TarifaRepository(CocheraDbContext context) : base(context)
    {
    }

    public async Task<Tarifa?> GetTarifaActivaAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(t => t.Activa, cancellationToken);
    }
}

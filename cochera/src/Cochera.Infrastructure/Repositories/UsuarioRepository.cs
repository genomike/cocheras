using Cochera.Domain.Entities;
using Cochera.Domain.Interfaces;
using Cochera.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Cochera.Infrastructure.Repositories;

public class UsuarioRepository : Repository<Usuario>, IUsuarioRepository
{
    public UsuarioRepository(CocheraDbContext context) : base(context)
    {
    }

    public async Task<Usuario?> GetByCodigoAsync(string codigo, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Codigo == codigo, cancellationToken);
    }

    public async Task<IEnumerable<Usuario>> GetUsuariosNoAdminAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(u => !u.EsAdmin).ToListAsync(cancellationToken);
    }
}

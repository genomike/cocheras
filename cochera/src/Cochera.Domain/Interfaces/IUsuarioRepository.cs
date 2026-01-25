using Cochera.Domain.Entities;

namespace Cochera.Domain.Interfaces;

public interface IUsuarioRepository : IRepository<Usuario>
{
    Task<Usuario?> GetByCodigoAsync(string codigo, CancellationToken cancellationToken = default);
    Task<IEnumerable<Usuario>> GetUsuariosNoAdminAsync(CancellationToken cancellationToken = default);
}

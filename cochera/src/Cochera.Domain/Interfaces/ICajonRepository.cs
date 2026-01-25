using Cochera.Domain.Entities;

namespace Cochera.Domain.Interfaces;

public interface ICajonRepository : IRepository<Cajon>
{
    Task<Cajon?> GetByNumeroAsync(int numero, CancellationToken cancellationToken = default);
    Task<IEnumerable<Cajon>> GetCajonesLibresAsync(CancellationToken cancellationToken = default);
}

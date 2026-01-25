using Cochera.Domain.Entities;

namespace Cochera.Domain.Interfaces;

public interface IEstadoCocheraRepository : IRepository<EstadoCochera>
{
    Task<EstadoCochera?> GetEstadoActualAsync(CancellationToken cancellationToken = default);
}

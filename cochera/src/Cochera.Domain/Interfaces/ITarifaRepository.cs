using Cochera.Domain.Entities;

namespace Cochera.Domain.Interfaces;

public interface ITarifaRepository : IRepository<Tarifa>
{
    Task<Tarifa?> GetTarifaActivaAsync(CancellationToken cancellationToken = default);
}

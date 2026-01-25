using Cochera.Domain.Entities;

namespace Cochera.Domain.Interfaces;

public interface IPagoRepository : IRepository<Pago>
{
    Task<IEnumerable<Pago>> GetPagosByFechaAsync(DateTime desde, DateTime hasta, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalRecaudadoAsync(DateTime desde, DateTime hasta, CancellationToken cancellationToken = default);
}

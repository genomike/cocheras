using Cochera.Application.DTOs;

namespace Cochera.Application.Interfaces;

public interface IEstadoCocheraService
{
    Task<EstadoCocheraDto?> GetEstadoActualAsync(CancellationToken cancellationToken = default);
    Task ActualizarEstadoAsync(bool cajon1Ocupado, bool cajon2Ocupado, CancellationToken cancellationToken = default);
}

using Cochera.Application.DTOs;

namespace Cochera.Application.Interfaces;

public interface ICajonService
{
    Task<IEnumerable<CajonDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CajonDto?> GetByNumeroAsync(int numero, CancellationToken cancellationToken = default);
    Task<IEnumerable<CajonDto>> GetCajonesLibresAsync(CancellationToken cancellationToken = default);
    Task ActualizarEstadoAsync(int numero, bool ocupado, CancellationToken cancellationToken = default);
    Task ResetearEstadoAsync(CancellationToken cancellationToken = default);
}

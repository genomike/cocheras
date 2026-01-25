using Cochera.Application.DTOs;

namespace Cochera.Application.Interfaces;

public interface ITarifaService
{
    Task<TarifaDto?> GetTarifaActivaAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<TarifaDto>> GetHistorialTarifasAsync(CancellationToken cancellationToken = default);
    Task<TarifaDto> ActualizarTarifaAsync(ActualizarTarifaRequest request, CancellationToken cancellationToken = default);
}

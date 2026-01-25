using Cochera.Application.DTOs;
using Cochera.Domain.Enums;

namespace Cochera.Application.Interfaces;

public interface ISesionService
{
    Task<SesionEstacionamientoDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<SesionEstacionamientoDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<SesionEstacionamientoDto>> GetSesionesActivasAsync(CancellationToken cancellationToken = default);
    Task<SesionEstacionamientoDto?> GetSesionActivaByUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SesionEstacionamientoDto>> GetSesionesByUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SesionEstacionamientoDto>> GetSesionesByFechaAsync(DateTime desde, DateTime hasta, CancellationToken cancellationToken = default);
    Task<SesionEstacionamientoDto> IniciarSesionAsync(IniciarSesionRequest request, CancellationToken cancellationToken = default);
    Task<SesionEstacionamientoDto> FinalizarSesionAsync(FinalizarSesionRequest request, CancellationToken cancellationToken = default);
    Task<decimal> CalcularMontoActualAsync(int sesionId, CancellationToken cancellationToken = default);
}

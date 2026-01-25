using Cochera.Domain.Entities;
using Cochera.Domain.Enums;

namespace Cochera.Domain.Interfaces;

public interface ISesionEstacionamientoRepository : IRepository<SesionEstacionamiento>
{
    Task<SesionEstacionamiento?> GetSesionActivaByUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default);
    Task<SesionEstacionamiento?> GetSesionActivaByCajonAsync(int cajonId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SesionEstacionamiento>> GetSesionesActivasAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<SesionEstacionamiento>> GetSesionesByUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SesionEstacionamiento>> GetSesionesByFechaAsync(DateTime desde, DateTime hasta, CancellationToken cancellationToken = default);
    Task<SesionEstacionamiento?> GetWithPagoAsync(int sesionId, CancellationToken cancellationToken = default);
}

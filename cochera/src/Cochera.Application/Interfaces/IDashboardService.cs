using Cochera.Application.DTOs;

namespace Cochera.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ResumenPorHora>> GetUsoPorHoraAsync(DateTime fecha, CancellationToken cancellationToken = default);
    Task<IEnumerable<ResumenPorDia>> GetUsoPorDiaAsync(DateTime desde, DateTime hasta, CancellationToken cancellationToken = default);
}

using Cochera.Domain.Entities;

namespace Cochera.Domain.Interfaces;

public interface IEventoSensorRepository : IRepository<EventoSensor>
{
    Task<IEnumerable<EventoSensor>> GetUltimosEventosAsync(int cantidad, CancellationToken cancellationToken = default);
    Task<IEnumerable<EventoSensor>> GetEventosByFechaAsync(DateTime desde, DateTime hasta, CancellationToken cancellationToken = default);
    Task<EventoSensor?> GetUltimoEventoAsync(CancellationToken cancellationToken = default);
}

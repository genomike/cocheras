using Cochera.Application.DTOs;

namespace Cochera.Application.Interfaces;

public interface IEventoSensorService
{
    Task<EventoSensorDto> ProcesarMensajeAsync(MensajeSensorMqtt mensaje, string jsonOriginal, CancellationToken cancellationToken = default);
    Task<IEnumerable<EventoSensorDto>> GetUltimosEventosAsync(int cantidad = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<EventoSensorDto>> GetEventosByFechaAsync(DateTime desde, DateTime hasta, CancellationToken cancellationToken = default);
    Task<EventoSensorDto?> GetUltimoEventoAsync(CancellationToken cancellationToken = default);
}

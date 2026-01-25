using Cochera.Application.DTOs;

namespace Cochera.Worker;

public interface ISignalRNotificationService
{
    Task NotificarNuevoEventoAsync(EventoSensorDto evento);
    Task NotificarCambioEstadoAsync(EstadoCocheraDto estado);
    Task NotificarNuevaSesionAsync(SesionEstacionamientoDto sesion);
    Task NotificarSesionFinalizadaAsync(SesionEstacionamientoDto sesion);
}

using Cochera.Application.DTOs;

namespace Cochera.Worker;

public interface ISignalRNotificationService
{
    Task NotificarNuevoEventoAsync(EventoSensorDto evento);
    Task NotificarCambioEstadoAsync(EstadoCocheraDto estado);
    Task NotificarNuevaSesionAsync(SesionEstacionamientoDto sesion);
    Task NotificarSesionFinalizadaAsync(SesionEstacionamientoDto sesion);
    
    // Nuevos métodos para el flujo basado en eventos
    Task NotificarVehiculoDetectadoAsync(EventoSensorDto evento);
    Task NotificarSolicitudCierreSesionAsync(SesionEstacionamientoDto sesion);
    Task NotificarPagoConfirmadoAsync(SesionEstacionamientoDto sesion);
    Task NotificarSesionCerradaAsync(SesionEstacionamientoDto sesion);
    Task NotificarActualizacionMontoAsync(SesionEstacionamientoDto sesion);
}

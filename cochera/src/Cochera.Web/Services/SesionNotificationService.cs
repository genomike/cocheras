using Cochera.Application.DTOs;
using Cochera.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Cochera.Web.Services;

public interface ISesionNotificationService
{
    Task NotificarNuevaSesionAsync(SesionEstacionamientoDto sesion);
    Task NotificarSolicitudCierreSesionAsync(SesionEstacionamientoDto sesion);
    Task NotificarPagoConfirmadoAsync(SesionEstacionamientoDto sesion);
    Task NotificarSesionCerradaAsync(SesionEstacionamientoDto sesion);
}

public class SesionNotificationService : ISesionNotificationService
{
    private readonly IHubContext<CocheraHub> _hubContext;

    public SesionNotificationService(IHubContext<CocheraHub> hubContext)
    {
        _hubContext = hubContext;
    }

    /// <summary>
    /// Notifica a admins y al usuario específico sobre la nueva sesión creada
    /// </summary>
    public async Task NotificarNuevaSesionAsync(SesionEstacionamientoDto sesion)
    {
        // Notificar al admin
        await _hubContext.Clients.Group("admins").SendAsync("NuevaSesionCreada", sesion);
        // Notificar al usuario específico
        await _hubContext.Clients.Group($"usuario_{sesion.UsuarioId}").SendAsync("MiNuevaSesion", sesion);
    }

    /// <summary>
    /// Notifica al usuario que el admin solicitó el cierre de su sesión (debe pagar)
    /// </summary>
    public async Task NotificarSolicitudCierreSesionAsync(SesionEstacionamientoDto sesion)
    {
        await _hubContext.Clients.Group($"usuario_{sesion.UsuarioId}").SendAsync("SolicitudCierreSesion", sesion);
    }

    /// <summary>
    /// Notifica al admin que el usuario confirmó el pago
    /// </summary>
    public async Task NotificarPagoConfirmadoAsync(SesionEstacionamientoDto sesion)
    {
        await _hubContext.Clients.Group("admins").SendAsync("UsuarioPagoConfirmado", sesion);
    }

    /// <summary>
    /// Notifica a todos que la sesión fue cerrada exitosamente
    /// </summary>
    public async Task NotificarSesionCerradaAsync(SesionEstacionamientoDto sesion)
    {
        await _hubContext.Clients.Group($"usuario_{sesion.UsuarioId}").SendAsync("SesionCerrada", sesion);
        await _hubContext.Clients.Group("admins").SendAsync("SesionCerradaAdmin", sesion);
    }
}

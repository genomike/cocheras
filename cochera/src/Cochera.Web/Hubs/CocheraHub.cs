using Cochera.Application.DTOs;
using Cochera.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Cochera.Web.Hubs;

public class CocheraHub : Hub
{
    private readonly ILogger<CocheraHub> _logger;
    private readonly IUsuarioService _usuarioService;

    public CocheraHub(ILogger<CocheraHub> logger, IUsuarioService usuarioService)
    {
        _logger = logger;
        _usuarioService = usuarioService;
    }

    // Grupos: "admins" y "usuario_{id}"
    [Authorize(Roles = "Admin")]
    public async Task UnirseComoAdmin()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
        _logger.LogInformation("👤 Admin conectado al grupo 'admins': {ConnectionId}", Context.ConnectionId);
    }

    [Authorize]
    public async Task UnirseComoUsuario(int usuarioId)
    {
        var codigo = Context.User?.Identity?.Name;
        if (string.IsNullOrWhiteSpace(codigo))
        {
            _logger.LogWarning("⛔ Intento de unión sin identidad en {ConnectionId}", Context.ConnectionId);
            throw new HubException("No autenticado");
        }

        var usuario = await _usuarioService.GetByCodigoAsync(codigo);
        if (usuario == null)
        {
            _logger.LogWarning("⛔ Usuario no encontrado para código {Codigo}", codigo);
            throw new HubException("Usuario inválido");
        }

        if (Context.User?.IsInRole("Admin") == true || usuario.Id == usuarioId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"usuario_{usuarioId}");
            _logger.LogInformation("👤 Usuario {UsuarioId} conectado al grupo: {ConnectionId}", usuarioId, Context.ConnectionId);
            return;
        }

        _logger.LogWarning("⛔ Intento de acceso a grupo de otro usuario. UsuarioAuth={AuthUserId}, UsuarioSolicitado={UsuarioId}", usuario.Id, usuarioId);
        throw new HubException("Acceso denegado");
    }

    public async Task SalirDeGrupoUsuario(int usuarioId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"usuario_{usuarioId}");
        _logger.LogInformation("👤 Usuario {UsuarioId} desconectado del grupo: {ConnectionId}", usuarioId, Context.ConnectionId);
    }

    // Eventos de sensores
    public async Task NuevoEvento(EventoSensorDto evento)
    {
        _logger.LogInformation("📡 NuevoEvento recibido: {Tipo} - {Detalle}", evento.TipoEventoTexto, evento.Detalle);
        await Clients.All.SendAsync("RecibirEvento", evento);
    }

    public async Task CambioEstado(EstadoCocheraDto estado)
    {
        _logger.LogInformation("📡 CambioEstado recibido - Cajón1: {Cajon1}, Cajón2: {Cajon2}", 
            estado.Cajon1Ocupado ? "OCUPADO" : "LIBRE", 
            estado.Cajon2Ocupado ? "OCUPADO" : "LIBRE");
        await Clients.All.SendAsync("RecibirEstado", estado);
        await Clients.All.SendAsync("ActualizarCajones", estado);
    }

    // === EVENTOS DE SESIONES ===

    // Admin: Vehículo detectado en entrada
    public async Task VehiculoDetectadoEnEntrada(EventoSensorDto evento)
    {
        _logger.LogInformation("🚗 VehiculoDetectadoEnEntrada - Enviando a grupo 'admins': {Tipo}", evento.TipoEventoTexto);
        await Clients.Group("admins").SendAsync("VehiculoDetectadoEnEntrada", evento);
    }

    // Admin -> Usuario: Nueva sesión creada
    public async Task NuevaSesionCreada(SesionEstacionamientoDto sesion)
    {
        // Notificar al admin
        await Clients.Group("admins").SendAsync("NuevaSesionCreada", sesion);
        // Notificar al usuario específico
        await Clients.Group($"usuario_{sesion.UsuarioId}").SendAsync("MiNuevaSesion", sesion);
    }

    // Admin -> Usuario: Solicitud de cierre de sesión (usuario debe pagar)
    public async Task SolicitudCierreSesion(SesionEstacionamientoDto sesion)
    {
        await Clients.Group($"usuario_{sesion.UsuarioId}").SendAsync("SolicitudCierreSesion", sesion);
    }

    // Usuario -> Admin: Usuario confirmó el pago
    public async Task UsuarioPagoConfirmado(SesionEstacionamientoDto sesion)
    {
        await Clients.Group("admins").SendAsync("UsuarioPagoConfirmado", sesion);
    }

    // Admin -> Usuario: Sesión cerrada exitosamente
    public async Task SesionCerrada(SesionEstacionamientoDto sesion)
    {
        await Clients.Group($"usuario_{sesion.UsuarioId}").SendAsync("SesionCerrada", sesion);
        await Clients.Group("admins").SendAsync("SesionCerradaAdmin", sesion);
    }

    // Actualización de monto en tiempo real
    public async Task ActualizarMontoSesion(SesionEstacionamientoDto sesion)
    {
        await Clients.Group($"usuario_{sesion.UsuarioId}").SendAsync("ActualizarMontoSesion", sesion);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("🔌 Cliente conectado: {ConnectionId}", Context.ConnectionId);
        await Clients.Caller.SendAsync("Connected");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("🔌 Cliente desconectado: {ConnectionId} - Error: {Error}", 
            Context.ConnectionId, exception?.Message ?? "Ninguno");
        await base.OnDisconnectedAsync(exception);
    }
}

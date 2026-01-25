using Cochera.Application.DTOs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cochera.Worker;

public class SignalRNotificationService : ISignalRNotificationService, IAsyncDisposable
{
    private readonly HubConnection _hubConnection;
    private readonly ILogger<SignalRNotificationService> _logger;
    private readonly string _hubUrl;
    private bool _isConnecting = false;

    public SignalRNotificationService(IConfiguration configuration, ILogger<SignalRNotificationService> logger)
    {
        _logger = logger;
        _hubUrl = configuration["SignalR:HubUrl"] ?? "http://localhost:5000/cocherahub";
        
        _logger.LogInformation("📡 Configurando conexión SignalR a: {HubUrl}", _hubUrl);
        
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(_hubUrl)
            .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
            .Build();

        _hubConnection.Closed += async (error) =>
        {
            _logger.LogWarning("❌ SignalR desconectado: {Error}", error?.Message ?? "Sin error");
            await Task.Delay(TimeSpan.FromSeconds(3));
            await EnsureConnectedAsync();
        };

        _hubConnection.Reconnecting += (error) =>
        {
            _logger.LogWarning("🔄 SignalR reconectando: {Error}", error?.Message ?? "Sin error");
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += (connectionId) =>
        {
            _logger.LogInformation("✅ SignalR reconectado con ID: {ConnectionId}", connectionId);
            return Task.CompletedTask;
        };

        _ = EnsureConnectedAsync();
    }

    private async Task EnsureConnectedAsync()
    {
        if (_isConnecting) return;
        if (_hubConnection.State == HubConnectionState.Connected) return;

        _isConnecting = true;
        try
        {
            int retries = 0;
            while (_hubConnection.State != HubConnectionState.Connected && retries < 10)
            {
                try
                {
                    _logger.LogInformation("🔌 Intentando conectar al Hub SignalR ({Url})... Intento {Retry}", _hubUrl, retries + 1);
                    await _hubConnection.StartAsync();
                    _logger.LogInformation("✅ Conectado al Hub SignalR exitosamente");
                    break;
                }
                catch (Exception ex)
                {
                    retries++;
                    _logger.LogWarning("⚠️ Error al conectar con SignalR Hub (intento {Retry}): {Error}", retries, ex.Message);
                    if (retries < 10)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(Math.Min(retries * 2, 30)));
                    }
                }
            }
        }
        finally
        {
            _isConnecting = false;
        }
    }

    private async Task<bool> CheckConnectionAsync()
    {
        if (_hubConnection.State != HubConnectionState.Connected)
        {
            _logger.LogWarning("⚠️ SignalR no está conectado (Estado: {State}). Intentando reconectar...", _hubConnection.State);
            await EnsureConnectedAsync();
        }
        return _hubConnection.State == HubConnectionState.Connected;
    }

    public async Task NotificarNuevoEventoAsync(EventoSensorDto evento)
    {
        try
        {
            if (await CheckConnectionAsync())
            {
                await _hubConnection.InvokeAsync("NuevoEvento", evento);
                _logger.LogDebug("📤 Evento notificado via SignalR: {Evento}", evento.TipoEventoTexto);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al notificar evento via SignalR");
        }
    }

    public async Task NotificarCambioEstadoAsync(EstadoCocheraDto estado)
    {
        try
        {
            if (await CheckConnectionAsync())
            {
                await _hubConnection.InvokeAsync("CambioEstado", estado);
                _logger.LogDebug("📤 Estado notificado via SignalR");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al notificar estado via SignalR");
        }
    }

    public async Task NotificarNuevaSesionAsync(SesionEstacionamientoDto sesion)
    {
        try
        {
            if (_hubConnection.State == HubConnectionState.Connected)
            {
                await _hubConnection.InvokeAsync("NuevaSesionCreada", sesion);
                _logger.LogInformation("Nueva sesión notificada: Usuario {Usuario}, Cajón {Cajon}", 
                    sesion.UsuarioNombre, sesion.CajonNumero);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al notificar nueva sesión via SignalR");
        }
    }

    public async Task NotificarSesionFinalizadaAsync(SesionEstacionamientoDto sesion)
    {
        try
        {
            if (_hubConnection.State == HubConnectionState.Connected)
            {
                await _hubConnection.InvokeAsync("SesionCerrada", sesion);
                _logger.LogInformation("Sesión finalizada notificada: ID {SesionId}", sesion.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al notificar sesión finalizada via SignalR");
        }
    }

    // === NUEVOS MÉTODOS PARA FLUJO BASADO EN EVENTOS ===

    /// <summary>
    /// Notifica a los admins que se detectó un vehículo en la entrada
    /// </summary>
    public async Task NotificarVehiculoDetectadoAsync(EventoSensorDto evento)
    {
        try
        {
            if (await CheckConnectionAsync())
            {
                await _hubConnection.InvokeAsync("VehiculoDetectadoEnEntrada", evento);
                _logger.LogInformation("🚗📤 Vehículo detectado notificado a admins: {TipoEvento} - Estado SignalR: {State}", 
                    evento.TipoEventoTexto, _hubConnection.State);
            }
            else
            {
                _logger.LogError("❌ No se pudo notificar vehículo detectado - SignalR desconectado");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al notificar vehículo detectado via SignalR");
        }
    }

    /// <summary>
    /// Notifica al usuario que el admin solicitó cierre de su sesión (debe pagar)
    /// </summary>
    public async Task NotificarSolicitudCierreSesionAsync(SesionEstacionamientoDto sesion)
    {
        try
        {
            if (_hubConnection.State == HubConnectionState.Connected)
            {
                await _hubConnection.InvokeAsync("SolicitudCierreSesion", sesion);
                _logger.LogInformation("Solicitud de cierre notificada al usuario {Usuario}: Monto S/ {Monto}", 
                    sesion.UsuarioNombre, sesion.MontoTotal);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al notificar solicitud de cierre via SignalR");
        }
    }

    /// <summary>
    /// Notifica al admin que el usuario confirmó el pago
    /// </summary>
    public async Task NotificarPagoConfirmadoAsync(SesionEstacionamientoDto sesion)
    {
        try
        {
            if (_hubConnection.State == HubConnectionState.Connected)
            {
                await _hubConnection.InvokeAsync("UsuarioPagoConfirmado", sesion);
                _logger.LogInformation("Pago confirmado notificado: Usuario {Usuario}, Sesión {SesionId}", 
                    sesion.UsuarioNombre, sesion.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al notificar pago confirmado via SignalR");
        }
    }

    /// <summary>
    /// Notifica a todos que la sesión fue cerrada exitosamente
    /// </summary>
    public async Task NotificarSesionCerradaAsync(SesionEstacionamientoDto sesion)
    {
        try
        {
            if (_hubConnection.State == HubConnectionState.Connected)
            {
                await _hubConnection.InvokeAsync("SesionCerrada", sesion);
                _logger.LogInformation("Sesión cerrada notificada: ID {SesionId}, Usuario {Usuario}", 
                    sesion.Id, sesion.UsuarioNombre);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al notificar sesión cerrada via SignalR");
        }
    }

    /// <summary>
    /// Actualiza el monto en tiempo real para el usuario
    /// </summary>
    public async Task NotificarActualizacionMontoAsync(SesionEstacionamientoDto sesion)
    {
        try
        {
            if (_hubConnection.State == HubConnectionState.Connected)
            {
                await _hubConnection.InvokeAsync("ActualizarMontoSesion", sesion);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al notificar actualización de monto via SignalR");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}

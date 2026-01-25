using Cochera.Application.DTOs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cochera.Worker;

public class SignalRNotificationService : ISignalRNotificationService, IAsyncDisposable
{
    private readonly HubConnection _hubConnection;
    private readonly ILogger<SignalRNotificationService> _logger;

    public SignalRNotificationService(IConfiguration configuration, ILogger<SignalRNotificationService> logger)
    {
        _logger = logger;
        var hubUrl = configuration["SignalR:HubUrl"] ?? "http://localhost:5000/cocherahub";
        
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _ = ConnectAsync();
    }

    private async Task ConnectAsync()
    {
        try
        {
            await _hubConnection.StartAsync();
            _logger.LogInformation("Conectado al Hub SignalR");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al conectar con SignalR Hub");
        }
    }

    public async Task NotificarNuevoEventoAsync(EventoSensorDto evento)
    {
        try
        {
            if (_hubConnection.State == HubConnectionState.Connected)
            {
                await _hubConnection.InvokeAsync("NuevoEvento", evento);
                _logger.LogDebug("Evento notificado via SignalR: {Evento}", evento.TipoEventoTexto);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al notificar evento via SignalR");
        }
    }

    public async Task NotificarCambioEstadoAsync(EstadoCocheraDto estado)
    {
        try
        {
            if (_hubConnection.State == HubConnectionState.Connected)
            {
                await _hubConnection.InvokeAsync("CambioEstado", estado);
                _logger.LogDebug("Estado notificado via SignalR");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al notificar estado via SignalR");
        }
    }

    public async Task NotificarNuevaSesionAsync(SesionEstacionamientoDto sesion)
    {
        try
        {
            if (_hubConnection.State == HubConnectionState.Connected)
            {
                await _hubConnection.InvokeAsync("NuevaSesion", sesion);
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
                await _hubConnection.InvokeAsync("SesionFinalizada", sesion);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al notificar sesión finalizada via SignalR");
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

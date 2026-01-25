using Cochera.Application.Interfaces;
using Cochera.Infrastructure.Mqtt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cochera.Worker;

public class MqttWorker : BackgroundService
{
    private readonly ILogger<MqttWorker> _logger;
    private readonly IMqttConsumerService _mqttConsumer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISignalRNotificationService _notificationService;

    public MqttWorker(
        ILogger<MqttWorker> logger,
        IMqttConsumerService mqttConsumer,
        IServiceScopeFactory scopeFactory,
        ISignalRNotificationService notificationService)
    {
        _logger = logger;
        _mqttConsumer = mqttConsumer;
        _scopeFactory = scopeFactory;
        _notificationService = notificationService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker MQTT iniciando...");

        _mqttConsumer.OnMensajeRecibido += async (mensaje, jsonOriginal) =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var eventoService = scope.ServiceProvider.GetRequiredService<IEventoSensorService>();
                
                var eventoDto = await eventoService.ProcesarMensajeAsync(mensaje, jsonOriginal, stoppingToken);
                
                _logger.LogInformation("Evento procesado: {TipoEvento} - {Detalle}", 
                    eventoDto.TipoEventoTexto, eventoDto.Detalle);

                // Notificar a los clientes conectados via SignalR
                await _notificationService.NotificarNuevoEventoAsync(eventoDto);
                
                // Notificar actualización de estado
                var estadoService = scope.ServiceProvider.GetRequiredService<IEstadoCocheraService>();
                var estado = await estadoService.GetEstadoActualAsync(stoppingToken);
                if (estado != null)
                {
                    await _notificationService.NotificarCambioEstadoAsync(estado);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar evento del sensor");
            }
        };

        await _mqttConsumer.StartAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("Worker MQTT ejecutándose. Conectado: {IsConnected}", _mqttConsumer.IsConnected);
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker MQTT deteniendo...");
        await _mqttConsumer.StopAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
